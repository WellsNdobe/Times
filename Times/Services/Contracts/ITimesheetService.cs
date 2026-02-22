using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.Timesheets;

namespace Times.Services.Contracts
{
	public interface ITimesheetService
	{
		Task<TimesheetResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateTimesheetRequest request);

		/// <summary>
		/// Manager/Admin: list timesheets for the organization.
		/// </summary>
		Task<List<TimesheetResponse>> ListOrgAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null);

		Task<List<TimesheetResponse>> ListMineAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null);

		Task<TimesheetResponse?> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid timesheetId);

		Task<TimesheetResponse?> SubmitAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, SubmitTimesheetRequest request);

		Task<TimesheetResponse?> ApproveAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, ApproveTimesheetRequest request);

		Task<TimesheetResponse?> RejectAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, RejectTimesheetRequest request);

		/// <summary>
		/// Manager/Admin: list timesheets in Submitted status (pending approval) for the organization.
		/// </summary>
		Task<List<TimesheetResponse>> ListPendingApprovalAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null);
	}
}
