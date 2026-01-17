using Times.Dto.Clients;
using Times.Database;
using Times.Entities;
using Times.Services.Contracts;

namespace Times.Services.Implementation
{
	public class ClientService : IClientService
	{
		private readonly DataContext _db;

		public ClientService(DataContext db)
		{
			_db = db;
		}

		public IReadOnlyList<ClientResponse> GetMyClients(Guid userId)
		{
			return _db.Clients
				.Where(c => c.UserId == userId)
				.Select(c => new ClientResponse
				{
					Id = c.Id,
					Name = c.Name,
					Email = c.Email,
					Phone = c.Phone
				})
				.ToList();
		}

		public ClientResponse CreateClient(Guid userId, CreateClientRequest request)
		{
			var client = new Client
			{
				Name = request.Name,
				Email = request.Email,
				Phone = request.Phone,
				UserId = userId
			};

			_db.Clients.Add(client);
			_db.SaveChanges();

			return new ClientResponse
			{
				Id = client.Id,
				Name = client.Name,
				Email = client.Email,
				Phone = client.Phone
			};
		}

		public ClientResponse GetClientById(Guid userId, Guid clientId)
		{
			var client = _db.Clients
				.FirstOrDefault(c => c.Id == clientId && c.UserId == userId);
			if (client == null)
			{
				throw new Exception("Client not found");
			}
			return new ClientResponse
			{
				Id = client.Id,
				Name = client.Name,
				Email = client.Email,
				Phone = client.Phone
			};
		}

		public void DeleteClient(Guid userId, Guid clientId)
		{
			var client = _db.Clients
				.FirstOrDefault(c => c.Id == clientId && c.UserId == userId);
			if (client == null)
			{
				throw new Exception("Client not found");
			}
			_db.Clients.Remove(client);
			_db.SaveChanges();
		}

		public ClientResponse UpdateClient(Guid userId, Guid clientId, UpdateClientRequest request)
		{
			var client = _db.Clients
				.FirstOrDefault(c => c.Id == clientId && c.UserId == userId);
			if (client == null)
			{
				throw new Exception("Client not found");
			}
			client.Name = request.Name;
			client.Email = request.Email;
			client.Phone = request.Phone;
			_db.SaveChanges();
			return new ClientResponse
			{
				Id = client.Id,
				Name = client.Name,
				Email = client.Email,
				Phone = client.Phone
			};
		}
	}

}
