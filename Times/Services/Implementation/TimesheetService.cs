using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.Timesheets;
using Times.Entities;
using Times.Services.Contracts;

namespace Times.Services.Implementation
{
	public class TimesheetService : ITimesheetService
	{
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;
		private readonly INotificationService _notifications;

		public TimesheetService(DataContext db, IOrganizationService orgs, INotificationService notifications)
		{
			_db = db;
			_orgs = orgs;
			_notifications = notifications;
		}

		public async Task<TimesheetResponse> CreateAsync(Guid actorUserId, Guid organizationId, CreateTimesheetRequest request)
		{
			// Any member can create their own timesheet
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new UnauthorizedAccessException("You are not a member of this organization.");

			var weekStart = NormalizeToWeekStart(request.WeekStartDate);
			var weekEnd = weekStart.AddDays(6);

			// One timesheet per user per week per org
			var existing = await _db.Timesheets
				.AsNoTracking()
				.FirstOrDefaultAsync(t => t.OrganizationId == organizationId
										 && t.UserId == actorUserId
										 && t.WeekStartDate == weekStart);

			if (existing != null)
				return await BuildResponseAsync(existing.Id); // idempotent-ish: return existing

			var now = DateTime.UtcNow;

			var ts = new Timesheet
			{
				OrganizationId = organizationId,
				UserId = actorUserId,
				WeekStartDate = weekStart,
				WeekEndDate = weekEnd,
				Status = TimesheetStatus.Draft,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			};

			_db.Timesheets.Add(ts);
			await _db.SaveChangesAsync();

			return await BuildResponseAsync(ts.Id);
		}

		public async Task<List<TimesheetResponse>> ListMineAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) return new List<TimesheetResponse>();

			var q = _db.Timesheets
				.AsNoTracking()
				.Where(t => t.OrganizationId == organizationId && t.UserId == actorUserId);

			if (fromWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate >= NormalizeToWeekStart(fromWeekStart.Value));

			if (toWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate <= NormalizeToWeekStart(toWeekStart.Value));

			var items = await q.OrderByDescending(t => t.WeekStartDate).ToListAsync();

			// Build totals efficiently
			var ids = items.Select(x => x.Id).ToList();

			var totals = await _db.TimesheetEntries
				.AsNoTracking()
				.Where(e => ids.Contains(e.TimesheetId) && !e.IsDeleted)
				.GroupBy(e => e.TimesheetId)
				.Select(g => new { TimesheetId = g.Key, TotalMinutes = g.Sum(x => x.DurationMinutes) })
				.ToListAsync();

			var totalsMap = totals.ToDictionary(x => x.TimesheetId, x => x.TotalMinutes);

			return items.Select(t => Map(t, totalsMap.TryGetValue(t.Id, out var m) ? m : 0)).ToList();
		}

		public async Task<List<TimesheetResponse>> ListOrgAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null)
		{
			var canView = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canView) return new List<TimesheetResponse>();

			var q = _db.Timesheets
				.AsNoTracking()
				.Where(t => t.OrganizationId == organizationId);

			if (fromWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate >= NormalizeToWeekStart(fromWeekStart.Value));

			if (toWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate <= NormalizeToWeekStart(toWeekStart.Value));

			var items = await q.OrderByDescending(t => t.WeekStartDate).ThenBy(t => t.UserId).ToListAsync();
			var ids = items.Select(x => x.Id).ToList();

			var totals = await _db.TimesheetEntries
				.AsNoTracking()
				.Where(e => ids.Contains(e.TimesheetId) && !e.IsDeleted)
				.GroupBy(e => e.TimesheetId)
				.Select(g => new { TimesheetId = g.Key, TotalMinutes = g.Sum(x => x.DurationMinutes) })
				.ToListAsync();

			var totalsMap = totals.ToDictionary(x => x.TimesheetId, x => x.TotalMinutes);

			return items.Select(t => Map(t, totalsMap.TryGetValue(t.Id, out var m) ? m : 0)).ToList();
		}

		public async Task<TimesheetResponse?> GetByIdAsync(Guid actorUserId, Guid organizationId, Guid timesheetId)
		{
			// Owner OR manager/admin in the org can view
			var ts = await _db.Timesheets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return null;

			if (ts.UserId != actorUserId)
			{
				var canView = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
				if (!canView) return null;
			}

			return await BuildResponseAsync(timesheetId);
		}

		public async Task<TimesheetResponse?> SubmitAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, SubmitTimesheetRequest request)
		{
			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return null;

			if (ts.UserId != actorUserId) return null; // only owner can submit

			if (ts.Status != TimesheetStatus.Draft && ts.Status != TimesheetStatus.Rejected)
				throw new ArgumentException("Only Draft or Rejected timesheets can be submitted.");

			// Must have at least one non-deleted entry (MyHours-like expectation)
			var hasEntries = await _db.TimesheetEntries
				.AsNoTracking()
				.AnyAsync(e => e.TimesheetId == timesheetId && !e.IsDeleted);

			if (!hasEntries)
				throw new ArgumentException("Cannot submit an empty timesheet.");

			var now = DateTime.UtcNow;
			ts.Status = TimesheetStatus.Submitted;
			ts.SubmittedAtUtc = now;
			ts.SubmissionComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
			ts.UpdatedAtUtc = now;

			// Optional: lock immediately on submit (MyHours typically locks on approval, but prevents edits in submitted state anyway)
			// ts.LockedAtUtc = now;

			await _db.SaveChangesAsync();
			await _notifications.NotifyTimesheetSubmittedAsync(actorUserId, ts);
			return await BuildResponseAsync(timesheetId);
		}

		public async Task<TimesheetResponse?> ApproveAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, ApproveTimesheetRequest request)
		{
			var canApprove = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canApprove) return null;

			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return null;

			if (ts.Status != TimesheetStatus.Submitted)
				throw new ArgumentException("Only Submitted timesheets can be approved.");

			var now = DateTime.UtcNow;
			ts.Status = TimesheetStatus.Approved;
			ts.ApprovedAtUtc = now;
			ts.ApprovedByUserId = actorUserId;

			// Clear rejection fields if it was previously rejected
			ts.RejectedAtUtc = null;
			ts.RejectionReason = null;

			// Lock on approval (MyHours-style)
			ts.LockedAtUtc = now;
			ts.UpdatedAtUtc = now;

			await _db.SaveChangesAsync();
			await _notifications.NotifyTimesheetApprovedAsync(actorUserId, ts, request.Comment);
			return await BuildResponseAsync(timesheetId);
		}

		public async Task<TimesheetResponse?> RejectAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, RejectTimesheetRequest request)
		{
			var canReject = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canReject) return null;

			if (string.IsNullOrWhiteSpace(request.Reason))
				throw new ArgumentException("Rejection reason is required.");

			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return null;

			if (ts.Status != TimesheetStatus.Submitted)
				throw new ArgumentException("Only Submitted timesheets can be rejected.");

			var now = DateTime.UtcNow;
			ts.Status = TimesheetStatus.Rejected;
			ts.RejectedAtUtc = now;
			ts.RejectionReason = request.Reason.Trim();

			// Set who acted
			ts.ApprovedByUserId = actorUserId;
			ts.ApprovedAtUtc = null;

			// Unlock for edits
			ts.LockedAtUtc = null;

			ts.UpdatedAtUtc = now;

			await _db.SaveChangesAsync();
			await _notifications.NotifyTimesheetRejectedAsync(actorUserId, ts, request.Reason);
			return await BuildResponseAsync(timesheetId);
		}

		public async Task<List<TimesheetResponse>> ListPendingApprovalAsync(Guid actorUserId, Guid organizationId, DateOnly? fromWeekStart = null, DateOnly? toWeekStart = null)
		{
			var canView = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
			if (!canView) return new List<TimesheetResponse>();

			var q = _db.Timesheets
				.AsNoTracking()
				.Where(t => t.OrganizationId == organizationId && t.Status == TimesheetStatus.Submitted);

			if (fromWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate >= NormalizeToWeekStart(fromWeekStart.Value));

			if (toWeekStart.HasValue)
				q = q.Where(t => t.WeekStartDate <= NormalizeToWeekStart(toWeekStart.Value));

			var items = await q.OrderBy(t => t.WeekStartDate).ThenBy(t => t.SubmittedAtUtc).ToListAsync();
			var ids = items.Select(x => x.Id).ToList();

			var totals = await _db.TimesheetEntries
				.AsNoTracking()
				.Where(e => ids.Contains(e.TimesheetId) && !e.IsDeleted)
				.GroupBy(e => e.TimesheetId)
				.Select(g => new { TimesheetId = g.Key, TotalMinutes = g.Sum(x => x.DurationMinutes) })
				.ToListAsync();

			var totalsMap = totals.ToDictionary(x => x.TimesheetId, x => x.TotalMinutes);

			return items.Select(t => Map(t, totalsMap.TryGetValue(t.Id, out var m) ? m : 0)).ToList();
		}

		// Helpers

		private static DateOnly NormalizeToWeekStart(DateOnly date)
		{
			// Standardize to Monday-start weeks (MyHours-style expectation).
			// DayOfWeek: Sunday=0, Monday=1, ... Saturday=6
			var dow = (int)date.DayOfWeek;
			var offset = dow == 0 ? 6 : dow - 1; // Sunday -> 6 days back, Monday -> 0
			return date.AddDays(-offset);
		}

		private async Task<TimesheetResponse> BuildResponseAsync(Guid timesheetId)
		{
			var ts = await _db.Timesheets.AsNoTracking().FirstAsync(t => t.Id == timesheetId);

			var totalMinutes = await _db.TimesheetEntries
				.AsNoTracking()
				.Where(e => e.TimesheetId == timesheetId && !e.IsDeleted)
				.SumAsync(e => (int?)e.DurationMinutes) ?? 0;

			return Map(ts, totalMinutes);
		}

		private static TimesheetResponse Map(Timesheet t, int totalMinutes) => new TimesheetResponse
		{
			Id = t.Id,
			OrganizationId = t.OrganizationId,
			UserId = t.UserId,
			WeekStartDate = t.WeekStartDate,
			WeekEndDate = t.WeekEndDate,
			Status = t.Status,
			SubmittedAtUtc = t.SubmittedAtUtc,
			SubmissionComment = t.SubmissionComment,
			ApprovedAtUtc = t.ApprovedAtUtc,
			ApprovedByUserId = t.ApprovedByUserId,
			RejectedAtUtc = t.RejectedAtUtc,
			RejectionReason = t.RejectionReason,
			LockedAtUtc = t.LockedAtUtc,
			TotalMinutes = totalMinutes,
			TotalHours = Math.Round(totalMinutes / 60m, 2)
		};
	}
}
