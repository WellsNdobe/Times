using Times.Dto.Clients;
using Times.Database;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;
using Microsoft.EntityFrameworkCore;

namespace Times.Services.Implementation
{
	public class ClientService : IClientService
	{
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public ClientService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<IReadOnlyList<ClientResponse>> ListAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			return await _db.Clients
				.AsNoTracking()
				.Where(c => c.OrganizationId == organizationId)
				.OrderBy(c => c.Name)
				.Select(c => Map(c))
				.ToListAsync();
		}

		public async Task<ClientResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateClientRequest request)
		{
			var canCreate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canCreate) throw new ForbiddenException("Only Admin/Manager can create clients.");

			var name = (request.Name ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(name))
				throw new ValidationException("Client name is required.", new Dictionary<string, string[]>
				{
					["name"] = new[] { "Client name is required." }
				});

			if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
				throw new ValidationException("Invalid email address.", new Dictionary<string, string[]>
				{
					["email"] = new[] { "Email address is not valid." }
				});

			var now = DateTime.UtcNow;

			var client = new Client
			{
				Name = name,
				Email = NormalizeOptional(request.Email),
				Phone = NormalizeOptional(request.Phone),
				UserId = actorUserId,              // if you want "created by"
				OrganizationId = organizationId     // <-- THIS was missing
			};

			_db.Clients.Add(client);

			try { await _db.SaveChangesAsync(); }
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Client could not be created due to a conflict.", ex);
			}

			return Map(client);
		}

		public async Task<ClientResponse> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid clientId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var client = await _db.Clients
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == clientId && c.OrganizationId == organizationId);

			if (client is null) throw new NotFoundException("Client not found.");

			return Map(client);
		}

		public async Task<ClientResponse> UpdateAsync(Guid actorUserId, Guid organizationId, Guid clientId, UpdateClientRequest request)
		{
			var canUpdate = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canUpdate) throw new ForbiddenException("Only Admin/Manager can update clients.");

			var client = await _db.Clients
				.FirstOrDefaultAsync(c => c.Id == clientId && c.OrganizationId == organizationId);

			if (client is null) throw new NotFoundException("Client not found.");

			if (request.Name != null)
			{
				var name = request.Name.Trim();
				if (string.IsNullOrWhiteSpace(name))
					throw new ValidationException("Client name cannot be empty.", new Dictionary<string, string[]>
					{
						["name"] = new[] { "Client name cannot be empty." }
					});
				client.Name = name;
			}

			if (request.Email != null)
			{
				if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
					throw new ValidationException("Invalid email address.", new Dictionary<string, string[]>
					{
						["email"] = new[] { "Email address is not valid." }
					});

				client.Email = NormalizeOptional(request.Email);
			}

			if (request.Phone != null)
				client.Phone = NormalizeOptional(request.Phone);

			try { await _db.SaveChangesAsync(); }
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Client could not be updated due to a conflict.", ex);
			}

			return Map(client);
		}

		public async Task DeleteAsync(Guid actorUserId, Guid organizationId, Guid clientId)
		{
			var canDelete = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canDelete) throw new ForbiddenException("Only Admin/Manager can delete clients.");

			var client = await _db.Clients
				.FirstOrDefaultAsync(c => c.Id == clientId && c.OrganizationId == organizationId);

			if (client is null) throw new NotFoundException("Client not found.");

			_db.Clients.Remove(client);

			try { await _db.SaveChangesAsync(); }
			catch (DbUpdateException ex)
			{
				throw new ConflictException("Client could not be deleted due to a conflict.", ex);
			}
		}

		private static ClientResponse Map(Client c) => new ClientResponse
		{
			Id = c.Id,
			Name = c.Name,
			Email = c.Email,
			Phone = c.Phone
		};

		private static string? NormalizeOptional(string? value)
			=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static bool IsValidEmail(string email)
			=> System.Net.Mail.MailAddress.TryCreate(email, out _);
	}
}
