using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.Notifications;
using Times.Entities;

namespace Times.Services.Contracts
{
	public interface INotificationService
	{
		Task<List<NotificationResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool unreadOnly = false, int take = 25);
		Task<int> MarkReadAsync(Guid actorUserId, Guid organizationId, IReadOnlyCollection<Guid> ids);
		Task<int> MarkAllReadAsync(Guid actorUserId, Guid organizationId);
		Task<NotificationResponse?> CreateReminderAsync(Guid actorUserId, Guid organizationId);

		Task NotifyTimesheetSubmittedAsync(Guid actorUserId, Timesheet timesheet);
		Task NotifyTimesheetApprovedAsync(Guid actorUserId, Timesheet timesheet, string? comment);
		Task NotifyTimesheetRejectedAsync(Guid actorUserId, Timesheet timesheet, string reason);
	}
}

