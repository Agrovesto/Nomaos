using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{

    public enum DashboardPeriod { Today, Week, Month, Quarter, Year, Custom }

    /// <summary>How the registration chart is bucketed across the year.</summary>
    public enum ChartGranularity { Day, Week, Month }

    /// <summary>
    /// ?period=month
    /// ?period=custom&amp;from=2026-09-01&amp;to=2026-09-30
    /// ?period=month&amp;chart=week     (chart: day | week | month, default week)
    /// </summary>
    public class DashboardQuery
    {
        public DashboardPeriod Period { get; set; } = DashboardPeriod.Month;
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public ChartGranularity Chart { get; set; } = ChartGranularity.Week;
    }

    /// <param name="Total">Cumulative total as of the end of the selected period.</param>
    /// <param name="NewInPeriod">Records onboarded inside the selected period.</param>
    /// <param name="NewInPreviousPeriod">Records onboarded inside the previous equivalent period.</param>
    /// <param name="ChangePercent">((NewInPeriod - NewInPreviousPeriod) / NewInPreviousPeriod) * 100, 1dp. Null when the previous period is 0.</param>
    /// <param name="Trend">"up" | "down" | "flat"</param>
    public record MetricDto(
        long Total,
        long NewInPeriod,
        long NewInPreviousPeriod,
        decimal? ChangePercent,
        string Trend);

    public record LeadingOrganizationDto(long Id, string Name, int Farmers, int Agents);

    public record TopAgentDto(long Id, string Name, int Farmers, int Organizations
      //  string? OrganizationName
        );

    public record TrendPointDto(
        string Label,              // x-axis label, e.g. "Sep 9"
        string Period,             // tooltip title, e.g. "Week 37 · Sep 9 – Sep 15"
        DateTime StartUtc,
        DateTime EndUtc,
        long Count,
        decimal? ChangeVsPrev,     // vs the previous bucket; null when the previous bucket was 0
        string Trend,              // "up" | "down" | "flat"
        bool InSelectedPeriod,     // overlaps the dashboard filter's period (highlight it in the chart)
        bool IsPartial);           // bucket hasn't finished yet (current day / week / month)

    public record RegistrationDto(
        long Total,                // users registered in the SELECTED period
        long PreviousTotal,        // users registered in the previous equivalent period
        decimal? ChangePercent,
        string Trend,
        string Granularity,        // "day" | "week" | "month"
        DateTime SeriesFromUtc,    // start of the year being charted
        DateTime SeriesToUtc,      // end of the year being charted
        List<TrendPointDto> Series);

    public record DashboardDto(
        DashboardPeriod Period,
        DateTime CurrentFromUtc,
        DateTime CurrentToUtc,
        DateTime PreviousFromUtc,
        DateTime PreviousToUtc,
        bool IsEmpty,
        MetricDto Farmers,
        MetricDto Agents,
        MetricDto Organizations,
        MetricDto MembershipBodies,
        MetricDto? ServiceProviders,   // null until service providers exist
        List<LeadingOrganizationDto> LeadingOrganizations,
        List<TopAgentDto> TopAgents,
        RegistrationDto Registrations);
}


