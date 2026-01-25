using System;
using System.Collections.Generic;
namespace Times.Entities
{
	public class Timesheet
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		// Scope & ownership
		public Guid OrganizationId { get; set; }
		public Organization Organization { get; set; } = null!;

		// employee who owns this timesheet
		public Guid UserId { get; set; }        
		public User User { get; set; } = null!;

		// Period (weekly timesheet)
		public DateOnly WeekStartDate { get; set; }
		public DateOnly WeekEndDate { get; set; }  

		public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

		// Submission / approval metadata
		public DateTime? SubmittedAtUtc { get; set; }
		public string? SubmissionComment { get; set; }

		public DateTime? ApprovedAtUtc { get; set; }
		public Guid? ApprovedByUserId { get; set; }   // manager who approved/rejected
		public User? ApprovedByUser { get; set; }

		public DateTime? RejectedAtUtc { get; set; }
		public string? RejectionReason { get; set; }

		// Locking approved timesheets to prevent edits
		public DateTime? LockedAtUtc { get; set; }

		// Audit
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

		public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
	}
}
