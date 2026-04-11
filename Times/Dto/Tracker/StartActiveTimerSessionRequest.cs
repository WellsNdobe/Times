using System;

namespace Times.Dto.Tracker
{
	public class StartActiveTimerSessionRequest
	{
		public Guid TimesheetId { get; set; }
		public Guid ProjectId { get; set; }
		public DateOnly WorkDate { get; set; }
		public string? Notes { get; set; }
		public int UtcOffsetMinutes { get; set; }
	}
}
