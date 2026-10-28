using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{

    public interface IAdminMembershipManager
    {
        Task<(bool status, string msg, MemberShipListItemDto data)> CreateMembershipAsync(AdminCreateMembershipRequest model, AdminOnBoardingArgs args, CancellationToken ct);
        Task<MembershipAgentsResponse> GetMembershipAgentsAsync(long businessId, long membershipId);
        Task<InputFinanceResponse> GetMembershipInputFinanceAsync(long membershipId, int page, int pageSize);
        Task<MembershipMembersResponse> GetMembershipMembersAsync(long businessId, long membershipId, string? search, int pageNumber, int pageSize);
        Task<MembershipOrganizationsResponse> GetMembershipOrganizationsAsync(long businessId, long membershipId);
        Task<MemberShipOverviewDto> GetMembershipOverviewAsync(long businessId, long membershipId);
        Task<PagedResult<MemberShipListItemDto>> GetMembershipsAsync(MemberShipListQuery p);
        Task<MemberShipSummaryDto> GetSummaryAsync(long businessId);
        Task<MembershipStatusChangeResult> ReactivateMembershipAsync(long businessId, long membershipId, string actingUserId, CancellationToken ct);
        Task<MembershipStatusChangeResult> SuspendMembershipAsync(long businessId, long membershipId, string? reason, string actingUserId, CancellationToken ct);
    }

    public class AdminMembershipManager : IAdminMembershipManager
    {
        private readonly IAWSService _email;
        private readonly INotificationService _smsService;
        private readonly DataContext _context;
        private readonly IAdminAuditLogger _audit;
        private readonly ILogger<AdminMembershipManager> _log;
        private readonly IConfiguration _configuration;
        private readonly IOnboardingRepository _onboardingRepository;

        private readonly IBusinessRepository _businessRepository;
        public AdminMembershipManager(ILogger<AdminMembershipManager> log,

            IOnboardingRepository onboardingRepository,
               IConfiguration configuration,

            IAWSService aWSService,
            INotificationService smsService,
            IBusinessRepository businessRepository, IAdminAuditLogger audit, DataContext context)
        {
            _businessRepository = businessRepository;
            _context = context;
            _log = log;
            _audit = audit;
            _configuration = configuration;
            _onboardingRepository = onboardingRepository;
            _email = aWSService;
            _smsService = smsService;
        }
        private static class MemberStatusLabel
        {
            public const string Verified = "Verified";
            public const string Pending = "Pending";
            public const string Suspended = "Suspended";
        }
        private IQueryable<MemberShip> MembershipsInBusiness(long businessId)
        {
            return _context.MemberShips
                .AsNoTracking()
                .Where(f => _context.UserBusinesses.Any(ub =>
                    ub.UserId == f.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive));
        }
        private IQueryable<MemberShip> ApplyFilters(IQueryable<MemberShip> q, MemberShipListQuery p)
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
                q = q.Where(f => f.MembershipStatus == p.Status.Value);

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
        private static string ResolveStatus(MembershipStatus status, bool isVerified) =>
      status == MembershipStatus.Suspended ? MemberStatusLabel.Suspended
      : (status == MembershipStatus.Active && isVerified) ? MemberStatusLabel.Verified
      : MemberStatusLabel.Pending;

        // ---------- summary ----------
        public async Task<MemberShipSummaryDto> GetSummaryAsync(long businessId)
        {
            var q = MembershipsInBusiness(businessId);

            var grouped = await q
                .GroupBy(m => new
                {
                    m.MembershipStatus,
                    IsVerified = m.User.IsPhoneVerified && m.User.IsNinVerified
                })
                .Select(g => new
                {
                    g.Key.MembershipStatus,
                    g.Key.IsVerified,
                    Count = g.Count()
                })
                .ToListAsync();

            var byStatus = grouped
                .GroupBy(x => ResolveStatus(x.MembershipStatus, x.IsVerified))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            int Get(string s) => byStatus.GetValueOrDefault(s);

            var linkedOrgs = await _context.OrganizationMemberships
                .Where(om => q.Any(m => m.Id == om.MembershipId))
                .Select(om => om.OrganizationId)
                .Distinct()
                .CountAsync();

            var totalMembers = await _context.FarmerCooperatives
                .Where(fc => q.Any(m => m.Id == fc.CooperativeId))
                .Select(fc => fc.FarmerId)
                .Distinct()
                .CountAsync();

            return new MemberShipSummaryDto
            {
                TotalMemberShip = byStatus.Values.Sum(),
                TotalActiveMemberShip = Get(MemberStatusLabel.Verified),
                TotalPendingApproval = Get(MemberStatusLabel.Pending),
                TotalSuspendedMembers = Get(MemberStatusLabel.Suspended),
                TotalLinkedOrganizations = linkedOrgs,
                TotalMembers = totalMembers
            };
        }

        // ---------- list ----------
        public async Task<PagedResult<MemberShipListItemDto>> GetMembershipsAsync(MemberShipListQuery p)
        {
            var page = Math.Max(p.PageNumber, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(MembershipsInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(m => m.OnBoardedOn)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(m => new
                {
                    m.Id,
                    m.UserId,
                    MemberShipName = m.Name,
                    MemberShipType = m.MemberShipType.Name,
                    m.State,
                    m.OnBoardedOn,
                    m.MembershipStatus,
                    IsVerified = m.User.IsPhoneVerified && m.User.IsNinVerified
                })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            var memberCounts = await _context.FarmerCooperatives
                .Where(fc => ids.Contains(fc.CooperativeId))
                .GroupBy(fc => fc.CooperativeId)
                .Select(g => new
                {
                    CooperativeId = g.Key,
                    Count = g.Select(x => x.FarmerId).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.CooperativeId, x => x.Count);

            var orgCounts = await _context.OrganizationMemberships
                .Where(om => ids.Contains(om.MembershipId))
                .GroupBy(om => om.MembershipId)
                .Select(g => new
                {
                    MembershipId = g.Key,
                    Count = g.Select(x => x.OrganizationId).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.MembershipId, x => x.Count);

            var items = rows.Select(r => new MemberShipListItemDto
            {
                Id = r.Id,
                MemberShipLeaderId = r.UserId,
                MemberShipName = r.MemberShipName,
                MemberShipType = r.MemberShipType,
                State = r.State,
                Members = memberCounts.GetValueOrDefault(r.Id),
                LinkedOrganizations = orgCounts.GetValueOrDefault(r.Id),
                RegDate = r.OnBoardedOn,
                Status = ResolveStatus(r.MembershipStatus, r.IsVerified)
            }).ToList();

            return new PagedResult<MemberShipListItemDto>(items, page, size, total);
        }

        #region createmembership
        public async Task<(bool status, string msg, MemberShipListItemDto data)> CreateMembershipAsync(
            AdminCreateMembershipRequest model, AdminOnBoardingArgs args, CancellationToken ct)
        {
            try
            {
                _log.LogInformation("Starting membership onboarding {CorrelationId}", Guid.NewGuid());

                // 1. Validate everything up front (throws once with all errors)
                var (business, membershipType, existingUser) = await ValidateMembershipInputs(model, args);

                var dateTime = DateTimeOffset.UtcNow;

                // 2. Leader user: reuse if the phone already belongs to a user, otherwise register
                string leaderUserId;
                OnboardingRequestModel? newUser = null;

                if (existingUser != null)
                {
                    leaderUserId = existingUser.Id;
                }
                else
                {
                    model.SignupMethod = SignupMethod.PhoneNumber;
                    model.Pin = GenerateRandomFourDigitsPin();

                    var created = await _onboardingRepository.RegisterUser(
                        BuildUserCreationModel(model),
                        new OnBoardingArgs { businessCode = args.businessCode, Env = args.Env, userType = args.userType });

                    newUser = created.model;
                    newUser.LogDescription = $"Added Membership {model.MembershipName}";
                    newUser.ActivityName = "New Membership Added";
                    leaderUserId = newUser.Id;
                }

                // 3. Membership (+ leader's farmer profile and cooperative link)
                var item = await CreateMembershipOrFails(model, args, business, membershipType, leaderUserId, dateTime);

                // 4. Audit + credentials notification (non-critical)
                await RunMembershipNonCriticalSteps(model, args, leaderUserId, newUser, ct);

                return (true, "Membership created successfully", item);
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateMembershipAsync failed unexpectedly");
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete onboarding at the moment.");
            }
        }
        private static string GenerateRandomFourDigitsPin() =>
          RandomNumberGenerator.GetInt32(1000, 10000).ToString();

        private async Task<(BusinessModel business, MemberShipType type, Entities.Common.User? existingUser)>
    ValidateMembershipInputs(AdminCreateMembershipRequest model, AdminOnBoardingArgs args)
        {
            var errors = new List<string>();
            BusinessModel? business = null;
            MemberShipType? type = null;
            Entities.Common.User? existingUser = null;

            model.PhoneNumber = model.PhoneNumber?.Trim();

            // Business
            if (string.IsNullOrWhiteSpace(args.businessCode))
                errors.Add("Business code is required.");
            else
            {
                business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
                if (business == null)
                    errors.Add($"Business '{args.businessCode}' does not exist.");
            }

            // Membership
            var name = model.MembershipName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Membership name is required.");

            type = await _context.MemberShipTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == model.MembershipTypeId);
            if (type == null)
                errors.Add("Selected membership type does not exist.");

            if (business != null && !string.IsNullOrWhiteSpace(name) &&
                await MembershipsInBusiness(business.BusinessId).AnyAsync(m => m.Name == name))
                errors.Add($"A membership named '{name}' already exists.");

            // Leader
            if (string.IsNullOrWhiteSpace(model.FirstName)) errors.Add("Leader first name is required.");
            if (string.IsNullOrWhiteSpace(model.LastName)) errors.Add("Leader last name is required.");
            if (string.IsNullOrWhiteSpace(model.Gender)) errors.Add("Leader gender is required.");

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                errors.Add("Leader phone number is required.");
            else if (!Regex.IsMatch(model.PhoneNumber, @"^\d{11}$"))
                errors.Add("Phone number must be exactly 11 digits and numeric only.");
            else
            {
                existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
                if (existingUser != null &&
                    await _context.MemberShips.AnyAsync(m => m.UserId == existingUser.Id))
                    errors.Add("This phone number already belongs to a membership leader.");
            }

            ThrowIfAny(errors);
            return (business!, type!, existingUser);
        }
        private static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0) return;

            var message = errors.Count == 1
                ? errors[0]
                : $"{errors.Count} validation errors: {string.Join("; ", errors)}";

            throw new BaseException(ExceptionType.BAD_REQUEST, message, errors);
        }
        private static OnboardingRequestModel BuildUserCreationModel(AdminCreateMembershipRequest model)
        {
            return new OnboardingRequestModel
            {
                PhoneNumber = model.PhoneNumber,
                Pin = model.Pin,
                FirstName = model.FirstName,
                LastName = model.LastName,
                FullName = $"{model.FirstName} {model.LastName}".Trim(),
                SignupMethod = model.SignupMethod
                // Gender = model.Gender  (if OnboardingRequestModel has it)
            };
        }

        private async Task<MemberShipListItemDto> CreateMembershipOrFails(
            AdminCreateMembershipRequest model, AdminOnBoardingArgs args, BusinessModel business,
            MemberShipType type, string leaderUserId, DateTimeOffset dateTime)
        {
            if (string.IsNullOrWhiteSpace(leaderUserId))
                throw new ValidationException("Could not find target user");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Make sure the membership is visible in this business's list
                    if (!await _context.UserBusinesses.AnyAsync(ub =>
                            ub.UserId == leaderUserId && ub.BusinessId == business.BusinessId))
                    {
                        _context.UserBusinesses.Add(new UserBusiness
                        {
                            UserId = leaderUserId,
                            BusinessId = business.BusinessId,
                            IsActive = true
                        });
                    }

                    var membership = new MemberShip
                    {
                        Name = model.MembershipName.Trim(),
                        UserId = leaderUserId,
                        MemberShipTypeId = type.Id,
                        PhoneNumber = model.PhoneNumber,
                        MembershipStatus = MembershipStatus.Active,
                        OnBoardedOn = dateTime,
                        BelongToBusinessCode = args.businessCode,
                        TierLevel = Tier.None,
                        OnboardedById = args.OnboardedByUserId,
                        IsActive = true,
                        IsLeaderAFarmer = model.IsLeaderAFarmer
                        // OnboardedByUserId = args.OnboardedByUserId (if the entity has it)
                    };
                    _context.MemberShips.Add(membership);

                    // Leader is also a farmer: reuse their farmer profile or create one
                    Farmer? farmer = null;
                    if (model.IsLeaderAFarmer)
                    {
                        farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == leaderUserId);
                        if (farmer == null)
                        {
                            farmer = BuildLeaderFarmerModel(model, args, leaderUserId, dateTime).Map();
                            await _context.Farmers.AddAsync(farmer);
                        }
                    }

                    await _context.SaveChangesAsync();   // generates membership.Id and farmer.Id

                    if (farmer != null)
                    {
                        _context.FarmerCooperatives.Add(new FarmerCooperative
                        {
                            FarmerId = farmer.Id,
                            CooperativeId = membership.Id,
                            IsActive = true,
                            OnboardedByUserId = args.OnboardedByUserId,
                            JoinedOn = DateTimeOffset.UtcNow
                        });
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return new MemberShipListItemDto
                    {
                        Id = membership.Id,
                        MemberShipLeaderId = leaderUserId,
                        MemberShipName = membership.Name,
                        MemberShipType = type.Name,
                        State = membership.State,
                        Members = farmer != null ? 1 : 0,
                        LinkedOrganizations = 0,
                        RegDate = membership.OnBoardedOn,
                        Status = "Pending"
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private static FarmerModel BuildLeaderFarmerModel(
            AdminCreateMembershipRequest model, AdminOnBoardingArgs args, string userId, DateTimeOffset dateTime)
        {
            return new FarmerModel
            {
                OnboardedByUserId = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Name = $"{model.FirstName} {model.LastName}".Trim(),
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                OnBoardedOn = dateTime,
                Level = FarmerLevel.Level1.ToString()
            };
        }

        private async Task RunMembershipNonCriticalSteps(
            AdminCreateMembershipRequest model, AdminOnBoardingArgs args,
            string leaderUserId, OnboardingRequestModel? newUser, CancellationToken ct)
        {
            try
            {
                await _audit.LogAsync("CreateMembership", args.OnboardedByUserId, leaderUserId,
                    JsonConvert.SerializeObject(model), AdminActiviLogModule.AdminPanel.UserMgt,
                    $"{newUser?.Email}", ct);

                // Only brand-new users get a default PIN; existing users keep their credentials
                if (newUser != null)
                    await SendDefaultCredentialChange(newUser, args.businessCode);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Non-critical onboarding step failed for membership leader {UserId}.", leaderUserId);
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

        #endregion createmembership


        private static string ResolveStatusLabel(MembershipStatus status, bool isVerified) =>
    status == MembershipStatus.Suspended ? "Suspended"
    : isVerified && status == MembershipStatus.Active ? "Verified"
    : "Pending";

        private async Task<(string channel, string? source)> ResolveRegistrationChannelAsync(
       string? createdBy, string userId)
        {
            if (string.IsNullOrEmpty(createdBy) || createdBy == userId)
                return ("Self Registration", null);

            var orgName = await _context.Organizations
                .Where(o => o.UserId == createdBy)
                .Select(o => o.CompanyName)
                .FirstOrDefaultAsync();
            if (orgName != null) return ("Organization Assisted", orgName);

            var membershipName = await _context.MemberShips
                .Where(m => m.UserId == createdBy)
                .Select(m => m.Name)
                .FirstOrDefaultAsync();
            if (membershipName != null) return ("Membership Assisted", membershipName);

            var farmerName = await _context.Farmers
                .Where(f => f.UserId == createdBy)
                .Select(f => f.User.FirstName + " " + f.User.LastName)
                .FirstOrDefaultAsync();
            if (farmerName != null) return ("Farmer Assisted", farmerName);

            var onboarder = await _context.Users
                .Where(u => u.Id == createdBy)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            return ("Assisted", onboarder == null ? null : $"{onboarder.FirstName} {onboarder.LastName}".Trim());
        }

        public async Task<MemberShipOverviewDto> GetMembershipOverviewAsync(long businessId, long membershipId)
        {
            var m = await MembershipsInBusiness(businessId)
                .Where(x => x.Id == membershipId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.MembershipStatus,
                    x.OnBoardedOn,
                    x.PhoneNumber,
                    x.State,
                    x.Lga,
                    x.Location,
                    TypeName = x.MemberShipType.Name,
                    x.UserId,
                    // from the users table
                    x.User.FirstName,
                    x.User.MiddleName,
                    x.User.LastName,
                    x.User.Email,
                    UserPhone = x.User.PhoneNumber,
                    x.User.CreatedOn,
                    x.User.DateModified,
                    x.User.CreatedBy,
                    IsVerified = x.User.IsPhoneVerified && x.User.IsNinVerified
                })
                .FirstOrDefaultAsync();

            if (m == null)
                throw new NotFoundException("Membership not found.");

            // ---- Stats ----
            var memberFarmerIds = _context.FarmerCooperatives
                .Where(fc => fc.CooperativeId == membershipId && fc.IsActive)
                .Select(fc => fc.FarmerId);

            var members = await memberFarmerIds.Distinct().CountAsync();

            var farms = _context.Farms.Where(f => memberFarmerIds.Contains(f.FarmerId));
            var farmCount = await farms.CountAsync();
            var totalLand = await farms.SumAsync(f => (decimal?)f.SizeInHectares) ?? 0m;

            var linkedOrgs = await _context.OrganizationMemberships
                .Where(om => om.MembershipId == membershipId)
                .Select(om => om.OrganizationId)
                .Distinct()
                .CountAsync();

            var activeProgrammes = 0;   // still needs the programme table, see notes

            // ---- Registration channel ----
            var (channel, source) = await ResolveRegistrationChannelAsync(m.CreatedBy, m.UserId);

            var contactName = string.Join(" ",
                new[] { m.FirstName, m.MiddleName, m.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            var status = ResolveStatusLabel(m.MembershipStatus, m.IsVerified);


            return new MemberShipOverviewDto
            {
                Stats = new MembershipStatsDto
                {
                    Members = members,
                    Farms = farmCount,
                    TotalLandSizeHectares = totalLand,
                    LinkedOrganizations = linkedOrgs,
                    ActiveProgrammes = activeProgrammes
                },
                GeneralInformation = new MembershipGeneralInfoDto
                {
                    Name = m.Name,
                    MembershipType = m.TypeName,
                    RegistrationNumber = "N/A",        // no column yet, see notes
                    RegistrationDate = m.OnBoardedOn,
                    Status = status,
                    Id = m.Id,
                    MembershipRepresentativeId = m.UserId,
                    CanSuspend = m.MembershipStatus == MembershipStatus.Active,
                    CanReactivate = m.MembershipStatus == MembershipStatus.Suspended
                },
                PrimaryContact = new MembershipContactDto
                {
                    ContactPerson = contactName,
                    Position = "Membership Leader",
                    PhoneNumber = m.PhoneNumber ?? m.UserPhone,
                    Email = m.Email
                },
                Address = new MembershipAddressDto
                {
                    //  Country = "Nigeria",
                    State = m.State,
                    Lga = m.Lga,
                    Address = m.Location,
                    PostalCode = "N/A"                 // no column yet, see notes
                },
                RegistrationInformation = new MembershipRegistrationInfoDto
                {
                    RegistrationChannel = channel,
                    RegistrationSource = source,      // add to the DTO, like the agent page
                    VerifiedBy = null,
                    VerificationDate = null,
                    LastUpdated = m.DateModified ?? m.CreatedOn
                }
            };
        }

        private IQueryable<Farmer> MembershipFarmers(long membershipId)
        {
            return _context.Farmers
                .AsNoTracking()
                .Where(f => _context.FarmerCooperatives.Any(fc =>
                    fc.FarmerId == f.Id &&
                    fc.CooperativeId == membershipId &&
                    fc.IsActive));
        }

        private static string ResolveFarmerStatusLabel(FarmerStatus status, bool isVerified) =>
            status == FarmerStatus.Suspended ? "Suspended"
            : isVerified ? "Verified"
            : "Pending";


        public async Task<MembershipMembersResponse> GetMembershipMembersAsync(
        long businessId, long membershipId, string? search, int pageNumber, int pageSize)
        {
            // The membership must exist in this business
            var exists = await MembershipsInBusiness(businessId).AnyAsync(m => m.Id == membershipId);
            if (!exists)
                throw new NotFoundException("Membership not found.");

            var page = Math.Max(pageNumber, 1);
            var size = Math.Clamp(pageSize, 1, 100);

            var q = MembershipFarmers(membershipId);

            var counts = await q
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Suspended = g.Count(f => f.FarmerStatus == FarmerStatus.Suspended),
                    Verified = g.Count(f => f.FarmerStatus != FarmerStatus.Suspended
                                            && f.User.IsPhoneVerified && f.User.IsNinVerified)
                })
                .FirstOrDefaultAsync();

            var total = counts?.Total ?? 0;
            var suspended = counts?.Suspended ?? 0;
            var verified = counts?.Verified ?? 0;

            var allFarmerIds = q.Select(f => f.Id);
            var farmsQ = _context.Farms.Where(x => allFarmerIds.Contains(x.FarmerId));
            var totalFarms = await farmsQ.CountAsync();
            var totalSize = await farmsQ.SumAsync(x => (decimal?)x.SizeInHectares) ?? 0m;

            var summary = new MembershipMembersSummaryDto
            {
                TotalMembers = total,
                VerifiedMembers = verified,
                PendingVerification = total - verified - suspended,
                SuspendedFarmers = suspended,
                TotalFarms = totalFarms,
                TotalFarmSizeHectares = totalSize
            };

            var tableQ = q;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                tableQ = tableQ.Where(f => f.Name.Contains(s) || f.PhoneNumber.Contains(s));
            }

            var tableTotal = await tableQ.CountAsync();

            var rows = await tableQ
                .OrderByDescending(f => f.OnBoardedOn)
                .ThenByDescending(f => f.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new
                {
                    f.Id,
                    f.UserId,
                    f.Name,
                    f.State,
                    f.FarmerStatus,
                    IsVerified = f.User.IsPhoneVerified && f.User.IsNinVerified
                })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            var farmCounts = await _context.Farms
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.FarmerId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            var agentCounts = await _context.FarmerAgents
                .Where(x => ids.Contains(x.FarmerId) && x.IsActive)
                .GroupBy(x => x.FarmerId)
                .Select(g => new { FarmerId = g.Key, Count = g.Select(a => a.AgentId).Distinct().Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            var items = rows.Select(r => new MembershipMemberItemDto
            {
                FarmerId = r.Id,
                FarmerUserId = r.UserId,
                FullName = r.Name,
                Farms = farmCounts.GetValueOrDefault(r.Id),
                State = r.State,
                LinkedAgents = agentCounts.GetValueOrDefault(r.Id),
                Status = ResolveFarmerStatusLabel(r.FarmerStatus, r.IsVerified)
            }).ToList();

            return new MembershipMembersResponse
            {
                Summary = summary,
                Members = new PagedResult<MembershipMemberItemDto>(items, page, size, tableTotal)
            };
        }



        public async Task<MembershipOrganizationsResponse> GetMembershipOrganizationsAsync(
    long businessId, long membershipId)
        {
            var exists = await MembershipsInBusiness(businessId).AnyAsync(m => m.Id == membershipId);
            if (!exists)
                throw new NotFoundException("Membership not found.");

            // ---- Linked organisations ----
            var orgs = await (from om in _context.OrganizationMemberships.AsNoTracking()
                              join o in _context.Organizations.AsNoTracking()
                                  on om.OrganizationId equals o.OrganizationId
                              where om.MembershipId == membershipId
                              orderby om.AssignedOn descending
                              select new
                              {
                                  o.OrganizationId,
                                  o.OrganizationCode,
                                  o.CompanyName,
                                  o.State,
                                  o.LGA,
                                  om.AssignedOn
                              })
                .ToListAsync();

            if (orgs.Count == 0)
                return new MembershipOrganizationsResponse { LinkedOrganizationCount = 0 };

            // ---- Performance, scoped to this membership's farmers ----
            var codes = orgs.Select(o => o.OrganizationCode).ToList();
            var memberFarmers = MembershipFarmers(membershipId);          // helper from the Members tab
            var memberFarmerIds = memberFarmers.Select(f => f.Id);

            var farmerStats = await (from fo in _context.FarmerOrganizations
                                     join f in memberFarmers on fo.FarmerId equals f.Id
                                     where codes.Contains(fo.OrganizationCode)
                                     group f by fo.OrganizationCode into g
                                     select new
                                     {
                                         Code = g.Key,
                                         Total = g.Count(),
                                         Verified = g.Count(x => x.User.IsPhoneVerified && x.User.IsNinVerified)
                                     })
                .ToDictionaryAsync(x => x.Code);

            var farmStats = await (from fo in _context.FarmerOrganizations
                                   join fm in _context.Farms on fo.FarmerId equals fm.FarmerId
                                   where codes.Contains(fo.OrganizationCode)
                                         && memberFarmerIds.Contains(fo.FarmerId)
                                   group fm by fo.OrganizationCode into g
                                   select new
                                   {
                                       Code = g.Key,
                                       Farms = g.Count(),
                                       Size = g.Sum(x => (decimal?)x.SizeInHectares)
                                   })
                .ToDictionaryAsync(x => x.Code);

            var items = orgs.Select(o =>
            {
                farmerStats.TryGetValue(o.OrganizationCode, out var fs);
                farmStats.TryGetValue(o.OrganizationCode, out var ms);

                var total = fs?.Total ?? 0;
                var verified = fs?.Verified ?? 0;

                return new MembershipOrganizationItemDto
                {
                    OrganizationId = o.OrganizationId,
                    OrganizationCode = o.OrganizationCode,
                    Name = o.CompanyName,
                    AssignmentDate = o.AssignedOn,
                    Region = o.State.State,
                    CoverageArea = o.LGA.Name,
                    Performance = new MembershipOrganizationPerformanceDto
                    {
                        MembersOnboarded = total,
                        VerificationSuccessRate = total == 0 ? 0m : Math.Round(verified * 100m / total, 1),
                        TotalFarms = ms?.Farms ?? 0,
                        TotalFarmSizeHectares = ms?.Size ?? 0m
                    }
                };
            }).ToList();

            return new MembershipOrganizationsResponse
            {
                LinkedOrganizationCount = items.Count,
                Organizations = items
            };
        }


        public async Task<MembershipAgentsResponse> GetMembershipAgentsAsync(long businessId, long membershipId)
        {
            var exists = await MembershipsInBusiness(businessId).AnyAsync(m => m.Id == membershipId);
            if (!exists)
                throw new NotFoundException("Membership not found.");

            // ---- Linked agents ----
            var agents = await (from am in _context.MemberShipAgents.AsNoTracking()
                                join a in _context.Agents.AsNoTracking() on am.AgentId equals a.Id
                                where am.MemberShipId == membershipId
                                orderby am.AssignedOn descending
                                select new { a.Id, a.UserId, a.Name, am.AssignedOn })
                .ToListAsync();

            if (agents.Count == 0)
                return new MembershipAgentsResponse { LinkedAgentCount = 0 };

            // ---- Performance, scoped to this membership's farmers ----
            var agentIds = agents.Select(a => a.Id).ToList();
            var memberFarmers = MembershipFarmers(membershipId);       // helper from the Members tab
            var memberFarmerIds = memberFarmers.Select(f => f.Id);

            var farmerStats = await (from fa in _context.FarmerAgents
                                     join f in memberFarmers on fa.FarmerId equals f.Id
                                     where agentIds.Contains(fa.AgentId) && fa.IsActive
                                     group f by fa.AgentId into g
                                     select new
                                     {
                                         AgentId = g.Key,
                                         Total = g.Count(),
                                         Verified = g.Count(x => x.User.IsPhoneVerified && x.User.IsNinVerified)
                                     })
                .ToDictionaryAsync(x => x.AgentId);

            var farmStats = await (from fa in _context.FarmerAgents
                                   join fm in _context.Farms on fa.FarmerId equals fm.FarmerId
                                   where agentIds.Contains(fa.AgentId) && fa.IsActive
                                         && memberFarmerIds.Contains(fa.FarmerId)
                                   group fm by fa.AgentId into g
                                   select new
                                   {
                                       AgentId = g.Key,
                                       Farms = g.Count(),
                                       Size = g.Sum(x => (decimal?)x.SizeInHectares)
                                   })
                .ToDictionaryAsync(x => x.AgentId);

            var items = agents.Select(a =>
            {
                farmerStats.TryGetValue(a.Id, out var fs);
                farmStats.TryGetValue(a.Id, out var ms);

                var total = fs?.Total ?? 0;
                var verified = fs?.Verified ?? 0;

                return new MembershipAgentItemDto
                {
                    AgentId = a.Id,
                    AgentUserId = a.UserId,
                    Name = a.Name,
                    AssignmentDate = a.AssignedOn,
                    Performance = new MembershipAgentPerformanceDto
                    {
                        FarmersOnboarded = total,
                        VerificationSuccessRate = total == 0 ? 0m : Math.Round(verified * 100m / total, 1),
                        TotalFarms = ms?.Farms ?? 0,
                        TotalFarmSizeHectares = ms?.Size ?? 0m
                    }
                };
            }).ToList();

            return new MembershipAgentsResponse
            {
                LinkedAgentCount = items.Count,
                Agents = items
            };
        }

        public async Task<InputFinanceResponse> GetMembershipInputFinanceAsync(
    long membershipId, int page, int pageSize)
        {
            // Step 1: the membership's own user id (a membership can subscribe as itself)
            var membershipUserId = await _context.MemberShips
                .Where(m => m.Id == membershipId)
                .Select(m => m.UserId)
                .FirstOrDefaultAsync();

            if (membershipUserId == null)
                throw new NotFoundException("Membership not found.");

            // Step 2: user ids of the membership's active farmers (they can subscribe individually)
            var farmerUserIds = _context.FarmerCooperatives
                .Where(fc => fc.CooperativeId == membershipId && fc.IsActive)
                .Select(fc => fc.Farmer.UserId);

            var memberCount = await farmerUserIds.CountAsync();

            // Step 3: financed subscriptions by the membership itself or by its farmers
            var subs = _context.InputPackageSubscriptions
                .Where(s => s.IsActive
                         && InputFinanceRules.FinancedStatuses.Contains(s.Status)
                         && (s.SubscriberUserId == membershipUserId
                          || farmerUserIds.Contains(s.SubscriberUserId)));

            // Step 4: summary cards
            var packagesFacilitated = await subs.CountAsync();

            // Farmers financed = members who subscribed individually
            //                  + ALL members, if the membership itself was financed (distinct)
            var membershipWasFinanced = await subs.AnyAsync(s => s.SubscriberUserId == membershipUserId);

            var financedFarmers = subs
                .Where(s => farmerUserIds.Contains(s.SubscriberUserId))
                .Select(s => s.SubscriberUserId);

            if (membershipWasFinanced)
                financedFarmers = financedFarmers.Concat(farmerUserIds);

            var farmersFinanced = await financedFarmers.Distinct().CountAsync();

            var totalValue = await subs
                .SumAsync(s => (decimal?)s.TotalFinancingValue) ?? 0m;

            var activePackages = await subs
                .CountAsync(s => InputFinanceRules.ActiveStatuses.Contains(s.Status));

            var summary = new MembersipInputFinanceSummaryDto
            {
                TotalPackages = packagesFacilitated,
                FarmersFinanced = farmersFinanced,
                TotalFinancingValue = totalValue,
                TotalValueFormatted = InputFinanceRules.FormatNaira(totalValue),
                ActivePackages = activePackages
            };

            // Step 5: table rows
            var rows = await subs
                .OrderByDescending(s => s.ApprovedOn ?? s.DisbursedOn ?? s.SubscribedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    PackageId = s.InputPackageId,
                    Package = s.InputPackage.Name,
                    Organization = (s.Organization ?? s.InputPackage.Organization).CompanyName,
                    Value = s.TotalFinancingValue,
                    s.Status,
                    ApprovedDate = s.ApprovedOn ?? s.DisbursedOn,
                    // the membership's own subscription covers all its members; a farmer's covers 1
                    Farmers = s.SubscriberUserId == membershipUserId ? memberCount : 1
                })
                .ToListAsync();

            var items = rows.Select(r => new MembershipInputFinancePackageDto
            {
                PackageId = r.PackageId,
                PackageName = r.Package,
                Farmers = r.Farmers,
                Organization = r.Organization,
                PackageValue = r.Value,
                PackageValueFormatted = InputFinanceRules.FormatNaira(r.Value),
                Status = InputFinanceRules.ToBadge(r.Status),
                ApprovedDate = r.ApprovedDate
            }).ToList();

            return new InputFinanceResponse
            {
                Summary = summary,
                Packages = new Core.Models.Admin.PagedResult<MembershipInputFinancePackageDto>(
                               items, page, pageSize, packagesFacilitated),

            };
        }

        public Task<MembershipStatusChangeResult> SuspendMembershipAsync(
    long businessId, long membershipId, string? reason, string actingUserId, CancellationToken ct)
    => ChangeMembershipStatusAsync(businessId, membershipId, suspend: true, reason, actingUserId, ct);

        public Task<MembershipStatusChangeResult> ReactivateMembershipAsync(
            long businessId, long membershipId, string actingUserId, CancellationToken ct)
            => ChangeMembershipStatusAsync(businessId, membershipId, suspend: false, null, actingUserId, ct);

        private async Task<MembershipStatusChangeResult> ChangeMembershipStatusAsync(
         long businessId, long membershipId, bool suspend, string? reason,
         string actingUserId,
         CancellationToken ct)
        {
            reason = reason?.Trim();

            if (suspend)
            {
                if (string.IsNullOrWhiteSpace(reason))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Please provide a reason for suspension.");
                if (reason.Length > 500)
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Reason must not exceed 500 characters.");
            }

            // Tracked entity, scoped to this business (no MembershipsInBusiness here, see note below)
            var membership = await _context.MemberShips
                .AsTracking()
                .Include(m => m.User)
                .Where(m => m.Id == membershipId)
                .Where(m => _context.UserBusinesses.Any(ub =>
                    ub.UserId == m.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive))
                .FirstOrDefaultAsync(ct);

            if (membership == null)
                throw new NotFoundException("Membership not found.");

            if (suspend && membership.MembershipStatus != MembershipStatus.Active)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Only an active membership can be suspended.");

            if (!suspend && membership.MembershipStatus != MembershipStatus.Suspended)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Only a suspended membership can be reactivated.");

            membership.MembershipStatus = suspend ? MembershipStatus.Suspended : MembershipStatus.Active;

            // Overview's "Last Updated" reads from the leader's user row
            if (membership.User != null)
                membership.User.DateModified = DateTimeOffset.UtcNow;

            var saved = await _context.SaveChangesAsync(ct);
            if (saved == 0)
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR,
                    "The membership status could not be saved.");

            // Audit (also feeds the Activity Timeline); the status change is already saved,
            // so a failure here is logged with the reason rather than failing the request.
            try
            {
                await _audit.LogAsync(
                    suspend ? "SuspendMembership" : "ReactivateMembership",
                    actingUserId,
                    membership.UserId,
                    JsonConvert.SerializeObject(new { membership.Id, membership.Name, Reason = reason }),
                    AdminActiviLogModule.AdminPanel.UserMgt,
                    membership.User?.Email ?? "",
                    ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Audit failed for membership {Id} ({Action}). Reason: {Reason}",
                    membershipId, suspend ? "suspend" : "reactivate", reason);
            }

            var isVerified = membership.User != null
                             && membership.User.IsPhoneVerified && membership.User.IsNinVerified;

            return new MembershipStatusChangeResult
            {
                MembershipId = membership.Id,
                Status = ResolveStatusLabel(membership.MembershipStatus, isVerified),
                CanSuspend = membership.MembershipStatus == MembershipStatus.Active,
                CanReactivate = membership.MembershipStatus == MembershipStatus.Suspended,
                Message = suspend ? "Membership suspended successfully."
                                  : "Membership reactivated successfully."
            };
        }

    }
}
