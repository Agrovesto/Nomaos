using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{

    /// <summary>UTC range. From is inclusive, To is exclusive.</summary>
    public readonly record struct DateRange(DateTime From, DateTime To);

    public record ResolvedPeriod(DateRange Current, DateRange Previous, TimeSpan Offset);

    public static class PeriodResolver
    {
        /// <summary>
        /// Works out the selected period and its equivalent previous period.
        /// Boundaries are computed in local time (utc + offset, e.g. +1h for Lagos) and returned as UTC,
        /// so "Today" and "This Month" line up with the admin's calendar.
        /// </summary>
        public static ResolvedPeriod Resolve(
            DashboardPeriod period, DateTime utcNow, TimeSpan offset, DateOnly? from, DateOnly? to)
        {
            var local = utcNow + offset;
            DateTime start, end;

            switch (period)
            {
                case DashboardPeriod.Today:
                    start = local.Date;
                    end = start.AddDays(1);
                    break;

                case DashboardPeriod.Week: // Monday start
                    start = local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));
                    end = start.AddDays(7);
                    break;

                case DashboardPeriod.Month:
                    start = new DateTime(local.Year, local.Month, 1);
                    end = start.AddMonths(1);
                    break;

                case DashboardPeriod.Quarter:
                    var firstMonth = ((local.Month - 1) / 3) * 3 + 1;
                    start = new DateTime(local.Year, firstMonth, 1);
                    end = start.AddMonths(3);
                    break;

                case DashboardPeriod.Year:
                    start = new DateTime(local.Year, 1, 1);
                    end = start.AddYears(1);
                    break;

                case DashboardPeriod.Custom:
                    if (from is null || to is null)
                        throw new ArgumentException("Custom period requires both 'from' and 'to'.");
                    start = from.Value.ToDateTime(TimeOnly.MinValue);
                    end = to.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(period));
            }

            var prevStart = period switch
            {
                DashboardPeriod.Today => start.AddDays(-1),
                DashboardPeriod.Week => start.AddDays(-7),
                DashboardPeriod.Month => start.AddMonths(-1),
                DashboardPeriod.Quarter => start.AddMonths(-3),
                DashboardPeriod.Year => start.AddYears(-1),
                _ => start - (end - start) // custom: same length, immediately before
            };

            return new ResolvedPeriod(
                new DateRange(ToUtc(start, offset), ToUtc(end, offset)),
                new DateRange(ToUtc(prevStart, offset), ToUtc(start, offset)),
                offset);
        }

        private static DateTime ToUtc(DateTime local, TimeSpan offset) =>
            DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
    }

    public static class PeriodMath
    {
        /// <summary>((current - previous) / previous) * 100, 1dp. Null when previous is 0.</summary>
        public static decimal? PercentChange(long current, long previous) =>
            previous == 0 ? null : Math.Round((decimal)(current - previous) / previous * 100m, 1);

        public static string Trend(long current, long previous) =>
            current > previous ? "up" : current < previous ? "down" : "flat";
    }

}
