using System;
using System.Threading.Tasks;
using Times.Dto.TimesheetEntries;
using Times.Dto.Tracker;

namespace Times.Services.Contracts
{
	public interface ITrackerService
	{
		Task<ActiveTimerSessionResponse?> GetActiveSessionAsync(Guid actorUserId, Guid organizationId);
		Task<ActiveTimerSessionResponse> StartAsync(Guid actorUserId, Guid organizationId, StartActiveTimerSessionRequest request);
		Task<ActiveTimerSessionResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateActiveTimerSessionRequest request);
		Task<TimesheetEntryResponse> StopAsync(Guid actorUserId, Guid organizationId, StopActiveTimerSessionRequest request);
		Task DeleteAsync(Guid actorUserId, Guid organizationId);
	}
}
