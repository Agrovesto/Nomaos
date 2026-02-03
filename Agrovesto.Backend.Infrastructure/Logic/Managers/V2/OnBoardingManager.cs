using AfricasTalkingCS;
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using CsvHelper;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NetTopologySuite.Operation.Valid;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOnBoardingManager
    {
        Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args);
        Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args);
        Task<bool> ConfirmOtp(ConfirmOtpRequest request);
        Task<OnboardingResponseModel> CreateBusinessUser(OnboardingRequestModel model, OnBoardingArgs args);
        Task<(bool status, string message)> CreateUserProfile(ProfileRequestModel model, OnBoardingArgs args);
        Task<FarmerProfileDto> GetFarmerProfileAsync(string userId);
        // Backwards-compatible wrapper
        Task<FarmerProfileDto> GetFarmerProfile(string userId);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        Task<bool> RemoveFarmerFromCooperative(string userId, long cooperativeId);
        Task ResendOnboardingOtp(ResendOtpRequest model);
        Task<ImageUploadResponse> SaveFarmerImage(ProfileImageModel model, OnBoardingArgs args);
        Task<CreateFarmDto> UpdateFarmDetails(CreateFarmDto model, BusinessRegistrationArgs args);
        Task<bool> VerifyNIN(OnBoardingArgs args, string nin, string CurrentUserId);
        Task<Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto> UpdateFarmerProfile(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update);
        Task<BulkOnboardingResult> BulkOnboardBusinessUsers(Stream excelStream, OnBoardingArgs args);
        Task<UsersProfileResponse> GetUserProfileAsync(string userId);
        Task<CreateMembershipResponseModel> CreateMembershipUser(MemberShipRequestModel model, OnBoardingArgs args);
    }
    public class OnBoardingManager : IOnBoardingManager
    {
        private readonly IFaceManager _faceManager;

        private readonly IWalletManager _walletManager;
        private readonly INotificationService _sMSService;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly IMembershipRepository _cooperativeRepository;
        private readonly IOtpService _otpService;
        private readonly ILogger<OnBoardingManager> _log;
        private readonly IFarmManager _farmManager;
        private readonly IIdentityVerificationManager _identityVerificationManager;
        private readonly IExcelConverterService _excelConverterService;
        public OnBoardingManager(IWalletManager walletManager,
            IFaceManager faceManager,
            IIdentityVerificationManager identityVerificationManager,
            IBusinessRepository businessRepository, IFarmerRepository farmerRepository, ILogger<OnBoardingManager> log, IMembershipRepository cooperativeRepository,
            IAgentRepository agentRepository, IOtpService otpService, IFarmManager farmManager, INotificationService sMSService, IOnboardingRepository onboardingRepository
            , IExcelConverterService excelConverterService)
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
            _faceManager = faceManager;
            _farmManager = farmManager;
            _identityVerificationManager = identityVerificationManager;
            _excelConverterService = excelConverterService;
        }
    //    public string EnqueueBulkOnboarding(
    //Stream excelStream,
    //OnBoardingArgs args,
    //bool disableOtp,
    //bool isDryRun)
    //    {
    //        var rows = _excelConverterService.ParseExcel(excelStream);

    //        var jobArgs = new BulkOnboardingJobArgs
    //        {
    //            businessCode = args.businessCode,
    //            Rows = rows,
    //            DisableOtp = disableOtp,
    //            IsDryRun = isDryRun
    //        };

    //        return BackgroundJob.Enqueue<IBulkOnboardingJob>(
    //            job => job.Process(jobArgs));
    //    }

        public async Task<BulkOnboardingResult> BulkOnboardBusinessUsers(
    Stream excelStream,
    OnBoardingArgs args)
        {
            if (excelStream == null)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Excel file is required.");

            var rows = _excelConverterService.ParseExcel(excelStream);

            if (!rows.Any())
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Excel file contains no records.");

            var result = new BulkOnboardingResult
            {
                Total = rows.Count
            };

            int rowNumber = 1; // header = 1

            foreach (var row in rows)
            {
                rowNumber++;

                try
                {
                    var model = new OnboardingRequestModel
                    {
                        
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        Email = row.Email,
                        PhoneNumber = row.PhoneNumber
                    };

                    args.userType = row.Gender;

                    await CreateBusinessUser(model, args);

                    result.Successful++;
                }
                catch (BaseException ex)
                {
                    result.Failed++;

                    result.Errors.Add(new BulkOnboardingError
                    {
                        RowNumber = rowNumber,
                        Email = row.Email,
                        Error = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Bulk onboarding failed on row {Row}", rowNumber);

                    result.Failed++;

                    result.Errors.Add(new BulkOnboardingError
                    {
                        RowNumber = rowNumber,
                        Email = row.Email,
                        Error = "Unexpected error occurred"
                    });
                }
            }

            return result;
        }


        public async Task<Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto> UpdateFarmerProfile(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "User id and update payload are required.");

            // validate identifiers if present
            if (!string.IsNullOrWhiteSpace(update.NIN))
                ValidateIdentifier(update.NIN, "NIN");

            if (!string.IsNullOrWhiteSpace(update.BVN))
                ValidateIdentifier(update.BVN, "BVN");

            var updated = await _farmerRepository.UpdateFarmerProfileAsync(userId, update);
            if (updated == null)
            {
                return new Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto
                {
                    Success = false,
                    Message = "Farmer not found or update failed."
                };
            }

            // Build updated fields list from the update model
            var updatedFields = new List<string>();
            foreach (var prop in typeof(Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel).GetProperties())
            {
                var val = prop.GetValue(update);
                if (val != null)
                {
                    // treat empty strings as not-updated
                    if (val is string s && string.IsNullOrWhiteSpace(s)) continue;
                    updatedFields.Add(prop.Name);
                }
            }

            return new Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto
            {
                Success = true,
                UpdatedFields = updatedFields,
                UpdatedFarmer = updated,
                Message = "Farmer profile updated successfully."
            };
        }
        private static void ValidateLoginModel(LoginModel model)
        {
            switch (model.LoginMethod)
            {
                case LoginType.Password:
                    if (string.IsNullOrWhiteSpace(model.Email))
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Email is required.");

                    if (!new EmailAddressAttribute().IsValid(model.Email))
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid email format.");

                    if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 4)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid password.");
                    break;

                case LoginType.Pin:
                    if (string.IsNullOrWhiteSpace(model.Phonenumber))
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Phone number is required.");

                    if (!Regex.IsMatch(model.Phonenumber, @"^\d{11}$"))
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Phone number must be 11 digits.");

                    if (string.IsNullOrWhiteSpace(model.Pin) || !Regex.IsMatch(model.Pin, @"^\d{4}$"))
                    {
                        throw new BaseException(
                            ExceptionType.BAD_REQUEST,
                            "PIN must be exactly 4 digits."
                        );
                    }

                    break;
            }
        }

        public async Task<(bool Status, string Message, TokenModel Response)> Login(
        LoginModel model,
        string businessCode
            //,
            // string header
            )
        {
            ValidateLoginModel(model);
            return await _onboardingRepository.Login(model, businessCode);
        }

        private static void ValidateQuickOnboarding(OnboardingRequestModel model)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(model.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(model.PhoneNumber);

        // ---- Must choose ONE method ----
        if (!hasEmail && !hasPhone)
        {
            throw new BaseException(
                Core.Enums.ExceptionType.BAD_REQUEST,
                "Either email or phone number must be provided.");
        }

        //if (hasEmail && hasPhone)
        //{
        //    throw new BaseException(
        //        Core.Enums.ExceptionType.BAD_REQUEST,
        //        "Use either email or phone number, not both.");
        //}

        // ================= EMAIL + PASSWORD =================
        if (hasEmail)
        {
            // Email format
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(model.Email))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Invalid email address format.");
            }

            //if (string.IsNullOrWhiteSpace(model.Password))
            //{
            //    throw new BaseException(
            //        Core.Enums.ExceptionType.BAD_REQUEST,
            //        "Password is required when using email.");
            //}

            //if (model.Password.Length < 4)
            //{
            //    throw new BaseException(
            //        Core.Enums.ExceptionType.BAD_REQUEST,
            //        "Password must be alphanumeric and at least 8 characters long.");
            //}
        }

        // ================= PHONE + PIN =================
        if (hasPhone)
        {
            // Must be exactly 11 digits
            if (!Regex.IsMatch(model.PhoneNumber!, @"^\d{11}$"))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Phone number must be exactly 11 digits and numeric only.");
            }

            //if (string.IsNullOrWhiteSpace(model.Pin))
            //{
            //    throw new BaseException(
            //        Core.Enums.ExceptionType.BAD_REQUEST,
            //        "PIN is required when using phone number.");
            //}

               
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
                //var user = await _onboardingRepository.GetUserById(args.UserId);
                //if (user.model == null)
                //{
                //    throw new BaseException(
                //           Core.Enums.ExceptionType.BAD_REQUEST,
                //           "User not found.");
                //}
                var updateUser = await _onboardingRepository.UpdateUserProfileAsync(args.UserId, model);
                model.CreatedOn = updateUser.CreatedOn;
                model.Id = args.UserId;
                model.Email =!string.IsNullOrEmpty(model.Email) ? model.Email :  updateUser.Email;
                model.PhoneNumber =!string.IsNullOrEmpty(model.PhoneNumber) ? model.PhoneNumber :  updateUser.PhoneNumber;

                args.userType = !string.IsNullOrEmpty(args.userType) ? args.userType : updateUser.UserType;
                await CreateUserCategory(model, args);
                //check if user is farmer,
                //if true, check model for compulsory fields
                //create user as farmer
                if (!string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
                {
                    ValidateIdentifier(model.NIN, "NIN");
                    ValidateIdentifier(model.Bvn, "BVN");

                    if (!string.IsNullOrWhiteSpace(model.NIN))
                        updateUser.NIN = model.NIN;

                    if (!string.IsNullOrWhiteSpace(model.Bvn))
                        updateUser.Bvn = model.Bvn;

                    await _onboardingRepository.OnNINBVNUpdate(updateUser);
                }

                return (true, "User Profile Created");
            }

            catch (Exception ex) {
                throw new BaseException(Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR, $"An error occurred {ex.Message}");
            }

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

                case UserType.MemberShip:
                   
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

            await _agentRepository.CreateUpdateAgent(new AgentModel
            {
                BelongToBusinessCode = args.businessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                // Name = model.FullName,
                Name = !string.IsNullOrWhiteSpace(model.FullName)
    ? model.FullName
    : string.Join(" ",
        new[] { model.FirstName, model.MiddleName, model.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))),

                OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                Ward = model.Ward,
                OnboardedById=args.OnboardedByUserId,
                OrganizationCode = args.organizationCode
            });
        }

        public async Task CreateCooperativeProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
        {
            if (string.IsNullOrEmpty(model.CooperativeName)) { 
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Cooperative Name is required.");
            }
            var isAssignedagent = false;
            if (args.OnboardedByUserId != null)
            {
                isAssignedagent = true;
            }
            if (!model.MembershipTypeId.HasValue)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Membership type is required");

            await _cooperativeRepository.CreateUpdateMembership(new MemberShipModel
             {
                 BelongToBusinessCode = args.businessCode,
                 IsLeaderAFarmer=model.IsLeaderAFarmer,
                 State = model.State,
                 Lga = model.Lga,
                 ContactPerson = model.FirstName,//string.Empty,
                 Email = model.Email,
                 Location = model.Location,
                 MembershipLeaderFirstName=model.FirstName,
                 MembershipLeaderGender=model.Gender,
                 MembershipLeaderLastName=model.LastName,
                 MembershipLeaderMiddleName=model.MiddleName,
                 MemberShipTypeId= model.MembershipTypeId.Value,
                 Tier= Tier.Tier1.ToString(),

    //             Name = !string.IsNullOrWhiteSpace(model.FullName)
    //? model.FullName
    //: string.Join(" ",
    //    new[] { model.FirstName, model.MiddleName, model.LastName }
    //        .Where(x => !string.IsNullOrWhiteSpace(x))),
    
                  Name= model.CooperativeName,
                 OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                 PhoneNumber = model.PhoneNumber,
                 UserId = model.Id,
                 OnboardedById=args.OnboardedByUserId,

                 IsAssignedToAgentId = args.OnboardedByUserId,
                 HasbeenAssignedAgent = isAssignedagent,
                 Ward = model.Ward,
                 OrganizationCode = args.organizationCode

             });

            if (model.IsLeaderAFarmer)
            {
                await CreateFarmerProfileAsync(model, args);
            }

        }


        private async Task CreateFarmerProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
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
                Name = !string.IsNullOrEmpty(model.FullName) ? model.FullName : $"{model.FirstName} {model.MiddleName} {model.LastName}",
                OnBoardedOn = model.CreatedOn,//DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                DateOfBirth = model.DateOfBirth,
               CooperativeIds = args.CooperativeIds?
    .Where(x => x.HasValue)
    .Select(x => x.Value)
    .ToList() ?? new List<long>(),

AgentIds = args.AgentIds?
    .Where(x => x.HasValue)
    .Select(x => x.Value)
    .ToList() ?? new List<long>(),

                //BelongToCooperativeId = args.Id,
                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,
                
                OnboardedById=model.OnboardedByUserId,
                HighestLevelOfEducation = model.HighestLevelOfEducation
                // ValueChainItems= model.ValueChainSelections
            };
           
            
            var farer = await _farmerRepository.CreateFarmer(farmer);

            
        }
        
        public async Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args)
        {
            if (!args.CooperativeIds.Any())
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "No Cooperative selected.");
            }
            if (args.Id < 0)
            {
                throw new BaseException(
    Core.Enums.ExceptionType.BAD_REQUEST,
    "Farmer Id is required.");
            }
            ;

            return await _farmerRepository.AssignFarmerToCooperatives(args, args.Id);
        }
        public async Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args)
        {
            if (!args.AgentIds.Any()) { 
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "No Agent selected.");
            }
            if(args.Id < 0)
            {
                                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Farmer Id is required.");
            };

            return await _farmerRepository.AssignFarmerToAgents(args, args.Id);
        }

        public async Task<OnboardingResponseModel> CreateBusinessUser(OnboardingRequestModel model, OnBoardingArgs args)
        {
            try
            {
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

                // Everything after this point is “best effort”
                try
                {
                    // ------------------ Wallet / Sub-account ------------------
                    await EnsureWalletExists(createUser.model, business);

                    // ------------------ Create Profile ------------------
                    var updateProfile = await CreateUserProfile(new ProfileRequestModel
                    {
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        MiddleName = model.MiddleName,
                        Id = createUser.model.Id,
                        PhoneNumber = model.PhoneNumber,
                    }, args);

                    // ------------------ Track role & user type ------------------
                    await _onboardingRepository.TrackUserRoleAssignment(
                        createUser.model.Id,
                        args.userType);

                    // ------------------ OTP Generation ------------------
                    await GenerateAndSendOtp(createUser.model);
                }
                catch (Exception ex)
                {
                    // Log as info/warning, do NOT throw
                    _log.LogWarning(ex, $"Non-critical onboarding step failed for user {createUser.model.Id}. User may need to update profile manually.");
                }

                model.Id = createUser.model.Id;

                return new OnboardingResponseModel
                {
                    Id = model.Id,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Message = "User created successfully. Some post-registration steps may need manual completion."
                };
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "User Registration failed unexpectedly");
                throw new BaseException(
                    Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR,
                    "An unexpected error occurred. Please try again.");
            }
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

        /**public async Task<OnboardingResponseModel> CreateBusinessUser(OnboardingRequestModel model, OnBoardingArgs args)
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
                    CreatedBy = createUser.model.Id,
                    
                });
              
                var updateProfile = await CreateUserProfile(new ProfileRequestModel {
                    
                    Email = model.Email,
                   FirstName= model.FirstName,
                   LastName= model.LastName,
                   MiddleName=model.MiddleName,
               Id= createUser.model.Id,
               PhoneNumber= model.PhoneNumber,
                }, args);
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
            //else
            //{
            //    await _sMSService.SendSmsAsync(
            //        destination,
            //        $"Your verification code is {otp.Code}. Expires in 5 minutes.");
            //}
            model.Id = createUser.model.Id;
            return new OnboardingResponseModel { 
                Id = model.Id,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
            };
            }
            catch (BaseException)
            {
                // Let known business errors bubble up untouched
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "User Registration failed unexpectedly");

                throw new BaseException(
                    Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR,
                    "An unexpected error occurred. Please try again.");
            }

        }
        
        **/
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
        public async Task<FarmerProfileDto> GetFarmerProfileAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");

                var profile = await _farmerRepository.GetFarmerProfileAsync(userId);

                if (profile == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Farmer profile not found.");

                return profile;
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error fetching farmer profile for {UserId}", userId);
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR, "An unexpected error occurred while fetching farmer profile.");
            }
        }

        public async Task<FarmerProfileDto> GetFarmerProfile(string userId)
        {
            var farmer= await GetFarmerProfileAsync(userId);

            if (farmer !=null &&  farmer.PersonalInformation!=null  && !string.IsNullOrEmpty(farmer.PersonalInformation.FarmerImage))
            {
                var image = farmer.PersonalInformation.FarmerImage;
                farmer.PersonalInformation.FarmerImage = await _faceManager.GetImageUrlAsync(image);
            }

            return farmer;
        }
        public async Task<UsersProfileResponse> GetUserProfileAsync(string userId)
        {
            var profile = await _onboardingRepository.GetUserProfile(userId);
            if (profile == null)
                throw new BaseException(ExceptionType.NOTFOUND, "User profile not found");

            return profile;
        }




        public async Task<bool> RemoveFarmerFromCooperative(string userId, long cooperativeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");
                var getFarmer = await _farmerRepository.GetFarmerByUserId(userId);
                if (getFarmer == null) { throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required."); }

                var profile = await _farmerRepository.RemoveFarmerFromCooperative(getFarmer.Id, cooperativeId);

                if (profile == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Remove Farmer failed.");

                return profile;
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error fetching removing farmer from cooperative", userId);
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR, "An unexpected error occurred while fetching farmer profile.");
            }
        }


        public async Task<bool> VerifyNIN(OnBoardingArgs args, string nin, string CurrentUserId)
        {
            var user = await _onboardingRepository.GetUserById(args.UserId);
            if (user.model == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "User not found.");
            }
            var verifyNin = await _identityVerificationManager.VerifyNIN(new NINDataModel
            {
                number = nin
            });
            if (!verifyNin.status)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    verifyNin.detail);
            }
            user.model.NIN = nin;
            var kyc = verifyNin.nin_data.ToEntity();
            kyc.VerifiedBy = CurrentUserId;
            kyc.UserId = args.UserId;
            kyc.LoggedOn= DateTimeOffset.UtcNow; 
            await _onboardingRepository.OnCreateKYC(kyc);
            await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level3);
            return true;
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


        public async Task<ImageUploadResponse> SaveFarmerImage(ProfileImageModel model, OnBoardingArgs args)
        {
            var saveFace = new ImageUploadResponse();
                try
            {
                saveFace = await _faceManager.UploadImage(model.File, args.UserId, args.businessCode, args.organizationCode);

                if (saveFace != null && !string.IsNullOrEmpty(saveFace.ImageKey)) 
                {
                    var updateLevel = await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level2);
                }
                return saveFace;
            }
            catch (Exception ex)
            {
                //await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
            // });
            //return saveFace;
        }
        public async Task<CreateFarmDto> UpdateFarmDetails(CreateFarmDto model, BusinessRegistrationArgs args)
        {
           
            try
            {
                var updateDetail = await _farmManager.CreateFarmAsync(model, args);

                if (updateDetail != null && (updateDetail.Id > 0))
                {
                    var updateLevel = await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level4);
                }
                return updateDetail;
            }
            catch (Exception ex)
            {
                //await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
            // });
            //return saveFace;
        }

        ///
        ///)
        ///Critical(must succeed or rollback)

        ///These define identity & access:

        //Validate onboarding

        //Fetch business

        //Register user(Users table)

        //Create membership(uses UserId)
        ////
        ///
        public async Task<CreateMembershipResponseModel> CreateMembershipUser(
      MemberShipRequestModel model,
      OnBoardingArgs args)
        {
            try
            {
                ValidateCriticalInputs(model, args);

                var dateTime = DateTimeOffset.UtcNow;

                var business = await GetBusinessOrFail(args.businessCode);

                var user = await RegisterMembershipLeaderOrFail(model, args);

                var membership = await CreateMembershipOrFail(
                    model, args, business, user.model.Id, dateTime);

                model.Id = membership.data.Id;
                // Everything below is non-critical
                await RunNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateMembershipResponseModel
                {
                    UserId = user.model.Id,
                    MembershipId = membership.data.Id,
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
        private void ValidateCriticalInputs(MemberShipRequestModel model, OnBoardingArgs args)
        {
            ValidateQuickOnboarding(new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            });

            if (string.IsNullOrEmpty(args.businessCode))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");

            if (string.IsNullOrEmpty(model.Name))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Cooperative name is required");

            if (model.MemberShipTypeId <= 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Membership type is required");
        }
        private async Task<BusinessModel> GetBusinessOrFail(string businessCode)
        {
            return await _businessRepository.GetBusinessByCodeAsync(businessCode)
                ?? throw new BaseException(ExceptionType.NOTFOUND, "Business does not exist");
        }
        private async Task<(OnboardingRequestModel model, bool status, string message)> RegisterMembershipLeaderOrFail(
            MemberShipRequestModel model,
            OnBoardingArgs args)
        {
            (OnboardingRequestModel model, bool status, string message) result = await _onboardingRepository.RegisterUser(
                new OnboardingRequestModel
                {
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password,
                    Pin = model.Pin,
                    FirstName = model.MembershipLeaderFirstName,
                    MiddleName = model.MembershipLeaderMiddleName,
                    LastName = model.MembershipLeaderLastName
                }, args);

            if (!result.status)
                throw new BaseException(ExceptionType.BAD_REQUEST, result.message);

            return result;
        }
        private async Task<(bool status, MemberShipModel data)> CreateMembershipOrFail(
    MemberShipRequestModel model,
    OnBoardingArgs args,
    BusinessModel business,
    string userId,
    DateTimeOffset dateTime)
        {
            return await _cooperativeRepository.CreateUpdateMembership(new MemberShipModel
            {
                UserId = userId,
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                State = model.State,
                Lga = model.Lga,
                Ward = model.Ward,
                Location = model.Location,
                MemberShipTypeId = model.MemberShipTypeId,
                IsLeaderAFarmer = model.IsLeaderAFarmer,
                Tier = Tier.Tier1.ToString(),
                OnBoardedOn = dateTime,
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                OnboardedById = args.OnboardedByUserId,
                HasbeenAssignedAgent = args.OnboardedByUserId != null,
                IsAssignedToAgentId = args.OnboardedByUserId,
                MembershipLeaderFirstName = model.MembershipLeaderFirstName,
                MembershipLeaderMiddleName = model.MembershipLeaderMiddleName,
                MembershipLeaderLastName = model.MembershipLeaderLastName,
                MembershipLeaderGender = model.MembershipLeaderGender,
                ContactPerson= $"{model.MembershipLeaderFirstName} {model.MembershipLeaderMiddleName} {model.MembershipLeaderLastName}"
            });
        }
        private async Task RunNonCriticalSteps(
    MemberShipRequestModel model,
    OnBoardingArgs args,
    BusinessModel business,
   OnboardingRequestModel user,    
    DateTimeOffset dateTime)
        {
            try
            {
                await EnsureWalletExists(user, business);
                await CreateFarmerIfLeaderIsFarmer(model, args, user.Id, dateTime);
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
        private async Task CreateFarmerIfLeaderIsFarmer(
            MemberShipRequestModel model,
            OnBoardingArgs args,
            string userId,
            DateTimeOffset dateTime)
        {
            if (!model.IsLeaderAFarmer) return;

            var farmer = new FarmerModel
            {
                UserId = userId,
                Name = $"{model.MembershipLeaderFirstName} {model.MembershipLeaderMiddleName} {model.MembershipLeaderLastName}",
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Gender = model.MembershipLeaderGender,
                State = model.State,
                Lga = model.Lga,
                Ward = model.Ward,
                Location = model.Location,
                CooperativeIds = new List<long> { model.Id },

                AgentIds = args.AgentIds?.Where(x => x.HasValue).Select(x => x.Value).ToList()
                             ?? new List<long>(),
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                OnBoardedOn = dateTime,
                OnboardedById = args.UserId
            };

            await _farmerRepository.CreateFarmer(farmer);
        }

    }

}
