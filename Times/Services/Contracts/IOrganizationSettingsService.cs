using System;
using System.Threading.Tasks;
using Times.Dto.OrganizationSettings;
using Times.Entities;

namespace Times.Services.Contracts
{
	public interface IOrganizationSettingsService
	{
		Task<OrganizationSettingsResponse> GetAsync(Guid actorUserId, Guid organizationId);
		Task<OrganizationSettingsResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateOrganizationSettingsRequest request);
		Task<OrganizationSettings> GetForOrganizationAsync(Guid organizationId);
	}
}
