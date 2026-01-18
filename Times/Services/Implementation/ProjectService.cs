using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.Projects;
using Times.Entities;
using Times.Services.Contracts;

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
			// Require Manager/Admin to create projects (you can relax this if you want)
			var canCreate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canCreate) throw new UnauthorizedAccessException("Only Admin/Manager can create projects.");

			if (string.IsNullOrWhiteSpace(request.Name))
				throw new ArgumentException("Project name is required.");

			// If ClientId provided, ensure it belongs to the same org
			if (request.ClientId.HasValue)
			{
				var clientOk = await _db.Clients
					.AsNoTracking()
					.AnyAsync(c => c.Id == request.ClientId.Value && c.OrganizationId == organizationId);

				if (!clientOk) throw new ArgumentException("Client does not belong to this organization.");
			}

			// Optional: enforce unique Name per org (recommended)
			var nameExists = await _db.Projects
				.AsNoTracking()
				.AnyAsync(p => p.OrganizationId == organizationId && p.Name == request.Name.Trim());

			if (nameExists) throw new ArgumentException("A project with this name already exists in the organization.");

			var now = DateTime.UtcNow;

			var project = new Project
			{
				OrganizationId = organizationId,
				ClientId = request.ClientId,
				Name = request.Name.Trim(),
				Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
				Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			_db.Projects.Add(project);
			await _db.SaveChangesAsync();

			return Map(project);
		}

		public async Task<List<ProjectResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool? isActive = null, Guid? clientId = null)
		{
			// Any org member can list projects
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) return new List<ProjectResponse>();

			var q = _db.Projects.AsNoTracking().Where(p => p.OrganizationId == organizationId);

			if (isActive.HasValue)
				q = q.Where(p => p.IsActive == isActive.Value);

			if (clientId.HasValue)
				q = q.Where(p => p.ClientId == clientId.Value);

			var items = await q
				.OrderBy(p => p.Name)
				.ToListAsync();

			return items.Select(Map).ToList();
		}

		public async Task<ProjectResponse?> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid projectId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) return null;

			var p = await _db.Projects
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == projectId && x.OrganizationId == organizationId);

			return p is null ? null : Map(p);
		}

		public async Task<ProjectResponse?> UpdateAsync(Guid actorUserId, Guid organizationId, Guid projectId, UpdateProjectRequest request)
		{
			var canUpdate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canUpdate) return null;

			var project = await _db.Projects
				.FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId);

			if (project is null) return null;

			if (!string.IsNullOrWhiteSpace(request.Name))
			{
				var newName = request.Name.Trim();

				var nameExists = await _db.Projects
					.AsNoTracking()
					.AnyAsync(p => p.OrganizationId == organizationId && p.Id != projectId && p.Name == newName);

				if (nameExists) throw new ArgumentException("A project with this name already exists in the organization.");

				project.Name = newName;
			}

			if (request.Code != null) // allow clearing via empty string
				project.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

			if (request.Description != null) // allow clearing via empty string
				project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

			// ClientId: allow setting or clearing. BUT: nullable Guid can't distinguish "not provided" vs "clear"
			// Your DTO uses Guid? so:
			// - If caller sends no "clientId" property => stays null in model binding? (It stays null either way.)
			// If you want true patch semantics, use a wrapper type. For now we’ll treat provided value as intent
			// only if the request includes it in JSON. Easiest is to accept a separate endpoint for assign/unassign.
			//
			// Pragmatic approach for now: if request.ClientId is not null -> set it. If it is null, do nothing.
			// If you want "clear" now, add a bool flag or a dedicated endpoint.
			if (request.ClientId.HasValue)
			{
				var clientOk = await _db.Clients
					.AsNoTracking()
					.AnyAsync(c => c.Id == request.ClientId.Value && c.OrganizationId == organizationId);

				if (!clientOk) throw new ArgumentException("Client does not belong to this organization.");

				project.ClientId = request.ClientId.Value;
			}

			if (request.IsActive.HasValue)
				project.IsActive = request.IsActive.Value;

			project.UpdatedAtUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Map(project);
		}

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
