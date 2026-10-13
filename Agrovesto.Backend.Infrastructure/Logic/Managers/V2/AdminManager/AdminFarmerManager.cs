
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime.Internal.Util;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FarmerPersonalInfoDto = Agrovesto.Backend.Core.Models.Admin.FarmerPersonalInfoDto;
using FarmerSummaryDto = Agrovesto.Backend.Core.Models.Admin.FarmerSummaryDto;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;

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
        Task<FarmerModel> UpdateFarmerProfileAsync(string userId, FarmerUpdateModel update);
        Task<(bool status, string msg, string?[] errors)> CreateFarmerAsync(AdminCreateFarmerRequest model, AdminOnBoardingArgs args, CancellationToken ct);
    }
    public class AdminFarmerManager : IAdminFarmerManager
    {
        private readonly IAWSService _email;
        private readonly IConfiguration _configuration;
        private readonly IAdminAuditLogger _audit;
        private readonly DataContext _context;
        private readonly ILogger<AdminFarmerManager> _log;
        private readonly IInputFinanceReader _inputFinance;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly INotificationService _smsService;
        public AdminFarmerManager(DataContext context, IInputFinanceReader inputFinance, ILogger<AdminFarmerManager> log,
            IAdminAuditLogger audit,
            IConfiguration configuration,
            IAWSService aWSService,
            INotificationService smsService,
            IBusinessRepository businessRepository, IOnboardingRepository onboardingRepository)
        {
            _context = context;
            _inputFinance = inputFinance;
            _log = log;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _audit = audit;
            _configuration = configuration;
            _email = aWSService;
        }

        private static readonly Expression<Func<Farmer, bool>> IsVerified =
          f => (f.User.IsPhoneVerified && f.User.IsNinVerified);

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
                    (f.UserId != null && f.UserId.Contains(s)) ||
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
            var page = Math.Max(p.PageNumber, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(FarmersInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(f => f.OnBoardedOn)
                .ThenByDescending(f => f.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.FarmerStatus, f.UserId })
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
                               select new { fo.FarmerId, o.CompanyName, o.OrganizationCode }).ToListAsync())
             .ToLookup(x => x.FarmerId);

            var agents = (await _context.FarmerAgents
                            .Where(x => ids.Contains(x.FarmerId) && x.IsActive)
                            .Select(x => new { x.FarmerId, AgentName = x.Agent.Name, AgentUserId = x.Agent.UserId })
                            .ToListAsync())
                         .ToLookup(x => x.FarmerId);

            var items = rows.Select(r => new FarmerListItemDto
            {
                Id = r.Id,
                FarmerId = r.UserId,
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = EnumExtensions.GetDisplayName(r.FarmerStatus),
                Farms = farmCounts.GetValueOrDefault(r.Id),

                AssignedAgent = string.Join(", ", agents[r.Id].Select(a => a.AgentName).Distinct()),
                AgentUserId = string.Join(", ", agents[r.Id].Select(a => a.AgentUserId).Distinct()),

                Organization = string.Join(", ", orgs[r.Id].Select(o => o.CompanyName).Distinct()),
                OrganizationCode = string.Join(", ", orgs[r.Id].Select(o => o.OrganizationCode).Distinct()),
            }).ToList();

            return new PagedResult<FarmerListItemDto>(items, total, page, size);
        }

        // ---------- Export: same filters, no paging ----------
        public async Task<List<FarmerListItemDto>> GetAllForExportAsync(FarmerListQuery p)
        {
            p.PageNumber = 1;
            p.PageSize = int.MaxValue;
            // Reuse the table logic without the 100-row clamp:
            var q = ApplyFilters(FarmersInBusiness(p.BusinessId), p);

            var rows = await q.OrderByDescending(f => f.OnBoardedOn)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.FarmerStatus, f.UserId })
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
                //SerialNo = i + 1,
                Id = r.Id,
                FarmerId = r.UserId,
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
                    f.UserId,
                    f.Name,
                    f.Email,
                    f.PhoneNumber,
                    f.Gender,
                    f.DateOfBirth,
                    f.Location,
                    f.State,
                    f.Lga,
                    f.Ward,
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
                                    select new
                                    {
                                        m.Name,
                                        //fc.Role,
                                        fc.JoinedOn,
                                        fc.IsActive
                                    })
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

        #region createfarmer
        public async Task<(bool status, string msg, string?[] errors)> CreateFarmerAsync(
      AdminCreateFarmerRequest model, AdminOnBoardingArgs args, CancellationToken ct)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                _log.LogInformation("Starting farmer onboarding {CorrelationId}", correlationId);

                // 1. Validate everything up front (throws once with all errors)
                var business = await ValidateCriticalInputs(model, args);

                // 2. Prepare
                var dateTime = DateTimeOffset.UtcNow;
                model.SignupMethod = !string.IsNullOrWhiteSpace(model.Email)
                    ? SignupMethod.Email : SignupMethod.PhoneNumber;
                ApplyDefaultCredentials(model);

                // 3. Register user
                var createUser = await _onboardingRepository.RegisterUser(
                    BuildUserCreationModel(model),
                    new OnBoardingArgs { businessCode = args.businessCode, Env = args.Env, userType = args.userType });

                // 4. Create farmer + map cooperatives/agents/organizations/value chain
                var farmerResult = await CreateFarmerOrFails(
                    model, args, business, createUser.model.Id, dateTime);

                createUser.model.LogDescription = $"Added Farmer {model.FirstName} {model.LastName}";
                createUser.model.ActivityName = "New Farmer Added";

                // 5. Audit + credentials notification (non-critical)
                await RunNonCriticalSteps(model, args, business, createUser.model, dateTime, ct);

                return (farmerResult.status, "Farmer created successfully", Array.Empty<string?>());
            }
            catch (BaseException)
            {
                throw;   // let it bubble up to whatever turns exceptions into HTTP responses
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateFarmerAsync failed unexpectedly");
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete onboarding at the moment.");
            }
        }
        private static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0) return;

            var message = errors.Count == 1
                ? errors[0]
                : $"{errors.Count} validation errors: {string.Join("; ", errors)}";

            throw new BaseException(ExceptionType.BAD_REQUEST, message, errors);
        }

        private async Task<BusinessModel> ValidateCriticalInputs(
        AdminCreateFarmerRequest model, AdminOnBoardingArgs args)
        {
            var errors = new List<string>();
            BusinessModel? business = null;

            // Business
            if (string.IsNullOrWhiteSpace(args.businessCode))
                errors.Add("Business code is required.");
            else
            {
                business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    errors.Add($"Business '{args.businessCode}' does not exist.");
            }

            // Basic profile
            if (string.IsNullOrWhiteSpace(model.FirstName))
                errors.Add("First name is required.");

            // Contact
            errors.AddRange(ValidateContact(model));

            // Identity
            AddIfNotNull(errors, ValidateIdentifier(model.NIN, "NIN"));
            AddIfNotNull(errors, ValidateIdentifier(model.Bvn, "BVN"));

            // Cooperatives / agents / organizations
            errors.AddRange(await ValidateAssignmentIdentifiers(model));

            ThrowIfAny(errors);
            return business!;
        }

        private static void AddIfNotNull(List<string> errors, string? error)
        {
            if (error != null) errors.Add(error);
        }

        private static IEnumerable<string> ValidateContact(AdminCreateFarmerRequest model)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(model.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(model.PhoneNumber);

            if (!hasEmail && !hasPhone)
            {
                yield return "Either email or phone number must be provided.";
                yield break;
            }

            if (hasEmail && !new EmailAddressAttribute().IsValid(model.Email))
                yield return "Invalid email address format.";

            if (hasPhone && !Regex.IsMatch(model.PhoneNumber!, @"^\d{11}$"))
                yield return "Phone number must be exactly 11 digits and numeric only.";
        }

        private static string? ValidateIdentifier(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (!value.All(char.IsDigit))
                return $"{fieldName} must contain only numbers.";

            if (value.Length != 11)
                return $"{fieldName} must be exactly 11 digits.";

            return null;
        }

        private static void ApplyDefaultCredentials(AdminCreateFarmerRequest model)
        {
            if (!string.IsNullOrWhiteSpace(model.Email))
                model.Password = GenerateRandomPassword();

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                model.Pin = GenerateRandomFourDigitsPin();
        }
        private static string GenerateRandomPassword()
        {
            const int length = 12;
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=[]{}|;:,.<>?";
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return new string(randomBytes.Select(b => validChars[b % validChars.Length]).ToArray());
        }
        private static string GenerateRandomFourDigitsPin() =>
            RandomNumberGenerator.GetInt32(1000, 10000).ToString();

        private async Task<List<string>> ValidateAssignmentIdentifiers(AdminCreateFarmerRequest model)
        {
            var errors = new List<string>();

            if (model.OrganizationCode?.Any() == true)
            {
                var requested = model.OrganizationCode.Distinct().ToList();
                var found = await _context.Organizations
                    .Where(o => requested.Contains(o.OrganizationCode))
                    .Select(o => o.OrganizationCode)
                    .ToListAsync();

                var missing = requested.Except(found).ToList();
                if (missing.Count > 0)
                    errors.Add($"Organization(s) not found: {string.Join(", ", missing)}.");
            }

            if (model.CooperativeIds?.Any() == true)
            {
                var requested = model.CooperativeIds.Distinct().ToList();
                var found = await _context.MemberShips
                    .Where(c => requested.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                var missing = requested.Except(found).ToList();
                if (missing.Count > 0)
                    errors.Add($"Cooperative(s) not found: {string.Join(", ", missing)}.");
            }

            if (model.AgentIds?.Any() == true)
            {
                var requested = model.AgentIds.Distinct().ToList();
                var found = await _context.Agents
                    .Where(a => requested.Contains(a.Id))
                    .Select(a => a.Id)
                    .ToListAsync();

                var missing = requested.Except(found).ToList();
                if (missing.Count > 0)
                    errors.Add($"Agent(s) not found: {string.Join(", ", missing)}.");
            }

            return errors;
        }


        private async Task<(bool status, FarmerModel data)> CreateFarmerOrFails(
       AdminCreateFarmerRequest model,
       AdminOnBoardingArgs args,
       BusinessModel business,
       string userId,
        DateTimeOffset dateTime)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ValidationException("Could not find target user");

            if (string.IsNullOrWhiteSpace(model.FirstName))
                throw new ValidationException("First name is required");

            var fullName = string.Join(" ",
                new[] { model.FirstName, model.MiddleName, model.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            FarmerModel farmer = BuildFarmerModel(model, args, userId, dateTime);

            if (!string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
            {


                await _onboardingRepository.OnNINBVNUpdate(new Core.Models.Common.UserModel
                {
                    Id = userId,
                    Bvn = model.Bvn,
                    NIN = model.NIN,
                }, false);

                farmer.Level = FarmerLevel.Level3.ToString();
                farmer.BVN = model.Bvn;
                farmer.NIN = model.NIN;
            }

            var result = await CreateOrUpdateFarmer(farmer, model, args);

            return result;
        }

        private static FarmerModel BuildFarmerModel(AdminCreateFarmerRequest model, AdminOnBoardingArgs args, string userId, DateTimeOffset datetime)
        {
            return new FarmerModel
            {
                OnboardedByUserId = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Email = model.Email,
                Gender = model.Gender,
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name = $"{model.FirstName} {model.MiddleName} {model.LastName}".Trim(),
                OnBoardedOn = datetime,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                DateOfBirth = model.DateOfBirth,
                Ward = model.Ward,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChain,
                YearsInAgric = model.YearsInAgric,
                Level = FarmerLevel.Level1.ToString(),
                HighestLevelOfEducation = model.HighestLevelOfEducation,
            };
        }




        private static OnboardingRequestModel BuildUserCreationModel(AdminCreateFarmerRequest model)
        {
            return new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                Pin = model.Pin,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                FullName = $"{model.FirstName} {model.MiddleName} {model.LastName}",
                DateOfBirth = model.DateOfBirth,
                SignupMethod = model.SignupMethod
            };
        }

       private async Task RunNonCriticalSteps(
        AdminCreateFarmerRequest model,
        AdminOnBoardingArgs args,
        BusinessModel business,
        OnboardingRequestModel user,
        DateTimeOffset dateTime, CancellationToken ct)
        {
            try
            {
                await _audit.LogAsync("CreateFarmer", args.OnboardedByUserId, user.Id, JsonConvert.SerializeObject(model),
                    AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);
                await SendDefaultCredentialChange(user, args.businessCode);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex,
                    $"Non-critical onboarding step failed for user {user.Id}. " +
                    "User can continue and complete setup later.");
            }
        }
        private async Task SendDefaultCredentialChange(OnboardingRequestModel model, string businessCode)
        {
            var senderEmail = _configuration["Official:Email"];

            var loginUrl = _configuration["Official:LoginUrl"];

            var otpTarget = !string.IsNullOrWhiteSpace(model.Email) ? OtpTarget.Email : OtpTarget.Phone;
            var destination = otpTarget == OtpTarget.Email ? model.Email : model.PhoneNumber;

            if (otpTarget == OtpTarget.Email)
            {
                var frontendUrl = $"{loginUrl}/login";
                var year = DateTime.UtcNow.Year;

                var email = EmailTemplateBuilder.BuildAccountCreatedEmail(
                    model.Password,
                    frontendUrl,
                    year
                );

                await _email.SendEmailAsync(
        senderEmail,
        destination,
        email.Subject,
        email.BodyHtml,
        email.BodyText
    );
            }
            else
            {
                // SMS fallback
                await _smsService.SendSmsAsync(
                    destination,
                    $"Your default pin is {model.Pin}. Kindly login and change this.",
                    businessCode
                );
            }
        }



        private async Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel farmerModel, AdminCreateFarmerRequest data, AdminOnBoardingArgs args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var farmer = await _context.Farmers
                        .FirstOrDefaultAsync(f => f.UserId == farmerModel.UserId);

                    if (farmer == null)
                    {
                        farmer = farmerModel.Map();
                        await _context.Farmers.AddAsync(farmer);
                    }
                    else
                    {
                        await UpdateFarmerProfileAsync(farmerModel.UserId, new FarmerUpdateModel
                        {
                            Name = farmerModel.Name,
                            PhoneNumber = farmerModel.PhoneNumber,
                            Email = farmerModel.Email,
                            Gender = farmerModel.Gender,
                            State = farmerModel.State,
                            Lga = farmerModel.Lga,
                            Location = farmerModel.Location,
                            Ward = farmerModel.Ward,
                            BVN = farmerModel.BVN,
                            NIN = farmerModel.NIN,
                            FarmerCategory = farmerModel.FarmerCategory,
                            HighestLevelOfEducation = farmerModel.HighestLevelOfEducation,
                            YearsInAgric = farmerModel.YearsInAgric,
                            DateOfBirth = farmerModel.DateOfBirth
                        });
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        farmer = await _context.Farmers
                            .FirstOrDefaultAsync(f => f.UserId == farmerModel.UserId);
                    }

                    // ===========================
                    // ORGANIZATIONs
                    // ===========================
                    if (data.OrganizationCode != null)
                    {
                        var exists = await _context.FarmerOrganizations
                        .Where(x => x.FarmerId == farmer.Id)
                        .ToListAsync();


                        var existingIds = exists.Select(x => x.OrganizationCode).ToHashSet();
                        var incoming = data.OrganizationCode.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = exists.Where(e => !incoming.Contains(e.OrganizationCode));

                        await _context.FarmerOrganizations.AddRangeAsync(
                            toAdd.Select(id => new FarmerOrganization
                            {
                                FarmerId = farmer.Id,
                                OrganizationCode = id,
                                IsActive = true,
                                OnboardedByUserId = args.OnboardedByUserId,
                                OnBoardedOn = DateTimeOffset.UtcNow,

                            }));

                        _context.FarmerOrganizations.RemoveRange(toRemove);

                    }

                    // ===========================
                    // COOPERATIVES (BULK SAFE)
                    // ===========================
                    if (data.CooperativeIds != null)
                    {
                        var existing = await _context.FarmerCooperatives
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.CooperativeId).ToHashSet();
                        var incoming = data.CooperativeIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.CooperativeId));

                        await _context.FarmerCooperatives.AddRangeAsync(
                            toAdd.Select(id => new FarmerCooperative
                            {
                                FarmerId = farmer.Id,
                                CooperativeId = id,
                                IsActive = true,
                                OnboardedByUserId = args.OnboardedByUserId,
                                JoinedOn = DateTimeOffset.UtcNow
                            }));

                        _context.FarmerCooperatives.RemoveRange(toRemove);
                    }


                    // ===========================
                    // AGENTS (BULK SAFE)
                    // ===========================
                    if (data.AgentIds != null)
                    {
                        var existing = await _context.FarmerAgents
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.AgentId).ToHashSet();
                        var incoming = data.AgentIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.AgentId));

                        await _context.FarmerAgents.AddRangeAsync(
                            toAdd.Select(id => new FarmerAgent
                            {
                                FarmerId = farmer.Id,
                                AgentId = id,
                                AssignedByUserId = args.OnboardedByUserId,
                                AssignedOn = DateTimeOffset.UtcNow,
                                IsActive = true,
                            }));

                        _context.FarmerAgents.RemoveRange(toRemove);
                    }

                    // ===========================
                    // VALUE CHAIN
                    // ===========================
                    if (data.ValueChain != null)
                    {
                        await UpdateFarmerValueChainItems(farmer.Id, data.ValueChain);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, farmer.Map());
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_Farmers_UserId") == true;
        }



        private async Task UpdateFarmerValueChainItems(long farmerId, List<ValueChainSelectionDto> valueChainSelections)
        {
            var existingItems = await _context.FarmerValueChainItems
                .Where(x => x.FarmerId == farmerId)
                .ToListAsync();

            var incomingItems = valueChainSelections
                .SelectMany(v => v.Items.Select(item => new { v.ValueChainType, ItemName = item }))
                .ToList();

            foreach (var incoming in incomingItems)
            {
                bool exists = existingItems.Any(e =>
                    e.ValueChainType == incoming.ValueChainType &&
                    e.ItemName == incoming.ItemName);

                if (!exists)
                {
                    _context.FarmerValueChainItems.Add(new FarmerValueChainItem
                    {
                        FarmerId = farmerId,
                        ValueChainType = incoming.ValueChainType,
                        ItemName = incoming.ItemName
                    });
                }
            }

            var toRemove = existingItems
                .Where(e => !incomingItems.Any(i =>
                    i.ValueChainType == e.ValueChainType &&
                    i.ItemName == e.ItemName))
                .ToList();

            _context.FarmerValueChainItems.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
        }
        public async Task<FarmerModel> UpdateFarmerProfileAsync(string userId, FarmerUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                return null;

            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farmer == null) return null;

            var isModified = false;

            if (update.Name != null && update.Name != farmer.Name) { farmer.Name = update.Name; isModified = true; }
            if (update.Location != null && update.Location != farmer.Location) { farmer.Location = update.Location; isModified = true; }
            if (update.State != null && update.State != farmer.State) { farmer.State = update.State; isModified = true; }
            if (update.Lga != null && update.Lga != farmer.Lga) { farmer.Lga = update.Lga; isModified = true; }
            if (update.Ward != null && update.Ward != farmer.Ward) { farmer.Ward = update.Ward; isModified = true; }
            if (update.PhoneNumber != null && update.PhoneNumber != farmer.PhoneNumber) { farmer.PhoneNumber = update.PhoneNumber; isModified = true; }
            if (update.Email != null && update.Email != farmer.Email) { farmer.Email = update.Email; isModified = true; }
            if (update.Gender != null && update.Gender != farmer.Gender) { farmer.Gender = update.Gender; isModified = true; }
            if (update.DateOfBirth != null && update.DateOfBirth != farmer.DateOfBirth) { farmer.DateOfBirth = update.DateOfBirth; isModified = true; }
            if (update.FarmerCategory != null && update.FarmerCategory != farmer.FarmerCategory) { farmer.FarmerCategory = update.FarmerCategory; isModified = true; }
            if (update.YearsInAgric.HasValue && update.YearsInAgric.Value != farmer.YearsInAgric) { farmer.YearsInAgric = update.YearsInAgric.Value; isModified = true; }
            if (update.HighestLevelOfEducation != null && update.HighestLevelOfEducation != farmer.HighestLevelOfEducation) { farmer.HighestLevelOfEducation = update.HighestLevelOfEducation; isModified = true; }
            if (update.SelectedItems != null)
            {
                await UpdateFarmerValueChainItems(farmer.Id, update.SelectedItems);
                isModified = true;
            }
            if (!string.IsNullOrEmpty(update.OrganizationCode))
            {
                bool exists = await _context.FarmerOrganizations.AnyAsync(x =>
                    x.FarmerId == farmer.Id &&
                    x.OrganizationCode == update.OrganizationCode);

                if (!exists)
                {
                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                    {
                        FarmerId = farmer.Id,
                        OrganizationCode = update.OrganizationCode,
                        OnboardedByUserId = farmer.OnboardedByUserId
                    });
                }
            }
            if (update.CooperativeIds != null)
            {
                var existing = _context.FarmerCooperatives.Where(fc => fc.FarmerId == farmer.Id);
                _context.FarmerCooperatives.RemoveRange(existing);
                foreach (var coopId in update.CooperativeIds.Distinct())
                {
                    await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                    {
                        FarmerId = farmer.Id,
                        CooperativeId = coopId,
                        OnboardedByUserId = farmer.OnboardedByUserId,
                        JoinedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (update.AgentIds != null)
            {
                var existingAgents = _context.FarmerAgents.Where(fa => fa.FarmerId == farmer.Id);
                _context.FarmerAgents.RemoveRange(existingAgents);
                foreach (var agentId in update.AgentIds.Distinct())
                {
                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agentId,
                        AssignedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(update.NIN) || !string.IsNullOrWhiteSpace(update.BVN))
            {
                var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == farmer.UserId);
                if (subAccount != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != subAccount.NIN)
                    {
                        subAccount.NIN = update.NIN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != subAccount.Bvn)
                    {
                        subAccount.Bvn = update.BVN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }
                }
                else
                {
                    var newSub = new SubAccount
                    {
                        UserId = farmer.UserId,
                        NIN = update.NIN,
                        Bvn = update.BVN,
                        CreatedOn = DateTimeOffset.UtcNow
                    };
                    await _context.SubAccounts.AddAsync(newSub);
                    isModified = true;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == farmer.UserId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != user.NIN)
                    {
                        user.NIN = update.NIN;
                        _context.Users.Update(user);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != user.BVN)
                    {
                        user.BVN = update.BVN;
                        _context.Users.Update(user);
                        isModified = true;
                    }
                }
            }

            if (isModified)
            {
                _context.Farmers.Update(farmer);
                await _context.SaveChangesAsync();
            }

            return farmer.Map();
        }

        #endregion createfarmer
    }
}
