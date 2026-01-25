using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.TimesheetEntries;
using Times.Entities;
using Times.Services.Contracts;

namespace Times.Services.Implementation
{
	public class TimesheetEntryService : ITimesheetEntryService
	{
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public TimesheetEntryService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<List<TimesheetEntryResponse>> ListAsync(Guid actorUserId, Guid organizationId, Guid timesheetId)
		{
			var ts = await GetTimesheetOrNullAsync(organizationId, timesheetId);
			if (ts is null) return new List<TimesheetEntryResponse>();

			// Owner OR manager/admin can view
			if (ts.UserId != actorUserId)
			{
				var canView = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin, OrganizationRole.Manager);
				if (!canView) return new List<TimesheetEntryResponse>();
			}

			var items = await _db.TimesheetEntries
				.AsNoTracking()
				.Where(e => e.OrganizationId == organizationId && e.TimesheetId == timesheetId && !e.IsDeleted)
				.OrderBy(e => e.WorkDate)
				.ThenBy(e => e.StartTime)
				.ToListAsync();

			return items.Select(Map).ToList();
		}

		public async Task<TimesheetEntryResponse> CreateAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, CreateTimesheetEntryRequest request)
		{
			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) throw new KeyNotFoundException("Timesheet not found.");

			// Only owner can add entries
			if (ts.UserId != actorUserId) throw new UnauthorizedAccessException("Only the owner can edit this timesheet.");

			EnsureEditable(ts);

			// Validate date is within week range
			if (request.WorkDate < ts.WeekStartDate || request.WorkDate > ts.WeekEndDate)
				throw new ArgumentException("WorkDate must fall within the timesheet week.");

			// Validate project is in org
			var projectOk = await _db.Projects
				.AsNoTracking()
				.AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == organizationId && p.IsActive);

			if (!projectOk) throw new ArgumentException("Project does not belong to this organization (or is inactive).");

			var duration = ComputeDurationMinutes(request.StartTime, request.EndTime, request.DurationMinutes);

			var now = DateTime.UtcNow;

			var entry = new TimesheetEntry
			{
				OrganizationId = organizationId,
				TimesheetId = timesheetId,
				ProjectId = request.ProjectId,
				TaskId = request.TaskId, // later validate
				WorkDate = request.WorkDate,
				StartTime = request.StartTime,
				EndTime = request.EndTime,
				DurationMinutes = duration,
				Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
				CreatedAtUtc = now,
				UpdatedAtUtc = now,
				IsDeleted = false
			};

			_db.TimesheetEntries.Add(entry);

			ts.UpdatedAtUtc = now;

			await _db.SaveChangesAsync();

			return Map(entry);
		}

		public async Task<TimesheetEntryResponse?> UpdateAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, Guid entryId, UpdateTimesheetEntryRequest request)
		{
			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return null;

			if (ts.UserId != actorUserId) return null;

			EnsureEditable(ts);

			var entry = await _db.TimesheetEntries
				.FirstOrDefaultAsync(e => e.Id == entryId && e.OrganizationId == organizationId && e.TimesheetId == timesheetId);

			if (entry is null) return null;

			if (request.IsDeleted.HasValue && request.IsDeleted.Value)
			{
				entry.IsDeleted = true;
				entry.UpdatedAtUtc = DateTime.UtcNow;
				ts.UpdatedAtUtc = entry.UpdatedAtUtc;
				await _db.SaveChangesAsync();
				return Map(entry);
			}

			if (request.WorkDate.HasValue)
			{
				if (request.WorkDate.Value < ts.WeekStartDate || request.WorkDate.Value > ts.WeekEndDate)
					throw new ArgumentException("WorkDate must fall within the timesheet week.");

				entry.WorkDate = request.WorkDate.Value;
			}

			if (request.ProjectId.HasValue)
			{
				var projectOk = await _db.Projects
					.AsNoTracking()
					.AnyAsync(p => p.Id == request.ProjectId.Value && p.OrganizationId == organizationId && p.IsActive);

				if (!projectOk) throw new ArgumentException("Project does not belong to this organization (or is inactive).");

				entry.ProjectId = request.ProjectId.Value;
			}

			// Notes: allow clearing by sending empty string
			if (request.Notes != null)
				entry.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

			// Times/duration: if any of these are provided, recompute
			var start = request.StartTime ?? entry.StartTime;
			var end = request.EndTime ?? entry.EndTime;
			var dur = request.DurationMinutes ?? (int?)null;

			// Update times if provided
			if (request.StartTime.HasValue) entry.StartTime = request.StartTime.Value;
			if (request.EndTime.HasValue) entry.EndTime = request.EndTime.Value;

			// If duration was explicitly provided in request, treat it as intent
			// Otherwise recompute only if start/end changed (handled below)
			if (request.DurationMinutes.HasValue || request.StartTime.HasValue || request.EndTime.HasValue)
			{
				var computed = ComputeDurationMinutes(entry.StartTime, entry.EndTime, request.DurationMinutes);
				entry.DurationMinutes = computed;
			}

			entry.UpdatedAtUtc = DateTime.UtcNow;
			ts.UpdatedAtUtc = entry.UpdatedAtUtc;

			await _db.SaveChangesAsync();
			return Map(entry);
		}

		public async Task<bool> DeleteAsync(Guid actorUserId, Guid organizationId, Guid timesheetId, Guid entryId)
		{
			var ts = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
			if (ts is null) return false;

			if (ts.UserId != actorUserId) return false;

			EnsureEditable(ts);

			var entry = await _db.TimesheetEntries
				.FirstOrDefaultAsync(e => e.Id == entryId && e.OrganizationId == organizationId && e.TimesheetId == timesheetId);

			if (entry is null) return false;

			entry.IsDeleted = true;
			entry.UpdatedAtUtc = DateTime.UtcNow;
			ts.UpdatedAtUtc = entry.UpdatedAtUtc;

			await _db.SaveChangesAsync();
			return true;
		}

		// Helpers

		private static void EnsureEditable(Timesheet ts)
		{
			if (ts.Status == TimesheetStatus.Submitted || ts.Status == TimesheetStatus.Approved)
				throw new ArgumentException("Timesheet is not editable in its current status.");
		}

		private static int ComputeDurationMinutes(TimeOnly? start, TimeOnly? end, int? durationMinutes)
		{
			// Either (start+end) OR durationMinutes
			if (durationMinutes.HasValue)
			{
				if (durationMinutes.Value <= 0) throw new ArgumentException("DurationMinutes must be greater than 0.");
				return durationMinutes.Value;
			}

			if (!start.HasValue || !end.HasValue)
				throw new ArgumentException("Provide either DurationMinutes, or both StartTime and EndTime.");

			// MVP: disallow overnight shifts (end must be after start)
			if (end.Value <= start.Value)
				throw new ArgumentException("EndTime must be after StartTime.");

			var minutes = (int)(end.Value.ToTimeSpan() - start.Value.ToTimeSpan()).TotalMinutes;
			if (minutes <= 0) throw new ArgumentException("Calculated duration must be greater than 0.");
			return minutes;
		}

		private async Task<Timesheet?> GetTimesheetOrNullAsync(Guid organizationId, Guid timesheetId)
		{
			return await _db.Timesheets
				.AsNoTracking()
				.FirstOrDefaultAsync(t => t.Id == timesheetId && t.OrganizationId == organizationId);
		}

		private static TimesheetEntryResponse Map(TimesheetEntry e) => new TimesheetEntryResponse
		{
			Id = e.Id,
			OrganizationId = e.OrganizationId,
			TimesheetId = e.TimesheetId,
			ProjectId = e.ProjectId,
			TaskId = e.TaskId,
			WorkDate = e.WorkDate,
			StartTime = e.StartTime,
			EndTime = e.EndTime,
			DurationMinutes = e.DurationMinutes,
			DurationHours = Math.Round(e.DurationMinutes / 60m, 2),
			Notes = e.Notes,
			IsDeleted = e.IsDeleted
		};
	}
}
