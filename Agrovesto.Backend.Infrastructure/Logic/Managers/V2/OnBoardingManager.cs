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
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using CsvHelper;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NetTopologySuite.Operation.Valid;
using Org.BouncyCastle.Asn1.Ocsp;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iTextSharp.text.pdf.codec.TiffWriter;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using BulkUploadJob = Agrovesto.Backend.Infrastructure.Entities.Common.BulkUploadJob;
using Path = System.IO.Path;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;

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
       
        Task<FarmerProfileDto> GetFarmerProfile(string userId);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        Task<bool> RemoveFarmerFromCooperatives(string userId, List<long> cooperativeIds, string? organizationCode);
        Task<string> ResendOnboardingOtp(ResendOtpRequest model, OnBoardingArgs args);
        Task<ImageUploadResponse> SaveFarmerImage(ProfileImageModel model, OnBoardingArgs args);
        Task<CreateFarmDto> UpdateFarmDetails(CreateFarmDto model, BusinessRegistrationArgs args);
        Task<bool> VerifyNIN(VerificationArgs args, KYCVerificationDto model, string CurrentUserId);
        Task<Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto> UpdateFarmerProfile(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update);
          Task<BulkOnboardingResult> BulkOnboardFarmers(Stream excelStream, OnBoardingArgs args);
        Task<UsersProfileResponse> GetUserProfileAsync(string userId);
        Task<CreateMembershipResponseModel> CreateMembershipUser(MemberShipRequestModel model, OnBoardingArgs args);
        Task<OrganizationResponseModel> CreateOrganization(OrganizationOnboardingModel model, OrganizationOnboardingArgs args);
        Task<bool> VerifyBvn(VerificationArgs args, KYCVerificationDto model, string CurrentUserId);
        Task<string> RequestEmailOrPhoneVerification(VerificationArgs args, KYCVerificationDto model, string CurrentUserId);
        Task CreateCooperativeProfileAsync(ProfileRequestModel model, OnBoardingArgs args);
        Task<CreateAgentResponseModel> CreateAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<FarmerModel> RemoveFarmerToAgents(AssignmentArgs args);
        Task<bool> RemoveFarmerFromCooperative(string userId, long cooperativeId, string? organizationCode);
        Task<FarmerModel?> ChangeFarmerAgents(AssignmentArgs args, long farmerId);
        Task<bool> IsUserInRoleAsync(string userId, string role);
        Task EnsureFarmerIdIsValidAsync(long farmerId);
        Task<BulkOnboardingQueueResponse> BulkOnboardFarmersBatchAsync(Stream excelStream, OnBoardingArgs args);
        Task<MemberShipUpdateResultDto> UpdateMembershipProfile(string userId, MemberShipModel update);
        Task<bool> EditAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<UsersProfileResponse> GetUseridentity(string userId);
        Task<(bool Status, string Message)> ChangePassword(ChnagePasswordModel model);
        Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model);
        Task<(bool Status, string Message)> ForgotPassword(string username);
    }
    public class OnBoardingManager : IOnBoardingManager
    {
        private readonly IAWSRekognitionManager _faceManager;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly DataContext _context;
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
        private readonly IIdentityVerificationService _identityVerificationManager;
        private readonly IExcelConverterService _excelConverterService;
        private readonly IAWSService _awsService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBackgroundTaskQueue _taskQueue;
        public OnBoardingManager(IBackgroundTaskQueue taskQueue,
              IServiceScopeFactory scopeFactory, IWalletManager walletManager,
            IConfiguration configuration,
            DataContext context,
            INotificationService notificationService,
            IAWSRekognitionManager faceManager,
            IIdentityVerificationService identityVerificationManager,
            IBusinessRepository businessRepository, IFarmerRepository farmerRepository, ILogger<OnBoardingManager> log, IMembershipRepository cooperativeRepository,
            IAgentRepository agentRepository, IOtpService otpService, IFarmManager farmManager, INotificationService sMSService, IOnboardingRepository onboardingRepository
            , IExcelConverterService excelConverterService, IAWSService awsService)
        {
            _scopeFactory = scopeFactory;
            _taskQueue = taskQueue;
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
            _context = context;
            _notificationService = notificationService;
            _configuration = configuration;
            _awsService = awsService;
        }
        public async Task<BulkOnboardingQueueResponse> BulkOnboardFarmersBatchAsync(
         Stream excelStream,
         OnBoardingArgs args)
        {
            if (excelStream == null)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "Excel file is required.");

            if (_taskQueue == null || _scopeFactory == null)
            {
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Background processing is not configured.");
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "Agrovesto", "bulk");

            Directory.CreateDirectory(tempDir);

            var filePath = Path.Combine(
                tempDir,
                $"bulk_{Guid.NewGuid()}.xlsx");

            await using (var fs = File.Create(filePath))
            {
                await excelStream.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            var job = new BulkUploadJob
            {
                Id = Guid.NewGuid(),
                FilePath = filePath,
                OrganizationCode = args.organizationCode,
                CreatedByUserId = args.UserId ?? args.OnboardedByUserId ?? "system",
                OnboardedByUserId = args.OnboardedByUserId ?? args.UserId ?? "system",
                Status = "Queued",
                ErrorMessage = string.Empty,
                CreatedAt = DateTime.UtcNow,
                TotalRows = 0,
                Successful = 0,
                Failed = 0
            };

            _context.BulkUploadJobs.Add(job);

            await _context.SaveChangesAsync();
            BackgroundJob.Enqueue<BulkOnboardingProcessor>(
    x => x.ProcessAsync(
        filePath,
        args,
        job.Id,
        CancellationToken.None));
           
            return new BulkOnboardingQueueResponse
            {
                JobId = job.Id,
                Status = "Queued",
                Message = "Bulk onboarding queued successfully."
            };
        }
        public async Task<OrganizationResponseModel> CreateOrganization(
     OrganizationOnboardingModel model,
     OrganizationOnboardingArgs args)
        {
            OrganizationOnboardingValidation(model, args);
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                   
              

                    var parentBusiness =
                        await _businessRepository.GetBusinessByCodeAsync(args.businessCode);

                    if (parentBusiness == null)
                        throw new BaseException(
                            Core.Enums.ExceptionType.NOTFOUND,
                            "Parent business not found or deactivated.");


                    var newOrganization =
                        await _businessRepository.CreateOrUpdateOrganization(model, new OnBoardingArgs
                        {
                            UserId=args.userId,
                            userType= args.userType,
                            OnboardedByUserId=args.onboardedByUserId,
                            Env= args.env,
                            businessCode=args.businessCode,
                            
                        });
                    var updatedUser = await _onboardingRepository.UpdateUserProfileAsync(
                        args.userId,
                        new ProfileRequestModel
                        {
                            FirstName = model.FirstName?.Trim(),
                            LastName = model.LastName?.Trim(),
                            PhoneNumber = model.PhoneNumber?.Trim(),
                            Email = model.ContactEmail?.Trim(),
                            organizationCode= newOrganization.OrganizationCode
                        });
                    args.userType ??= updatedUser.UserType;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (args.env == Env.Live.ToString())
                            {
                                await SendOnboardingNotificationsAsync(newOrganization, args.userId, args.businessCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Notification failed");
                        }
                    });

                    return newOrganization;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private static void OrganizationOnboardingValidation(OrganizationOnboardingModel model, OrganizationOnboardingArgs args)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (string.IsNullOrWhiteSpace(args.businessCode))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Business Code is required.");


            if (string.IsNullOrWhiteSpace(args.env))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Environment is required.");


            if (string.IsNullOrWhiteSpace(args.userId))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "User Id is required.");
        }

          private async Task SendOnboardingNotificationsAsync(
    OrganizationResponseModel organization,
    string userId, string businessCode)
        {
            var accountOfficer = await _onboardingRepository.GetUserById(userId);

            var mailRequest = new WelcomeMailBody
            {
                To = organization.ContactEmail,
                ToName = organization.CompanyName,
                Subject = $"Welcome Onboard {organization.CompanyName}",
                AccountOfficer = accountOfficer.model.FirstName,
                Email = accountOfficer.model.Email,
                PhoneNumber = accountOfficer.model.PhoneNumber
            };

            await SendGridService.DynamicBusinessOnboarding(mailRequest);
            await SendOnboardingSMSM(
                organization.CompanyName,
                organization.PhoneNumber, businessCode);
        }
        private async Task SendOnboardingSMSM(string businessName, string phoneNumber, string businessCode)
        {
            var phone = _configuration["Official:Phone"];
            var messageBody = $"A new business {businessName} has been added to your merchant on NomaOS Platform." +
                         $"You can always reach out to the business using {phoneNumber} ";

            var sms = await _notificationService.HandleWhisperSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { $"0{phone}" },
                message = messageBody,
            });
            var sms1 = await _notificationService.HandleAfricasTalkingSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { $"+234{phone}" },
                message = messageBody,
            });
            var sms2 = await _notificationService.HandleTermiiSMS(new TermiiSmsRequest
            {
                to = $"+234{phone}",
                sms = messageBody
            }, businessCode);
        }
     
        public async Task<BulkOnboardingResult> BulkOnboardFarmers(Stream excelStream, OnBoardingArgs args
)
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

            var result = new BulkOnboardingResult { Total = rows.Count };
            int rowNumber = 1;


            foreach (var row in rows)
            {
                if (!long.TryParse(row.YearsInAgric, out var yearsInAgric))
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        $"Invalid YearsInAgric value: {row.YearsInAgric}"
                    );
                }
                var valueChains = new List<ValueChainSelectionDto>();

                if (!string.IsNullOrWhiteSpace(row.PartOfAgriculture))
                {
                    var items = string.IsNullOrWhiteSpace(row.ProductTypes)
                        ? new List<string>()
                        : row.ProductTypes
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .ToList();

                    valueChains.Add(new ValueChainSelectionDto
                    {
                        ValueChainType = row.PartOfAgriculture,
                        Items = items
                    });
                }
                rowNumber++;
                try
                {
                    if (string.IsNullOrWhiteSpace(row.Email) && string.IsNullOrWhiteSpace(row.PhoneNumber))
                        throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Either phone number or email is required.");

                    var model = new OnboardingRequestModel
                    {
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        Email = row.Email,
                        PhoneNumber = row.PhoneNumber,
                        Password =row.PhoneNumber, 
                        Pin = "1234",

                    };

                    var farmerArgs = new OnBoardingArgs
                    {
                        businessCode = args.businessCode,
                        organizationCode = args.organizationCode,
                        userType = "Farmer",
                        OnboardedByUserId = args.OnboardedByUserId,
                        AgentIds = args.AgentIds,
                        CooperativeIds = args.CooperativeIds
                    };

                    await CreateBusinessUser(model, farmerArgs);

                    if (args.CooperativeIds?.Any() == true)
                    {
                        var createdUserId = model.Id;
                        if (!string.IsNullOrWhiteSpace(createdUserId))
                        {
                            var assignArgs = new AssignmentArgs
                            {
                                UserId = createdUserId,
                                CooperativeIds = args.CooperativeIds,
                                businessCode = args.businessCode,
                                organizationCode = args.organizationCode
                            };
                            await AssignFarmerToCooperatives(assignArgs);
                        }
                    }
                    if (args.AgentIds?.Any() == true)
                    {
                        var createdUserId = model.Id;
                        if (!string.IsNullOrWhiteSpace(createdUserId))
                        {
                            var assignArgs = new AssignmentArgs
                            {
                                UserId = createdUserId,
                                AgentIds = args.AgentIds,
                                businessCode = args.businessCode,
                                organizationCode = args.organizationCode
                            };
                            await AssignFarmerToAgents(assignArgs);
                        }
                    }

                    result.Successful++;
                }
                catch (BaseException ex)
                {
                    result.Failed++;
                    result.Errors.Add(new BulkOnboardingError { RowNumber = rowNumber, Email = row.Email, Error = ex.Message });
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Bulk farmer onboarding failed on row {Row}", rowNumber);
                    result.Failed++;
                    result.Errors.Add(new BulkOnboardingError { RowNumber = rowNumber, Email = row.Email, Error = "Unexpected error occurred" });
                }
            }

            return result;
        }

        public async Task<MemberShipUpdateResultDto> UpdateMembershipProfile(string userId,
           MemberShipModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "User id and update payload are required.");

             if (!string.IsNullOrWhiteSpace(update.NIN))
                ValidateIdentifier(update.NIN, "NIN");

            if (!string.IsNullOrWhiteSpace(update.BVN))
                ValidateIdentifier(update.BVN, "BVN");

            var updated = await _cooperativeRepository.CreateUpdateMembership(update);
            if (!updated.status)
            {
                return new MemberShipUpdateResultDto
                {
                    Success = false,
                    Message = "Membership not found or update failed."
                };
            }
            var updatedFields = new List<string>();
            foreach (var prop in typeof(Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel).GetProperties())
            {
                var val = prop.GetValue(update);
                if (val != null)
                {
                        if (val is string s && string.IsNullOrWhiteSpace(s)) continue;
                    updatedFields.Add(prop.Name);
                }
            }

            return new MemberShipUpdateResultDto
            {
                Success = true,
                UpdatedFields = updatedFields,
                UpdatedMembership = updated.data,
                Message = "Membership profile updated successfully."
            };

        }


        public async Task<Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto> UpdateFarmerProfile(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "User id and update payload are required.");

            if (!string.IsNullOrWhiteSpace(update.NIN))
                ValidateIdentifier(update.NIN, "NIN");

            if (!string.IsNullOrWhiteSpace(update.BVN))
                ValidateIdentifier(update.BVN, "BVN");

            var updateUser = await _onboardingRepository.UpdateUserProfileAsync(userId, new ProfileRequestModel
            {
               
                PhoneNumber = update.PhoneNumber,
                Email = update.Email,
                NIN = update.NIN,
                DateOfBirth = update.DateOfBirth,
               Bvn = update.BVN,
            });
            var updated = await _farmerRepository.UpdateFarmerProfileAsync(userId, update);
            if (updated == null)
            {
                return new Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateResultDto
                {
                    Success = false,
                    Message = "Farmer not found or update failed."
                };
            }

            var updatedFields = new List<string>();
            foreach (var prop in typeof(Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel).GetProperties())
            {
                var val = prop.GetValue(update);
                if (val != null)
                {
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
        public async Task<bool> IsUserInRoleAsync(string userId, string role)
        {
            var login = await _onboardingRepository.IsUserInRoleAsync(userId, role);

            return login;
        }
        public async Task<(bool Status, string Message, TokenModel Response)> Login(
        LoginModel model,
        string businessCode
            )
        {
            ValidateLoginModel(model);
            model.BusinessCode = businessCode;
            var login= await _onboardingRepository.Login(model);

            return login;
        }
        private async Task SendDefaultCredentialNotification(User user, string businessCode)
        {
            var senderEmail = _configuration["Official:Email"];
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _awsService.SendEmailAsync(senderEmail,
                    user.Email, 
                    "Action Required: Change Your Default Password" ,
                    $"You are currently using a default credential. Please log in and change it immediately.",
                    $"You are currently using a default credential. Please log in and change it immediately."
                );
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                await _sMSService.SendSmsAsync( 
                    user.PhoneNumber,
                    "You're using a default PIN/password. Please change it immediately.",
                    businessCode
                );
            }
        }

        private static void ValidateQuickOnboarding(OnboardingRequestModel model, string onboardedByUserid)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(model.Email);
            var hasPhone = !string.IsNullOrWhiteSpace(model.PhoneNumber);

            //if (!hasEmail && !hasPhone)
            //{
            //    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
            //        "Either email or phone number must be provided.");
            //}

            //if (hasEmail && hasPhone)
            //{
            //    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
            //        "Provide either email/password or phone/pin, not both.");
            //}

            if (hasEmail)
            {
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(model.Email))
                {
                    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                        "Invalid email address format.");
                }
                if (string.IsNullOrWhiteSpace(onboardedByUserid))
                {
                    if (string.IsNullOrWhiteSpace(model.Password))
                    {
                        throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                            "Password is required when signing up with email.");
                    }
                }
            }

            if (hasPhone)
            {
                if (!Regex.IsMatch(model.PhoneNumber!, @"^\d{11}$"))
                {
                    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                        "Phone number must be exactly 11 digits and numeric only.");
                }

                if (string.IsNullOrWhiteSpace(model.Pin) && string.IsNullOrWhiteSpace(onboardedByUserid))
                {
                    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                        "PIN is required when signing up with phone number.");
                  
                }
                if (string.IsNullOrWhiteSpace(onboardedByUserid))
                {
                    if (!string.IsNullOrEmpty(model.Pin) && !Regex.IsMatch(model.Pin, @"^\d{4}$"))
                    {
                        throw new BaseException(
                            Core.Enums.ExceptionType.BAD_REQUEST,
                            "PIN must be exactly 4 digits.");
                    }
                }

            }
        }
        public async Task<(bool status, string message)> CreateUserProfile(ProfileRequestModel model, OnBoardingArgs args)
        {
            try
            {
                var userId = !string.IsNullOrEmpty(args.UserId)
                    ? args.UserId
                    : model?.Id;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "User Id missing.");
                }
                if (!string.IsNullOrEmpty(model.PhoneNumber) || !string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
                 {
                    ValidateIdentifier(model.NIN, "NIN");
                    ValidateIdentifier(model.Bvn, "BVN");
                    ValidateIdentifier(model.PhoneNumber, "Phonenumber");
                }
                model.organizationCode = args.organizationCode;
                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                var updateUser = await _onboardingRepository.UpdateUserProfileAsync(userId, model);
                model.CreatedOn = updateUser.CreatedOn;
                model.Id = updateUser.Id;
                model.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : updateUser.Email;
                model.PhoneNumber = !string.IsNullOrEmpty(model.PhoneNumber) ? model.PhoneNumber : updateUser.PhoneNumber;

                args.userType = !string.IsNullOrEmpty(args.userType) ? args.userType : updateUser.UserType;
               
                //if (!string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
                //{
                //    if (!string.IsNullOrWhiteSpace(model.NIN))
                //        updateUser.NIN = model.NIN;

                //    if (!string.IsNullOrWhiteSpace(model.Bvn))
                //        updateUser.Bvn = model.Bvn;

                //    await _onboardingRepository.OnNINBVNUpdate(updateUser);
                //}
                await CreateUserCategory(model, args);
                return (true, "User Profile Created");
            }

            catch (Exception ex)
            {
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

        private async Task CreateUserCategory(ProfileRequestModel model, OnBoardingArgs args)
        {

            var accountType = args.userType.ToEnum<UserType>();

            switch (accountType)
            {
                case UserType.Trader:
                    break;
                case UserType.Agent:
                    await CreateAgentProfileAsync(model, args);
                    break;

                case UserType.MemberShip:
                    await CreateCooperativeProfileAsync(model, args);
                    break;

                case UserType.Farmer:
                    await CreateFarmerProfileAsync(model, args);
                    break;

                case UserType.Organization:
                    await CreateOrganization(model, args);
                    break;

                case UserType.Processors:
                    break;             
                case UserType.Suppliers:
                    break;
                case UserType.ServiceProvider:
                    break;
            }

        }

        private async Task CreateOrganization(ProfileRequestModel model, OnBoardingArgs args)
        {
            var fullName = !string.IsNullOrWhiteSpace(model.FullName)
                ? model.FullName
                : string.Join(" ",
                    new[] { model.FirstName, model.MiddleName, model.LastName }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

            var newOrganization = await _businessRepository.CreateOrUpdateOrganization(new OrganizationOnboardingModel
            {
                PhoneNumber = model.PhoneNumber,
                CompanyEmail = model.Email,
                ContactEmail = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                OrganizationCode = args.organizationCode,
            }, args);
        }
        private async Task CreateAgentProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
        {
            
            await _agentRepository.CreateUpdateAgent(new AgentModel
            {
                BelongToBusinessCode = args.businessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                Name = !string.IsNullOrWhiteSpace(model.FullName)
    ? model.FullName
    : string.Join(" ",
        new[] { model.FirstName, model.MiddleName, model.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))),

                OnBoardedOn = model.CreatedOn,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                Ward = model.Ward,
                OnboardedById = args.OnboardedByUserId,
                OrganizationId = model.OrganizationId
            });
        }
        private async Task<CreateUserResult> UpdateUserProfile(
       ProfileRequestModel model,
       OnBoardingArgs args)
        {
            var result = await _onboardingRepository.RegisterUser(
                new OnboardingRequestModel
                {
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password,
                    Pin = model.Pin,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                }, args);

            if (!result.status)
                throw new BaseException(ExceptionType.BAD_REQUEST, result.message);

            return result;
        }
        public async Task CreateCooperativeProfileAsync(ProfileRequestModel model, OnBoardingArgs args)
        {

            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
            if (hasOrg != null)
            {
                model.OrganizationId = hasOrg.OrganizaionId;
            }
            var isAssignedagent = false;
            if (args.OnboardedByUserId != null)
            {
                isAssignedagent = true;
            }
            await UpdateUserProfile(model, args);

            await _cooperativeRepository.CreateUpdateMembership(new MemberShipModel
            {
                BelongToBusinessCode = args.businessCode,
                IsLeaderAFarmer = model.IsLeaderAFarmer,
                State = model.State,
                Lga = model.Lga,
                ContactPerson = model.FirstName,
                Email = model.Email,
                Location = model.Location,
                MembershipLeaderFirstName = model.FirstName,
                MembershipLeaderGender = model.Gender,
                MembershipLeaderLastName = model.LastName,
                MembershipLeaderMiddleName = model.MiddleName,
                MemberShipTypeId = model.MembershipTypeId.HasValue ? model.MembershipTypeId.Value : 1,
                Tier = Tier.Tier1.ToString(),
                

                Name = model.CooperativeName,
                OnBoardedOn = model.CreatedOn,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                OnboardedById = args.OnboardedByUserId,

                IsAssignedToAgentId = args.OnboardedByUserId,
                HasbeenAssignedAgent = isAssignedagent,
                Ward = model.Ward,

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
                OnBoardedOn = model.CreatedOn,
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

                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,

                OnboardedById = model.OnboardedByUserId,
                HighestLevelOfEducation = model.HighestLevelOfEducation
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
            

            return await _farmerRepository.AssignFarmerToCooperatives(args, args.Id);
        }
        public async Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args)
        {
            if (!args.AgentIds.Any())
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "No Agent selected.");
            }
            if (args.Id < 0)
            {
                throw new BaseException(
    Core.Enums.ExceptionType.BAD_REQUEST,
    "Farmer Id is required.");
            }
            

            return await _farmerRepository.AssignFarmerToAgents(args, args.Id);
        }
        public async Task<FarmerModel?> ChangeFarmerAgents(AssignmentArgs args, long farmerId)
        {
            if (args == null || args.AgentIds == null || !args.AgentIds.Any())
                throw new ArgumentException("AgentIds are required");

            var result = await _farmerRepository.ChangeFarmerAgents(args, farmerId);

            return result; 
        }
        public async Task<FarmerModel> RemoveFarmerToAgents(AssignmentArgs args)
        {


            try
            {
                if (string.IsNullOrWhiteSpace(args.UserId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");
                var getFarmer = await _farmerRepository.GetFarmerByUserId(args.UserId, args.organizationCode);
                if (getFarmer == null) { throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required."); }

                var agentIds = args.AgentIds
    .Where(x => x.HasValue)
    .Select(x => x.Value)
    .ToList();

                var profile = await _farmerRepository.RemoveFarmerFromAgent(getFarmer.Id, agentIds);
                if (profile == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Could not remove farmer from this agent.");

                return getFarmer;
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error fetching removing farmer from cooperative", args.UserId);
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR, "An unexpected error occurred while fetching farmer profile.");
            }
        }

        public async Task<OnboardingResponseModel> CreateBusinessUser(
    OnboardingRequestModel model,
    OnBoardingArgs args)
        {
            try
            {
                ValidateRequest(model, args);
                var env = ResolveEnvironment(args);
                var business = await GetBusinessOrThrow(args.businessCode);

                CreateUserResult createUserResult = null;

                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        createUserResult = await RegisterUser(model, args);

                        if (!createUserResult.status)
                        {
                            throw new BaseException(
                                Core.Enums.ExceptionType.BAD_REQUEST,
                                createUserResult.message);
                        }

                        await ExecutePostCreationStepsAsync(
                            model,
                            args,
                            createUserResult,
                            business,
                            env);

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return BuildResponse(model, createUserResult);
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
        private void ValidateRequest(OnboardingRequestModel model, OnBoardingArgs args)
        {
            ValidateUserType(args);
            ValidateQuickOnboarding(model, args.OnboardedByUserId);

            if (string.IsNullOrEmpty(args.businessCode))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Business code is required.");
            }
        }
        private string ResolveEnvironment(OnBoardingArgs args)
        {
            return !string.IsNullOrEmpty(args.Env)
                ? args.Env
                : _configuration["Official:Env"] ?? string.Empty;
        }
        private async Task<BusinessModel> GetBusinessOrThrow(string businessCode)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(businessCode);

            if (business == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "Business does not exist.");
            }

            return business;
        }
        private Task<CreateUserResult> RegisterUser(
    OnboardingRequestModel model,
    OnBoardingArgs args)
        {
            return _onboardingRepository.RegisterUser(model, args);
        }

        private async Task ExecutePostCreationStepsAsync(
    OnboardingRequestModel model,
    OnBoardingArgs args,
    CreateUserResult createUser,
    BusinessModel business,
    string env)
        {
            try
            {
                await CreateUserProfile(new ProfileRequestModel
                {
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    MiddleName = model.MiddleName,
                    Id = createUser.model.Id,
                    PhoneNumber = model.PhoneNumber,
                    OnboardedByUserId = args.OnboardedByUserId
                }, args);

                await _onboardingRepository.TrackUserRoleAssignment(
                    createUser.model.Id,
                    args.userType);
            }
            catch (Exception ex)
            {
                _log.LogError(
                    ex,
                    "Critical onboarding step failed for user {UserId}, role {UserType}. Rolling back.",
                    createUser.model.Id,
                    args.userType);
                throw new BaseException(
                    Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR,
                    $"Failed to complete profile setup for role '{args.userType}'. Please try again or contact support.");
            }

            createUser.model.Env = env;
            if (createUser.IsExisting)
                return;

            try
            {
                await HandleUserNotificationsAsync(createUser.model, env, args.businessCode);
            }
            catch (Exception ex)
            {
                _log.LogWarning(
                    ex,
                    "Onboarding notification failed for user {UserId}. User was still onboarded successfully.",
                    createUser.model.Id);
            }
        }
       

        private async Task HandleUserNotificationsAsync(OnboardingRequestModel user, string env, string businessCode)
        {
            if (user.HasDefaultPassword)
            {
                await SendDefaultCredentialChange(user, businessCode);
            }

            await GenerateAndSendOtp(user, businessCode);
        }
        private OnboardingResponseModel BuildResponse(
    OnboardingRequestModel model,
    CreateUserResult createUser)
        {
            model.Id = createUser.model.Id;

            return new OnboardingResponseModel
            {
                Id = model.Id,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                IsExisting = createUser.IsExisting,
                Message = createUser.IsExisting
                    ? createUser.message
                    : "User created successfully.",
                ShouldLogin=createUser.ShouldLogin
            };
        }
        private static void ValidateUserType(OnBoardingArgs args)
        {
            if (!Enum.TryParse<UserType>(args.userType, true, out _))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Invalid User Type.");
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

        private async Task SendDefaultCredentialChange(OnboardingRequestModel model, string businessCode)
        {
            var senderEmail = _configuration["Official:Email"];
            var otpTarget = !string.IsNullOrWhiteSpace(model.Email) ? OtpTarget.Email : OtpTarget.Phone;
            var destination = otpTarget == OtpTarget.Email ? model.Email : model.PhoneNumber;

            if (otpTarget == OtpTarget.Email)
            {
                await _awsService.SendTemplatedEmailAsync(
                    senderEmail,
                    destination,
                    "AgrovestoWelcomeTemplate",
                    new
                    {
                        name = model.FirstName ?? "User",
                        loginLink = "https://app.agrovesto.com/login",
                        password = model.Password
                    }
                );
            }
            else
            {
                // SMS fallback
                await _sMSService.SendSmsAsync(
                    destination,
                    $"Your default pin is {model.Pin}. Kindly login and change this.",
                    businessCode
                );
            }
        }
        private async Task GenerateAndSendOtp(OnboardingRequestModel model, string businessCode)
        {
            var senderEmail = _configuration["Official:Email"];
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

            if (otpTarget == OtpTarget.Email)
            {
                //use aws
                await _awsService.SendEmailAsync(
                    senderEmail,
                    destination,
                    "Confirm your account",
                    $"Your verification code is {otp.Code}. It expires in 5 minutes.",
                    $"Your verification code is {otp.Code}. It expires in 5 minutes."
                    );
                
                          }
            else
            {
                await _sMSService.SendSmsAsync(
                        otp.Destination,
                        $"Your verification code is {otp.Code}. Expires in 5 minutes.",
                        businessCode);
            }
            await _awsService.SendEmailAsync(
                  senderEmail,
                  "agrovesto@gmail.com",
                  $"New Registration Alert on NomaOS {model.Env} environment",
                  $"New registration from {model.Email} {model.PhoneNumber}. Login to Admin Console for details.",
                  $"New registration from {model.Email} {model.PhoneNumber}. Login to Admin Console for details."
                  );
        }

        public async Task<bool> ConfirmOtp(ConfirmOtpRequest request)
        {
            var validate = await _otpService.VerifyOtpAsync(
                request.UserId,
                request.Code,
                OtpPurpose.Onboarding);
            if (validate == null)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Invalid or expired OTP.");

            await _onboardingRepository.ConfirmUserEmailOrPassword(request.UserId, validate);
            return validate.IsUsed;
        }
      
        public async Task<string> ResendOnboardingOtp(ResendOtpRequest model, OnBoardingArgs args)
        {
            try
            {
                var otp = await ResendOtpAsync(model.UserId, model.OtpTarget, OtpPurpose.Onboarding);
                string env = !string.IsNullOrEmpty(args.Env)
           ? args.Env
           : _configuration["Official:Env"] ?? string.Empty;

                if (env == Env.Live.ToString())
                {
                    if (model.OtpTarget == OtpTarget.Email)
                    {
                        await _awsService.SendEmailAsync(
                            _configuration["Official:Email"],
                            otp.Destination,
                            "Confirm your account",
                            $"Your verification code is {otp.Code}. It expires in 5 minutes.",
                            $"Your verification code is {otp.Code}. It expires in 5 minutes."
                            );
                    }
                    else
                    {
                        await _sMSService.SendSmsAsync(
                            otp.Destination,
                            $"Your verification code is {otp.Code}. Expires in 5 minutes.",
                          args.businessCode);
                    }
                    return "";
                }
                else { return otp.Code; }
               
            }
            catch (Exception ex)
            {
                throw new BaseException(exceptionType: Core.Enums.ExceptionType.BAD_REQUEST, ex.Message);
            }
        }
        public async Task<OtpResult> ResendOtpAsync(string userId, OtpTarget target, OtpPurpose purpose)
        {
            var user = await _onboardingRepository.GetUserById(userId);
            await _otpService.InvalidateActiveOtps(userId, target);
            var destination = "";
            if (target == OtpTarget.Email)
            {
                destination = user.model.Email;
            }
            else
            {
                destination = user.model.PhoneNumber;
            }
            var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = userId,
                Target = target,
                Destination = destination,
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
            var farmer = await GetFarmerProfileAsync(userId);

            if (farmer != null && farmer.PersonalInformation != null && !string.IsNullOrEmpty(farmer.PersonalInformation.FarmerImage))
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
        public async Task<UsersProfileResponse> GetUseridentity(string userId)
        {
            var profile = await _onboardingRepository.GetUseridentity(userId);
            if (profile == null)
                throw new BaseException(ExceptionType.NOTFOUND, "User profile not found");

            return profile;
        }


        public async Task<string> RequestEmailOrPhoneVerification(VerificationArgs args, KYCVerificationDto model, string CurrentUserId)
        {
            var user = await _onboardingRepository.GetUserById(args.UserId);
            if (user.model == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "User not found.");
            }
            user.model.Env = args.Env;
            if (args.Env == Env.Live.ToString())
            {
                await GenerateAndSendOtp(user.model.ToEntity(), args.businessCode);
                return "";
            }
            else { return await DummyGenerateAndSendOtp(user.model.ToEntity()); }
       
        }
        private async Task<string> DummyGenerateAndSendOtp(OnboardingRequestModel model)
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

            return otp.Code;
       
        }


        public async Task<bool> RemoveFarmerFromCooperative(string userId, long cooperativeId, string? organizationCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");
                var getFarmer = await _farmerRepository.GetFarmerByUserId(userId, organizationCode);
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
        public async Task<bool> RemoveFarmerFromCooperatives(string userId, List<long> cooperativeIds, string? organizationCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");
                var getFarmer = await _farmerRepository.GetFarmerByUserId(userId, organizationCode);
                if (getFarmer == null) { throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required."); }
              
                var profile = await _farmerRepository.RemoveFarmerFromCooperatives(getFarmer.Id, cooperativeIds);

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


        public async Task<bool> VerifyNIN(VerificationArgs args, KYCVerificationDto model, string CurrentUserId)
        {
            var user = await _onboardingRepository.GetUserById(args.UserId);
            if (user.model == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "User not found.");
            }

            var number = !string.IsNullOrEmpty(model.Number) ? model.Number : user.model.NIN;

            if (string.IsNullOrEmpty(number))
            {
                throw new BaseException(
                   Core.Enums.ExceptionType.NOTFOUND,
                   "No number supplied.");
            }
            ValidateIdentifier(number, "NIN");
            var isExistingNin = await _onboardingRepository.GetKYCDetails(number, KYCVerificationType.NIN);
            if (isExistingNin != null)
            {
                return true;
            }
            NINDataVerificationResponse<NINData, Verification> verifyNin = null;
            if (args.Env == Env.Live.ToString())
            {
                verifyNin = await _identityVerificationManager.VerifyNIN(new NINDataModel
                {
                    number = number
                });
            }
            else
            {
                verifyNin = UseDummyNINEnvironment(number = number);
            }

            if (!verifyNin.status)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    verifyNin.detail);
            }
            user.model.NIN = number;
            var kyc = verifyNin.nin_data.ToEntity();
            kyc.VerifiedBy = CurrentUserId;
            kyc.UserId = args.UserId;
            kyc.LoggedOn = DateTimeOffset.UtcNow;
            kyc.Photourl = verifyNin.nin_data?.photo;
            kyc.VerificationType = KYCVerificationType.NIN;
            kyc.Number = verifyNin.nin_data?.nin;
            await _onboardingRepository.OnCreateKYC(kyc);
            await _onboardingRepository.OnNINBVNUpdate(new UserModel
            {
                Id = args.UserId,
                NIN = number,
            }, true);
            await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level4);
            return true;
        }

        private NINDataVerificationResponse<NINData, Verification>? UseDummyNINEnvironment(string v)
        {
            var nindata = new NINData
            {
                birthcountry = "Nigeria",
                birthdate = "1995-06-15",
                birthlga = "Ikeja",
                centralID = "CEN123456789",
                educationallevel = "Bachelor's Degree",
                email = "john.doe@example.com",
                middlename = "Michael",
                emplymentstatus = "Employed",
                firstname = "John",
                gender = "Male",
                heigth = "175cm",
                maritalstatus = "Single",
                nin = v,
                nok_address1 = "12 Palm Street",
                nok_address2 = "Apartment 3B",
                nok_firstname = "Mary",
                nok_lga = "Surulere",
                nok_middlename = "Anne",
                nok_postalcode = "100001",
                nok_state = "Lagos",
                nok_surname = "Doe",
                nok_town = "Lagos",
                nspokenlang = "English",
                ospokenlang = "Yoruba",
                pfirstname = "Robert",
                photo = "base64-photo-string",
                pmiddlename = "James",
                profession = "Software Engineer",
                psurname = "Doe",
                religion = "Religion",
                residence_address = "45 Allen Avenue",
                residence_Town = "Ikeja",
                residence_lga = "Ikeja",
                residence_state = "Lagos",
                residencestatus = "Permanent",
                self_origin_lga = "Oshodi-Isolo",
                self_origin_place = "Isolo",
                self_origin_state = "Lagos",
                signature = "base64-signature-string",
                surname = "Doe",
                telephoneno = "08031234567",
                title = "Mr"
            
            };

            return new NINDataVerificationResponse<NINData, Verification>
            {
                nin_data = nindata,
                response_code = "OK",
                detail =null,
                status = true,
                verification = new Verification()
            };
        }

        public async Task<bool> VerifyBvn(VerificationArgs args, KYCVerificationDto model, string CurrentUserId)
        {
            if (string.IsNullOrEmpty(args.Env))
            {
                throw new ValidationException("Environment is required.");
            }
            var user = await _onboardingRepository.GetUserById(args.UserId);
            if (user.model == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "User not found.");
            }
            var number = !string.IsNullOrEmpty(model.Number) ? model.Number : user.model.Bvn;
            if (string.IsNullOrEmpty(number))
            {
                throw new BaseException(
                   Core.Enums.ExceptionType.NOTFOUND,
                   "No number supplied.");
            }
            ValidateIdentifier(number, "BVN");
            var isExistingBvn = await _onboardingRepository.GetKYCDetails(number, KYCVerificationType.BVN);
            if (isExistingBvn != null)
            {
                return true;
            }
            BVNVerificationResponse<BVN_data, Verification> verifyBvn = null;
            if(args.Env == Env.Live.ToString())
            {
                verifyBvn = await _identityVerificationManager.VerifyBvn(new BVNDataModel
                {
                    number = number
                });
            }
            else
            {
                verifyBvn = UseDummyBVNVerification(number);
            }
         
            if (!verifyBvn.status)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    verifyBvn.detail);
            }
            user.model.NIN = number;
            var kyc = verifyBvn.data.ToEntity();
            kyc.VerifiedBy = CurrentUserId;
            kyc.UserId = args.UserId;
            kyc.LoggedOn = DateTimeOffset.UtcNow;
            kyc.Photourl = verifyBvn.data?.base64Image;
            kyc.VerificationType = KYCVerificationType.BVN;
            kyc.Number = verifyBvn.data?.bvn;
            await _onboardingRepository.OnCreateKYC(kyc);
            await _onboardingRepository.OnNINBVNUpdate(new UserModel
            {
                Id = args.UserId,
                Bvn = number,
            }, true);
            await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level4);
            return true;
        }

        private BVNVerificationResponse<BVN_data, Verification>? UseDummyBVNVerification(string number)
        {
            var bvnData = new BVN_data
            {
                firstName = "Adebayo",
                lastName = "Olawale",
                middleName = "Samuel",
                dateOfBirth = "1988-03-22",
                phoneNumber1 = "08034567891",
                phoneNumber2 = "07012345678",
                registrationDate = "2015-09-14",
                enrollmentBank = "First Bank of Nigeria",
                enrollmentBranch = "Ikeja Branch",
                email = "adebayo.olawale@example.com",
                gender = "Male",
                levelOfAccount = "Level 3",
                lgaOfOrigin = "Abeokuta South",
                lgaOfResidence = "Ikeja",
                maritalStatus = "Married",
                nin = "23456789012",
                nameOnCard = "ADEBAYO OLAWALE S",
                nationality = "Nigerian",
                residentialAddress = "23 Toyin Street, Ikeja",
                stateOfOrigin = "Ogun",
                stateOfResidence = "Lagos",
                title = "Mr",
                watchListed = "No",
                bvn = "22113344556",
                base64Image = "base64-image-string"
            };
            return new BVNVerificationResponse<BVN_data, Verification>
            {
                data = bvnData,
                response_code = "OK",
                detail = null,
                status = true,
                verification = new Verification()
            };
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

            string dateOfBirth = date; 
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
        private async Task SendOnboardingSMS(string businessName, string phoneNumber, string businessCode)
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
            }, businessCode);
        }


        public async Task<ImageUploadResponse> SaveFarmerImage(ProfileImageModel model, OnBoardingArgs args)
        {
            var saveFace = new ImageUploadResponse();
            try
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(args.UserId, args.organizationCode)
                  ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Farmer does not exist.");


                //if (farmer.Level != EnumExtensions.GetDisplayName(FarmerLevel.Level2))
                //{
                //    throw new BaseException(ExceptionType.ACCESS_DENIED, "You’re almost there! " +
                //        "Complete your phonenumber or email Verification to continue to submit photo");
                //}

                saveFace = await _faceManager.UploadImage(model.File, args.UserId, args.businessCode, args.organizationCode);

                if (saveFace != null && !string.IsNullOrEmpty(saveFace.ImageKey))
                {
                    var updateLevel = await _farmerRepository.OnFarmerLevelUpdate(args.UserId, FarmerLevel.Level3);
                }
                return saveFace;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
        public async Task EnsureFarmerIdIsValidAsync(long farmerId)
        {
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            if (farmer == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Farmer not found.");

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
                throw new Exception($"{ex.Message}", ex);
            }
        }


        public async Task<CreateMembershipResponseModel> CreateMembershipUser(MemberShipRequestModel model, OnBoardingArgs args)
        {
            try
            {

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                ValidateCriticalInputs(model, args);

                var dateTime = DateTimeOffset.UtcNow;

                var business = await GetBusinessOrFail(args.businessCode);

                var user = await RegisterMembershipLeaderOrFail(model, args);

                var membership = await CreateMembershipOrFail(
                    model, args, business, user.model.Id, dateTime);

                model.Id = membership.data.Id;
                await RunNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateMembershipResponseModel
                {
                    UserId = user.model.Id,
                    MembershipId = membership.data.Id,
                    RequiresProfileCompletion = true,
                    IsExisting = user.IsExisting,
                    Message = user.IsExisting
        ? user.message
        : $"User created successfully."
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
            }, args.OnboardedByUserId);

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
        private async Task<CreateUserResult> RegisterMembershipLeaderOrFail(
            MemberShipRequestModel model,
            OnBoardingArgs args)
        {
      var result = await _onboardingRepository.RegisterUser(
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
                AgentIds = args.AgentIds?
  .Where(x => x.HasValue)
  .Select(x => x.Value)
  .ToList() ?? new List<long>(),
                BelongToBusinessCode = args.businessCode,
                OrganizationId = model.OrganizationId,
                OnboardedById = args.OnboardedByUserId,
                HasbeenAssignedAgent = args.OnboardedByUserId != null,
                IsAssignedToAgentId = args.OnboardedByUserId,
                MembershipLeaderFirstName = model.MembershipLeaderFirstName,
                MembershipLeaderMiddleName = model.MembershipLeaderMiddleName,
                MembershipLeaderLastName = model.MembershipLeaderLastName,
                MembershipLeaderGender = model.MembershipLeaderGender,
                ContactPerson = $"{model.MembershipLeaderFirstName} {model.MembershipLeaderMiddleName} {model.MembershipLeaderLastName}"
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
                await GenerateAndSendOtp(user, args.businessCode);
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


        public async Task<CreateAgentResponseModel> CreateAgentUser(
 ProfileRequestModel model,
 OnBoardingArgs args)
        {
            try
            {
                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                ValidateQuickOnboarding(new OnboardingRequestModel
                {
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                }, args.OnboardedByUserId);
                if (string.IsNullOrEmpty(args.businessCode))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");


                var business = await GetBusinessOrFail(args.businessCode);

               var registerUser = await _onboardingRepository.RegisterUser(
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


              var agent=  await _agentRepository.CreateUpdateAgent(new AgentModel
                {
                    BelongToBusinessCode = args.businessCode,
                    State = model.State,
                    Lga = model.Lga,
                    Email = model.Email,
                    Location = model.Location,
                    Name = !string.IsNullOrWhiteSpace(model.FullName)
        ? model.FullName
        : string.Join(" ",
            new[] { model.FirstName, model.MiddleName, model.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))),

                    OnBoardedOn =DateTimeOffset.Now,
                    PhoneNumber = model.PhoneNumber,
                    UserId = registerUser.model.Id,
                    Ward = model.Ward,
                    OnboardedById = args.OnboardedByUserId,
                  OrganizationId = model.OrganizationId,
                   
                });

               
                var coopId = new List<long>();
                coopId.Add(args.Id);

                var assignMembership = await _cooperativeRepository.AssignMemberShipToAgent(registerUser.model.Id, coopId);

                await EnsureWalletExists(registerUser.model, business);

                await _onboardingRepository.TrackUserRoleAssignment(registerUser.model.Id, args.userType);

                return new CreateAgentResponseModel
                {
                    UserId = args.UserId,
                    AgentId = agent.data.Id,
                    AgentUserId = registerUser.model.Id,
                    RequiresProfileCompletion = true,
                    IsExisting = registerUser.IsExisting,
                    Message = registerUser.IsExisting
        ? registerUser.message
        : $"User created successfully."
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
        public async Task<bool> EditAgentUser(
            ProfileRequestModel model,
            OnBoardingArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.UserId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "UserId is required");
                ValidateRequest(model, args);

                var business = await GetBusinessOrFail(args.businessCode);

                var organization = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (organization != null)
                    model.OrganizationId = organization.OrganizaionId;

                var onboardingRequest = BuildOnboardingRequest(model);

                await _onboardingRepository.UpdateUserDetails(onboardingRequest, args);

                var updateprofile= await _agentRepository.CreateUpdateAgent(new AgentModel
                {
                    BelongToBusinessCode = args.businessCode,
                    State = model.State,
                    Lga = model.Lga,
                    Email = model.Email,
                    Location = model.Location,
                    Name = BuildFullName(model),
                    OnBoardedOn = DateTimeOffset.Now,
                    PhoneNumber = model.PhoneNumber,
                    UserId = args.UserId,
                    Ward = model.Ward,
                    OnboardedById = args.OnboardedByUserId,
                    OrganizationId = model.OrganizationId
                });

                return true;
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete profile.");
            }
        }
        private string BuildFullName(ProfileRequestModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.FullName))
                return model.FullName;

            return string.Join(" ",
                new[] { model.FirstName, model.MiddleName, model.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        private OnboardingRequestModel BuildOnboardingRequest(ProfileRequestModel model)
        {
            return new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName
            };
        }
        private void ValidateRequest(ProfileRequestModel model, OnBoardingArgs args)
        {
            ValidateQuickOnboarding(new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            }, args.OnboardedByUserId);

            if (string.IsNullOrWhiteSpace(args.businessCode))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");

        }

        public async Task<(bool Status, string Message)> ForgotPassword(string username)
        {
            var fgPwd = await _onboardingRepository.ForgotPassword(username);
            if (fgPwd.Status && !string.IsNullOrEmpty(fgPwd.token))
            {
                var tokenGeneratedByte = Encoding.UTF8.GetBytes(fgPwd.token);

                var encodedToken = WebEncoders.Base64UrlEncode(tokenGeneratedByte);
                string callbackUrl = $"{_configuration["ResetPasswordLink"]}?email={username}&token={encodedToken}";

                var message = $"Please have your password reset by <a href='{callbackUrl}'>clicking here</a>.";


                await _awsService.SendEmailAsync(
                          _configuration["Official:Email"],
                          username,
                          "Password Reset Request",
                          message,
                         message );

                return (true, "An Email has been sent to your registered email");
            }
            return (false, "Invalid username");
        }

        public async Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model)
        {
            var resetPwd = await _onboardingRepository.ResetPassword(model);
            return resetPwd;
        }
        public async Task<(bool Status, string Message)> ChangePassword(ChnagePasswordModel model)
        {
            var changePwd = await _onboardingRepository.ChangePassword(model);
            return changePwd;
        }

    }

}
