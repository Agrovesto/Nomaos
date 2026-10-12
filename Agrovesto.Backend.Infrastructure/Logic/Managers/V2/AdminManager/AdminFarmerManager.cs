
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FarmerSummaryDto = Agrovesto.Backend.Core.Models.Admin.FarmerSummaryDto;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{

    public interface IAdminFarmerManager
    {
        Task<FarmerSummaryDto> GetSummaryAsync(long businessId);
        Task<PagedResult<FarmerListItemDto>> GetFarmersAsync(FarmerListQuery p);
        Task<List<FarmerListItemDto>> GetAllForExportAsync(FarmerListQuery p);
        Task<FarmerDetailsDto?> GetFarmerDetailsAsync(long businessId, string farmerUserId);
        Task<bool> SuspendFarmerAsync(long businessId, string farmerUserId, string actingUserId, string? reason);
        Task<FarmSummaryDto?> GetFarmSummaryAsync(long businessId, string farmerUserId);
        Task<PagedResult<FarmListItemDto>?> GetFarmsAsync(long businessId, string farmerUserId, int page = 1, int pageSize = 20);
        Task<FarmerOrgMembershipDto?> GetOrganizationMembershipAsync(long businessId, string farmerUserId);
    }
    public class AdminFarmerManager : IAdminFarmerManager
    {
        private readonly DataContext _context;
        private readonly IInputFinanceReader _inputFinance;
        public AdminFarmerManager(DataContext context, IInputFinanceReader inputFinance)
        {
            _context = context;
            _inputFinance = inputFinance;
        }

        private static readonly Expression<Func<Farmer, bool>> IsVerified =
          f => f.User.IsPhoneVerified;

        // ---------- Cards ----------
        public async Task<FarmerSummaryDto> GetSummaryAsync(long businessId)
        {
            var q = FarmersInBusiness(businessId);

            var total = await q.CountAsync();
            var verified = await q.CountAsync(IsVerified);
            var suspended = await q.CountAsync(f => f.FarmerStatus == FarmerStatus.Suspended);
            var pending = total - verified;

            double Pct(int n) => total == 0 ? 0 : Math.Round(n * 100.0 / total, 1);

            return new FarmerSummaryDto
            {
                TotalFarmers = total,
                VerifiedFarmers = verified,
                VerifiedPercent = Pct(verified),
                PendingVerification = pending,
                PendingPercent = Pct(pending),
                SuspendedFarmers = suspended
            };
        }

        // ---------- Filters: shared by table and export ----------
        private IQueryable<Farmer> ApplyFilters(IQueryable<Farmer> q, FarmerListQuery p)
        {
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim();
                var digits = new string(s.Where(char.IsDigit).ToArray());
                long.TryParse(digits, out var idMatch);   // "FMR-1712" -> 1712

                q = q.Where(f =>
                    (f.Name != null && f.Name.Contains(s)) ||
                    (f.PhoneNumber != null && f.PhoneNumber.Contains(s)) ||
                    (idMatch > 0 && f.Id == idMatch));
            }

            if (p.Status.HasValue)
                q = q.Where(f => f.FarmerStatus == p.Status.Value);

            if (!string.IsNullOrWhiteSpace(p.OrganizationCode))
                q = q.Where(f => _context.FarmerOrganizations.Any(fo =>
                    fo.FarmerId == f.Id &&
                    fo.OrganizationCode == p.OrganizationCode &&
                    fo.IsActive));

            if (p.From.HasValue)
                q = q.Where(f => f.OnBoardedOn >= p.From.Value);

            if (p.To.HasValue)
                q = q.Where(f => f.OnBoardedOn < p.To.Value.Date.AddDays(1));

            return q;
        }
        
        public async Task<PagedResult<FarmerListItemDto>> GetFarmersAsync(FarmerListQuery p)
        {
            var page = Math.Max(p.Page, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(FarmersInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(f => f.OnBoardedOn)
                .ThenByDescending(f => f.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.FarmerStatus })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            // Related data for this page only (3 small queries instead of N+1)
            var farmCounts = await _context.Farms
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.FarmerId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            var orgs = (await (from fo in _context.FarmerOrganizations
                               join o in _context.Organizations on fo.OrganizationCode equals o.OrganizationCode
                               where ids.Contains(fo.FarmerId) && fo.IsActive
                               select new { fo.FarmerId, o.CompanyName }).ToListAsync())
                       .ToLookup(x => x.FarmerId, x => x.CompanyName);

            var agents = (await _context.FarmerAgents
                            .Where(x => ids.Contains(x.FarmerId) && x.IsActive)
                            .Select(x => new { x.FarmerId, AgentName = x.Agent.Name })
                            .ToListAsync())
                         .ToLookup(x => x.FarmerId, x => x.AgentName);

            var items = rows.Select((r, i) => new FarmerListItemDto
            {
                SerialNo = (page - 1) * size + i + 1,
                Id = r.Id,
                FarmerId = $"FMR-{r.Id}",
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = r.FarmerStatus.ToString(),
                Farms = farmCounts.GetValueOrDefault(r.Id),
                AssignedAgent = string.Join(", ", agents[r.Id].Distinct()),
                Organization = string.Join(", ", orgs[r.Id].Distinct())
            }).ToList();

            return new PagedResult<FarmerListItemDto>(items, total, page, size);
        }

        // ---------- Export: same filters, no paging ----------
        public async Task<List<FarmerListItemDto>> GetAllForExportAsync(FarmerListQuery p)
        {
            p.Page = 1;
            p.PageSize = int.MaxValue;
            // Reuse the table logic without the 100-row clamp:
            var q = ApplyFilters(FarmersInBusiness(p.BusinessId), p);

            var rows = await q.OrderByDescending(f => f.OnBoardedOn)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.FarmerStatus })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            var farmCounts = await _context.Farms
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.FarmerId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            var orgs = (await (from fo in _context.FarmerOrganizations
                               join o in _context.Organizations on fo.OrganizationCode equals o.OrganizationCode
                               where ids.Contains(fo.FarmerId) && fo.IsActive
                               select new { fo.FarmerId, o.CompanyName }).ToListAsync())
                       .ToLookup(x => x.FarmerId, x => x.CompanyName);

            var agents = (await _context.FarmerAgents
                            .Where(x => ids.Contains(x.FarmerId) && x.IsActive)
                            .Select(x => new { x.FarmerId, AgentName = x.Agent.Name })
                            .ToListAsync())
                         .ToLookup(x => x.FarmerId, x => x.AgentName);

            return rows.Select((r, i) => new FarmerListItemDto
            {
                SerialNo = i + 1,
                Id = r.Id,
                FarmerId = $"FMR-{r.Id}",
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = r.FarmerStatus.ToString(),
                Farms = farmCounts.GetValueOrDefault(r.Id),
                AssignedAgent = string.Join(", ", agents[r.Id].Distinct()),
                Organization = string.Join(", ", orgs[r.Id].Distinct())
            }).ToList();
        }

        private IQueryable<Farmer> FarmersInBusiness(long businessId)
        {
            return _context.Farmers
                .AsNoTracking()
                .Where(f => _context.UserBusinesses.Any(ub =>
                    ub.UserId == f.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive));
        }

        public async Task<FarmerDetailsDto?> GetFarmerDetailsAsync(long businessId, string farmerUserId)
        {
            var d = await (
                from f in FarmersInBusiness(businessId)
                where f.UserId == farmerUserId
                join ob in _context.Users on f.OnboardedByUserId equals ob.Id into obj
                from ob in obj.DefaultIfEmpty()
                select new
                {
                    f.Id,
                    f.Name,
                    f.Email,
                    f.PhoneNumber,
                    f.Gender,
                    f.DateOfBirth,
                    f.Location,
                    f.State,
                    f.Lga,
                    f.Ward,
                    f.UserId,
                    f.OnboardedByUserId,
                    f.OnBoardedOn,
                    f.FarmerStatus,
                    UserEmail = f.User.Email,
                    UserPhone = f.User.PhoneNumber,
                    Nin = f.User.NIN,
                    LastUpdated = f.User.DateModified,
                    OnboarderName = ob.Name
                }).FirstOrDefaultAsync();

            if (d == null) return null;

            var orgNames = await (from fo in _context.FarmerOrganizations
                                  join o in _context.Organizations on fo.OrganizationCode equals o.OrganizationCode
                                  where fo.FarmerId == d.Id && fo.IsActive
                                  select o.CompanyName).Distinct().ToListAsync();

            var status = d.FarmerStatus.ToString();
            var fullName = d.Name;

            // Self-registered if nobody else onboarded them
            var isAgentAssisted = !string.IsNullOrEmpty(d.OnboardedByUserId)
                                  && d.OnboardedByUserId != d.UserId;

            return new FarmerDetailsDto
            {
                Id = d.Id,
                FarmerId = $"FMR-{d.Id}",
                FullName = fullName,
                Status = status,
                Organization = orgNames.Count > 0 ? string.Join(", ", orgNames) : null,
                RegistrationDate = d.OnBoardedOn,
                CanSuspend = d.FarmerStatus != FarmerStatus.Suspended,

                Personal = new FarmerPersonalInfoDto
                {
                    FullName = fullName,
                    Email = d.Email ?? d.UserEmail,
                    PhoneNumber = FormatPhone(d.PhoneNumber ?? d.UserPhone),
                    Gender = d.Gender,
                    DateOfBirth = FormatDob(d.DateOfBirth),
                    NationalId = d.Nin,
                    Address = d.Location,
                    State = d.State,
                    Lga = d.Lga,
                    Ward = d.Ward
                },

                Registration = new FarmerRegistrationInfoDto
                {
                    RegistrationDate = d.OnBoardedOn,
                    RegistrationChannel = isAgentAssisted ? "Agent Assisted" : "Self Registration",
                    RegistrationSource = isAgentAssisted ? d.OnboarderName : fullName,
                    Status = status,
                    LastUpdated = d.LastUpdated
                }
            };
        }

        public async Task<bool> SuspendFarmerAsync(long businessId, string farmerUserId, string actingUserId, string? reason)
        {
            // Needs tracking, so don't use the AsNoTracking base query
            var farmer = await _context.Farmers
                .Include(f => f.User)
                .Where(f => f.UserId == farmerUserId &&
                    _context.UserBusinesses.Any(ub =>
                        ub.UserId == f.UserId && ub.BusinessId == businessId && ub.IsActive))
                .FirstOrDefaultAsync();

            if (farmer == null || farmer.FarmerStatus == FarmerStatus.Suspended)
                return false;

            farmer.FarmerStatus = FarmerStatus.Suspended;
            farmer.User.DateModified = DateTimeOffset.UtcNow;   // drives "Last Updated"
            farmer.User.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();
            return true;
        }


        private Task<bool> FarmerInBusinessAsync(long businessId, string farmerUserId) =>
    FarmersInBusiness(businessId).AnyAsync(f => f.UserId == farmerUserId);

        public async Task<FarmSummaryDto?> GetFarmSummaryAsync(long businessId, string farmerUserId)
        {
            var farmer = await _context.Farmers.AsNoTracking()
                .Where(f => f.UserId == farmerUserId &&
                    _context.UserBusinesses.Any(ub =>
                        ub.UserId == f.UserId && ub.BusinessId == businessId && ub.IsActive))
                .FirstOrDefaultAsync();
            if (farmer == null)
                return null;

            var farms = _context.Farms.AsNoTracking().Where(x => x.FarmerId == farmer.Id);

            var stats = await farms
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Land = g.Sum(x => x.SizeInHectares),
                    Verified = g.Count(x => x.MappingStatus == MappingStatus.Verified)
                })
                .FirstOrDefaultAsync();

            // Crops ranked by land under cultivation, top 3
            var crops = await farms
                .Where(x => x.PrimaryCrop != null && x.PrimaryCrop != "")
                .GroupBy(x => x.PrimaryCrop!)
                .Select(g => new { Crop = g.Key, Land = g.Sum(x => x.SizeInHectares) })
                .OrderByDescending(x => x.Land)
                .Take(3)
                .Select(x => x.Crop)
                .ToListAsync();

            return new FarmSummaryDto
            {
                TotalFarms = stats?.Total ?? 0,
                TotalLandSizeHectares = Math.Round(stats?.Land ?? 0, 1),
                VerifiedFarms = stats?.Verified ?? 0,
                PrimaryCrops = crops
            };
        }

        public async Task<PagedResult<FarmListItemDto>?> GetFarmsAsync(
            long businessId, string farmerUserId, int page = 1, int pageSize = 20)
        {
            var farmer = await _context.Farmers.AsNoTracking()
                  .Where(f => f.UserId == farmerUserId &&
                      _context.UserBusinesses.Any(ub =>
                          ub.UserId == f.UserId && ub.BusinessId == businessId && ub.IsActive))
                  .FirstOrDefaultAsync();
            if (farmer == null)
                return null;

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.Farms.AsNoTracking().Where(x => x.FarmerId == farmer.Id);
            var total = await q.CountAsync();

            var rows = await q
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.FarmState,
                    x.FarmLga,
                    x.SizeInHectares,
                    x.PrimaryCrop,
                    x.MappingStatus
                })
                .ToListAsync();

            var items = rows.Select((r, i) => new FarmListItemDto
            {
                SerialNo = (page - 1) * pageSize + i + 1,
                Id = r.Id,
                FarmId = $"FM{r.Id:D6}",
                FarmName = r.Name,
                State = r.FarmState,
                Lga = r.FarmLga,
                SizeInHectares = r.SizeInHectares,
                PrimaryCrop = r.PrimaryCrop,
                Status = r.MappingStatus.ToString()
            }).ToList();

            return new PagedResult<FarmListItemDto>(items, total, page, pageSize);
        }

        public async Task<FarmerOrgMembershipDto?> GetOrganizationMembershipAsync(long businessId, string farmerUserId)
        {
            var farmer = await _context.Farmers.AsNoTracking()
                  .Where(f => f.UserId == farmerUserId &&
                      _context.UserBusinesses.Any(ub =>
                          ub.UserId == f.UserId && ub.BusinessId == businessId && ub.IsActive))
                  .FirstOrDefaultAsync();
            if (farmer == null)
                return null;

            // ---- Linked organizations (many) ----
            var orgRows = await (from fo in _context.FarmerOrganizations.AsNoTracking()
                                 join o in _context.Organizations on fo.OrganizationCode equals o.OrganizationCode
                                 where fo.FarmerId == farmer.Id
                                 orderby fo.IsActive descending, fo.OnBoardedOn descending
                                 select new { o.CompanyName, o.OrganizationCode, fo.OnBoardedOn, fo.IsActive })
                                .ToListAsync();

            // ---- Assigned agents (many) ----
            var agentRows = await (from fa in _context.FarmerAgents.AsNoTracking()
                                   join o in _context.Organizations on fa.OrganizationCode equals o.OrganizationCode into oj
                                   from o in oj.DefaultIfEmpty()
                                   where fa.FarmerId == farmer.Id
                                   orderby fa.IsActive descending
                                   select new
                                   {
                                       AgentName = fa.Agent.Name,
                                       AgentCode = fa.Agent.UserId,
                                       Phone = fa.Agent.PhoneNumber,
                                       Organization = o != null ? o.CompanyName : null,
                                       fa.IsActive
                                   }).ToListAsync();

            // ---- Membership bodies (cooperatives) ----
            var memberRows = await (from fc in _context.FarmerCooperatives.AsNoTracking()
                                    join m in _context.MemberShips on fc.CooperativeId equals m.Id
                                    where fc.FarmerId == farmer.Id
                                    orderby fc.IsActive descending, fc.JoinedOn descending
                                    select new { m.Name,
                                        //fc.Role, 
                                        fc.JoinedOn, fc.IsActive })
                                   .ToListAsync();

            return new FarmerOrgMembershipDto
            {
                Organizations = orgRows.Select(x => new LinkedOrganizationDto
                {
                    Organization = x.CompanyName,
                    OrganizationId = x.OrganizationCode,
                    JoinedOn = x.OnBoardedOn,
                    Status = x.IsActive ? "Active" : "Inactive"
                }).ToList(),

                Agents = agentRows.Select(x => new AssignedAgentDto
                {
                    AgentName = x.AgentName,
                    AgentId = x.AgentCode,
                    Phone = FormatPhone(x.Phone),
                    Organization = x.Organization,
                    Status = x.IsActive ? "Active" : "Inactive"
                }).ToList(),

                Memberships = memberRows.Select((x, i) => new MembershipBodyDto
                {
                    SerialNo = i + 1,
                    Membership = x.Name,
                   // Role = x.Role,
                    Joined = x.JoinedOn,
                    Status = x.IsActive
                    //? "Inactive" : x.IsVerified ? "Verified" : "Pending"
                }).ToList()
            };
        }
        // ---------- helpers ----------
        private static string? FormatPhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var d = new string(raw.Where(char.IsDigit).ToArray());
            if (d.StartsWith("234") && d.Length == 13) d = "0" + d[3..];
            return d.Length == 11 && d.StartsWith("0")
                ? $"+234 {d.Substring(1, 3)} {d.Substring(4, 3)} {d.Substring(7, 4)}"
                : raw;
        }

        private static string? FormatDob(string? raw)
        {
            if (!DateTime.TryParse(raw, out var dt)) return raw;   // fall back to the stored text
            var day = dt.Day;
            var suffix = (day % 100) is >= 11 and <= 13 ? "th"
                : (day % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
            return $"{day}{suffix} {dt:MMMM, yyyy}";
        }

        //--------------Input finance summary and packages----------------
        public async Task<InputFinanceSummaryDto?> GetInputFinanceSummaryAsync(long businessId, string farmerUserId)
        {
            if (!await FarmerInBusinessAsync(businessId, farmerUserId)) return null;
            return await _inputFinance.GetSummaryAsync(farmerUserId);
        }

        public async Task<PagedResult<InputFinancePackageDto>?> GetInputFinancePackagesAsync(
            long businessId, string farmerUserId, int page = 1, int pageSize = 20)
        {
            if (!await FarmerInBusinessAsync(businessId, farmerUserId)) return null;

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _inputFinance.GetPackagesAsync(farmerUserId, page, pageSize);
            return new PagedResult<InputFinancePackageDto>(items, total, page, pageSize);
        }

        public async Task<bool> ReactivateFarmerAsync(long businessId, string farmerUserId, string actingUserId)
        {
            var farmer = await _context.Farmers
                .Include(f => f.User)
                .Where(f => f.UserId == farmerUserId &&
                    _context.UserBusinesses.Any(ub =>
                        ub.UserId == f.UserId && ub.BusinessId == businessId && ub.IsActive))
                .FirstOrDefaultAsync();

            if (farmer == null || farmer.FarmerStatus != FarmerStatus.Suspended)
                return false;

            farmer.FarmerStatus = FarmerStatus.Active;
            farmer.User.DateModified = DateTimeOffset.UtcNow;
            farmer.User.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();
            return true;
        }

    }
}

