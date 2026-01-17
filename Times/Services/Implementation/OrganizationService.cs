using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.OrganizationMembers;
using Times.Dto.Organizations;
using Times.Entities;
using Times.Services.Contracts;

namespace Times.Services.Implementation
{
	public class OrganizationService : IOrganizationService
	{
		private readonly DataContext _db;

		public OrganizationService(DataContext db)
		{
			_db = db;
		}

		public async Task<OrganizationResponse> CreateAsync(Guid actorUserId, CreateOrganizationRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Name))
				throw new ArgumentException("Organization name is required.");

			var org = new Organization
			{
				Name = request.Name.Trim(),
				IsActive = true,
				CreatedAtUtc = DateTime.UtcNow,
				UpdatedAtUtc = DateTime.UtcNow
			};

			var membership = new OrganizationMember
			{
				Organization = org,
				UserId = actorUserId,
				Role = OrganizationRole.Admin,
				IsActive = true,
				CreatedAtUtc = DateTime.UtcNow,
				UpdatedAtUtc = DateTime.UtcNow
			};

			_db.Organizations.Add(org);
			_db.OrganizationMembers.Add(membership);

			await _db.SaveChangesAsync();

			return Map(org);
		}

		public async Task<List<OrganizationResponse>> GetMyOrganizationsAsync(Guid actorUserId)
		{
			var orgs = await _db.OrganizationMembers
				.AsNoTracking()
				.Where(m => m.UserId == actorUserId && m.IsActive)
				.Select(m => m.Organization)
				.Where(o => o.IsActive)
				.OrderBy(o => o.Name)
				.ToListAsync();

			return orgs.Select(Map).ToList();
		}

		public async Task<OrganizationResponse?> GetByIdAsync(Guid actorUserId, Guid organizationId)
		{
			var isMember = await _db.OrganizationMembers
				.AsNoTracking()
				.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == actorUserId && m.IsActive);

			if (!isMember) return null;

			var org = await _db.Organizations
				.AsNoTracking()
				.FirstOrDefaultAsync(o => o.Id == organizationId);

			return org is null ? null : Map(org);
		}

		public async Task<OrganizationResponse?> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateOrganizationRequest request)
		{
			// Only Admin can update org details
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) return null;

			var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
			if (org is null) return null;

			if (!string.IsNullOrWhiteSpace(request.Name))
				org.Name = request.Name.Trim();

			if (request.IsActive.HasValue)
				org.IsActive = request.IsActive.Value;

			org.UpdatedAtUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Map(org);
		}

		public async Task<List<OrganizationMemberResponse>> GetMembersAsync(Guid actorUserId, Guid organizationId)
		{
			var canView = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canView) return new List<OrganizationMemberResponse>();

			var members = await _db.OrganizationMembers
				.AsNoTracking()
				.Where(m => m.OrganizationId == organizationId)
				.OrderBy(m => m.Role)
				.ThenBy(m => m.CreatedAtUtc)
				.ToListAsync();

			return members.Select(Map).ToList();
		}

		public async Task<OrganizationMemberResponse> AddMemberAsync(Guid actorUserId, Guid organizationId, AddMemberRequest request)
		{
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) throw new UnauthorizedAccessException("Only Admin can add members.");

			// prevent duplicates
			var existing = await _db.OrganizationMembers
				.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == request.UserId);

			if (existing != null)
			{
				existing.Role = request.Role;
				existing.IsActive = true;
				existing.UpdatedAtUtc = DateTime.UtcNow;
				await _db.SaveChangesAsync();
				return Map(existing);
			}

			var member = new OrganizationMember
			{
				OrganizationId = organizationId,
				UserId = request.UserId,
				Role = request.Role,
				IsActive = true,
				CreatedAtUtc = DateTime.UtcNow,
				UpdatedAtUtc = DateTime.UtcNow
			};

			_db.OrganizationMembers.Add(member);
			await _db.SaveChangesAsync();

			return Map(member);
		}

		public async Task<OrganizationMemberResponse?> UpdateMemberAsync(Guid actorUserId, Guid organizationId, Guid memberId, UpdateMemberRequest request)
		{
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) return null;

			var member = await _db.OrganizationMembers
				.FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

			if (member is null) return null;

			if (request.Role.HasValue)
				member.Role = request.Role.Value;

			if (request.IsActive.HasValue)
				member.IsActive = request.IsActive.Value;

			member.UpdatedAtUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Map(member);
		}

		public async Task<OrganizationMember?> GetMembershipAsync(Guid actorUserId, Guid organizationId)
		{
			return await _db.OrganizationMembers
				.AsNoTracking()
				.FirstOrDefaultAsync(m =>
					m.OrganizationId == organizationId &&
					m.UserId == actorUserId &&
					m.IsActive);
		}

		public async Task<bool> IsInRoleAsync(Guid actorUserId, Guid organizationId, params OrganizationRole[] roles)
		{
			return await _db.OrganizationMembers
				.AsNoTracking()
				.AnyAsync(m =>
					m.OrganizationId == organizationId &&
					m.UserId == actorUserId &&
					m.IsActive &&
					roles.Contains(m.Role));
		}

		private static OrganizationResponse Map(Organization org) => new OrganizationResponse
		{
			Id = org.Id,
			Name = org.Name,
			IsActive = org.IsActive,
			CreatedAtUtc = org.CreatedAtUtc,
			UpdatedAtUtc = org.UpdatedAtUtc
		};

		private static OrganizationMemberResponse Map(OrganizationMember m) => new OrganizationMemberResponse
		{
			Id = m.Id,
			OrganizationId = m.OrganizationId,
			UserId = m.UserId,
			Role = m.Role,
			IsActive = m.IsActive,
			CreatedAtUtc = m.CreatedAtUtc,
			UpdatedAtUtc = m.UpdatedAtUtc
		};
	}
}
