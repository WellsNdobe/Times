using Times.Dto.Clients;

namespace Times.Services.Contracts
{
    public interface IClientService
    {
		IReadOnlyList<ClientResponse> GetMyClients(Guid userId);

		ClientResponse CreateClient(Guid userId, CreateClientRequest request);

		void DeleteClient(Guid userId, Guid clientId);

		ClientResponse GetClientById(Guid userId, Guid clientId);

		ClientResponse UpdateClient(Guid userId, Guid clientId, UpdateClientRequest request);

	}
}
