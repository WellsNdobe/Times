using System;
using Times.Entities;

namespace Times.Services.Implementation
{
	internal static class OrganizationSettingsPolicy
	{
		public static DateOnly NormalizeToWeekStart(DateOnly date, WeekStartDay weekStartDay)
		{
			var dow = (int)date.DayOfWeek;
			var offset = weekStartDay switch
			{
				WeekStartDay.Sunday => dow,
				_ => dow == 0 ? 6 : dow - 1
			};

			return date.AddDays(-offset);
		}

		public static DateOnly GetWeekEnd(DateOnly weekStartDate) => weekStartDate.AddDays(6);

		public static DateOnly GetMaxAllowedWeekStart(DateOnly today, OrganizationSettings settings)
		{
			var maxDate = today.AddDays(Math.Max(0, settings.FutureTimesheetWindowDays));
			return NormalizeToWeekStart(maxDate, settings.WeekStartDay);
		}

		public static bool CanAccessWeekStart(DateOnly weekStartDate, DateOnly today, OrganizationSettings settings)
		{
			var normalizedTarget = NormalizeToWeekStart(weekStartDate, settings.WeekStartDay);
			var currentWeekStart = NormalizeToWeekStart(today, settings.WeekStartDay);
			if (normalizedTarget <= currentWeekStart)
			{
				return true;
			}

			if (!settings.AllowFutureTimesheets)
			{
				return false;
			}

			return normalizedTarget <= GetMaxAllowedWeekStart(today, settings);
		}

		public static bool IsEditable(TimesheetStatus status, OrganizationSettings settings)
		{
			if (status == TimesheetStatus.Approved)
			{
				return false;
			}

			if (status == TimesheetStatus.Submitted && settings.LockTimesheetOnSubmit)
			{
				return false;
			}

			return true;
		}

		public static int ComputeDurationMinutes(TimeOnly? start, TimeOnly? end, int? durationMinutes, bool allowOvernightEntries)
		{
			if (durationMinutes.HasValue)
			{
				if (durationMinutes.Value <= 0)
				{
					throw new ArgumentException("DurationMinutes must be greater than 0.");
				}

				return durationMinutes.Value;
			}

			if (!start.HasValue || !end.HasValue)
			{
				throw new ArgumentException("Provide either DurationMinutes, or both StartTime and EndTime.");
			}

			var startSpan = start.Value.ToTimeSpan();
			var endSpan = end.Value.ToTimeSpan();

			if (endSpan <= startSpan)
			{
				if (!allowOvernightEntries)
				{
					throw new ArgumentException("EndTime must be after StartTime.");
				}

				endSpan = endSpan.Add(TimeSpan.FromDays(1));
			}

			var minutes = (int)(endSpan - startSpan).TotalMinutes;
			if (minutes <= 0)
			{
				throw new ArgumentException("Calculated duration must be greater than 0.");
			}

			return minutes;
		}
	}
}
