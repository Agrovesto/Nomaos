using AfricasTalkingCS;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Operation.Valid;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOnBoardingManager
    {
        Task<bool> ConfirmOtp(ConfirmOtpRequest request);
        Task<OnboardingRequestModel> CreateBusinessUser(OnboardingRequestModel model, OnBoardingArgs args);
        Task<(bool status, string message)> CreateUserProfile(ProfileRequestModel model, OnBoardingArgs args);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        Task ResendOnboardingOtp(ResendOtpRequest model);
    }
    public class OnBoardingManager : IOnBoardingManager
    {
        private readonly IWalletManager _walletManager;
        private readonly INotificationService _sMSService;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly ICooperativeRepository _cooperativeRepository;
        private readonly IOtpService _otpService;
        private readonly ILogger<OnBoardingManager> _log;
        public OnBoardingManager(IWalletManager walletManager, IBusinessRepository businessRepository, IFarmerRepository farmerRepository, ILogger<OnBoardingManager> log, ICooperativeRepository cooperativeRepository,
            IAgentRepository agentRepository, IOtpService otpService, INotificationService sMSService, IOnboardingRepository onboardingRepository)
        {
            _onboardingRepository = onboardingRepository;
            _farmerRepository = farmerRepository;
            _agentRepository = agentRepository;
            _cooperativeRepository = cooperativeRepository;
            _log = log;
            _businessRepository = businessRepository;
            _sMSService = sMSService;
            _walletManager = walletManager;
            _otpService = otpService;
        }

        public async Task<(bool Status, string Message, TokenModel Response)> Login(
     LoginModel model,
     string businessCode
            //,
            // string header
            )
        {
            return await _onboardingRepository.Login(model, businessCode);
        }
        private static void ValidateQuickOnboarding(OnboardingRequestModel model)
        {
            // ---- Phone validation ----
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                if (model.PhoneNumber.Length != 11)
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Phone number must be exactly 11 digits.");

                if (string.IsNullOrWhiteSpace(model.Pin))
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Registration with phone number must include a PIN.");
            }

            // ---- Email validation ----
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                if (string.IsNullOrWhiteSpace(model.Password))
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Password is required when registering with email.");

                if (model.Password.Length < 6)
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Password must be at least 6 characters long.");
            }

            // ---- Ensure at least one onboarding method is used ----
            if (string.IsNullOrWhiteSpace(model.Email) &&
                string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Either email or phone number must be provided.");
            }
        }
        public async Task<(bool status, string message)> CreateUserProfile(ProfileRequestModel model, OnBoardingArgs args)
        {
            try
            { // fetch user by id
                if (string.IsNullOrEmpty(args.UserId))
                {
                    throw new BaseException(
                               Core.Enums.ExceptionType.BAD_REQUEST,
                               "User Id missing.");
                }
                var user = await _onboardingRepository.GetUserById(args.UserId);
                if (user.model == null)
                {
                    throw new BaseException(
                           Core.Enums.ExceptionType.BAD_REQUEST,
                           "User not found.");
                }
                model.CreatedOn = user.model.CreatedOn;
                model.Id = args.UserId;
                args.userType = !string.IsNullOrEmpty(args.userType) ? args.userType : user.model.UserType;
                await CreateUserCategory(model, args);
                //check if user is farmer,
                //if true, check model for compulsory fields
                //create user as farmer

                return (true, "User Profile Created");
            }

            catch (Exception ex) {
                throw new BaseException(Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR, "An error occurred");
            }

        }
        private async Task CreateUserCategory(ProfileRequestModel model, OnBoardingArgs args )
        {
            
                var accountType = args.userType.ToEnum<UserType>();
            
            switch (accountType)
            {
                case UserType.Trader:
                    //model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                //    await CreateTraderProfile(model, args);
                    break;
                case UserType.Agent:
                    await CreateAgentProfileAsync(model,args);

                    break;

                case UserType.Cooperative:
                    //create first as cooperative
                     await CreateCooperativeProfileAsync(model, args);                   
                     break;

                case UserType.Farmer:
                    
                    await CreateFarmerProfileAsync(model, args);
                    break;
                case UserType.Organization:
                    break;
                    case UserType.Processors:
                    break;
                    case UserType.Suppliers: 
                    break;
            }

        }
        //private async Task CreateTraderProfile(UserModel model, BusinessModel business, string userId)
        //{
        //    var trader = new TraderModel
        //    {
        //        BelongToBusinessCode = business.BusinessCode,
        //        State = model.State,
        //        Lga = model.Lga,
        //        Email = model.Email,
        //        Location = model.Location,
        //        Name = model.Name,
        //        OnBoardedOn = DateTimeOffset.UtcNow,
        //        PhoneNumber = model.PhoneNumber,
        //        UserId = userId,

        //    };

        //    await _traderRepository.CreateTrader(trader);
        //}

        private async Task CreateAgentProfileAsync(ProfileRequestModel model,  OnBoardingArgs args)
        {

            await _agentRepository.CreateAgent(new AgentModel
            {
                BelongToBusinessCode = args.businessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                Name = model.FullName,
                OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                Ward = model.Ward,
                OnboardedById=model.OnboardedByUserId,
                OrganizationCode = args.organizationCode
            });
        }

        private async Task CreateCooperativeProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
        {

             await _cooperativeRepository.CreateCooperative(new CooperativeModel
             {
                 BelongToBusinessCode = args.businessCode,
                 State = model.State,
                 Lga = model.Lga,
                 ContactPerson = model.FirstName,//string.Empty,
                 Email = model.Email,
                 Location = model.Location,
                 Name = model.FullName,
                 OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                 PhoneNumber = model.PhoneNumber,
                 UserId = model.Id,
                 OnboardedById=model.OnboardedByUserId,
                // IsAssignedToAgentId = userId,
                 //HasbeenAssignedAgent = isAssignedagent,
                 Ward = model.Ward,
                 OrganizationCode = args.organizationCode

             });
        }


        private async Task CreateFarmerProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
        {
            var farmer = new FarmerModel
            {
                OnboardedByUserId = args.UserId,
                BelongToBusinessCode = args.businessCode,
                Email = model.Email,
                Gender = model.Gender,
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name = !string.IsNullOrEmpty(model.FullName) ? model.FullName : $"{model.FirstName} {model.MiddleName} {model.LastName}",
                OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                DateOfBirth = model.DateOfBirth,
                BelongToCooperativeId = args.Id,
                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,
                OnboardedById=model.OnboardedByUserId,
                HighestLevelOfEducation = model.HighestLevelOfEducation,
                // ValueChainItems= model.ValueChainSelections
            };

            var farer = await _farmerRepository.CreateFarmer(farmer);


        }

        public async Task<OnboardingRequestModel> CreateBusinessUser(OnboardingRequestModel model, OnBoardingArgs args)
        {
            try { 
            ValidateQuickOnboarding(model);

            if (string.IsNullOrEmpty(args.businessCode))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Business code is required.");

            // ------------------ Fetch business ------------------
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "Business does not exist.");

            // ------------------ Register or attach user ------------------
            var createUser = await _onboardingRepository.RegisterUser(model, args);

            if (!createUser.status)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    createUser.message);

            // ------------------ Wallet / Sub-account ------------------
            var subAccount =
                await _walletManager.GetSubAccountByUserId(createUser.model.Id)
                ?? await _walletManager.CreateSubAccount(new SubAccountModel
                {
                    UserId = createUser.model.Id,
                    Email = createUser.model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BusinessId = business.BusinessId,
                    CreatedBy = createUser.model.Id
                });

            // ------------------ Track role & user type ------------------
            await _onboardingRepository.TrackUserRoleAssignment(
                createUser.model.Id,
                args.userType);

            // ------------------ OTP Generation ------------------
            var otpTarget = !string.IsNullOrWhiteSpace(model.Email)
                ? OtpTarget.Email
                : OtpTarget.Phone;

            var destination = otpTarget == OtpTarget.Email
                ? model.Email
                : model.PhoneNumber;

            // Invalidate any existing OTPs first
            await _otpService.InvalidateActiveOtps(
                createUser.model.Id,
                otpTarget);

            // Generate OTP
            var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = createUser.model.Id,
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
            else
            {
                await _sMSService.SendSmsAsync(
                    destination,
                    $"Your verification code is {otp.Code}. Expires in 5 minutes.");
            }
            model.Id = createUser.model.Id;
            return model;
            }
            catch(Exception ex)
            {
                _log.LogError($"User Registration failed with error: {ex.Message}");
                throw new BaseException(Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR, "An error occured");

            }
        }
        public async Task<bool> ConfirmOtp(ConfirmOtpRequest request)
        {
            var isValid= await _otpService.VerifyOtpAsync(
                request.UserId,
                request.Code,
                OtpPurpose.Onboarding);
            if (!isValid)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Invalid or expired OTP.");

            await _onboardingRepository.ConfirmUserEmailOrPassword(request.UserId);
            return isValid;
        }
        public async Task ConfirmOnboardingOtp(string userId, string code)
        {
            var isValid = await _otpService.VerifyOtpAsync(
                userId,
                code,
                OtpPurpose.Onboarding);

            if (!isValid)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Invalid or expired OTP.");

            await _onboardingRepository.ConfirmUserEmailOrPassword(userId);
        }
        public async Task ResendOnboardingOtp(ResendOtpRequest model)
        {
            try
            {
                var otp = await ResendOtpAsync(model.UserId, model.OtpTarget, OtpPurpose.Onboarding);
                // ------------------ Send OTP ------------------
                if (model.OtpTarget == OtpTarget.Email)
                {
                    await _sMSService.SendEmailAsync(
                        otp.Destination,
                        "Confirm your account",
                        $"Your verification code is {otp.Code}. It expires in 5 minutes.");
                }
                else
                {
                    await _sMSService.SendSmsAsync(
                        otp.Destination,
                        $"Your verification code is {otp.Code}. Expires in 5 minutes.");
                }
            }
           catch(Exception ex)
            {
                throw new BaseException(exceptionType: Core.Enums.ExceptionType.BAD_REQUEST, ex.Message);
            }
        }
        public async Task<OtpResult> ResendOtpAsync(string userId, OtpTarget target, OtpPurpose purpose)
        {
            var user = await _onboardingRepository.GetUserById(userId);
            await _otpService.InvalidateActiveOtps(userId, target);
            var destination = "";
            if(target== OtpTarget.Email)
            {
                destination = user.model.Email;
            }
            else
            {
                destination = user.model.PhoneNumber;
            }
           var otp= await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = userId,
                Target = target,
                Destination=destination,
                Purpose = purpose,
                ExpiryMinutes = 5
            });

            return otp;
        }

        private static string GenerateEmail(string fullName, string domain)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException("Invalid input provided.");
            }
            domain = ExtractDomain(domain);
            fullName = Regex.Replace(fullName.Trim(), @"[^a-zA-Z\s]", "");

            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                throw new ArgumentException("Name must contain at least one word.");
            }

            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "user";

            firstName = firstName.ToLower();
            lastName = lastName.ToLower();

            return $"{firstName}.{lastName}@{domain}";
        }

        private static string ExtractDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return string.Empty;
            }
            string[] parts = email.Split('@');
            return parts.Length == 2 ? parts[1] : string.Empty;
        }

        private string ConvertedDate(string date)
        {
            string[] dateFormats = {
            "dd-MMM-yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd",
            "MM/dd/yyyy", "MMM dd, yyyy", "yyyy/MM/dd"
        };

            string dateOfBirth = date; // Example input, change to test other formats

            if (DateTime.TryParseExact(dateOfBirth, dateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                string formattedDate = parsedDate.ToString("yyyy-MM-dd");
                return formattedDate;
            }
            else
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Invalid date of birth");
            }
        }

        private static void SendWorkerMail(BusinessWorkerModel bWorker, BusinessRegistrationArgs args, BusinessModel business)
        {
            var propagationUrl = AppSettingsHelper.Setting("propagation:url");
            var mailRequest = new WelcomeMailBody
            {
                To = bWorker.Email,
                ToName = $"{bWorker.FullName}",
                Subject = $"Welcome OnBoard {business.BusinessName}",
                From = business.BusinessEmail,
                FromName = business.BusinessName,
                FullName = bWorker.FullName,
                Platform = business.BusinessName,
                Url = business.PropagateUrl,
                DefaultPassword = args.DefaultPassword,
                BusinessName = business.BusinessName,
                PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={business.BusinessCodeSignageSalt}",
                logincred = $"login?bsCd={business.BusinessCode}&signage={business.BusinessCodeSignageSalt}&pwd={args.DefaultPassword}"
            };
            var mail = SendGridService.DynamicBusinessStaffOnboarding(mailRequest);
        }
        private async Task SendOnboardingSMS(string businessName, string phoneNumber)
        {
            var messageBody = $"A new business {businessName} has been added to your merchant list for Farmer Management Platform." +
                         $"You can always reach out to the business using {phoneNumber} ";

            var sms = await _sMSService.HandleWhisperSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { "09061562512" },
                message = messageBody,
            });
            var sms1 = await _sMSService.HandleAfricasTalkingSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { $"+2349061562512" },
                message = messageBody,
            });
            var sms2 = await _sMSService.HandleTermiiSMS(new TermiiSmsRequest
            {
                to = $"2349061562512",
                sms = messageBody
            });
        }
    }

}
