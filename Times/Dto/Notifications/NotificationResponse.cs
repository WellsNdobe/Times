using System;

namespace Times.Dto.Notifications
{
	public class NotificationResponse
	{
		public Guid Id { get; set; }
		public Guid OrganizationId { get; set; }
		public Guid RecipientUserId { get; set; }
		public Guid? ActorUserId { get; set; }
		public Guid? TimesheetId { get; set; }
		public int Type { get; set; }
		public string Title { get; set; } = null!;
		public string Message { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; }
		public DateTime? ReadAtUtc { get; set; }
		public bool IsRead { get; set; }
	}
}

