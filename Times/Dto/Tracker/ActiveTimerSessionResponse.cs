using System;

namespace Times.Dto.Tracker
{
	public class ActiveTimerSessionResponse
	{
		public Guid Id { get; set; }
		public Guid OrganizationId { get; set; }
		public Guid UserId { get; set; }
		public Guid TimesheetId { get; set; }
		public Guid ProjectId { get; set; }
		public DateOnly WorkDate { get; set; }
		public string? Notes { get; set; }
		public DateTime StartedAtUtc { get; set; }
		public int UtcOffsetMinutes { get; set; }
	}
}
