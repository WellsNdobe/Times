using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Times.Dto.Notifications;
using Times.Entities;

namespace Times.Services.Contracts
{
	public interface INotificationService
	{
		Task<List<NotificationResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool unreadOnly = false, int take = 20);
		Task<int> MarkReadAsync(Guid actorUserId, Guid organizationId, IReadOnlyCollection<Guid> ids);
		Task<int> MarkAllReadAsync(Guid actorUserId, Guid organizationId);
		Task<NotificationResponse?> CreateReminderAsync(Guid actorUserId, Guid organizationId);

		Task CreateTimesheetNotificationAsync(
			NotificationType type,
			Guid actorUserId,
			Guid organizationId,
			Guid timesheetId,
			Guid recipientUserId,
			string title,
			string message);

		Task CreateTimesheetNotificationsAsync(
			NotificationType type,
			Guid actorUserId,
			Guid organizationId,
			Guid timesheetId,
			IReadOnlyCollection<Guid> recipientUserIds,
			string title,
			string message);
	}
}
