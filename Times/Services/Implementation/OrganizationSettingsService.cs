using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Times.Database;
using Times.Dto.OrganizationSettings;
using Times.Entities;
using Times.Services.Contracts;
using Times.Services.Errors;

namespace Times.Services.Implementation
{
	public class OrganizationSettingsService : IOrganizationSettingsService
	{
		private readonly DataContext _db;
		private readonly IOrganizationService _orgs;

		public OrganizationSettingsService(DataContext db, IOrganizationService orgs)
		{
			_db = db;
			_orgs = orgs;
		}

		public async Task<OrganizationSettingsResponse> GetAsync(Guid actorUserId, Guid organizationId)
		{
			var membership = await _orgs.GetMembershipAsync(actorUserId, organizationId);
			if (membership is null)
			{
				throw new ForbiddenException("You are not a member of this organization.");
			}

			var settings = await GetForOrganizationAsync(organizationId);
			return Map(settings);
		}

		public async Task<OrganizationSettingsResponse> UpdateAsync(Guid actorUserId, Guid organizationId, UpdateOrganizationSettingsRequest request)
		{
			var isAdmin = await _orgs.IsInRoleAsync(actorUserId, organizationId, OrganizationRole.Admin);
			if (!isAdmin)
			{
				throw new ForbiddenException("Only Admin can update organization settings.");
			}

			var settings = await _db.OrganizationSettings
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

			if (settings is null)
			{
				throw new NotFoundException("Organization settings not found.");
			}

			if (request.WeekStartDay != null)
			{
				settings.WeekStartDay = ParseWeekStartDay(request.WeekStartDay);
			}

			if (request.AllowFutureTimesheets.HasValue)
			{
				settings.AllowFutureTimesheets = request.AllowFutureTimesheets.Value;
			}

			if (request.FutureTimesheetWindowDays.HasValue)
			{
				if (request.FutureTimesheetWindowDays.Value < 0)
				{
					throw new ValidationException("FutureTimesheetWindowDays must be zero or greater.", new Dictionary<string, string[]>
					{
						["futureTimesheetWindowDays"] = new[] { "FutureTimesheetWindowDays must be zero or greater." }
					});
				}

				settings.FutureTimesheetWindowDays = request.FutureTimesheetWindowDays.Value;
			}

			if (request.LockTimesheetOnSubmit.HasValue)
			{
				settings.LockTimesheetOnSubmit = request.LockTimesheetOnSubmit.Value;
			}

			if (request.AllowOvernightEntries.HasValue)
			{
				settings.AllowOvernightEntries = request.AllowOvernightEntries.Value;
			}

			settings.UpdatedAtUtc = DateTime.UtcNow;
			settings.UpdatedByUserId = actorUserId;

			await _db.SaveChangesAsync();
			return Map(settings);
		}

		public async Task<OrganizationSettings> GetForOrganizationAsync(Guid organizationId)
		{
			var settings = await _db.OrganizationSettings
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

			if (settings is null)
			{
				throw new NotFoundException("Organization settings not found.");
			}

			return settings;
		}

		private static OrganizationSettingsResponse Map(OrganizationSettings settings) => new OrganizationSettingsResponse
		{
			OrganizationId = settings.OrganizationId,
			WeekStartDay = ToApiValue(settings.WeekStartDay),
			AllowFutureTimesheets = settings.AllowFutureTimesheets,
			FutureTimesheetWindowDays = settings.FutureTimesheetWindowDays,
			LockTimesheetOnSubmit = settings.LockTimesheetOnSubmit,
			AllowOvernightEntries = settings.AllowOvernightEntries,
			CreatedAtUtc = settings.CreatedAtUtc,
			UpdatedAtUtc = settings.UpdatedAtUtc,
			UpdatedByUserId = settings.UpdatedByUserId
		};

		public static WeekStartDay ParseWeekStartDay(string value)
		{
			return value.Trim().ToLowerInvariant() switch
			{
				"monday" => WeekStartDay.Monday,
				"sunday" => WeekStartDay.Sunday,
				_ => throw new ValidationException("WeekStartDay is invalid.", new Dictionary<string, string[]>
				{
					["weekStartDay"] = new[] { "WeekStartDay must be either 'monday' or 'sunday'." }
				})
			};
		}

		public static string ToApiValue(WeekStartDay weekStartDay) => weekStartDay switch
		{
			WeekStartDay.Sunday => "sunday",
			_ => "monday"
		};
	}
}
