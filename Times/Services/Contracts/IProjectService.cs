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

		Task<ProjectResponse?> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid projectId);

		Task<ProjectResponse?> UpdateAsync(Guid actorUserId, Guid organizationId, Guid projectId, UpdateProjectRequest request);
	}
}
