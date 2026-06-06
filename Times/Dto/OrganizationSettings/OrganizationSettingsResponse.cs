using System;

namespace Times.Dto.OrganizationSettings
{
	public class OrganizationSettingsResponse
	{
		public Guid OrganizationId { get; set; }
		public string WeekStartDay { get; set; } = "monday";
		public bool AllowFutureTimesheets { get; set; }
		public int FutureTimesheetWindowDays { get; set; }
		public bool LockTimesheetOnSubmit { get; set; }
		public bool AllowOvernightEntries { get; set; }
		public DateTime CreatedAtUtc { get; set; }
		public DateTime UpdatedAtUtc { get; set; }
		public Guid? UpdatedByUserId { get; set; }
	}
}
