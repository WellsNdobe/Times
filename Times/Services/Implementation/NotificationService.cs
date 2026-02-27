using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.Notifications;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Services.Implementation
{
	public class NotificationService : INotificationService
	{
		private const int MaxTake = 100;

		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public NotificationService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<List<NotificationResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool unreadOnly = false, int take = 25)
		{
			await EnsureMemberAsync(actorUserId, organizationId);

			var finalTake = Math.Clamp(take, 1, MaxTake);

			var q = _db.Notifications
				.AsNoTracking()
				.Where(n => n.OrganizationId == organizationId && n.RecipientUserId == actorUserId);

			if (unreadOnly)
				q = q.Where(n => !n.IsRead);

			var items = await q
				.OrderByDescending(n => n.CreatedAtUtc)
				.Take(finalTake)
				.ToListAsync();

			return items.Select(Map).ToList();
		}

		public async Task<int> MarkReadAsync(Guid actorUserId, Guid organizationId, IReadOnlyCollection<Guid> ids)
		{
			await EnsureMemberAsync(actorUserId, organizationId);

			if (ids is null || ids.Count == 0) return 0;

			var items = await _db.Notifications
				.Where(n => n.OrganizationId == organizationId
							&& n.RecipientUserId == actorUserId
							&& ids.Contains(n.Id))
				.ToListAsync();

			var now = DateTime.UtcNow;
			var updated = 0;

			foreach (var n in items)
			{
				if (n.IsRead) continue;
				n.IsRead = true;
				n.ReadAtUtc ??= now;
				updated++;
			}

			if (updated > 0)
				await _db.SaveChangesAsync();

			return updated;
		}

		public async Task<int> MarkAllReadAsync(Guid actorUserId, Guid organizationId)
		{
			await EnsureMemberAsync(actorUserId, organizationId);

			var items = await _db.Notifications
				.Where(n => n.OrganizationId == organizationId
							&& n.RecipientUserId == actorUserId
							&& !n.IsRead)
				.ToListAsync();

			if (items.Count == 0) return 0;

			var now = DateTime.UtcNow;

			foreach (var n in items)
			{
				n.IsRead = true;
				n.ReadAtUtc ??= now;
			}

			await _db.SaveChangesAsync();
			return items.Count;
		}

		public async Task<NotificationResponse?> CreateReminderAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var isApprover = membership.Role == OrganizationRole.Admin || membership.Role == OrganizationRole.Manager;
			if (!isApprover) return null;

			var pendingCount = await _db.Timesheets
				.AsNoTracking()
				.CountAsync(t => t.OrganizationId == organizationId && t.Status == TimesheetStatus.Submitted);

			if (pendingCount <= 0) return null;

			var now = DateTime.UtcNow;
			var recentWindow = now.AddHours(-12);

			var hasRecentUnreadReminder = await _db.Notifications
				.AsNoTracking()
				.AnyAsync(n => n.OrganizationId == organizationId
							   && n.RecipientUserId == actorUserId
							   && n.Type == NotificationType.Reminder
							   && !n.IsRead
							   && n.CreatedAtUtc >= recentWindow);

			if (hasRecentUnreadReminder) return null;

			var notif = new Notification
			{
				OrganizationId = organizationId,
				RecipientUserId = actorUserId,
				ActorUserId = null,
				TimesheetId = null,
				Type = NotificationType.Reminder,
				Title = "Timesheets awaiting approval",
				Message = $"You have {pendingCount} timesheet(s) pending approval.",
				CreatedAtUtc = now,
				IsRead = false,
				ReadAtUtc = null
			};

			_db.Notifications.Add(notif);
			await _db.SaveChangesAsync();

			return Map(notif);
		}

		public async Task NotifyTimesheetSubmittedAsync(Guid actorUserId, Timesheet timesheet)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, timesheet.OrganizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId);
			var actorName = actor is null ? "An employee" : $"{actor.FirstName} {actor.LastName}".Trim();
			var weekStart = timesheet.WeekStartDate.ToString("yyyy-MM-dd");

			var recipients = await _db.OrganizationMembers
				.AsNoTracking()
				.Where(m => m.OrganizationId == timesheet.OrganizationId
							&& m.IsActive
							&& (m.Role == OrganizationRole.Admin || m.Role == OrganizationRole.Manager))
				.Select(m => m.UserId)
				.ToListAsync();

			if (recipients.Count == 0) return;

			var now = DateTime.UtcNow;

			foreach (var recipientUserId in recipients.Distinct())
			{
				if (recipientUserId == actorUserId) continue;

				_db.Notifications.Add(new Notification
				{
					OrganizationId = timesheet.OrganizationId,
					RecipientUserId = recipientUserId,
					ActorUserId = actorUserId,
					TimesheetId = timesheet.Id,
					Type = NotificationType.TimesheetSubmitted,
					Title = "Timesheet submitted",
					Message = $"{actorName} submitted a timesheet for the week starting {weekStart}.",
					CreatedAtUtc = now,
					IsRead = false,
					ReadAtUtc = null
				});
			}

			await _db.SaveChangesAsync();
		}

		public async Task NotifyTimesheetApprovedAsync(Guid actorUserId, Timesheet timesheet, string? comment)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, timesheet.OrganizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var weekStart = timesheet.WeekStartDate.ToString("yyyy-MM-dd");
			var message = $"Your timesheet for the week starting {weekStart} was approved.";
			if (!string.IsNullOrWhiteSpace(comment))
				message = $"{message} Comment: {comment.Trim()}";

			var notif = new Notification
			{
				OrganizationId = timesheet.OrganizationId,
				RecipientUserId = timesheet.UserId,
				ActorUserId = actorUserId,
				TimesheetId = timesheet.Id,
				Type = NotificationType.TimesheetApproved,
				Title = "Timesheet approved",
				Message = message,
				CreatedAtUtc = DateTime.UtcNow,
				IsRead = false,
				ReadAtUtc = null
			};

			_db.Notifications.Add(notif);
			await _db.SaveChangesAsync();
		}

		public async Task NotifyTimesheetRejectedAsync(Guid actorUserId, Timesheet timesheet, string reason)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, timesheet.OrganizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var weekStart = timesheet.WeekStartDate.ToString("yyyy-MM-dd");
			var cleanedReason = string.IsNullOrWhiteSpace(reason) ? "No reason provided." : reason.Trim();

			var notif = new Notification
			{
				OrganizationId = timesheet.OrganizationId,
				RecipientUserId = timesheet.UserId,
				ActorUserId = actorUserId,
				TimesheetId = timesheet.Id,
				Type = NotificationType.TimesheetRejected,
				Title = "Timesheet rejected",
				Message = $"Your timesheet for the week starting {weekStart} was rejected. Reason: {cleanedReason}",
				CreatedAtUtc = DateTime.UtcNow,
				IsRead = false,
				ReadAtUtc = null
			};

			_db.Notifications.Add(notif);
			await _db.SaveChangesAsync();
		}

		private static NotificationResponse Map(Notification n) => new()
		{
			Id = n.Id,
			OrganizationId = n.OrganizationId,
			RecipientUserId = n.RecipientUserId,
			ActorUserId = n.ActorUserId,
			TimesheetId = n.TimesheetId,
			Type = (int)n.Type,
			Title = n.Title,
			Message = n.Message,
			CreatedAtUtc = DateTime.SpecifyKind(n.CreatedAtUtc, DateTimeKind.Utc),
			ReadAtUtc = n.ReadAtUtc.HasValue
				? DateTime.SpecifyKind(n.ReadAtUtc.Value, DateTimeKind.Utc)
				: null,
			IsRead = n.IsRead
		};

		private async Task EnsureMemberAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");
		}
	}
}
