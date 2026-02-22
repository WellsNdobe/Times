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
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public NotificationService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<List<NotificationResponse>> ListAsync(Guid actorUserId, Guid organizationId, bool unreadOnly = false, int take = 20)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var normalizedTake = take <= 0 ? 20 : Math.Min(take, 100);

			var q = _db.Notifications
				.AsNoTracking()
				.Where(n => n.OrganizationId == organizationId && n.RecipientUserId == actorUserId);

			if (unreadOnly)
				q = q.Where(n => n.ReadAtUtc == null);

			var items = await q
				.OrderByDescending(n => n.CreatedAtUtc)
				.Take(normalizedTake)
				.ToListAsync();

			return items.Select(Map).ToList();
		}

		public async Task<int> MarkReadAsync(Guid actorUserId, Guid organizationId, IReadOnlyCollection<Guid> ids)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			if (ids.Count == 0) return 0;

			var items = await _db.Notifications
				.Where(n => n.OrganizationId == organizationId && n.RecipientUserId == actorUserId && ids.Contains(n.Id))
				.ToListAsync();

			if (items.Count == 0) return 0;

			var now = DateTime.UtcNow;
			var updated = 0;
			foreach (var item in items)
			{
				if (item.ReadAtUtc != null) continue;
				item.ReadAtUtc = now;
				updated++;
			}

			if (updated > 0)
				await _db.SaveChangesAsync();

			return updated;
		}

		public async Task<int> MarkAllReadAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var items = await _db.Notifications
				.Where(n => n.OrganizationId == organizationId && n.RecipientUserId == actorUserId && n.ReadAtUtc == null)
				.ToListAsync();

			if (items.Count == 0) return 0;

			var now = DateTime.UtcNow;
			foreach (var item in items)
				item.ReadAtUtc = now;

			await _db.SaveChangesAsync();
			return items.Count;
		}

		public async Task<NotificationResponse?> CreateReminderAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null) throw new ForbiddenException("You are not a member of this organization.");

			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			var weekStart = NormalizeToWeekStart(today);

			var ts = await _db.Timesheets
				.AsNoTracking()
				.FirstOrDefaultAsync(t => t.OrganizationId == organizationId
										 && t.UserId == actorUserId
										 && t.WeekStartDate == weekStart);

			if (ts is null) return null;
			if (ts.Status == TimesheetStatus.Submitted || ts.Status == TimesheetStatus.Approved)
				return null;

			var since = DateTime.UtcNow.AddHours(-24);
			var recentReminder = await _db.Notifications.AsNoTracking()
				.AnyAsync(n => n.OrganizationId == organizationId
							   && n.RecipientUserId == actorUserId
							   && n.Type == NotificationType.Reminder
							   && n.TimesheetId == ts.Id
							   && n.CreatedAtUtc >= since);

			if (recentReminder) return null;

			var weekLabel = ts.WeekStartDate.ToString("yyyy-MM-dd");
			var notif = new Notification
			{
				OrganizationId = organizationId,
				RecipientUserId = actorUserId,
				ActorUserId = null,
				TimesheetId = ts.Id,
				Type = NotificationType.Reminder,
				Title = "Reminder: submit timesheet",
				Message = $"Don't forget to submit your timesheet for week of {weekLabel}.",
				CreatedAtUtc = DateTime.UtcNow
			};

			_db.Notifications.Add(notif);
			await _db.SaveChangesAsync();
			return Map(notif);
		}

		public Task CreateTimesheetNotificationAsync(
			NotificationType type,
			Guid actorUserId,
			Guid organizationId,
			Guid timesheetId,
			Guid recipientUserId,
			string title,
			string message)
		{
			return CreateTimesheetNotificationsAsync(
				type,
				actorUserId,
				organizationId,
				timesheetId,
				new List<Guid> { recipientUserId },
				title,
				message);
		}

		public async Task CreateTimesheetNotificationsAsync(
			NotificationType type,
			Guid actorUserId,
			Guid organizationId,
			Guid timesheetId,
			IReadOnlyCollection<Guid> recipientUserIds,
			string title,
			string message)
		{
			if (recipientUserIds.Count == 0) return;

			var recipients = recipientUserIds
				.Where(id => id != Guid.Empty)
				.Distinct()
				.ToList();

			if (recipients.Count == 0) return;

			var now = DateTime.UtcNow;
			var items = recipients.Select(recipientId => new Notification
			{
				OrganizationId = organizationId,
				RecipientUserId = recipientId,
				ActorUserId = actorUserId,
				TimesheetId = timesheetId,
				Type = type,
				Title = title,
				Message = message,
				CreatedAtUtc = now
			}).ToList();

			_db.Notifications.AddRange(items);
			await _db.SaveChangesAsync();
		}

		private static DateOnly NormalizeToWeekStart(DateOnly date)
		{
			var dow = (int)date.DayOfWeek;
			var offset = dow == 0 ? 6 : dow - 1;
			return date.AddDays(-offset);
		}

		private static NotificationResponse Map(Notification n) => new NotificationResponse
		{
			Id = n.Id,
			OrganizationId = n.OrganizationId,
			RecipientUserId = n.RecipientUserId,
			ActorUserId = n.ActorUserId,
			TimesheetId = n.TimesheetId,
			Type = n.Type,
			Title = n.Title,
			Message = n.Message,
			CreatedAtUtc = n.CreatedAtUtc,
			ReadAtUtc = n.ReadAtUtc
		};
	}
}
