using System;

namespace Times.Dto.TimesheetEntries
{
	public class CreateTimesheetEntryRequest
	{
		public Guid ProjectId { get; set; }
		public Guid? TaskId { get; set; }

		public DateOnly WorkDate { get; set; }

		public TimeOnly? StartTime { get; set; }
		public TimeOnly? EndTime { get; set; }

		public int? DurationMinutes { get; set; }

		public string? Notes { get; set; }
	}
}
