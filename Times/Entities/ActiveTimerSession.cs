using System;

namespace Times.Entities
{
	public class ActiveTimerSession
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		public Guid TimesheetId { get; set; }
		public Timesheet Timesheet { get; set; } = null!;

		public Guid ProjectId { get; set; }
		public Project Project { get; set; } = null!;

		public DateOnly WorkDate { get; set; }
		public string? Notes { get; set; }

		public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
		public int UtcOffsetMinutes { get; set; }
	}
}
