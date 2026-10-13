using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IAdminDashboardManager
    {
        Task<DashboardDto> GetAsync(DashboardPeriod period, DateOnly? from, DateOnly? to, ChartGranularity chartGranularity , CancellationToken ct);
    }


    public class AdminDashboardManager : IAdminDashboardManager
    {
        private readonly DataContext _db;
        private readonly TimeProvider _time;
        private readonly IConfiguration _config;

        public AdminDashboardManager(DataContext db, TimeProvider time, IConfiguration config)
        {
            _db = db;
            _time = time;
            _config = config;
        }

        public async Task<DashboardDto> GetAsync(
            DashboardPeriod periodType, DateOnly? from, DateOnly? to, ChartGranularity chart, CancellationToken ct)
        {
            var utcNow = _time.GetUtcNow().UtcDateTime;
            var offset = TimeSpan.FromHours(_config.GetValue("Dashboard:UtcOffsetHours", 1.0)); // Lagos = UTC+1
            var period = PeriodResolver.Resolve(periodType, utcNow, offset, from, to);

            // A DbContext is not thread-safe, so these run one after another.
            var farmers = await Metric(_db.Farmers.Select(x => x.OnBoardedOn), period, ct);
            var agents = await Metric(_db.Agents.Select(x => x.OnBoardedOn), period, ct);
            var organizations = await Metric(_db.Organizations.Select(x => x.OnboardedOn), period, ct);
            var memberships = await Metric(_db.MemberShips.Select(x => x.OnBoardedOn), period, ct);

            // Enable when service providers exist:
            // var providers = await Metric(_db.ServiceProviders.Select(x => x.OnBoardedOn), period, ct);
            MetricDto? providers = null;

            var leadingOrgs = await LeadingOrganizations(period.Current, ct);
            var topAgents = await TopAgents(period.Current, ct);
            var registrations = await Registrations(period, utcNow, chart, ct);

            var isEmpty =
                farmers.NewInPeriod == 0 && agents.NewInPeriod == 0 && organizations.NewInPeriod == 0 &&
                memberships.NewInPeriod == 0 && (providers?.NewInPeriod ?? 0) == 0 &&
                registrations.Total == 0;

            return new DashboardDto(
                periodType,
                period.Current.From, period.Current.To,
                period.Previous.From, period.Previous.To,
                isEmpty,
                farmers, agents, organizations, memberships, providers,
                leadingOrgs, topAgents, registrations);
        }

        // ================================================================ summary cards

        /// <summary>
        /// One round trip per card:
        ///   Total   = everything onboarded up to the end of the selected period (cumulative)
        ///   NewCur  = onboarded inside the selected period
        ///   NewPrev = onboarded inside the previous equivalent period
        /// The % change compares NewCur with NewPrev (not the cumulative totals).
        /// </summary>
        private static async Task<MetricDto> Metric(
            IQueryable<DateTimeOffset> onboardedOn, ResolvedPeriod period, CancellationToken ct)
        {
            // The boundaries are UTC, so these conversions give offset +00:00.
            DateTimeOffset curFrom = period.Current.From;
            DateTimeOffset curTo = period.Current.To;
            DateTimeOffset prevFrom = period.Previous.From;
            DateTimeOffset prevTo = period.Previous.To;

            var r = await onboardedOn
                .Where(d => d < curTo)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    NewCur = g.Count(d => d >= curFrom),
                    NewPrev = g.Count(d => d >= prevFrom && d < prevTo)
                })
                .FirstOrDefaultAsync(ct);

            long total = r?.Total ?? 0, cur = r?.NewCur ?? 0, prev = r?.NewPrev ?? 0;

            return new MetricDto(total, cur, prev,
                PeriodMath.PercentChange(cur, prev),
                PeriodMath.Trend(cur, prev));
        }

        // ================================================================ leading organizations

        /// <summary>
        /// Ranking: farmers onboarded in the selected period (desc), then agents onboarded in the
        /// period (desc), then company name. Remove the date conditions to rank on all-time numbers.
        /// </summary>
        private async Task<List<LeadingOrganizationDto>> LeadingOrganizations(DateRange range, CancellationToken ct)
        {
            DateTimeOffset fromOffset = range.From;
            DateTimeOffset toOffset = range.To;

            var rows = await _db.Organizations
                .Select(o => new
                {
                    o.OrganizationId,
                    o.CompanyName,
                    o.OrganizationCode,
                    // farmers onboarded in the period who are linked to this organization
                    Farmers = _db.Farmers.Count(
                        f =>
                        //f.OnBoardedOn >= fromOffset && f.OnBoardedOn < toOffset &&
                        _db.FarmerOrganizations.Any(fo =>
                            fo.FarmerId == f.Id && fo.OrganizationCode == o.OrganizationCode)),

                    // agents onboarded in the period who are linked to this organization
                    Agents = _db.Agents.Count(a =>
                        //a.OnBoardedOn >= fromOffset && a.OnBoardedOn < toOffset &&
                        _db.OrganizationAgents.Any(oa =>
                            oa.AgentId == a.Id && oa.OrganizationId == o.OrganizationId))
                })
                .Where(x => x.Farmers > 0 || x.Agents > 0)
                .OrderByDescending(x => x.Farmers)
                .ThenByDescending(x => x.Agents)
                .ThenBy(x => x.CompanyName)
                .Take(3)
                .ToListAsync(ct);

            return rows
                .Select(x => new LeadingOrganizationDto(x.OrganizationCode, x.CompanyName, x.Farmers, x.Agents))
                .ToList();
        }

        // ================================================================ top agents

        /// <summary>Ranking: farmers onboarded by the agent in the selected period.</summary>
        private async Task<List<TopAgentDto>> TopAgents(DateRange range, CancellationToken ct)
        {
            DateTimeOffset fromOffset = range.From;
            DateTimeOffset toOffset = range.To;

            var rows = await _db.Agents
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.UserId,
                    // an agent can belong to several organizations; show one deterministically
                    //Organization = _db.OrganizationAgents
                    //    .Where(oa => oa.AgentId == a.Id)
                    //    .Join(_db.Organizations, oa => oa.OrganizationId, o => o.OrganizationId,
                    //          (oa, o) => o.CompanyName)
                    //    .OrderBy(name => name)
                    //    .FirstOrDefault(),
                    Organization = _db.OrganizationAgents.Count(oa => oa.AgentId == a.Id),


                    Farmers = _db.Farmers.Count(f =>
                       // f.OnBoardedOn >= fromOffset && f.OnBoardedOn < toOffset &&
                        _db.FarmerAgents.Any(fa => fa.FarmerId == f.Id && fa.AgentId == a.Id))
                })
                .Where(x => x.Farmers > 0)
                .OrderByDescending(x => x.Farmers)
                .ThenBy(x => x.Name)
                .Take(3)
                .ToListAsync(ct);

            return rows.Select(x => new TopAgentDto(x.UserId, x.Name, x.Farmers, x.Organization)).ToList();
        }

        // ================================================================ registrations + yearly chart

        /// <summary>
        /// Headline numbers (Total / PreviousTotal / ChangePercent) describe the SELECTED period.
        /// The series always covers the whole calendar year that contains the selected period, up to "now".
        /// </summary>
        private async Task<RegistrationDto> Registrations(
            ResolvedPeriod period, DateTime utcNow, ChartGranularity granularity, CancellationToken ct)
        {
            var offset = period.Offset;
            var offsetHours = offset.TotalHours;
            var cur = period.Current;
            var prev = period.Previous;
            var localNow = utcNow + offset;

            // ---- headline numbers: selected period vs previous equivalent period
            // (Users.CreatedOn is a DateTime here. If it is a DateTimeOffset, declare these four as DateTimeOffset.)
            var curFrom = cur.From;
            var curTo = cur.To;
            var prevFrom = prev.From;
            var prevTo = prev.To;

            var total = await _db.Users.LongCountAsync(
            //    u => 
            //u.CreatedOn >= curFrom && u.CreatedOn < curTo,  //uncomment if it is to get only within the period
                ct);
            var previousTotal = await _db.Users.LongCountAsync(u => u.CreatedOn >= prevFrom && u.CreatedOn < prevTo, ct);

            // ---- chart: the whole calendar year (local time) that contains the end of the selected period
            var anchor = (cur.To + offset).AddTicks(-1);
            if (anchor > localNow) anchor = localNow;

            var yearFromLocal = new DateTime(anchor.Year, 1, 1);
            var yearToLocal = yearFromLocal.AddYears(1);

            var seriesFromUtc = DateTime.SpecifyKind(yearFromLocal - offset, DateTimeKind.Utc);
            var seriesToUtc = DateTime.SpecifyKind(yearToLocal - offset, DateTimeKind.Utc);

            // Buckets stop at "now" so the line doesn't fall to zero for days that haven't happened yet.
            var buckets = BuildBuckets(yearFromLocal, yearToLocal, localNow, granularity);

            if (buckets.Count == 0)
            {
                return new RegistrationDto(total, previousTotal,
                    PeriodMath.PercentChange(total, previousTotal), PeriodMath.Trend(total, previousTotal),
                    granularity.ToString().ToLowerInvariant(), seriesFromUtc, seriesToUtc, []);
            }

            // One extra bucket before January so the first point can show "vs prev".
            var firstPrevStartLocal = Step(buckets[0].Start, granularity, -1);
            var fetchFromUtc = firstPrevStartLocal - offset;
            var fetchToUtc = seriesToUtc;

            // Group by local DAY in the database (at most ~366 rows), then roll up to weeks/months in memory.
            var rows = await _db.Users
                .Where(u => u.CreatedOn >= fetchFromUtc && u.CreatedOn < fetchToUtc)
                .Select(u => u.CreatedOn.AddHours(offsetHours))
                .GroupBy(d => new { d.Year, d.Month, d.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() })
                .ToListAsync(ct);

            var perDay = rows.ToDictionary(r => new DateTime(r.Year, r.Month, r.Day), r => (long)r.Count);

            long CountIn(DateTime start, DateTime end)
            {
                long sum = 0;
                for (var d = start.Date; d < end; d = d.AddDays(1))
                    if (perDay.TryGetValue(d, out var c)) sum += c;
                return sum;
            }

            var selectedFromLocal = cur.From + offset;
            var selectedToLocal = cur.To + offset;

            var series = new List<TrendPointDto>(buckets.Count);
            var previousBucketCount = CountIn(firstPrevStartLocal, buckets[0].Start);

            for (var i = 0; i < buckets.Count; i++)
            {
                var (s, e) = buckets[i];
                var count = CountIn(s, e);

                series.Add(new TrendPointDto(
                    Label: AxisLabel(s, granularity),
                    Period: TooltipPeriod(s, e, i, granularity),
                    StartUtc: DateTime.SpecifyKind(s - offset, DateTimeKind.Utc),
                    EndUtc: DateTime.SpecifyKind(e - offset, DateTimeKind.Utc),
                    Count: count,
                    ChangeVsPrev: PeriodMath.PercentChange(count, previousBucketCount),
                    Trend: PeriodMath.Trend(count, previousBucketCount),
                    InSelectedPeriod: s < selectedToLocal && e > selectedFromLocal,
                    IsPartial: e > localNow));

                previousBucketCount = count;
            }

            return new RegistrationDto(
                total, previousTotal,
                PeriodMath.PercentChange(total, previousTotal),
                PeriodMath.Trend(total, previousTotal),
                granularity.ToString().ToLowerInvariant(),
                seriesFromUtc, seriesToUtc,
                series);
        }

        // ================================================================ bucket helpers (local time)

        private static DateTime Step(DateTime anchor, ChartGranularity g, int n) => g switch
        {
            ChartGranularity.Day => anchor.AddDays(n),
            ChartGranularity.Week => anchor.AddDays(7 * n),   // rolling 7-day buckets starting Jan 1
            _ => anchor.AddMonths(n)
        };

        /// <summary>Buckets from <paramref name="from"/> up to now (never into the future).</summary>
        private static List<(DateTime Start, DateTime End)> BuildBuckets(
            DateTime from, DateTime to, DateTime now, ChartGranularity g)
        {
            var list = new List<(DateTime, DateTime)>();
            for (var i = 0; ; i++)
            {
                var s = Step(from, g, i);
                if (s >= to || s > now) break;
                var e = Step(from, g, i + 1);
                if (e > to) e = to;
                list.Add((s, e));
            }
            return list;
        }

        private static string F(DateTime d, string fmt) => d.ToString(fmt, CultureInfo.InvariantCulture);

        private static string AxisLabel(DateTime start, ChartGranularity g) => g switch
        {
            ChartGranularity.Month => F(start, "MMM"),
            _ => F(start, "MMM d")
        };

        private static string TooltipPeriod(DateTime start, DateTime end, int index, ChartGranularity g) => g switch
        {
            ChartGranularity.Day => F(start, "ddd, MMM d"),
            ChartGranularity.Week => $"Week {index + 1} · {F(start, "MMM d")} – {F(end.AddDays(-1), "MMM d")}",
            _ => F(start, "MMMM yyyy")
        };
    }
}
