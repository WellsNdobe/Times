using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.OrganizationMembers;
using Times.Dto.Organizations;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Services.Implementation
{
	public class OrganizationService : IOrganizationService
	{
		private const int MinPasswordLength = 6;
		private readonly DataContext _db;
		private readonly PasswordHasher<User> _passwordHasher;

		public OrganizationService(DataContext db)
		{
			_db = db;
			_passwordHasher = new PasswordHasher<User>();
		}

		public async Task<OrganizationResponse> CreateAsync(Guid actorUserId, CreateOrganizationRequest request)
		{
			var name = (request.Name ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(name))
				throw new ValidationException("Organization name is required.", new Dictionary<string, string[]>
				{
					["name"] = new[] { "Organization name is required." }
				});

			// Optional but recommended: unique org name per system / per owner / per tenant.
			// If you enforce it in DB, keep this as a friendly pre-check.
			// var nameExists = await _db.Organizations.AsNoTracking().AnyAsync(o => o.Name == name);
			// if (nameExists) throw new ConflictException("An organization with this name already exists.");

			var now = DateTime.UtcNow;

			var org = new Organization
			{
				Name = name,
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			var membership = new OrganizationMember
			{
				Organization = org,
				OrganizationId = org.Id,
				UserId = actorUserId,
				Role = OrganizationRole.Admin,
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == actorUserId);
			if (user is null) throw new NotFoundException("User not found.");
			if (!HasRole(user.Role, "Admin"))
				user.Role = string.IsNullOrWhiteSpace(user.Role) ? "Admin" : $"{user.Role},Admin";

			_db.Organizations.Add(org);
			_db.OrganizationMembers.Add(membership);
			org.Members.Add(membership);

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				// If you add unique constraints later, convert races to a clean 409 instead of 500.
				throw new ConflictException("Organization could not be created due to a conflict.", ex);
			}

			return Map(org);
		}

		public async Task<List<OrganizationResponse>> GetMyOrganizationsAsync(Guid actorUserId)
		{
			// This one is naturally "empty list is fine"—not having orgs isn't an error.
			var orgs = await _db.OrganizationMembers
				.AsNoTracking()
				.Where(m => m.UserId == actorUserId && m.IsActive)
				.Select(m => m.Organization)
				.Where(o => o.IsActive)
				.OrderBy(o => o.Name)
				.ToListAsync();

			return orgs.Select(Map).ToList();
		}

		public async Task<OrganizationResponse> GetByIdAsync(Guid actorUserId, Guid organizationId)
		{
			// Membership is authorization. Decide if you want to hide existence:
			// - Security-hiding: return NotFound if not member
			// - Clearer: Forbidden if not member
			// I’ll go with Forbidden here for consistency with your ProjectService cleanup.
			var isMember = await _db.OrganizationMembers
				.AsNoTracking()
				.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == actorUserId && m.IsActive);

			if (!isMember) throw new ForbiddenException("You are not a member of this organization.");

			var org = await _db.Organizations
				.AsNoTracking()
				.FirstOrDefaultAsync(o => o.Id == organizationId);

			if (org is null) throw new NotFoundException("Organization not found.");

			return Map(org);
		}

		public async Task<OrganizationResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateOrganizationRequest request)
		{
			// Only Admin can update org details
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) throw new ForbiddenException("Only Admin can update organization details.");

			var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
			if (org is null) throw new NotFoundException("Organization not found.");

			if (request.Name != null)
			{
				var name = request.Name.Trim();
				if (string.IsNullOrWhiteSpace(name))
					throw new ValidationException("Organization name cannot be empty.", new Dictionary<string, string[]>
					{
						["name"] = new[] { "Organization name cannot be empty." }
					});

				org.Name = name;
			}

			if (request.IsActive.HasValue)
				org.IsActive = request.IsActive.Value;

			org.UpdatedAtUtc = DateTime.UtcNow;

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Organization update could not be saved due to a conflict.", ex);
			}

			return Map(org);
		}

		public async Task<List<OrganizationMemberResponse>> GetMembersAsync(Guid actorUserId, Guid organizationId)
		{
			var isMember = await _db.OrganizationMembers
				.AsNoTracking()
				.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == actorUserId && m.IsActive);
			if (!isMember) throw new ForbiddenException("You are not a member of this organization.");

			// Optional: if you want a cleaner error when org doesn't exist
			// you can check existence here and throw NotFoundException.
			// (But it does add an extra query.)
			// var orgExists = await _db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId);
			// if (!orgExists) throw new NotFoundException("Organization not found.");

			var members = await _db.OrganizationMembers
				.AsNoTracking()
				.Include(m => m.User)
				.Where(m => m.OrganizationId == organizationId)
				.OrderBy(m => m.Role)
				.ThenBy(m => m.CreatedAtUtc)
				.ToListAsync();

			return members.Select(m => Map(m, m.User)).ToList();
		}

		public async Task<OrganizationMemberResponse> AddMemberAsync(Guid actorUserId, Guid organizationId, AddMemberRequest request)
		{
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) throw new ForbiddenException("Only Admin can add members.");

			if (request.UserId == Guid.Empty)
				throw new ValidationException("UserId is required.", new Dictionary<string, string[]>
				{
					["userId"] = new[] { "UserId is required." }
				});

			// Optional guard: ensure org exists (otherwise you get a foreign key fail later)
			var orgExists = await _db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId);
			if (!orgExists) throw new NotFoundException("Organization not found.");

			// prevent duplicates: if exists, re-activate / update role
			var existing = await _db.OrganizationMembers
				.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == request.UserId);

			var now = DateTime.UtcNow;

			if (existing != null)
			{
				existing.Role = request.Role;
				existing.IsActive = true;
				existing.UpdatedAtUtc = now;

				try
				{
					await _db.SaveChangesAsync();
				}
				catch (DbUpdateException ex)
				{
					throw new ConflictException("Member could not be updated due to a conflict.", ex);
				}

				return await MapWithUserAsync(existing);
			}

			var member = new OrganizationMember
			{
				OrganizationId = organizationId,
				UserId = request.UserId,
				Role = request.Role,
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			_db.OrganizationMembers.Add(member);

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Member could not be added due to a conflict.", ex);
			}

			return await MapWithUserAsync(member);
		}

		public async Task<OrganizationMemberResponse> CreateUserInOrganizationAsync(Guid actorUserId, Guid organizationId, CreateOrganizationUserRequest request)
		{
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) throw new ForbiddenException("Only Admin can create users in the organization.");

			var errors = ValidateCreateOrganizationUserRequest(request);
			if (errors.Count > 0)
				throw new ValidationException("Validation failed.", errors);

			var orgExists = await _db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId);
			if (!orgExists) throw new NotFoundException("Organization not found.");

			var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
			var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email);

			var now = DateTime.UtcNow;
			User user;
			if (existingUser != null)
			{
				user = await _db.Users.FirstAsync(u => u.Id == existingUser.Id);
				// Add existing user to org (same as AddMember)
				var existingMember = await _db.OrganizationMembers
					.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == user.Id);
				if (existingMember != null)
				{
					existingMember.Role = request.Role;
					existingMember.IsActive = true;
					existingMember.UpdatedAtUtc = now;
					await _db.SaveChangesAsync();
					return Map(existingMember, user);
				}
				var member = new OrganizationMember
				{
					OrganizationId = organizationId,
					UserId = user.Id,
					Role = request.Role,
					IsActive = true,
					CreatedAtUtc = now,
					UpdatedAtUtc = now
				};
				_db.OrganizationMembers.Add(member);
				await _db.SaveChangesAsync();
				return Map(member, user);
			}

			// Create new user and add to org
			user = new User
			{
				Id = Guid.NewGuid(),
				Email = request.Email!.Trim(),
				FirstName = (request.FirstName ?? string.Empty).Trim(),
				LastName = (request.LastName ?? string.Empty).Trim(),
				IsActive = true,
				CreatedAt = now
			};
			user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
			var newMember = new OrganizationMember
			{
				OrganizationId = organizationId,
				UserId = user.Id,
				Role = request.Role,
				IsActive = true,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};
			_db.Users.Add(user);
			_db.OrganizationMembers.Add(newMember);
			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("User could not be created or added due to a conflict.", ex);
			}
			return Map(newMember, user);
		}

		public async Task<OrganizationMemberResponse> UpdateMemberAsync(Guid actorUserId, Guid organizationId, Guid memberId, UpdateMemberRequest request)
		{
			var isAdmin = await IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin) throw new ForbiddenException("Only Admin can update members.");

			var member = await _db.OrganizationMembers
				.FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

			if (member is null) throw new NotFoundException("Organization member not found.");

			if (request.Role.HasValue)
				member.Role = request.Role.Value;

			if (request.IsActive.HasValue)
				member.IsActive = request.IsActive.Value;

			member.UpdatedAtUtc = DateTime.UtcNow;

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Member update could not be saved due to a conflict.", ex);
			}

			return await MapWithUserAsync(member);
		}

		public async Task<OrganizationMember?> GetMembershipAsync(Guid actorUserId, Guid organizationId)
		{
			// This stays nullable because it's used as a helper/lookup.
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

		private static bool HasRole(string? roles, string role)
		{
			if (string.IsNullOrWhiteSpace(roles)) return false;
			return roles.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Any(r => string.Equals(r.Trim(), role, StringComparison.OrdinalIgnoreCase));
		}

		private static OrganizationMemberResponse Map(OrganizationMember m, User? user) => new OrganizationMemberResponse
		{
			Id = m.Id,
			OrganizationId = m.OrganizationId,
			UserId = m.UserId,
			FirstName = user?.FirstName ?? string.Empty,
			LastName = user?.LastName ?? string.Empty,
			Role = m.Role,
			IsActive = m.IsActive,
			CreatedAtUtc = m.CreatedAtUtc,
			UpdatedAtUtc = m.UpdatedAtUtc
		};

		private async Task<OrganizationMemberResponse> MapWithUserAsync(OrganizationMember member)
		{
			var user = member.User ?? await _db.Users.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == member.UserId);
			return Map(member, user);
		}

		private static Dictionary<string, string[]> ValidateCreateOrganizationUserRequest(CreateOrganizationUserRequest request)
		{
			var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(request.Email))
				AddError(errors, "email", "Email is required.");
			else if (!MailAddress.TryCreate(request.Email, out _))
				AddError(errors, "email", "Email format is invalid.");
			if (string.IsNullOrWhiteSpace(request.Password))
				AddError(errors, "password", "Password is required.");
			else if (request.Password.Length < MinPasswordLength)
				AddError(errors, "password", $"Password must be at least {MinPasswordLength} characters.");
			if (string.IsNullOrWhiteSpace(request.FirstName))
				AddError(errors, "firstName", "First name is required.");
			if (string.IsNullOrWhiteSpace(request.LastName))
				AddError(errors, "lastName", "Last name is required.");
			return errors;
		}

		private static void AddError(Dictionary<string, string[]> errors, string key, string message)
		{
			errors[key] = errors.TryGetValue(key, out var existing)
				? existing.Append(message).ToArray()
				: new[] { message };
		}
	}
}
