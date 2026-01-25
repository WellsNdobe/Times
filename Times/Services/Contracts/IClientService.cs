using Times.Dto.Clients;

namespace Times.Services.Contracts
{
	public interface IClientService
	{
		Task<IReadOnlyList<ClientResponse>> ListAsync(Guid actorUserId, Guid organizationId);
		Task<ClientResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateClientRequest request);
		Task<ClientResponse> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid clientId);
		Task<ClientResponse> UpdateAsync(Guid actorUserId, Guid organizationId, Guid clientId, UpdateClientRequest request);
		Task DeleteAsync(Guid actorUserId, Guid organizationId, Guid clientId);
	}
}
