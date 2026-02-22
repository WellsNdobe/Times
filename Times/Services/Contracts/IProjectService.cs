using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.Projects;

namespace Times.Services.Contracts
{
	public interface IProjectService
	{
		Task<ProjectResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateProjectRequest request);

		Task<List<ProjectResponse>> ListAsync(
			Guid actorUserId,
			Guid organizationId,
			bool? isActive = null,
			Guid? clientId = null);

		Task<ProjectResponse> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid projectId);

		Task<ProjectResponse> UpdateAsync(Guid actorUserId, Guid organizationId, Guid projectId, UpdateProjectRequest request);

		/// <summary>Manager/Admin: assign a user to a project. User must be an org member.</summary>
		Task<ProjectAssignmentResponse> AssignUserAsync(Guid actorUserId, Guid organizationId, Guid projectId, AssignUserToProjectRequest request);

		/// <summary>Manager/Admin: remove a user from a project.</summary>
		Task UnassignUserAsync(Guid actorUserId, Guid organizationId, Guid projectId, Guid userId);

		/// <summary>List users assigned to a project.</summary>
		Task<List<ProjectAssignmentResponse>> GetProjectAssignmentsAsync(Guid actorUserId, Guid organizationId, Guid projectId);
	}
}
