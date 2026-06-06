namespace Times.Dto.OrganizationSettings
{
	public class UpdateOrganizationSettingsRequest
	{
		public string? WeekStartDay { get; set; }
		public bool? AllowFutureTimesheets { get; set; }
		public int? FutureTimesheetWindowDays { get; set; }
		public bool? LockTimesheetOnSubmit { get; set; }
		public bool? AllowOvernightEntries { get; set; }
	}
}
