using System;
using Times.Entities;

namespace Times.Dto.Notifications
{
	public class NotificationResponse
	{
		public Guid Id { get; set; }
		public Guid OrganizationId { get; set; }
		public Guid RecipientUserId { get; set; }
		public Guid? ActorUserId { get; set; }
		public Guid? TimesheetId { get; set; }
		public NotificationType Type { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public DateTime CreatedAtUtc { get; set; }
		public DateTime? ReadAtUtc { get; set; }
		public bool IsRead => ReadAtUtc.HasValue;
	}
}
