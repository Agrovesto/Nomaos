using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using DocumentFormat.OpenXml.Spreadsheet;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOrganizationManager
    {
        Task AssignAgentLocation(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<bool> AssignAgentToTole(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<CreateAgentResponseModel> CreateAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<OrganizationLocationModel> CreateLocation(OrganizationLocationModel model, ListQueryParams query);
        Task<OrganizationRoleModel> CreateRole(OrganizationRoleModel model, ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams query);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<Response<OrganizationAgentDetailResponse>> GetAgentDetail(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams query);
        Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationById(ListQueryParams query);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams query);
        Task<OrganizationRoleModel> GetRoleById(ListQueryParams query);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<bool> ReassignAgentToLocation(ProfileRequestModel model, OrganizationAgentArgs args);
    }
    public class OrganizationManager : IOrganizationManager
    {
        private readonly IWalletManager _walletManager;
        private readonly IBusinessRepository _businessRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IAgentRepository _agentRepository;
        public OrganizationManager(IBusinessRepository businessRepository,
            IOrganizationRepository organizationRepository,
            IOnboardingRepository onboardingRepository, IWalletManager walletManager, IAgentRepository agentRepository)
        {
            _organizationRepository = organizationRepository;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _organizationRepository = organizationRepository;
            _agentRepository= agentRepository;
            _walletManager = walletManager;
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
                    RequiresProfileCompletion = true
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
        private async Task AssignAgentToOrganization(
    ProfileRequestModel model,
    OnBoardingArgs args,
    string userId)
        {
            var agentAssignment = new OrganizationAgentArgs
            {
                LocationId = model.LocationId!.Value,
                OnboardedById = args.OnboardedByUserId,
                OrganizationId = model.OrganizationId,
                RoleId = model.RoleId!.Value,
                UserId = userId
            };

            await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);
            await _agentRepository.UpdateOrganizationAgentRole(agentAssignment);
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

            if (!model.LocationId.HasValue)
                throw new BaseException(ExceptionType.BAD_REQUEST, "LocationId is required");

            if (!model.RoleId.HasValue)
                throw new BaseException(ExceptionType.BAD_REQUEST, "RoleId is required");
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

    }

}

