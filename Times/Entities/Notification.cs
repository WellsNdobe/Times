using System;

namespace Times.Entities
{
	public class Notification
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public Guid RecipientUserId { get; set; }
		public User RecipientUser { get; set; } = null!;

		public Guid? ActorUserId { get; set; }
		public User? ActorUser { get; set; }

		public Guid? TimesheetId { get; set; }
		public Timesheet? Timesheet { get; set; }

		public NotificationType Type { get; set; }

		public string Title { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;

		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime? ReadAtUtc { get; set; }
	}
}
