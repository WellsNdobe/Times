using System;

namespace Times.Dto.TimesheetEntries
{
	public class TimesheetEntryResponse
	{
		public Guid Id { get; set; }

		public Guid OrganizationId { get; set; }
		public Guid TimesheetId { get; set; }

		public Guid ProjectId { get; set; }
		public Guid? TaskId { get; set; }

		public DateOnly WorkDate { get; set; }

		public TimeOnly? StartTime { get; set; }
		public TimeOnly? EndTime { get; set; }

		public int DurationMinutes { get; set; }
		public decimal DurationHours { get; set; }

		public string? Notes { get; set; }

		public bool IsDeleted { get; set; }
	}
}
