using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.Projects;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;
using ValidationException = Times.Services.Errors.ValidationException; // <-- new namespace for custom exceptions

namespace Times.Services.Implementation
{
	public class ProjectService : IProjectService
	{
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public ProjectService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<ProjectResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateProjectRequest request)
		{
			// AuthZ: only Admin/Manager can create
			var canCreate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canCreate) throw new ForbiddenException("Only Admin/Manager can create projects.");

			var name = (request.Name ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(name))
				throw new ValidationException("Project name is required.", new Dictionary<string, string[]>
				{
					["name"] = new[] { "Project name is required." }
				});

			// If ClientId provided, ensure it belongs to the same org
			if (request.ClientId.HasValue)
			{
				var clientOk = await _db.Clients
					.AsNoTracking()
					.AnyAsync(c => c.Id == request.ClientId.Value && c.OrganizationId == organizationId);

				if (!clientOk)
					throw new ValidationException("Client does not belong to this organization.", new Dictionary<string, string[]>
					{
						["clientId"] = new[] { "Client does not belong to this organization." }
					});
			}

			// Recommended: also enforce with a DB unique index; this is a friendly pre-check
			var nameExists = await _db.Projects
				.AsNoTracking()
				.AnyAsync(p => p.OrganizationId == organizationId && p.Name == name);

			if (nameExists)
				throw new ConflictException("A project with this name already exists in the organization.");

			var now = DateTime.UtcNow;

			var project = new Project
			{
				OrganizationId = organizationId,
				ClientId = request.ClientId,
				Name = name,
				Code = NormalizeOptional(request.Code),
				Description = NormalizeOptional(request.Description),
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			_db.Projects.Add(project);

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				// If you add a unique index later, a race can still happen; this converts it to a clean 409.
				// (Optionally inspect inner exception / provider codes for "unique constraint violation".)
				throw new ConflictException("A project with this name already exists in the organization.", ex);
			}

			return Map(project);
		}

		public async Task<List<ProjectResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool? isActive = null, Guid? clientId = null)
		{
			// If the user isn't in the org, this should not silently return an empty list.
			// That's almost always a footgun for the UI.
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var q = _db.Projects.AsNoTracking().Where(p => p.OrganizationId == organizationId);

			if (isActive.HasValue)
				q = q.Where(p => p.IsActive == isActive.Value);

			if (clientId.HasValue)
				q = q.Where(p => p.ClientId == clientId.Value);

			var items = await q.OrderBy(p => p.Name).ToListAsync();
			return items.Select(Map).ToList();
		}

		public async Task<ProjectResponse> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid projectId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var p = await _db.Projects
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == projectId && x.OrganizationId == organizationId);

			if (p is null) throw new NotFoundException("Project not found.");
			return Map(p);
		}

		public async Task<ProjectResponse> UpdateAsync(Guid actorUserId, Guid organizationId, Guid projectId, UpdateProjectRequest request)
		{
			var canUpdate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canUpdate) throw new ForbiddenException("Only Admin/Manager can update projects.");

			var project = await _db.Projects
				.FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId);

			if (project is null) throw new NotFoundException("Project not found.");

			if (request.Name != null)
			{
				var newName = request.Name.Trim();
				if (string.IsNullOrWhiteSpace(newName))
					throw new ValidationException("Project name cannot be empty.", new Dictionary<string, string[]>
					{
						["name"] = new[] { "Project name cannot be empty." }
					});

				var nameExists = await _db.Projects
					.AsNoTracking()
					.AnyAsync(p => p.OrganizationId == organizationId && p.Id != projectId && p.Name == newName);

				if (nameExists) throw new ConflictException("A project with this name already exists in the organization.");

				project.Name = newName;
			}

			if (request.Code != null) // allow clearing via empty string
				project.Code = NormalizeOptional(request.Code);

			if (request.Description != null) // allow clearing via empty string
				project.Description = NormalizeOptional(request.Description);

			// Patch semantics for ClientId:
			// - If request.ClientId is not provided => do nothing
			// - If provided with value => set after validation
			// - If provided explicitly as null => clear
			//
			// This requires UpdateProjectRequest.ClientId to be Optional<Guid?> (suggested below).
			if (request.ClientId != null && request.ClientId.IsProvided)
			{
				if (request.ClientId.Value is null)
				{
					project.ClientId = null;
				}
				else
				{
					var cid = request.ClientId.Value.Value;

					var clientOk = await _db.Clients
						.AsNoTracking()
						.AnyAsync(c => c.Id == cid && c.OrganizationId == organizationId);

					if (!clientOk)
						throw new ValidationException("Client does not belong to this organization.", new Dictionary<string, string[]>
						{
							["clientId"] = new[] { "Client does not belong to this organization." }
						});

					project.ClientId = cid;
				}
			}

			if (request.IsActive.HasValue)
				project.IsActive = request.IsActive.Value;

			project.UpdatedAtUtc = DateTime.UtcNow;

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Update could not be saved due to a conflict.", ex);
			}

			return Map(project);
		}

		public async Task<ProjectAssignmentResponse> AssignUserAsync(Guid actorUserId, Guid organizationId, Guid projectId, AssignUserToProjectRequest request)
		{
			var canAssign = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canAssign) throw new ForbiddenException("Only Admin/Manager can assign users to projects.");

			if (request.UserId == Guid.Empty)
				throw new ValidationException("UserId is required.", new Dictionary<string, string[]>
				{
					["userId"] = new[] { "UserId is required." }
				});

			var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId);
			if (project is null) throw new NotFoundException("Project not found.");

			var userIsMember = await _db.OrganizationMembers.AsNoTracking()
				.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == request.UserId && m.IsActive);
			if (!userIsMember) throw new ValidationException("User is not a member of this organization.", new Dictionary<string, string[]>
			{
				["userId"] = new[] { "User must be a member of the organization." }
			});

			var existing = await _db.ProjectAssignments.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.UserId == request.UserId);
			if (existing != null) return MapAssignment(existing);

			var assignment = new ProjectAssignment
			{
				OrganizationId = organizationId,
				ProjectId = projectId,
				UserId = request.UserId,
				AssignedByUserId = actorUserId,
				AssignedAtUtc = DateTime.UtcNow
			};
			_db.ProjectAssignments.Add(assignment);
			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Assignment could not be saved due to a conflict.", ex);
			}
			return MapAssignment(assignment);
		}

		public async Task UnassignUserAsync(Guid actorUserId, Guid organizationId, Guid projectId, Guid userId)
		{
			var canUnassign = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canUnassign) throw new ForbiddenException("Only Admin/Manager can unassign users from projects.");

			var projectExists = await _db.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId && p.OrganizationId == organizationId);
			if (!projectExists) throw new NotFoundException("Project not found.");

			var assignment = await _db.ProjectAssignments.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.UserId == userId);
			if (assignment is null) throw new NotFoundException("Assignment not found.");

			_db.ProjectAssignments.Remove(assignment);
			await _db.SaveChangesAsync();
		}

		public async Task<List<ProjectAssignmentResponse>> GetProjectAssignmentsAsync(Guid actorUserId, Guid organizationId, Guid projectId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var projectExists = await _db.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId && p.OrganizationId == organizationId);
			if (!projectExists) throw new NotFoundException("Project not found.");

			var assignments = await _db.ProjectAssignments
				.AsNoTracking()
				.Where(a => a.ProjectId == projectId)
				.OrderBy(a => a.AssignedAtUtc)
				.ToListAsync();
			return assignments.Select(MapAssignment).ToList();
		}

		private static ProjectAssignmentResponse MapAssignment(ProjectAssignment a) => new ProjectAssignmentResponse
		{
			Id = a.Id,
			ProjectId = a.ProjectId,
			UserId = a.UserId,
			AssignedByUserId = a.AssignedByUserId,
			AssignedAtUtc = a.AssignedAtUtc
		};

		private static string? NormalizeOptional(string? value)
			=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static ProjectResponse Map(Project p) => new ProjectResponse
		{
			Id = p.Id,
			OrganizationId = p.OrganizationId,
			ClientId = p.ClientId,
			Name = p.Name,
			Code = p.Code,
			Description = p.Description,
			IsActive = p.IsActive,
			CreatedAtUtc = p.CreatedAtUtc,
			UpdatedAtUtc = p.UpdatedAtUtc
		};
	}
}
