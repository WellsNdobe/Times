using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.OrganizationMembers;
using Times.Dto.Organizations;
using Times.Entities;
namespace Times.Services.Contracts
{
	public interface IOrganizationService
	{
		Task<OrganizationResponse> CreateAsync(Guid actorUserId, CreateOrganizationRequest request);
		Task<List<OrganizationResponse>> GetMyOrganizationsAsync(Guid actorUserId);
		Task<OrganizationResponse> GetByIdAsync(Guid actorUserId, Guid organizationId);
		Task<OrganizationResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateOrganizationRequest request);

		Task<List<OrganizationMemberResponse>> GetMembersAsync(Guid actorUserId, Guid organizationId);
		Task<OrganizationMemberResponse> AddMemberAsync(Guid actorUserId, Guid organizationId, AddMemberRequest request);
		/// <summary>
		/// Creates a new user (if email not registered) and adds them to the organization, or adds existing user to the org.
		/// Admin only.
		/// </summary>
		Task<OrganizationMemberResponse> CreateUserInOrganizationAsync(Guid actorUserId, Guid organizationId, CreateOrganizationUserRequest request);
		Task<OrganizationMemberResponse> UpdateMemberAsync(Guid actorUserId, Guid organizationId, Guid memberId, UpdateMemberRequest request);

		Task<OrganizationMember?> GetMembershipAsync(Guid actorUserId, Guid organizationId);
		Task<bool> IsInRoleAsync(Guid actorUserId, Guid organizationId, params OrganizationRole[] roles);
	}
}
