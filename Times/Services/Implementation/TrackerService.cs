using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.TimesheetEntries;
using Times.Dto.Tracker;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Services.Implementation
{
		public class TrackerService : ITrackerService
		{
			private readonly DataContext _db;
			private readonly IOrganizationService _orgs;
			private readonly IOrganizationSettingsService _settings;

		public TrackerService(DataContext db, IOrganizationService orgs, IOrganizationSettingsService settings)
		{
			_db = db;
			_orgs = orgs;
			_settings = settings;
		}

		public async Task<ActiveTimerSessionResponse?> GetActiveSessionAsync(Guid actorUserId, Guid organizationId)
		{
			await EnsureMembershipAsync(actorUserId, organizationId);

			var session = await _db.ActiveTimerSessions
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == actorUserId);

			return session is null ? null : Map(session);
		}

		public async Task<ActiveTimerSessionResponse> StartAsync(Guid actorUserId, Guid organizationId, StartActiveTimerSessionRequest request)
		{
			await EnsureMembershipAsync(actorUserId, organizationId);

			var timesheet = await _db.Timesheets
				.FirstOrDefaultAsync(t => t.Id == request.TimesheetId && t.OrganizationId == organizationId);

			if (timesheet is null)
				throw new NotFoundException("Timesheet not found.");

			if (timesheet.UserId != actorUserId)
				throw new ForbiddenException("You can only track time against your own timesheet.");

			var settings = await _settings.GetForOrganizationAsync(organizationId);
			EnsureEditable(timesheet, settings);

			if (request.WorkDate < timesheet.WeekStartDate || request.WorkDate > timesheet.WeekEndDate)
				throw new ValidationException("WorkDate must fall within the timesheet week.", new Dictionary<string, string[]>
				{
					["workDate"] = new[] { "WorkDate must fall within the timesheet week." }
				});

			var projectExists = await _db.Projects
				.AsNoTracking()
				.AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == organizationId && p.IsActive);

			if (!projectExists)
				throw new ValidationException("Project does not belong to this organization.", new Dictionary<string, string[]>
				{
					["projectId"] = new[] { "Project does not belong to this organization (or is inactive)." }
				});

			var now = DateTime.UtcNow;
			var session = await _db.ActiveTimerSessions
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == actorUserId);

			if (session is null)
			{
				session = new ActiveTimerSession
				{
					OrganizationId = organizationId,
					UserId = actorUserId,
					TimesheetId = request.TimesheetId,
					ProjectId = request.ProjectId,
					WorkDate = request.WorkDate,
					Notes = NormalizeNotes(request.Notes),
					StartedAtUtc = now,
					UtcOffsetMinutes = request.UtcOffsetMinutes
				};
				_db.ActiveTimerSessions.Add(session);
			}
			else
			{
				session.TimesheetId = request.TimesheetId;
				session.ProjectId = request.ProjectId;
				session.WorkDate = request.WorkDate;
				session.Notes = NormalizeNotes(request.Notes);
				session.StartedAtUtc = now;
				session.UtcOffsetMinutes = request.UtcOffsetMinutes;
			}

			timesheet.UpdatedAtUtc = now;
			await _db.SaveChangesAsync();

			return Map(session);
		}

		public async Task<ActiveTimerSessionResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateActiveTimerSessionRequest request)
		{
			await EnsureMembershipAsync(actorUserId, organizationId);

			var session = await _db.ActiveTimerSessions
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == actorUserId);

			if (session is null)
				throw new NotFoundException("No active timer session found.");

			session.Notes = NormalizeNotes(request.Notes);
			await _db.SaveChangesAsync();

			return Map(session);
		}

		public async Task<TimesheetEntryResponse> StopAsync(Guid actorUserId, Guid organizationId, StopActiveTimerSessionRequest request)
		{
			await EnsureMembershipAsync(actorUserId, organizationId);

			var session = await _db.ActiveTimerSessions
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == actorUserId);

			if (session is null)
				throw new NotFoundException("No active timer session found.");

			var timesheet = await _db.Timesheets
				.FirstOrDefaultAsync(t => t.Id == session.TimesheetId && t.OrganizationId == organizationId);

			if (timesheet is null)
				throw new NotFoundException("Timesheet not found.");

			if (timesheet.UserId != actorUserId)
				throw new ForbiddenException("You can only stop your own active timer.");

			var settings = await _settings.GetForOrganizationAsync(organizationId);
			EnsureEditable(timesheet, settings);

			var projectExists = await _db.Projects
				.AsNoTracking()
				.AnyAsync(p => p.Id == session.ProjectId && p.OrganizationId == organizationId && p.IsActive);

			if (!projectExists)
				throw new ValidationException("Project does not belong to this organization.", new Dictionary<string, string[]>
				{
					["projectId"] = new[] { "Project does not belong to this organization (or is inactive)." }
				});

			var nowUtc = DateTime.UtcNow;
			var durationMinutes = (int)Math.Max(1, Math.Round((nowUtc - session.StartedAtUtc).TotalMinutes));
			var startLocal = session.StartedAtUtc.AddMinutes(session.UtcOffsetMinutes);
			var endLocal = nowUtc.AddMinutes(session.UtcOffsetMinutes);

			if (!settings.AllowOvernightEntries && startLocal.Date != endLocal.Date)
				throw new ValidationException("Overnight entries are disabled for this organization.", new Dictionary<string, string[]>
				{
					["workDate"] = new[] { "Overnight entries are disabled for this organization." }
				});

			var entry = new TimesheetEntry
			{
				OrganizationId = organizationId,
				TimesheetId = session.TimesheetId,
				ProjectId = session.ProjectId,
				WorkDate = session.WorkDate,
				StartTime = TimeOnly.FromDateTime(startLocal),
				EndTime = TimeOnly.FromDateTime(endLocal),
				DurationMinutes = durationMinutes,
				Notes = NormalizeNotes(request.Notes) ?? session.Notes,
				CreatedAtUtc = nowUtc,
				UpdatedAtUtc = nowUtc,
				IsDeleted = false
			};

			_db.TimesheetEntries.Add(entry);
			_db.ActiveTimerSessions.Remove(session);
			timesheet.UpdatedAtUtc = nowUtc;
			await _db.SaveChangesAsync();

			return Map(entry);
		}

		public async Task DeleteAsync(Guid actorUserId, Guid organizationId)
		{
			await EnsureMembershipAsync(actorUserId, organizationId);

			var session = await _db.ActiveTimerSessions
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == actorUserId);

			if (session is null)
				return;

			_db.ActiveTimerSessions.Remove(session);
			await _db.SaveChangesAsync();
		}

		private async Task EnsureMembershipAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null)
				throw new ForbiddenException("You are not a member of this organization.");
		}

		private static void EnsureEditable(Timesheet timesheet, OrganizationSettings settings)
		{
			if (!OrganizationSettingsPolicy.IsEditable(timesheet.Status, settings))
				throw new ValidationException("Timesheet is not editable in its current status.", new Dictionary<string, string[]>
				{
					["timesheetId"] = new[] { "Timesheet is not editable in its current status." }
				});
		}

		private static string? NormalizeNotes(string? notes)
		{
			return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
		}

		private static ActiveTimerSessionResponse Map(ActiveTimerSession session) => new ActiveTimerSessionResponse
		{
			Id = session.Id,
			OrganizationId = session.OrganizationId,
			UserId = session.UserId,
			TimesheetId = session.TimesheetId,
			ProjectId = session.ProjectId,
			WorkDate = session.WorkDate,
			Notes = session.Notes,
			StartedAtUtc = session.StartedAtUtc,
			UtcOffsetMinutes = session.UtcOffsetMinutes
		};

		private static TimesheetEntryResponse Map(TimesheetEntry entry) => new TimesheetEntryResponse
		{
			Id = entry.Id,
			OrganizationId = entry.OrganizationId,
			TimesheetId = entry.TimesheetId,
			ProjectId = entry.ProjectId,
			TaskId = entry.TaskId,
			WorkDate = entry.WorkDate,
			StartTime = entry.StartTime,
			EndTime = entry.EndTime,
			DurationMinutes = entry.DurationMinutes,
			DurationHours = Math.Round(entry.DurationMinutes / 60m, 2),
			Notes = entry.Notes,
			IsDeleted = entry.IsDeleted
		};
	}
}
