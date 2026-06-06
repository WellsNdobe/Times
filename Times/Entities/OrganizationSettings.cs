using System;

namespace Times.Entities
{
	public class OrganizationSettings
	{
		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		public WeekStartDay WeekStartDay { get; set; } = WeekStartDay.Monday;
		public bool AllowFutureTimesheets { get; set; } = true;
		public int FutureTimesheetWindowDays { get; set; } = 7;
		public bool LockTimesheetOnSubmit { get; set; } = true;
		public bool AllowOvernightEntries { get; set; } = false;

		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

		public Guid? UpdatedByUserId { get; set; }
		public User? UpdatedByUser { get; set; }
	}
}
