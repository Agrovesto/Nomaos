using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IMembershipManager
    {
        Task<CreateMembershipResponseModel> CreateFarmerAsync(CreateFarmerRequest model, OnBoardingArgs args);
        Task<AgentOverviewDto> GetAgentOverview(ListQueryParams args);
        Task<PagedListResponse<List<AgentMembershipDto>>> GetAgentsByMembership(ListQueryParams query);

        //Task<MembershipDashboardDto> GetMembershipDashboardData(ListQueryParams query);
        Task<MembershipDashboardDto> GetMembershipDashboardAsync(ListQueryParams query);
        Task<PagedListResponse<List<FarmerMembershipDto>>> GetMembershipFarmers(ListQueryParams query);
    }
    public class MembershipManager : IMembershipManager
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly INotificationService _sMSService;
        private readonly IOtpService _otpService;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly ILogger<AgentManager> _log;
        private readonly IAWSRekognitionManager _faceManager;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IBusinessRepository _businessRepository;

        private readonly IWalletManager _walletManager;
        public MembershipManager(IFarmerRepository farmerRepository,
            IActivityLogRepository activityLogRepository,
            IMembershipRepository membershipRepository,
            IBusinessRepository businessRepository,
            IOtpService otpService,
            IWalletManager walletManager,
            INotificationService notificationService,
            IAWSRekognitionManager faceManager,
            ILogger<AgentManager> log, IOnboardingRepository onboardingRepository)
        {
            _activityLogRepository = activityLogRepository;
            _walletManager = walletManager;
            _businessRepository = businessRepository;
            _farmerRepository = farmerRepository;
            _faceManager = faceManager;
            _log = log;
            _onboardingRepository = onboardingRepository;
            _membershipRepository = membershipRepository;
            _sMSService = notificationService;
        }
        private static void ValidateQuickOnboarding(OnboardingRequestModel model)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(model.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(model.PhoneNumber);

            if (!hasEmail && !hasPhone)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Either email or phone number must be provided.");
            }
            if (hasEmail)
            {
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(model.Email))
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Invalid email address format.");
                }
            }
            if (hasPhone)
            {
                if (!Regex.IsMatch(model.PhoneNumber!, @"^\d{11}$"))
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Phone number must be exactly 11 digits and numeric only.");
                }

                if (!string.IsNullOrWhiteSpace(model.Pin)
    && !Regex.IsMatch(model.Pin, @"^\d{4}$"))
                {
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "PIN must be exactly 4 digits."
                    );
                }

            }
        }

        private void ValidateCriticalInputs(CreateFarmerRequest model, OnBoardingArgs args)
        {
            ValidateQuickOnboarding(new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            });

            if (string.IsNullOrEmpty(args.businessCode))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");

        }
        private async Task<BusinessModel> GetBusinessOrFail(string businessCode)
        {
            return await _businessRepository.GetBusinessByCodeAsync(businessCode)
                ?? throw new BaseException(ExceptionType.NOTFOUND, "Business does not exist");
        }
        private async Task<(OnboardingRequestModel model, bool status, string message)> RegisterFarmerOrFail(
          CreateFarmerRequest model,
          OnBoardingArgs args)
        {
            (OnboardingRequestModel model, bool status, string message) result = await _onboardingRepository.RegisterUser(
                new OnboardingRequestModel
                {
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password,
                    Pin = model.Pin,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName
                }, args);

            if (!result.status)
                throw new BaseException(ExceptionType.BAD_REQUEST, result.message);

            return result;
        }
        private static void ValidateIdentifier(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (value.Length != 11)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    $"{fieldName} must be exactly 11 digits."
                );

            if (!value.All(char.IsDigit))
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    $"{fieldName} must contain only numbers."
                );
        }

        private async Task<(bool status, FarmerModel data)> CreateFarmerOrFails(
CreateFarmerRequest model,
OnBoardingArgs args,
BusinessModel business,
string userId,
DateTimeOffset dateTime)
        {
            var farmer = new FarmerModel
            {
                OnboardedByUserId = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Email = model.Email,
                Gender = model.Gender,
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name =  $"{model.FirstName} {model.MiddleName} {model.LastName}",
                OnBoardedOn = DateTimeOffset.Now,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                DateOfBirth = model.DateOfBirth,
                CooperativeIds = args.CooperativeIds?
  .Where(x => x.HasValue)
  .Select(x => x.Value)
  .ToList() ?? new List<long>(),

                AgentIds = model.AgentIds?
  .Where(x => x.HasValue)
  .Select(x => x.Value)
  .ToList() ?? new List<long>(),

                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,
                Level= FarmerLevel.Level1.ToString(),
                OnboardedById = model.OnboardedByUserId,
                HighestLevelOfEducation = model.HighestLevelOfEducation,
                
            };

            if (!string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
            {
                ValidateIdentifier(model.NIN, "NIN");
                ValidateIdentifier(model.Bvn, "BVN");

                if (!string.IsNullOrWhiteSpace(model.NIN))
                    model.NIN = model.NIN;

                if (!string.IsNullOrWhiteSpace(model.Bvn))
                    model.Bvn = model.Bvn;

                await _onboardingRepository.OnNINBVNUpdate(new Core.Models.Common.UserModel
                {
                    Id= userId,
                    Bvn=model.Bvn,
                    NIN=model.NIN,
                });
                farmer.Level = FarmerLevel.Level3.ToString();
                farmer.BVN = model.Bvn;
                farmer.NIN= model.NIN;
            }
         
          

            var farer = await _farmerRepository.CreateFarmer(farmer);
            return farer;
        }
        private async Task LogActivity(OnboardingRequestModel model, OnBoardingArgs args)
        {
            var subAccount =
                                    await _activityLogRepository.OnLogActivtyAsync(new Entities.Common.ActivityLog
                                    {
                                        PerformedByUserId = args.UserId,
                                        Description = $"{model.LogDescription}",
                                        ActivityName= $"{model.ActivityName}",
                                        ActivityType= Entities.Common.ActivityType.Onboarding,
                                        BusinessCode=args.businessCode,
                                        OrganizationCode=args.organizationCode,
                                        CreatedOn= DateTimeOffset.Now,
                                        
                                    });
        }
        private async Task EnsureWalletExists(OnboardingRequestModel model, BusinessModel business)
        {
            var subAccount =
                                    await _walletManager.GetSubAccountByUserId(model.Id)
                                    ?? await _walletManager.CreateSubAccount(new SubAccountModel
                                    {
                                        UserId = model.Id,
                                        Email = model.Email,
                                        PhoneNumber = model.PhoneNumber,
                                        BusinessId = business.BusinessId,
                                        CreatedBy = model.Id,
                                    });
        }

        private async Task GenerateAndSendOtp(OnboardingRequestModel model)
        {
            var otpTarget = !string.IsNullOrWhiteSpace(model.Email)
                ? OtpTarget.Email
                : OtpTarget.Phone;

            var destination = otpTarget == OtpTarget.Email
                ? model.Email
                : model.PhoneNumber;

            await _otpService.InvalidateActiveOtps(
                model.Id,
                otpTarget);

            var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = model.Id,
                Target = otpTarget,
                Destination = destination,
                Purpose = OtpPurpose.Onboarding,
                ExpiryMinutes = 5
            });

            // ------------------ Send OTP ------------------
            if (otpTarget == OtpTarget.Email)
            {
                await _sMSService.SendEmailAsync(
                    destination,
                    "Confirm your account",
                    $"Your verification code is {otp.Code}. It expires in 5 minutes.");
            }
        }

        private async Task RunNonCriticalSteps(
 CreateFarmerRequest model,
 OnBoardingArgs args,
 BusinessModel business,
OnboardingRequestModel user,
 DateTimeOffset dateTime)
        {
            try
            {
                await LogActivity(user, args);
                await EnsureWalletExists(user, business);
                     await _onboardingRepository.TrackUserRoleAssignment(user.Id, args.userType);
                await GenerateAndSendOtp(user);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex,
                    $"Non-critical onboarding step failed for user {user.Id}. " +
                    "User can continue and complete setup later.");
            }
        }
        public async Task<CreateMembershipResponseModel> CreateFarmerAsync(
     CreateFarmerRequest model,
     OnBoardingArgs args)
        {
            try
            {
                ValidateCriticalInputs(model, args);

                var dateTime = DateTimeOffset.UtcNow;

                var business = await GetBusinessOrFail(args.businessCode);

                var user = await RegisterFarmerOrFail(model, args);

                var farmer = await CreateFarmerOrFails(
                    model, args, business, user.model.Id, dateTime);

                model.Id = farmer.data.Id;
                user.model.LogDescription = $"Added  Farmer {model.FirstName} {model.MiddleName} {model.LastName}";
                user.model.ActivityName = $"New Farmer Added";
                await RunNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateMembershipResponseModel
                {
                    UserId = user.model.Id,
                    MembershipId = farmer.data.Id,
                    RequiresProfileCompletion = true
                };
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateMembershipUser failed unexpectedly");
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete onboarding at the moment.");
            }
        }
        public async Task<MembershipDashboardDto> GetMembershipDashboardAsync(ListQueryParams query)
        {
           
            var profile = await _membershipRepository.GetMembershipDetailByUserId(query.UserId);
            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            return await _membershipRepository.GetMembershipDashboardAsync(profile.Id);
        }
        public async Task<PagedListResponse<List<FarmerMembershipDto>>> GetMembershipFarmers(ListQueryParams query)
        {
            var profile = await _membershipRepository.GetMembershipDetailByUserId(query.UserId);
            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }
            // Farmers linked to this membership
            return await _farmerRepository.GetFarmersByMembership(profile.Id, query);

        }
        public async Task<PagedListResponse<List<AgentMembershipDto>>> GetAgentsByMembership(ListQueryParams query)
        {
            var profile = await _membershipRepository.GetMembershipDetailByUserId(query.UserId);
            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }
            // Agents linked to this membership
            return await _membershipRepository.GetAgentsByMembership(profile.Id, query);

        }
        public async Task<AgentOverviewDto> GetAgentOverview(ListQueryParams args) 
        { 
        
            // Agents linked to this membership
            return await _membershipRepository.GetAgentOverview(args);

        }
    }
}
