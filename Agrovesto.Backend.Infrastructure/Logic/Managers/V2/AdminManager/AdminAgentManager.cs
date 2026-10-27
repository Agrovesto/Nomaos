using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IAdminAgentManager
    {
        Task<(bool status, string msg, string?[] errors)> CreateAgentAsync(AdminCreateAgentRequest model, AdminOnBoardingArgs args, CancellationToken ct);
        Task<AgentFarmersResponse> GetAgentFarmersAsync(string agentUserId, string search, int page, int pageSize);
        Task<AgentInputFinanceResponse> GetAgentInputFinanceAsync(string agentUserId, int page, int pageSize);
        Task<AgentMembershipsResponse> GetAgentMembershipsAsync(string agentUserId);
        Task<AgentOrganizationsResponse> GetAgentOrganizationsAsync(string agentUserId);
        Task<AgentOverviewResponse> GetAgentOverviewAsync(string agentUserId);
        Task<Core.Models.Admin.PagedResult<AgentListItemDto>> GetAgentsAsync(AgentListQuery p);
        Task<AgentWalletResponse> GetAgentWalletsAsync(string agentUserId, string businessCode);
        Task<List<AgentListItemDto>> GetAllForExportAsync(AgentListQuery p);
        Task<AgentSummaryDto> GetSummaryAsync(long businessId);
        Task<bool> ReactivateAgentAsync(long businessId, string agentUserId, string actingUserId, string? reason);
        Task<bool> SuspendAgentAsync(long businessId, string agentUserId, string actingUserId, string? reason);
    }
    public class AdminAgentManager : IAdminAgentManager
    {
        private readonly IAWSService _email;
        private readonly ILogger<AdminAgentManager> _log;
        private readonly DataContext _context;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IConfiguration _configuration;
        private readonly IAdminAuditLogger _audit;
        private readonly INotificationService _smsService;
        public AdminAgentManager(DataContext context, ILogger<AdminAgentManager> log,
            IBusinessRepository businessRepository, IAdminAuditLogger audit,
            IConfiguration configuration,
            IAWSService aWSService,
            INotificationService smsService, IOnboardingRepository onboardingRepository)
        {
            _context = context;
            _log = log;
            _businessRepository = businessRepository;
            _email = aWSService;
            _onboardingRepository = onboardingRepository;
            _configuration = configuration;
            _audit = audit;
            _smsService = smsService;
        }
         
        private IQueryable<Agent> AgentsInBusiness(long businessId)
        {
            return _context.Agents
                .AsNoTracking()
                .Where(f => _context.UserBusinesses.Any(ub =>
                    ub.UserId == f.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive));
        }
        private IQueryable<Agent> ApplyFilters(IQueryable<Agent> q, AgentListQuery p)
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
                q = q.Where(f => f.AgentStatus == p.Status.Value);

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

        public async Task<Core.Models.Admin.PagedResult<AgentListItemDto>> GetAgentsAsync(AgentListQuery p)
        {
            var page = Math.Max(p.PageNumber, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(AgentsInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(f => f.OnBoardedOn)
                .ThenByDescending(f => f.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.AgentStatus, f.UserId })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            var farmerCounts = await _context.FarmerAgents
               .AsNoTracking()
               .Where(x => ids.Contains(x.AgentId))
               .GroupBy(x => x.AgentId)
               .Select(g => new { AgentId = g.Key, Count = g.Count() })
               .ToDictionaryAsync(x => x.AgentId, x => x.Count);


            var orgCounts = await _context.OrganizationAgents
    .Where(x => ids.Contains(x.AgentId) && x.IsActive)
    .GroupBy(x => x.AgentId)
    .Select(g => new { AgentId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId, x => x.Count);


            var items = rows.Select(r => new AgentListItemDto
            {
                Id = r.Id,
                AgentId = r.UserId,
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = EnumExtensions.GetDisplayName(r.AgentStatus),
                Farmers = farmerCounts.GetValueOrDefault(r.Id),               
                Organizations = orgCounts.GetValueOrDefault(r.Id)

            }).ToList();

            return new Core.Models.Admin.PagedResult<AgentListItemDto>(items,  page, size, total);
        }

        private static readonly Expression<Func<Agent, bool>> IsVerified =
        f => (f.User.IsPhoneVerified && f.User.IsNinVerified);
        public async Task<AgentSummaryDto> GetSummaryAsync(long businessId)
        {
            var q =  AgentsInBusiness(businessId);

            var total = await q.CountAsync();
            var verified = await q.CountAsync(IsVerified);
            var suspended = await q.CountAsync(f => f.AgentStatus == AgentStatus.Suspended);
            var pending = total - verified;

            int Count(AgentStatus s) => q.Count(f => f.AgentStatus == s);
         

            return new AgentSummaryDto
            {
                TotalAgents = total,
                TotalActiveAgents = Count(AgentStatus.Active),
                TotalPendingVerificationAgents =pending,
                TotalSuspendedAgents = Count(AgentStatus.Suspended)
            };
        }

        public async Task<List<AgentListItemDto>> GetAllForExportAsync(AgentListQuery p)
        {
            p.PageNumber = 1;
            p.PageSize = int.MaxValue;
            // Reuse the table logic without the 100-row clamp:
            var q = ApplyFilters(AgentsInBusiness(p.BusinessId), p);

            var rows = await q.OrderByDescending(f => f.OnBoardedOn)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.AgentStatus, f.UserId })
                .ToListAsync();


            var ids = rows.Select(r => r.Id).ToList();

            var farmerCounts = await _context.FarmerAgents
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.AgentId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);


            var orgCounts = await _context.OrganizationAgents
    .Where(x => ids.Contains(x.AgentId) && x.IsActive)
    .GroupBy(x => x.AgentId)
    .Select(g => new { AgentId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.AgentId, x => x.Count);


         

            return rows.Select((r, i) => new AgentListItemDto
            {
                //SerialNo = i + 1,
                Id = r.Id,
                AgentId = r.UserId,
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = EnumExtensions.GetDisplayName(r.AgentStatus),
                Farmers = farmerCounts.GetValueOrDefault(r.Id),
                Organizations = orgCounts.GetValueOrDefault(r.Id)
            }).ToList();
        }

        #region createagent
        public async Task<(bool status, string msg, string?[] errors)> CreateAgentAsync(
      AdminCreateAgentRequest model, AdminOnBoardingArgs args, CancellationToken ct)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                _log.LogInformation("Starting agent onboarding {CorrelationId}", correlationId);

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

                // 4. Create agent + map cooperatives/agents/organizations/value chain
                var agentResult = await CreateAgentOrFails(
                    model, args, business, createUser.model.Id, dateTime);

                createUser.model.LogDescription = $"Added Agent {model.FirstName} {model.LastName}";
                createUser.model.ActivityName = "New Agent Added";

                // 5. Audit + credentials notification (non-critical)
                await RunNonCriticalSteps(model, args, business, createUser.model, dateTime, ct);

                return (agentResult.status, "Agent created successfully", Array.Empty<string?>());
            }
            catch (BaseException)
            {
                throw;   // let it bubble up to whatever turns exceptions into HTTP responses
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateAgentAsync failed unexpectedly");
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
        AdminCreateAgentRequest model, AdminOnBoardingArgs args)
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

            ThrowIfAny(errors);
            return business!;
        }

       

        private static IEnumerable<string> ValidateContact(AdminCreateAgentRequest model)
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

     

        private static void ApplyDefaultCredentials(AdminCreateAgentRequest model)
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


        private async Task<(bool status, AgentModel data)> CreateAgentOrFails(
       AdminCreateAgentRequest model,
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

            AgentModel agent = BuildAgentModel(model, args, userId, dateTime);
            

            var result = await CreateOrUpdateAgent(agent, model, args);

            return result;
        }

        private static AgentModel BuildAgentModel(AdminCreateAgentRequest model, AdminOnBoardingArgs args, string userId, DateTimeOffset datetime)
        {
            return new AgentModel
            {
                OnboardedById = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Email = model.Email,
               
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name = $"{model.FirstName} {model.MiddleName} {model.LastName}".Trim(),
                OnBoardedOn = datetime,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                Ward = model.Ward,
            };
        }




        private static OnboardingRequestModel BuildUserCreationModel(AdminCreateAgentRequest model)
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
         AdminCreateAgentRequest model,
         AdminOnBoardingArgs args,
         BusinessModel business,
         OnboardingRequestModel user,
         DateTimeOffset dateTime, CancellationToken ct)
        {
            try
            {
                await _audit.LogAsync("CreateAgent", args.OnboardedByUserId, user.Id, JsonConvert.SerializeObject(model),
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



        private async Task<(bool status, AgentModel data)> CreateOrUpdateAgent(AgentModel agentModel, AdminCreateAgentRequest data, AdminOnBoardingArgs args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var agent = await _context.Agents
                        .FirstOrDefaultAsync(f => f.UserId == agentModel.UserId);

                    if (agent == null)
                    {
                        agent = agentModel.Map();
                        await _context.Agents.AddAsync(agent);
                    }
                    else
                    {
                        await UpdateAgentProfileAsync(agentModel.UserId, new AgentUpdateModel
                        {
                            Name = agentModel.Name,
                            PhoneNumber = agentModel.PhoneNumber,
                            Email = agentModel.Email,
                            State = agentModel.State,
                            Lga = agentModel.Lga,
                            Location = agentModel.Location,
                            Ward = agentModel.Ward,
                            
                        });
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        agent = await _context.Agents
                            .FirstOrDefaultAsync(f => f.UserId == agentModel.UserId);
                    }


                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, agent.Map());
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
            return ex.InnerException?.Message.Contains("IX_Agents_UserId") == true;
        }



         public async Task<AgentModel> UpdateAgentProfileAsync(string userId, AgentUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                return null;

            var agent = await _context.Agents.FirstOrDefaultAsync(f => f.UserId == userId);
            if (agent == null) return null;

            var isModified = false;

            if (update.Name != null && update.Name != agent.Name) { agent.Name = update.Name; isModified = true; }
            if (update.Location != null && update.Location != agent.Location) { agent.Location = update.Location; isModified = true; }
            if (update.State != null && update.State != agent.State) { agent.State = update.State; isModified = true; }
            if (update.Lga != null && update.Lga != agent.Lga) { agent.Lga = update.Lga; isModified = true; }
            if (update.Ward != null && update.Ward != agent.Ward) { agent.Ward = update.Ward; isModified = true; }
            if (update.PhoneNumber != null && update.PhoneNumber != agent.PhoneNumber) { agent.PhoneNumber = update.PhoneNumber; isModified = true; }
            if (update.Email != null && update.Email != agent.Email) { agent.Email = update.Email; isModified = true; }
     
            //if (!string.IsNullOrEmpty(update.OrganizationCode))
            //{
            //    bool exists = await _context.OrganizationAgents.AnyAsync(x =>
            //        x.AgentId == agent.Id &&
            //        x.OrganizationCode == update.OrganizationCode);

            //    if (!exists)
            //    {
            //        await _context.OrganizationAgents.AddAsync(new OrganizationAgent
            //        {
            //            AgentId = agent.Id,
            //            OrganizationCode = update.OrganizationCode,
            //            OnboardedByUserId = agent.OnboardedByUserId
            //        });
            //    }
            //}
        

            if (!string.IsNullOrWhiteSpace(update.NIN) || !string.IsNullOrWhiteSpace(update.BVN))
            {
                var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == agent.UserId);
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
                        UserId = agent.UserId,
                        NIN = update.NIN,
                        Bvn = update.BVN,
                        CreatedOn = DateTimeOffset.UtcNow
                    };
                    await _context.SubAccounts.AddAsync(newSub);
                    isModified = true;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == agent.UserId);
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
                _context.Agents.Update(agent);
                await _context.SaveChangesAsync();
            }

            return agent.Map();
        }

        #endregion createagent
        private static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> expr)
        {
            var not = Expression.Not(expr.Body);
            return Expression.Lambda<Func<T, bool>>(not, expr.Parameters);
        }
        public async Task<AgentFarmersResponse> GetAgentFarmersAsync(
        string agentUserId, string search, int page, int pageSize)
        {
          
            var agentId = await _context.Agents
                .Where(a => a.UserId == agentUserId)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (agentId == default)
                throw new NotFoundException("Agent not found.");

            var links = _context.FarmerAgents
                .Where(fa => fa.AgentId == agentId);

            var farmers = links.Select(fa => fa.Farmer).Distinct();
            var activeFarmers = farmers.Where(Not(FarmerIsSuspended));

            var summary = new AgentFarmersSummaryDto
            {
                TotalFarmers = await farmers.CountAsync(),
                SuspendedFarmers = await farmers.CountAsync(FarmerIsSuspended),
                VerifiedFarmers = await activeFarmers.CountAsync(FarmerIsVerified),
                PendingVerification = await activeFarmers.CountAsync(Not(FarmerIsVerified))
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                links = links.Where(fa =>
                    fa.Farmer.User.FirstName.Contains(s) ||
                    fa.Farmer.User.LastName.Contains(s) ||
                    fa.Farmer.User.PhoneNumber.Contains(s) ||
                    fa.Farmer.State.Contains(s));
            }

            var total = await links.CountAsync();

            var items = await links
                .OrderByDescending(fa => fa.AssignedOn)          
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(fa => new AgentFarmerItemDto
                {
                    FarmerId = fa.Farmer.UserId,
                    FarmerName = fa.Farmer.User.FirstName + " " + fa.Farmer.User.LastName,
                    Phone = fa.Farmer.User.PhoneNumber,
                    Farms = _context.Farms.Count(x => x.FarmerId == fa.FarmerId),
                    RegDate = fa.AssignedOn,                
                    State = fa.Farmer.State,
                    Status = fa.Farmer.FarmerStatus == FarmerStatus.Suspended ? "Suspended"
                               : (fa.Farmer.User.IsPhoneVerified && fa.Farmer.User.IsNinVerified) ? "Active"
                               : "Pending"
                })
                .ToListAsync();

            return new AgentFarmersResponse
            {
                Summary = summary,
                Farmers = new Core.Models.Admin.PagedResult<AgentFarmerItemDto>(items, total, page, pageSize)
            };
        }

        public async Task<AgentMembershipsResponse> GetAgentMembershipsAsync(string agentUserId)
        {
            var agentId = await _context.Agents
                .Where(a => a.UserId == agentUserId)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (agentId == default)
                throw new NotFoundException("Agent not found.");

            var links = _context.MemberShipAgents
                .Where(am => am.AgentId == agentId);  

            var totalLinked = await links.CountAsync();

            var memberships = await links
                .OrderByDescending(am => am.AssignedOn)         
                .Select(am => new AgentMembershipItemDto
                {
                    MembershipId = am.Id,
                    RepresentativeId= am.MemberShip.UserId,
                    Name = am.MemberShip.Name,
                    Type = am.MemberShip.MemberShipType.Name,
                    Level = EnumExtensions.GetDisplayName(am.MemberShip.TierLevel),
                    Status = am.IsActive  ? "Active" : "Inactive",
                   // Status =  MemberShip.IsActive ? "Active" : "Inactive",
                    Farmers = _context.FarmerCooperatives
                                        .Where(fc => fc.CooperativeId == am.MemberShipId && fc.IsActive)
                                        .Select(fc => fc.FarmerId)
                                        .Distinct()
                                        .Count(),
                    DateRegistered = am.AssignedOn  //is it the date the membership was created or the date it was linked to the agent? For now, using the link date.
                })
                .ToListAsync();

            return new AgentMembershipsResponse
            {
                TotalLinked = totalLinked,
                Memberships = memberships
            };
        }

        public async Task<AgentOrganizationsResponse> GetAgentOrganizationsAsync(string agentUserId)
        {
            // Step 1: AgentUserId -> AgentId
            var agentId = await _context.Agents
                .Where(a => a.UserId == agentUserId)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (agentId == default)
                throw new NotFoundException("Agent not found.");

            // Step 2: link rows + organisation details (raw fields only, no string building in SQL)
            var rows = await _context.OrganizationAgents
                .Where(oa => oa.AgentId == agentId)          // add && oa.IsActive if you soft-delete links
                .OrderByDescending(oa => oa.AssignedOn)
                .Select(oa => new
                {
                    oa.OrganizationId,
                    Code = oa.Organization.OrganizationCode,
                    Name = oa.Organization.CompanyName,
                    AssignmentDate = oa.AssignedOn,
                    State = oa.Organization.State.State,
                    Lga = oa.Organization.LGA.Name,
                    Address = oa.Organization.CompanyAddress
                })
                .ToListAsync();

            // Step 3: coverage + performance per organisation
            var items = new List<AgentOrganizationItemDto>();

            foreach (var r in rows)
            {
                // farmers assigned to THIS agent who belong to THIS organisation
                var farmerIds = _context.FarmerAgents
                    .Where(fa => fa.AgentId == agentId)
                    .Join(_context.FarmerOrganizations.Where(fo => fo.OrganizationCode == r.Code),
                          fa => fa.FarmerId,
                          fo => fo.FarmerId,
                          (fa, fo) => fa.FarmerId)
                    .Distinct();

                var farmers = _context.Farmers.Where(f => farmerIds.Contains(f.Id));

                // Coverage: distinct LGAs among those farmers. Cast to int? and drop nulls,
                // otherwise a NULL LgaId would be counted as one extra "LGA".
                var coverageCount = await farmers
                    .Select(f => f.Lga !=null)
                    .Where(id => id != null)
                    .Distinct()
                    .CountAsync();

                // Region built in memory, skipping empty parts
                var region = string.Join(", ",
      new string?[] { r.Lga?.ToString(), r.State?.ToString(), r.Address?.ToString() }
          .Where(x => !string.IsNullOrWhiteSpace(x)));

                items.Add(new AgentOrganizationItemDto
                {
                    OrganizationId = r.OrganizationId,
                    OrganizationCode = r.Code,
                    Name = r.Name,
                    AssignmentDate = r.AssignmentDate,
                    Region = region,
                    CoverageAreaCount = coverageCount,
                    //CoverageArea = $"{coverageCount} {(coverageCount == 1 ? "LGA" : "LGAs")}",
                    Performance = await BuildPerformanceAsync(farmers)
                });
            }

            return new AgentOrganizationsResponse
            {
                TotalLinked = items.Count,
                //Label = $"Linked Organization ({items.Count})",
               // IsEmpty = items.Count == 0,
                Organizations = items
            };
        }
        private async Task<AgentOrganizationPerformanceDto> BuildPerformanceAsync(IQueryable<Farmer> farmers)
        {
            var total = await farmers.CountAsync();
            if (total == 0)
                return new AgentOrganizationPerformanceDto();   // all zeros / null

            // 1. Verification success rate
            var verified = await farmers.CountAsync(FarmerIsVerified);

            // 2. Average approval time (hours): registration -> OnBoardedOn (verification date)
            var approvals = await farmers
                .Where(f => f.OnBoardedOn != null)
                .Select(f => new { Start = f.User.CreatedOn, End = f.OnBoardedOn })
                .ToListAsync();

            decimal? avgHours = approvals.Count == 0
                ? null
                : (decimal)Math.Round(approvals.Average(a => (a.End - a.Start).TotalHours), 1);

            // 3. Data quality: farmers with all required profile fields filled
            var complete = await farmers.CountAsync(f =>
                f.User.PhoneNumber != null && f.User.PhoneNumber != "" &&
                f.User.FirstName != null && f.User.LastName != null &&
                f.State != null && f.Lga != null &&
                f.User.IsNinVerified);

            // 4. GPS accuracy: farms with captured coordinates
            var farmIds = farmers.Select(f => f.Id);
            var farmsTotal = await _context.Farms.CountAsync(x => farmIds.Contains(x.FarmerId));
            var farmsWithGps = await _context.Farms.CountAsync(x =>
                farmIds.Contains(x.FarmerId) && x.PinnedLatitude != null && x.PinnedLongitude != null);

            // 5. Duplicate records: counted in memory (simpler and works on any database)
            var contacts = await farmers
                .Select(f => new { f.User.PhoneNumber, f.User.NIN })
                .ToListAsync();

            var dupPhones = contacts
                .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
                .GroupBy(c => c.PhoneNumber)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            var dupNins = contacts
                .Where(c => !string.IsNullOrWhiteSpace(c.NIN))
                .GroupBy(c => c.NIN)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            return new AgentOrganizationPerformanceDto
            {
                FarmersOnboarded = total,
                VerificationSuccessRate = Math.Round(verified * 100m / total, 0),
                AverageApprovalTimeHours = avgHours,
                DataQualityScore = Math.Round(complete * 100m / total, 0),
                GpsAccuracy = farmsTotal == 0 ? 0 : Math.Round(farmsWithGps * 100m / farmsTotal, 0),
                DuplicateRecords = dupPhones + dupNins
            };
        }

        public async Task<AgentInputFinanceResponse> GetAgentInputFinanceAsync(
      string agentUserId, int page, int pageSize)
        {
            // Step 1: AgentUserId -> AgentId
            var agentId = await _context.Agents
                .Where(a => a.UserId == agentUserId)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (agentId == default)
                throw new NotFoundException("Agent not found.");

            // Step 2: user ids of the agent's farmers and memberships (the possible subscribers)
            var farmerUserIds = _context.FarmerAgents
                .Where(fa => fa.AgentId == agentId)
                .Select(fa => fa.Farmer.UserId);

            var membershipUserIds = _context.MemberShipAgents
                .Where(am => am.AgentId == agentId)
                .Select(am => am.MemberShip.UserId);

            // Step 3: subscriptions made by those subscribers
            var subs = _context.InputPackageSubscriptions
     .Where(s => s.IsActive
              && FinancedStatuses.Contains(s.Status)
              && (farmerUserIds.Contains(s.SubscriberUserId)
               || membershipUserIds.Contains(s.SubscriberUserId)));

            // Step 4: summary cards
            var packagesFacilitated = await subs.CountAsync();

            // Farmers financed = farmers who subscribed directly
            //                  + farmers inside memberships that subscribed (distinct)
            var directFarmers = subs
                .Where(s => farmerUserIds.Contains(s.SubscriberUserId))
                .Select(s => s.SubscriberUserId);

            var subscribedMembershipUserIds = subs
                .Where(s => membershipUserIds.Contains(s.SubscriberUserId))
                .Select(s => s.SubscriberUserId);

            var membershipFarmers =
                from fc in _context.FarmerCooperatives
                join m in _context.MemberShips on fc.CooperativeId equals m.Id
                join f in _context.Farmers on fc.FarmerId equals f.Id
                where fc.IsActive && subscribedMembershipUserIds.Contains(m.UserId)
                select f.UserId;

            var farmersFinanced = await directFarmers
                .Concat(membershipFarmers)
                .Distinct()
                .CountAsync();

            // Value allocated = approved subscriptions only
            var totalValue = await subs
               .SumAsync(s => (decimal?)s.TotalFinancingValue) ?? 0m;

            var activePackages = await subs.CountAsync(s => ActiveStatuses.Contains(s.Status));
           
            var summary = new AgentInputFinanceSummaryDto
            {
                PackagesFacilitated = packagesFacilitated,
                FarmersFinanced = farmersFinanced,
                TotalValueAllocated = totalValue,
                TotalValueFormatted = FormatNaira(totalValue),
                ActivePackages = activePackages
            };

            // Step 5: table rows
            var rows = await subs
                .OrderByDescending(s => s.ApprovedOn ?? s.SubscribedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    Package = s.InputPackage.Name,
                    Organization = (s.Organization ?? s.InputPackage.Organization).CompanyName,
                    Value = s.TotalFinancingValue,
                    s.Status,
                    s.ApprovedOn,
                    // membership subscriber -> its active farmers; individual farmer -> 1
                    Farmers = membershipUserIds.Contains(s.SubscriberUserId)
                        ? (from fc in _context.FarmerCooperatives
                           join m in _context.MemberShips on fc.CooperativeId equals m.Id
                           where fc.IsActive && m.UserId == s.SubscriberUserId
                           select fc.FarmerId).Count()
                        : 1
                })
                .ToListAsync();

            var items = rows.Select(r => new AgentInputFinancePackageDto
            {
                Package = r.Package,
                Farmers = r.Farmers,
                Organization = r.Organization,
                PackageValue = r.Value,
                PackageValueFormatted = FormatNaira(r.Value),
                Status = ToBadge(r.Status),
                ApprovedDate = r.ApprovedOn
            }).ToList();

            return new AgentInputFinanceResponse
            {
                Summary = summary,
                Packages = new Core.Models.Admin.PagedResult<AgentInputFinancePackageDto>(items, page, pageSize, packagesFacilitated),
               // IsEmpty = packagesFacilitated == 0
            };
        }

        // ₦450K, ₦12.5M, ₦186M, ₦1.2B
        // ₦20K, ₦2.46M, ₦186M, ₦1.2B  (truncates, never rounds up)
        private static string FormatNaira(decimal amount)
        {
            static string Trim(decimal v)
            {
                var truncated = Math.Truncate(v * 100m) / 100m;     // 2 decimals, no rounding
                return truncated.ToString("0.##", CultureInfo.InvariantCulture);
            }

            if (amount >= 1_000_000_000m) return $"₦{Trim(amount / 1_000_000_000m)}B";
            if (amount >= 1_000_000m) return $"₦{Trim(amount / 1_000_000m)}M";
            if (amount >= 1_000m) return $"₦{Trim(amount / 1_000m)}K";
            return $"₦{Trim(amount)}";
        }

        private static readonly SubscriptionStatus[] ActiveStatuses =
{
    SubscriptionStatus.Disbursed,
    SubscriptionStatus.PartiallyDisbursed,
    SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment,
    SubscriptionStatus.Completed,
    SubscriptionStatus.Defaulted
};
        // A subscriber counts as financed once money has actually been released
        private static readonly SubscriptionStatus[] FinancedStatuses =
        {
    SubscriptionStatus.Disbursed,
    SubscriptionStatus.PartiallyDisbursed,
    SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment,
    SubscriptionStatus.Completed,
    SubscriptionStatus.Defaulted
};

        private static readonly SubscriptionStatus[] PendingStatuses =
        {
    SubscriptionStatus.PendingDisbursement,
    // e.g. PendingApproval, PendingCommitment
};

        private static string ToBadge(SubscriptionStatus s) => s switch
        {
            SubscriptionStatus.Completed => "Completed",
            SubscriptionStatus.Defaulted => "Defaulted",
            _ => "Active"     // Disbursed, PartiallyDisbursed, Active, PendingRepayment
        };

        private static readonly Expression<Func<Farmer, bool>> FarmerIsVerified =
         f => f.User.IsPhoneVerified && f.User.IsNinVerified;

        private static readonly Expression<Func<Farmer, bool>> FarmerIsSuspended =
            f => f.FarmerStatus == FarmerStatus.Suspended;


        public async Task<AgentWalletResponse> GetAgentWalletsAsync(string agentUserId, string businessCode)
        {
            // Step 1: all of the agent's wallets for this business
            var wallets = await _context.UserWallets
                .Where(w => w.UserId == agentUserId
                         && w.BelongToBusinessCode == businessCode)
                .OrderBy(w => w.AddedOn)                       // your "date added" column
                .Select(w => new AgentWalletItemDto
                {
                    WalletId = w.Id,
                    WalletName = w.WalletProvider.Name,      // adjust to your provider fields
                    LogoUrl = w.WalletProvider.LogoUrl,
                    CurrentBalance = w.WalletBalance,
                    DateAdded = w.AddedOn
                })
                .ToListAsync();

            if (wallets.Count == 0)
            {
                return new AgentWalletResponse
                {
                    Summary = new AgentWalletSummaryDto(),
                    Wallets = wallets,
                   // IsEmpty = true
                };
            }

            var walletIds = wallets.Select(w => w.WalletId).ToList();

            // Step 2: transaction totals across ALL those wallets, one query
            var totals = await _context.UserWalletTransactions
                .Where(t => t.UserWalletId.HasValue && walletIds.Contains(t.UserWalletId.Value))
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Inflow = g.Sum(x => x.WalletTransactionType == WalletTransactionType.Credit
                                        ? x.TransactionAmount : 0m),
                    Outflow = g.Sum(x => x.WalletTransactionType == WalletTransactionType.Debit
                                         ? x.TransactionAmount : 0m),
                    Pending = g.Sum(x => (x.WalletTransactionStatus == WalletTransactionStatus.DebitPending
                                       || x.WalletTransactionStatus == WalletTransactionStatus.CreditPending)
                                         ? x.TransactionAmount : 0m)
                })
                .FirstOrDefaultAsync();

            // Step 3: summary cards
            var summary = new AgentWalletSummaryDto
            {
                TotalWalletBalance = wallets.Sum(w => w.CurrentBalance),
                TotalInflow = totals?.Inflow ?? 0m,
                TotalOutflows = totals?.Outflow ?? 0m,
                PendingSettlement = totals?.Pending ?? 0m
            };

            return new AgentWalletResponse
            {
                Summary = summary,
                Wallets = wallets,
              //  IsEmpty = false
            };
        }

        public async Task<bool> SuspendAgentAsync(long businessId, string agentUserId, string actingUserId, string? reason)
        {
            // Needs tracking, so don't use the AsNoTracking base query
            var agent = await _context.Agents
                .Include(a => a.User)
                .Where(a => a.UserId == agentUserId &&
                    _context.UserBusinesses.Any(ub =>
                        ub.UserId == a.UserId && ub.BusinessId == businessId && ub.IsActive))
                .FirstOrDefaultAsync();

            if (agent == null || agent.AgentStatus == AgentStatus.Suspended)
                return false;

            agent.AgentStatus = AgentStatus.Suspended;
            agent.User.DateModified = DateTimeOffset.UtcNow;   // drives "Last Updated"
            agent.User.ModifiedBy = actingUserId;


            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ReactivateAgentAsync(long businessId, string agentUserId, string actingUserId, string? reason)
        {
            // Needs tracking, so don't use the AsNoTracking base query
            var agent = await _context.Agents
                .Include(a => a.User)
                .Where(a => a.UserId == agentUserId &&
                    _context.UserBusinesses.Any(ub =>
                        ub.UserId == a.UserId && ub.BusinessId == businessId && ub.IsActive))
                .FirstOrDefaultAsync();

            if (agent == null || agent.AgentStatus == AgentStatus.Active)
                return false;

            agent.AgentStatus = AgentStatus.Active;
            agent.User.DateModified = DateTimeOffset.UtcNow;   // drives "Last Updated"
            agent.User.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<AgentOverviewResponse> GetAgentOverviewAsync(string agentUserId)
        {
            var a = await _context.Agents
                .Where(x => x.UserId == agentUserId)
                .Select(x => new
                {
                    x.Id,
                    UserId=x.UserId,
                    x.AgentStatus,
                    x.Location,
                    State = x.State,
                    Lga = x.Lga,
                    Ward = x.Ward,
                    x.User.CreatedOn,
                    x.User.DateModified,
                    x.User.FirstName,
                    x.User.MiddleName,
                    x.User.LastName,
                    x.User.Email,
                    Phone = x.User.PhoneNumber,
                    x.User.Gender,
                    x.User.DateOfBirth,
                    Nin = x.User.NIN,
                    x.User.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (a == null)
                throw new NotFoundException("Agent not found.");

            // Channel: self-registered when nobody else onboarded them
            var isSelfRegistered = string.IsNullOrEmpty(a.CreatedBy)
                                || a.CreatedBy == a.UserId;

            string channel;
            string source = null;

            if (isSelfRegistered)
            {
                channel = "Self Registration";
            }
            else
            {
                // who assisted? check organisation, then membership, then any other user
                var orgName = await _context.Organizations
                    .Where(o => o.UserId == a.CreatedBy)
                    .Select(o => o.CompanyName)
                    .FirstOrDefaultAsync();

                if (orgName != null)
                {
                    channel = "Organization Assisted";
                    source = orgName;
                }
                else
                {
                    var membershipName = await _context.MemberShips
                        .Where(m => m.UserId == a.CreatedBy)
                        .Select(m => m.Name)
                        .FirstOrDefaultAsync();

                    var farmerName = await _context.Farmers
                        .Where(f => f.UserId == a.CreatedBy)
                        .Select(f => f.User.FirstName + " " + f.User.LastName)
                        .FirstOrDefaultAsync();

                    if (membershipName != null)
                    {
                        channel = "Membership Assisted";
                        source = membershipName;
                    }
                    else if(farmerName != null)
                    {
                        channel = "Farmer Assisted";
                        source = farmerName;
                    }
                    else
                    {
                        var onboarder = await _context.Users
                            .Where(u => u.Id == a.CreatedBy)
                            .Select(u => new { u.FirstName, u.LastName })
                            .FirstOrDefaultAsync();

                        channel = "Assisted";
                        source = onboarder == null
                            ? null
                            : $"{onboarder.FirstName} {onboarder.LastName}".Trim();
                    }
                }
            }

            // Header organisation: first organisation the agent was assigned to
            var organization = await _context.OrganizationAgents
                .Where(oa => oa.AgentId == a.Id)
                .OrderBy(oa => oa.AssignedOn)
                .Select(oa => oa.Organization.CompanyName)
                .FirstOrDefaultAsync();

            var fullName = string.Join(" ",
                new[] { a.FirstName, a.MiddleName, a.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            var status =EnumExtensions.GetDisplayName(a.AgentStatus);
            var lastUpdated = a.DateModified ?? a.CreatedOn;

            return new AgentOverviewResponse
            {
                PersonalInformation = new AgentPersonalInfoDto
                {
                    FullName = fullName,
                    Email = a.Email,
                    PhoneNumber = a.Phone,
                    Gender = a.Gender,
                    DateOfBirth = a.DateOfBirth,
                    NIN = a.Nin,
                    Address = a.Location,
                    State = a.State,
                    Lga = a.Lga,
                    Ward = a.Ward,
                    AgentUserId = a.UserId,
                    Status = status,
                    CanSuspend = a.AgentStatus != AgentStatus.Suspended,
                   // Organization = organization,
                    RegistrationDate = a.CreatedOn
                },
                RegistrationInformation = new AgentRegistrationInfoDto
                {
                    RegistrationDate = a.CreatedOn,
                    RegistrationChannel = channel,
                    RegistrationSource = source,
                    Status = status,
                    LastUpdated = lastUpdated
                }
            };
        }
    }
}
