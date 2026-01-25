using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.TimesheetEntries;

namespace Times.Services.Contracts
{
	public interface ITimesheetEntryService
	{
		Task<List<TimesheetEntryResponse>> ListAsync(Guid actorUserId, Guid organizationId, Guid timesheetId);

		Task<TimesheetEntryResponse> CreateAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, CreateTimesheetEntryRequest request);

		Task<TimesheetEntryResponse?> UpdateAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, Guid entryId, UpdateTimesheetEntryRequest request);

		Task<bool> DeleteAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, Guid entryId);
	}
}
