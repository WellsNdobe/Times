using System;

namespace Times.Entities
{
	public class TimesheetEntry
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public Guid TimesheetId { get; set; }
		public Timesheet Timesheet { get; set; } = null!;

		public Guid ProjectId { get; set; }
		public Project Project { get; set; } = null!;

		public Guid? TaskId { get; set; }
		// public ProjectTask? Task { get; set; }

		// Work info
		public DateOnly WorkDate { get; set; }

		public TimeOnly? StartTime { get; set; }
		public TimeOnly? EndTime { get; set; }

		public int DurationMinutes { get; set; }

		public string? Notes { get; set; }

		// Audit
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

		// Optional soft delete
		public bool IsDeleted { get; set; } = false;
	}
}
