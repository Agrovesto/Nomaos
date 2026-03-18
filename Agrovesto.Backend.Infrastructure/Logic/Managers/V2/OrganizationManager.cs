using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOrganizationManager
    {
        Task<bool> AssignAgent(OrganizationAgentAssignmArgs model);
        Task AssignAgentLocation(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<bool> AssignAgentToTole(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<CreateAgentResponseModel> CreateAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<CreateFarmerResponseModel> CreateFarmerAsync(CreateFarmerRequest model, OnBoardingArgs args);
        Task<OrganizationLocationModel> CreateLocation(OrganizationLocationModel model, ListQueryParams query);
        Task<OrganizationRoleModel> CreateRole(OrganizationRoleModel model, ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams query);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<Response<OrganizationAgentDetailResponse>> GetAgentDetail(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams query);
        Task<Response<OrganizationFarmerDetailResponse>> GetFarmerDetailsAsync(ListQueryParams query);
        Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args);
        Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationById(ListQueryParams query);
        Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams query);
        Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(ListQueryParams query);
        Task<OrganizationRoleModel> GetRoleById(ListQueryParams query);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<bool> ReassignAgentToLocation(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<OrganizationLocationModel> UpdateLocationAsync(OrganizationLocationModel model, ListQueryParams query);
    }
    public class OrganizationManager : IOrganizationManager
    {
        private readonly IWalletManager _walletManager;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly INotificationService _sMSService;
        private readonly IOtpService _otpService;
        private readonly ILogger<OrganizationManager> _log;
        private readonly IFarmerRepository _farmerRepository;

        public OrganizationManager(
            IWalletManager walletManager,
            IBusinessRepository businessRepository,
              IOnboardingRepository onboardingRepository,
            IOrganizationRepository organizationRepository,
            IFarmerRepository farmerRepository,
            IAgentRepository agentRepository,
             IActivityLogRepository activityLogRepository,
              IOtpService otpService,
            INotificationService notificationService,
              ILogger<OrganizationManager> log)
        {
            _walletManager = walletManager;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _organizationRepository = organizationRepository;
       _farmerRepository = farmerRepository;
            _agentRepository= agentRepository;
         
            _log = log;
            _otpService = otpService;
            _sMSService = notificationService;
            _activityLogRepository = activityLogRepository;
        }

        public async Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args)
        {
            try
            {  
                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    args.Id = hasOrg.OrganizaionId;
                }
                return await _organizationRepository.GetOrganizationDashboardAsync(args);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }


            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
            return await _organizationRepository.GetLocationAsync(query);

        }
        public async Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }


            var agent = await _agentRepository.GetAgentDetailByUserId(query.UserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "User not found.");

            query.Id = agent.Id;
            return await _organizationRepository.GetAgentAssignedLocations(query);

        }
        public async Task<OrganizationLocationModel> GetLocationById(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetLocationByIAsync(query.Id);

        }
        public async Task<OrganizationLocationModel> UpdateLocationAsync(OrganizationLocationModel model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Location Id is required");
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

                var locationType = await _businessRepository.GetLocationByIAsync(model.LocationTypeId);
                if (locationType == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Invalid Location Type"); }

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                model.BusinessCode = business.BusinessCode;


                return await _organizationRepository.UpdateLocationAsync(query.Id,model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<OrganizationLocationModel> CreateLocation(OrganizationLocationModel model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

                var locationType = await _businessRepository.GetLocationByIAsync(model.LocationTypeId);
                if (locationType == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Invalid Location Type"); }

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                model.BusinessCode = business.BusinessCode;


                return await _organizationRepository.CreateLocationAsync(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetRolesAsync(query);

        }

        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetOrganizationAgents(query);


        }
        public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(
    ListQueryParams query)
        { 
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetOrganizationFarmersAsync(query);


        }
             public async Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args)
        {
          
            var details = await _organizationRepository.GetAgentsByLocationAsync(args);

            if (details == null)
                throw new BaseException(ExceptionType.NOTFOUND, "No record");

            return details;
        }
        public async Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args)
        {

            var details = await _organizationRepository.GetLocationInventoryBreakdownAsync(args);

            if (details == null)
                throw new BaseException(ExceptionType.NOTFOUND, "No record");

            return details;
        }
        public async Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args)
        {

            var details = await _organizationRepository.GetLocationActivityLogsAsync(args);

            if (details == null)
                throw new BaseException(ExceptionType.NOTFOUND, "No record");

            return details;
        }
        public async Task<Response<OrganizationAgentDetailResponse>> GetAgentDetail(ListQueryParams query)
        {
           await EnrichAgentAsync(query);
            var agent = await _organizationRepository.GetOrganizationAgentDetail(query);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return new Response<OrganizationAgentDetailResponse>
            {
                Data = agent,
                Message = "Agent detail retrieved successfully"
            };
        }
        #region agentviews
        //Rework this view to wire real data
        public async Task<PagedListResponse<List<OrganizationAgentActivityLog>>>
    GetAgentActivityLogs(ListQueryParams query)
        {
            await EnrichAgentAsync(query);
            var agent = await _organizationRepository.GetAgentActivityLogs(query);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>>
    GetAgentTransactionHistory(ListQueryParams query)
        {
            await EnrichAgentAsync(query);
            var agent = await _organizationRepository.GetAgentTransactionHistory(query);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return agent;
        }
        public async Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args)

        { 
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetOrganizationAgentPerformanceSummary(args);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args)
        {
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetAgentSubscribers(args);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args)
        {
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetAgentInputAllocations(args);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return agent;
        }
        #endregion agentviews

        private async Task EnrichAgentAsync(ListQueryParams args)
        {
            if (args.Id != 0) return;

            var agent = await _agentRepository.GetAgentDetailByUserId(args.UserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "User not found.");

            args.Id = agent.Id;

        }
       private async Task FetchOrganizationDetails(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
        }

        public async Task<OrganizationRoleModel> GetRoleById(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetRoleByIAsync(query.Id);

        }

        public async Task<OrganizationRoleModel> CreateRole(OrganizationRoleModel model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                model.BusinessCode = business.BusinessCode;


                return await _organizationRepository.CreateRoleAsync(model);
            }
            catch (Exception ex)
            {
                throw new BaseException(
                      ExceptionType.INTERNAL_SERVER_ERROR,
                      "Could not create organization Role.");
            }

        }
        public async Task<bool> AssignAgentToTole(
     
            ProfileRequestModel model,
   OrganizationAgentArgs args)
        {
            var organization = await _businessRepository.GetOrganizationByCodeAsync(args.OrganizationCode);
            if (organization != null)
                model.OrganizationId = organization.OrganizaionId;
            var agentAssignment = new OrganizationAgentArgs
                {
                    OnboardedById = args.OnboardedById,
                    OrganizationId = model.OrganizationId,
                    RoleId = model.RoleId!.Value,
                    UserId = args.UserId
                };
                return await _agentRepository.UpdateOrganizationAgentRole(agentAssignment);


        }
        public async Task AssignAgentLocation(
    ProfileRequestModel model,
    OrganizationAgentArgs args)
        {
            var organization = await _businessRepository.GetOrganizationByCodeAsync(args.OrganizationCode);
            if (organization != null)
                model.OrganizationId = organization.OrganizaionId;
            var agentAssignment = new OrganizationAgentArgs
            {
                LocationId = model.LocationId!.Value,
                OnboardedById = args.OnboardedById,
                OrganizationId = model.OrganizationId,
                UserId = args.UserId
            };

            await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);
        }
        public async Task<bool> ReassignAgentToLocation(ProfileRequestModel model, OrganizationAgentArgs args)
        {
            var business = await GetBusinessOrFail(args.BusinessCode);

            var organization = await _businessRepository.GetOrganizationByCodeAsync(args.OrganizationCode);
            if (organization != null)
                args.OrganizationId = organization.OrganizaionId;
            args.LocationId = model.LocationId!.Value;
            return await _organizationRepository.ReassignAgentToLocation(args);


        }
        public async Task<bool> AssignAgent(
         OrganizationAgentAssignmArgs model)
        {
            try
            {

                var business = await GetBusinessOrFail(model.BusinessCode);

                var organization = await _businessRepository.GetOrganizationByCodeAsync(model.OrganizationCode);
                if (organization != null)
                    model.OrganizationId = organization.OrganizaionId;

                var agentAssignment = new OrganizationAgentArgs
                {
                    LocationId = model.LocationId,
                    OnboardedById = model.OnboardedById,
                    OrganizationId = model.OrganizationId,
                    RoleId = model.RoleId,
                    UserId = model.UserId
                };

                await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);

                await _agentRepository.UpdateOrganizationAgentRole(agentAssignment);


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
                    "Unable to complete onboarding at the moment.");
            }
        }

        public async Task<CreateAgentResponseModel> CreateAgentUser(
            ProfileRequestModel model,
            OnBoardingArgs args)
        {
            try
            {
                ValidateRequest(model, args);

                var business = await GetBusinessOrFail(args.businessCode);

                var organization = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (organization != null)
                    model.OrganizationId = organization.OrganizaionId;

                var onboardingRequest = BuildOnboardingRequest(model);

                var registerUser = await _onboardingRepository.RegisterUser(onboardingRequest, args);

                var agent = await CreateAgent(model, args, registerUser.model.Id);
                
                await AssignAgentToOrganization(model, args, registerUser.model.Id);

                await EnsureWalletExists(registerUser.model, business);

                await _onboardingRepository.TrackUserRoleAssignment(registerUser.model.Id, args.userType);

                return new CreateAgentResponseModel
                {
                    UserId = args.UserId,
                    AgentId = agent.data.Id,
                    AgentUserId = registerUser.model.Id,
                    RequiresProfileCompletion = true,
                    Message= registerUser.message
                };
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete onboarding at the moment.");
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

        private async Task<BusinessModel> GetBusinessOrFail(string businessCode)
        {
            return await _businessRepository.GetBusinessByCodeAsync(businessCode)
                ?? throw new BaseException(ExceptionType.NOTFOUND, "Business does not exist");
        }
        private OnboardingRequestModel BuildOnboardingRequest(ProfileRequestModel model)
        {
            return new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                Pin = model.Pin,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName
            };
        }
        private async Task<(bool status, AgentModel data)> CreateAgent(
    ProfileRequestModel model,
    OnBoardingArgs args,
    string userId)
        {
            return await _agentRepository.CreateUpdateAgent(new AgentModel
            {
                BelongToBusinessCode = args.businessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                Name = BuildFullName(model),
                OnBoardedOn = DateTimeOffset.Now,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                Ward = model.Ward,
                OnboardedById = args.OnboardedByUserId,
                OrganizationId = model.OrganizationId
            });
        }
        public async Task AssignAgentToOrganization(
        ProfileRequestModel model,
        OnBoardingArgs args,
        string userId)
        {
            if (!model.OrganizationId.HasValue)
                throw new ValidationException("Organization is required");

            var agentAssignment = new OrganizationAgentArgs
            {
                        // nullable
                OrganizationId = model.OrganizationId,
                OnboardedById = args.OnboardedByUserId,
                UserId = userId
            };

            if (model.LocationId.HasValue)
            {
                agentAssignment.LocationId = model.LocationId.Value;
                await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);
            }
            if (model.RoleId.HasValue)
            {
                agentAssignment.RoleId = model.RoleId.Value;
                await _agentRepository.UpdateOrganizationAgentRole(agentAssignment);
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
        private void ValidateRequest(ProfileRequestModel model, OnBoardingArgs args)
        {
            ValidateQuickOnboarding(new OnboardingRequestModel
            {
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            });

            if (string.IsNullOrWhiteSpace(args.businessCode))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");

            //if (!model.LocationId.HasValue)
            //    throw new BaseException(ExceptionType.BAD_REQUEST, "LocationId is required");

            //if (!model.RoleId.HasValue)
            //    throw new BaseException(ExceptionType.BAD_REQUEST, "RoleId is required");
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


        #region FarmerMgt
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

        public async Task<CreateFarmerResponseModel> CreateFarmerAsync(
       CreateFarmerRequest model,
       OnBoardingArgs args)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();

                _log.LogInformation("Starting farmer onboarding {CorrelationId}", correlationId);

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }

                ValidateCriticalInputs(model, args);

                var dateTime = DateTimeOffset.UtcNow;

                var business = await GetBusinessOrFail(args.businessCode);

                var user = await RegisterFarmerOrFail(model, args);

                var farmerResult = await CreateFarmerOrFails(
                    model, args, business, user.model.Id, dateTime);

                model.Id = farmerResult.data.Id;

                user.model.LogDescription = $"Added Farmer {model.FirstName} {model.LastName}";
                user.model.ActivityName = $"New Farmer Added";

                // ✅ Non-critical (safe)
                await RunNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateFarmerResponseModel
                {
                    UserId = user.model.Id,
                    RequiresProfileCompletion = true,
                    Message = user.message
                };
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateFarmerAsync failed unexpectedly");
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to complete onboarding at the moment.");
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
        private async Task LogActivity(OnboardingRequestModel model, OnBoardingArgs args)
        {
            var subAccount =
                                    await _activityLogRepository.OnLogActivtyAsync(new Entities.Common.ActivityLog
                                    {
                                        PerformedByUserId = args.UserId,
                                        Description = $"{model.LogDescription}",
                                        ActivityName = $"{model.ActivityName}",
                                        ActivityType = Entities.Common.ActivityType.Onboarding,
                                        BusinessCode = args.businessCode,
                                        OrganizationCode = args.organizationCode,
                                        CreatedOn = DateTimeOffset.Now,

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
        private async Task<(bool status, FarmerModel data)> CreateFarmerOrFails(
       CreateFarmerRequest model,
       OnBoardingArgs args,
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

            var farmer = new FarmerModel
            {
                OnboardedByUserId = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Email = model.Email,
                Gender = model.Gender,
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name = fullName,
                OnBoardedOn = dateTime, 
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                DateOfBirth = model.DateOfBirth,

                CooperativeIds = model.CooperativeIds?
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToList(),

                AgentIds = model.AgentIds?
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToList(),

                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,
                Level = FarmerLevel.Level1.ToString(),
                HighestLevelOfEducation = model.HighestLevelOfEducation
            };

            if (!string.IsNullOrWhiteSpace(model.NIN) || !string.IsNullOrWhiteSpace(model.Bvn))
            {
                if (!string.IsNullOrWhiteSpace(model.NIN))
                    ValidateIdentifier(model.NIN, "NIN");

                if (!string.IsNullOrWhiteSpace(model.Bvn))
                    ValidateIdentifier(model.Bvn, "BVN");

                await _onboardingRepository.OnNINBVNUpdate(new Core.Models.Common.UserModel
                {
                    Id = userId,
                    Bvn = model.Bvn,
                    NIN = model.NIN,
                });

                farmer.Level = FarmerLevel.Level3.ToString();
                farmer.BVN = model.Bvn;
                farmer.NIN = model.NIN;
            }

            var result = await _organizationRepository.CreateOrUpdateFarmer(farmer);

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

        public async Task<Response<OrganizationFarmerDetailResponse>> GetFarmerDetailsAsync(ListQueryParams query)
        {
            await EnrichFarmerAsync(query);
            var farmer = await _organizationRepository.GetFarmerDetailsAsync(query);

            if (farmer == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Farmer Detail not found");

            return new Response<OrganizationFarmerDetailResponse>
            {
                Data = farmer,
                Message = "Farmer detail retrieved successfully"
            };
        }
        private async Task EnrichFarmerAsync(ListQueryParams args)
        {
            if (args.Id != 0) return;

            var farmer = await _farmerRepository.GetFarmerByUserId(args.UserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "User not found.");

            args.Id = farmer.Id;

        }
        #endregion FarmerMgt
    }

}

