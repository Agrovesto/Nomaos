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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        Task<PagedResult<AgentListItemDto>> GetAgentsAsync(AgentListQuery p);
        Task<List<AgentListItemDto>> GetAllForExportAsync(AgentListQuery p);
        Task<AgentSummaryDto> GetSummaryAsync(long businessId);
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
        public AdminAgentManager(DataContext context, ILogger<AdminAgentManager> log, IBusinessRepository businessRepository, IAdminAuditLogger audit,
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

        public async Task<PagedResult<AgentListItemDto>> GetAgentsAsync(AgentListQuery p)
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
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.AgentId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);


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

            return new PagedResult<AgentListItemDto>(items,  page, size, total);
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

    }
}
