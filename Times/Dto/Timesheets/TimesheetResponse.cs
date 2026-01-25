using System;
using Times.Entities;

namespace Times.Dto.Timesheets
{
	public class TimesheetResponse
	{
		public Guid Id { get; set; }

		public Guid OrganizationId { get; set; }
		public Guid UserId { get; set; }

		public DateOnly WeekStartDate { get; set; }
		public DateOnly WeekEndDate { get; set; }

		public TimesheetStatus Status { get; set; }

		public DateTime? SubmittedAtUtc { get; set; }
		public string? SubmissionComment { get; set; }

		public DateTime? ApprovedAtUtc { get; set; }
		public Guid? ApprovedByUserId { get; set; }

		public DateTime? RejectedAtUtc { get; set; }
		public string? RejectionReason { get; set; }

		public DateTime? LockedAtUtc { get; set; }

		//Neat UI helpers
		public int TotalMinutes { get; set; }
		public decimal TotalHours { get; set; }
	}
}
