
using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BulkUploadJob = Agrovesto.Backend.Infrastructure.Entities.Common.BulkUploadJob;
using DateTime = System.DateTime;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;
namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOrganizationManager
    {
        Task<bool> RemoveAgentFromLocationAsync(RemoveAgentFromLocationDto dto);
        Task<LocationAgentsResponseDto> GetLocationAgentsAsync(ListQueryParams query);
        Task<bool> ReassignPrimaryAgentAsync(ReassignPrimaryAgentDto dto);
        Task<bool> AssignAgent(OrganizationAgentAssignmArgs model);
        Task AssignAgentLocation(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<bool> AssignAgentToTole(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args);
        Task<BulkOnboardingQueueResponse> BulkOnboardFarmersBatchAsync(Stream excelStream, OnBoardingArgs args);
        Task<CreateAgentResponseModel> CreateAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<CreateFarmerResponseModel> CreateFarmerAsync(CreateFarmerRequest model, OnBoardingArgs args);
        Task<LocationCreateResponse> CreateLocation(
      CreateLocationDto model,
      ListQueryParams query);
        Task<CreateMembershipResponseModel> CreateMembershipUser(MemberShipRequestModel model, OnBoardingArgs args);
        Task<OrganizationRoleModel> CreateRole(OrganizationRoleModel model, ListQueryParams query);
        Task<OrganizationLocationModel> CreateSubLocationAsync(CreateSubLocationDto model, ListQueryParams query);
        Task<bool> DeactivateFarmerFromOrganization(AssignmentArgs args, string? message =null);
        Task<bool> DeleteLocation(DeleteLocationDto model, ListQueryParams query);
        Task<bool> DeleteSubLocationAsync(DeleteLocationDto model, ListQueryParams query);
        Task<OrganizationLocationModel> EditSubLocationAsync(EditSubLocationDto model, ListQueryParams query);
        Task<PagedListResponse<List<OrganizationFarmerSalesHistor>>> FarmerSalesHistory(ListQueryParams request, CancellationToken cancellationToken);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams query);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<Response<OrganizationAgentDetailResponse>> GetAgentDetail(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams query);
        Task<Response<OrganizationFarmerDetailResponse>> GetFarmerDetailsAsync(ListQueryParams query);
            Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args);
        Task<PagedListResponse<List<LocationListResponseDto>>> GetLocationAsync(ListQueryParams query);
        Task<LocationResponseDto> GetLocationById(ListQueryParams query);
        Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationMemberShipResponseModel>>> GetMembershipsByOrganization(ListQueryParams query);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams query);
        Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(ListQueryParams query);
        Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(ListQueryParams args);
        Task<OrganizationRoleModel> GetRoleById(ListQueryParams query);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<PagedListResponse<List<FarmerSalesHistoryModel>>> GetSalesHistoryByFarmer(ListQueryParams query);
        Task<Response<FarmerSalesSummaryDto?>> GetSalesSummary(ListQueryParams args);
        Task<LocationResponseDto> GetSubLocationByIdAsync(ListQueryParams query);
        Task<PagedListResponse<List<LocationResponseDto>>> GetSubLocationsAsync(ListQueryParams query);
        Task<PagedListResponse<List<SubLocationListDto>>> GetSubLocationsByParentAsync(long parentLocationId, ListQueryParams query);
        Task<bool> ReassignAgentToLocation(ProfileRequestModel model, OrganizationAgentArgs args);
        Task<bool> RemoveFarmerFromCooperatives(string userId, List<long> cooperativeIds, string? organizationCode);
        Task<OrganizationLocationModel> UpdateLocationAsync(EditLocationDto model, ListQueryParams query);
        Task<LocationInventory> AddInventoryAsync(CreateInventoryDto model, ListQueryParams query);
        Task<LocationInventory> EditInventoryAsync(EditInventoryDto model, ListQueryParams query);
        Task<PagedListResponse<List<InventoryListDto>>> GetInventoryByLocationAsync(ListQueryParams query);
        Task<LocationDashboardFullDto> GetFullDashboardAsync(ListQueryParams query);
        Task<OrganizationProductCategory> CreateProductCategoryAsync(ProductCategoryDto model, ListQueryParams query);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetCategories(ListQueryParams query);
        Task<OrganizationProduct> CreateOrganizationProduct(OrganizationProductDto model, ListQueryParams query);
        Task<PagedListResponse<List<OrganizationProductDto>>> GetOrganizationProductList(ListQueryParams query);
        Task<OrganizationProductDto> GetorganizationProductById(ListQueryParams query);
        Task<bool> EditProductAsync(ListQueryParams query, EditOrganizationProductDto request);
        Task<bool> DeactivateProductAsync(ListQueryParams query);
        Task<(bool Success, string Message)> AssignAgentToLocation(LocationAgentAssignment model, ListQueryParams query);
        Task DeleteInventory(DeleteLocationDto model, ListQueryParams query);
        Task SyncLocationStockLevels(ListQueryParams query);
        Task<bool> EditAgentUser(ProfileRequestModel model, OnBoardingArgs args);
        Task<PagedListResponse<List<AgentLocationListResponseDto>>> GetLocationsByAgentAsync(ListQueryParams query);
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
        private readonly ILogger<OrganizationManager> _log;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly UserManager<User> _userManager;
        private readonly ISalesRepository _salesRepository;
        private readonly DataContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IAWSService _awsService;
        private readonly IFarmerTransactionRepository _farmerTransactionRepository;
        private readonly IConfiguration _configuration;
        private readonly IAWSRekognitionManager _faceManager;
        public OrganizationManager(IAWSRekognitionManager faceManager,
            IFarmerTransactionRepository farmerTransactionRepository,
            IWalletManager walletManager,
            IBusinessRepository businessRepository,
              IOnboardingRepository onboardingRepository,
            IOrganizationRepository organizationRepository,
            IFarmerRepository farmerRepository,
            IAgentRepository agentRepository,
             IActivityLogRepository activityLogRepository,
            INotificationService notificationService,
            IMembershipRepository membershipRepository,
            UserManager<User> userManager,
            ISalesRepository salesRepository,
            DataContext context,
              ILogger<OrganizationManager> log,
              IBackgroundTaskQueue taskQueue,
              IServiceScopeFactory scopeFactory,
               IAWSService awsService,
               IConfiguration configuration)
        {
            _faceManager = faceManager;
            _farmerTransactionRepository =farmerTransactionRepository;
            _context=context;
            _walletManager = walletManager;
            _businessRepository = businessRepository;
            _onboardingRepository = onboardingRepository;
            _organizationRepository = organizationRepository;
       _farmerRepository = farmerRepository;
            _agentRepository= agentRepository;
            _userManager = userManager;
            _log = log;
            _sMSService = notificationService;
            _activityLogRepository = activityLogRepository;
            _membershipRepository = membershipRepository;
            _salesRepository = salesRepository;
            _taskQueue = taskQueue;
            _scopeFactory=scopeFactory;
            _awsService = awsService;

            _configuration = configuration;
        }

        #region salesTracker
        public async Task<PagedListResponse<List<FarmerSalesHistoryModel>>> GetSalesHistoryByFarmer(
         ListQueryParams query)
        {
            try
            {
                var transactions = await _farmerTransactionRepository.GetSalesHistory(query);
                if (transactions == null) return new PagedListResponse<List<FarmerSalesHistoryModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<FarmerSalesHistoryModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving sales: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<Response<FarmerSalesSummaryDto?>> GetSalesSummary(
   ListQueryParams args)
        {
            try
            {
                var transactions = await _farmerTransactionRepository.GetSalesSummary(args);
             
                return new Response<FarmerSalesSummaryDto?>
                {
                    Succeeded = true,
                    Data = transactions,

                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving sales: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        #endregion salesTracker
        #region bulkupload
        public async Task<BulkOnboardingQueueResponse> BulkOnboardFarmersBatchAsync(
    Stream excelStream,
    OnBoardingArgs args)
        {
            if (excelStream == null)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "Excel file is required.");

            // Ensure services exist
            if (_taskQueue == null || _scopeFactory == null)
            {
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Background processing is not configured.");
            }

            // Save file directly to disk
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

            // Create job immediately
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
            //_taskQueue.QueueTask(async cancellationToken =>
            //{
            //    try
            //    {
            //        using var scope = _scopeFactory.CreateScope();

            //        var processor = scope.ServiceProvider
            //            .GetRequiredService<BulkOnboardingProcessor>();

            //        await processor.ProcessAsync(
            //            filePath,
            //            args,
            //            job.Id,
            //            cancellationToken);
            //    }
            //    catch (Exception ex)
            //    {
            //        using var scope = _scopeFactory.CreateScope();

            //        var logger = scope.ServiceProvider
            //            .GetRequiredService<ILogger<OrganizationManager>>();

            //        logger.LogError(
            //            ex,
            //            "Queued bulk onboarding failed for JobId {jobId}",
            //            job.Id);
            //    }
            //});

            return new BulkOnboardingQueueResponse
            {
                JobId = job.Id,
                Status = "Queued",
                Message = "Bulk onboarding queued successfully."
            };
        }
        /**   public async Task<BulkOnboardingResult> BulkOnboardFarmersBatchAsync(
               Stream excelStream,
               OnBoardingArgs args)
           {
               if (excelStream == null)
                   throw new BaseException(ExceptionType.BAD_REQUEST, "Excel file is required.");
               using var ms = new MemoryStream();
               await excelStream.CopyToAsync(ms);
               ms.Position = 0;

               var rows = _excelConverterService.BulkParseExcel(ms);

               if (!rows.Any())
                   throw new BaseException(ExceptionType.BAD_REQUEST, "Excel file contains no records.");

               var result = new BulkOnboardingResult
               {
                   Total = rows.Count,
                   Errors = new List<BulkOnboardingError>(),
                   Successful = 0
               };

               var tempDir = Path.Combine(Path.GetTempPath(), "Agrovesto", "bulk");
               Directory.CreateDirectory(tempDir);
               var tmpFile = Path.Combine(tempDir, $"bulk_{Guid.NewGuid()}.xlsx");
               ms.Position = 0;
               await using (var fs = File.Create(tmpFile))
               {
                   ms.Position = 0;
                   await ms.CopyToAsync(fs);
                   await fs.FlushAsync();
               }

               if (_taskQueue == null || _scopeFactory == null)
               {
                   throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR, "Background processing is not configured. IBackgroundTaskQueue or IServiceScopeFactory is missing.");
               }

               _taskQueue.QueueTask(async cancellationToken =>
               {
                   try
                   {
                       if (_scopeFactory == null)
                       {
                           return;
                       }

                       using var scope = _scopeFactory.CreateScope();
                       var processor = scope.ServiceProvider.GetRequiredService<BulkOnboardingProcessor>();
                       var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrganizationManager>>();
                       logger.LogInformation("Queued bulk onboarding job invoking processor for file {file}", tmpFile);

                    
                       await processor.ProcessAsync(tmpFile, args, cancellationToken);
                         }
                   catch (Exception ex)
                   {
                       try
                       {
                           if (_scopeFactory != null)
                           {
                               using var scope = _scopeFactory.CreateScope();
                               var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrganizationManager>>();
                               logger.LogError(ex, "Background bulk onboarding failed to start processor");
                           }
                       }
                       catch { }
                   }
               });
               result.Message = "File has been queued for upload and is being processed in the background. You will receive a notification once processing is complete.";

               return result; 
       }
           **/
        private List<ValueChainSelectionDto> GetValueChains(BulkOnboardingExcelRow row)
        {
            if (string.IsNullOrWhiteSpace(row.PartOfAgriculture))
                return new List<ValueChainSelectionDto>();

            var items = string.IsNullOrWhiteSpace(row.ProductTypes)
                ? new List<string>()
                : row.ProductTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            return new List<ValueChainSelectionDto>
    {
        new() { ValueChainType = row.PartOfAgriculture, Items = items }
    };
        }

        private bool IsValidEmail(string email) => new EmailAddressAttribute().IsValid(email);
        private bool IsValidPhone(string phone) => Regex.IsMatch(phone, @"^\+?\d{10,15}$");    
        #endregion bulkupload

        private async Task SetOnboardingStatus(string userId, FarmerOnboardingStatus status, string error = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.FarmerOnboardingStatus = status;
                user.OnboardingError = error;
                await _userManager.UpdateAsync(user);
            }
        }
       
        //public async Task RetryFailedOnboarding(string userId)
        //{
        //    var user = await _userManager.FindByIdAsync(userId);

        //    if (user == null)
        //        throw new Exception("User not found");

        //    if (user.FarmerOnboardingStatus != FarmerOnboardingStatus.Failed)
        //        throw new Exception("User is not in failed state");

        //    // rebuild request (important)
        //    var model = new CreateFarmerRequest
        //    {
        //        FirstName = user.Name,
        //        LastName = user.Name,
        //        Email = user.Email,
        //        PhoneNumber = user.PhoneNumber,
        //        // fill other fields as needed
        //    };

        //    var args = new OnBoardingArgs
        //    {
        //        businessCode = user.BusinessCode,
        //        organizationCode = user.OrganizationCode
        //    };

        //    await CreateFarmerAsync(model, args);
        //}
        private string GenerateUserId(OnBoardingArgs args)
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomDigits = new Random().Next(100, 999);

            if (!string.IsNullOrEmpty(args.businessCode))
                return $"{args.businessCode}{datePart}{randomDigits}";

            if (!string.IsNullOrEmpty(args.organizationCode))
                return $"{args.organizationCode}{datePart}{randomDigits}";

            return Guid.NewGuid().ToString();
        }
        private User BuildUserEntity(OnboardingRequestModel model, OnBoardingArgs args, byte[] salt)
        {
            return new User
            {
                Id = GenerateUserId(args),
                UserName = model.Email ?? model.PhoneNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Pin = model.Pin,
                PinEncrypt = string.IsNullOrEmpty(model.Pin)
                    ? null
                    : model.Pin.EncryptTransactionPin(salt),
                Salt = Convert.ToBase64String(salt),
                Password = model.Password,
                CreatedOn = DateTimeOffset.Now,
                IsActive = true,
                UserType = args.userType,
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                Name = model.FullName

            };
        }
        public async Task<(OnboardingRequestModel model, bool status, string message)> RegisterOrganizationUserAsync(OnboardingRequestModel model, OnBoardingArgs args, string role)
        {
          

            var existingUser = await _onboardingRepository.FindExistingUserAsync(model);
            if (existingUser != null)
            {
                bool hasRole = await _userManager.IsInRoleAsync(existingUser, role);
                   

                if (!hasRole)
                {
                    var result = await _userManager.AddToRoleAsync(existingUser, role);

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                    }
                }

            }
            else
            {
                var salt = EncryptionUtil.GetSalt();

                var user = BuildUserEntity(model, args, salt);

                IdentityResult createUser = string.IsNullOrEmpty(model.Password)
                    ? await _userManager.CreateAsync(user)
                    : await _userManager.CreateAsync(user, model.Password);

                if (!createUser.Succeeded)
                {
                    var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));

                }

                await _userManager.AddToRoleAsync(user, role);

                existingUser = user;
            }

            bool exists = await _context.UserOrganizations
               .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode);

            if (!exists)
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = existingUser.Id,
                    OrganizationCode = args.organizationCode
                });
            }
            return (
              new OnboardingRequestModel
              {
                  Email = existingUser.Email,
                  PhoneNumber = existingUser.PhoneNumber,
                  Id = existingUser.Id
              },
                true,
                "Registration Successful.");
        }

        //public async Task<BulkOnboardingResult> BulkOnboardFarmers(Stream excelStream, OnBoardingArgs args)
        //{
        //    string userId = "";
        //    var dateTime = DateTimeOffset.UtcNow;

        //    var business = await GetBusinessOrFail(args.businessCode);
        //    var role = await _roleManager.FindByNameAsync(args.userType);

        //    if (role == null)
        //        throw new BaseException(ExceptionType.BAD_REQUEST, "Could not map role.");

        //    if (excelStream == null)
        //        throw new BaseException(
        //            Core.Enums.ExceptionType.BAD_REQUEST,
        //            "Excel file is required.");

        //    var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
        //    if (hasOrg != null)
        //    {
        //        args.OrganizationId = hasOrg.OrganizaionId;
        //    }

        //    var rows = _excelConverterService.ParseExcel(excelStream);

        //    if (!rows.Any())
        //        throw new BaseException(
        //            Core.Enums.ExceptionType.BAD_REQUEST,
        //            "Excel file contains no records.");

        //    var result = new BulkOnboardingResult
        //    {
        //        Total = rows.Count,
        //        Errors = new List<BulkOnboardingError>()
        //    };

        //    int rowNumber = 0;

        //    var tasks = new List<Task>();
        //    var semaphore = new SemaphoreSlim(5); // safe concurrency

        //    foreach (var row in rows)
        //    {
        //        rowNumber++;
        //        var currentRow = row;
        //        var currentRowNumber = rowNumber;

        //        await semaphore.WaitAsync();

        //        tasks.Add(Task.Run(async () =>
        //        {
        //            try
        //            {
        //                // Validate YearsInAgric
        //                if (!long.TryParse(currentRow.YearsInAgric, out var years))
        //                {
        //                    throw new BaseException(
        //                        Core.Enums.ExceptionType.BAD_REQUEST,
        //                        $"Invalid YearsInAgric: {currentRow.YearsInAgric}");
        //                }

        //                // ValueChain mapping
        //                var valueChains = new List<ValueChainSelectionDto>();

        //                if (!string.IsNullOrWhiteSpace(currentRow.PartOfAgriculture))
        //                {
        //                    var items = string.IsNullOrWhiteSpace(currentRow.ProductTypes)
        //                        ? new List<string>()
        //                        : currentRow.ProductTypes
        //                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                            .Select(x => x.Trim())
        //                            .ToList();

        //                    valueChains.Add(new ValueChainSelectionDto
        //                    {
        //                        ValueChainType = currentRow.PartOfAgriculture,
        //                        Items = items
        //                    });
        //                }

        //                // Validate contact
        //                if (string.IsNullOrWhiteSpace(currentRow.Email) &&
        //                    string.IsNullOrWhiteSpace(currentRow.PhoneNumber))
        //                {
        //                    throw new BaseException(
        //                        Core.Enums.ExceptionType.BAD_REQUEST,
        //                        "Either phone or email is required.");
        //                }

        //                FarmerModel model = BuildFarmerEntity(args, currentRow, years, valueChains);
                       
        //                BuildOnboardingArgs(args);

        //                OnboardingRequestModel registrationModel = new OnboardingRequestModel
        //                {
        //                    Email = model.Email,
        //                    PhoneNumber = model.PhoneNumber,
        //                    Password = "Admin123@",
        //                    Pin ="0000",
        //                    FirstName = currentRow.FirstName,
        //                    MiddleName = currentRow.MiddleName,
        //                    LastName = currentRow.LastName,
        //                };
        //                // STEP 1: Create user
        //                var user = await RegisterOrganizationUserAsync(registrationModel, args, role.Name);
        //                userId = user.model.Id;

        //                model.UserId = userId;
        //                model.OnBoardedOn = dateTime;

        //                if (!string.IsNullOrWhiteSpace(currentRow.NIN) || !string.IsNullOrWhiteSpace(currentRow.BVN))
        //                {
        //                    if (!string.IsNullOrWhiteSpace(currentRow.NIN))
        //                        ValidateIdentifier(currentRow.NIN, "NIN");

        //                    if (!string.IsNullOrWhiteSpace(currentRow.BVN))
        //                        ValidateIdentifier(currentRow.BVN, "BVN");

        //                    await _onboardingRepository.OnNINBVNUpdate(new Core.Models.Common.UserModel
        //                    {
        //                        Id = userId,
        //                        Bvn = currentRow.BVN,
        //                        NIN = model.NIN,
        //                    });

        //                    model.Level = FarmerLevel.Level3.ToString();
        //                    model.BVN = model.BVN;
        //                    model.NIN = model.NIN;
        //                }
        //                // STEP 2: Create farmer
        //                var farmerResult = await _organizationRepository.CreateOrUpdateFarmer(model);

        //                model.Id = farmerResult.data.Id;

        //                // Non-critical steps
        //                await LogActivity(user.model, args);
        //                await EnsureWalletExists(user.model, business);
        //                await _onboardingRepository.TrackUserRoleAssignment(user.model.Id, args.userType);
        //                await GenerateAndSendOtp(user.model);

        //                // await CreateOrganizationFarmerAsync(model, farmerArgs);

        //                lock (result)
        //                {
        //                    result.Successful++;
        //                }
        //            }
        //            catch (BaseException ex)
        //            {
        //                lock (result)
        //                {
        //                    result.Failed++;
        //                    result.Errors.Add(new BulkOnboardingError
        //                    {
        //                        RowNumber = currentRowNumber,
        //                        Email = currentRow.Email,
        //                        Error = ex.Message
        //                    });
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _log.LogError(ex, "Error on row {Row}", currentRowNumber);

        //                lock (result)
        //                {
        //                    result.Failed++;
        //                    result.Errors.Add(new BulkOnboardingError
        //                    {
        //                        RowNumber = currentRowNumber,
        //                        Email = currentRow.Email,
        //                        Error = "Unexpected error"
        //                    });
        //                }
        //            }
        //            finally
        //            {
        //                semaphore.Release();
        //            }
        //        }));
        //    }

        //    await Task.WhenAll(tasks);

        //    return result;
        //}

        private static void BuildOnboardingArgs(OnBoardingArgs args)
        {
            var farmerArgs = new OnBoardingArgs
            {
                businessCode = args.businessCode,
                organizationCode = args.organizationCode,
                userType = "Farmer",
                OnboardedByUserId = args.OnboardedByUserId,
                AgentIds = args.AgentIds,
                CooperativeIds = args.CooperativeIds
            };
        }

        private static Farmer BuildFarmerEntity(OnBoardingArgs args, BulkOnboardingExcelRow currentRow, long years, List<ValueChainSelectionDto> valueChains)
        {
           

            var fullName = string.Join(" ",
                new[] { currentRow.FirstName, currentRow.MiddleName, currentRow.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

           return new Farmer
            {
                OnboardedByUserId = args.OnboardedByUserId,
                BelongToBusinessCode = args.businessCode,
                Email = currentRow.Email,
                Gender = currentRow.Gender,
                State = currentRow.StateOfResidence,
                Lga = currentRow.LGA,
                Location = currentRow.ResidentialAddress,
                Name = fullName,
                PhoneNumber = currentRow.PhoneNumber,
                DateOfBirth = currentRow.DateOfBirth,
                Level= FarmerLevel.Level1,
                
                
               Ward = currentRow.Ward,
                FarmerCategory = currentRow.CategoryOfFarmer,
                YearsInAgric = years,
                HighestLevelOfEducation = currentRow.HighestLevelOfEducation,


            };

            
            
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

        #region product
        public async Task<OrganizationProductCategory> CreateProductCategoryAsync(ProductCategoryDto model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                model.BusinessCode = business.BusinessCode;
                var response = await _organizationRepository.CreateCategory(model);
                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PagedListResponse<List<ProductCategoryDto>>> GetCategories(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                return await _organizationRepository.GetCategories(query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<ProductCategoryDto> GetCategoryById(long id)
        {
            try
            {
                return await _organizationRepository.GetCategoryById(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        #endregion product
        #region organization product
        public async Task<OrganizationProduct> CreateOrganizationProduct(OrganizationProductDto model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null)
                    throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found");

                var organization = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
                if (organization != null)
                    model.OrganizationId = organization.OrganizaionId;

                model.BusinessCode = business.BusinessCode;

                return await _organizationRepository.CreateOrganizationProduct(model);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<PagedListResponse<List<OrganizationProductDto>>> GetOrganizationProductList(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetOrganizationProductList(query);
        }

        public async Task<OrganizationProductDto> GetorganizationProductById(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Product Id is required");

            return await _organization_repository_compat_GetById(query.Id);
        }

        // small compatibility shim to call repository (keeps logic readable in one place)
        private async Task<OrganizationProductDto> _organization_repository_compat_GetById(long id)
        {
            return await _organizationRepository.GetorganizationProductById(id);
        }

        public async Task<bool> EditProductAsync(ListQueryParams query, EditOrganizationProductDto request)
        {
            if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Product Id is required");
            return await _organizationRepository.EditProductAsync(query.Id, request);
        }

        public async Task<bool> DeactivateProductAsync(ListQueryParams query)
        {
            if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Product Id is required");
            return await _organizationRepository.DeactivateProductAsync(query.Id);
        }
        #endregion organization product
        #region location
        public async Task<LocationCreateResponse> CreateLocation(
        CreateLocationDto model,
        ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode)
      ?? throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Business record not found");

            var locationType = await _businessRepository.GetLocationByIAsync(model.LocationTypeId)
          ?? throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Invalid location type");

            var organization = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            model.OrganizationId = organization?.OrganizaionId;
            model.BusinessCode = business.BusinessCode;
            model.CreatedByUserId = query.UserId;

            var location = await _organizationRepository.CreateLocationAsync(model);

            //var agentResult = await TryAssignAgentsSafe(location.Id, model, query);

            return location.CreateViewMap();
               // AgentAssigned = agentResult.Success,
                //AgentWarning = agentResult.Message
          
        }
        public async Task<(bool Success, string Message)> AssignAgentToLocation(
        LocationAgentAssignment model,
        ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode)
      ?? throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Business record not found");

            var organization = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            model.OrganizationId = organization?.OrganizaionId;

            var assignAgent = await TryAssignAgentsSafe(query.Id, model, query);

            return assignAgent;
               // AgentAssigned = agentResult.Success,
                //AgentWarning = agentResult.Message
          
        }

        private async Task<(bool Success, string Message)> TryAssignAgentsSafe(
        long locationId,
        LocationAgentAssignment model,
        ListQueryParams query)
        {
            try
            {
                bool assignedAny = false;


                if (!string.IsNullOrWhiteSpace(model.AgentUserId))
                {
                    var mainAgent = await _agentRepository.GetAgentDetailByUserId(model.AgentUserId);
                    if (mainAgent != null)
                    {
                        await AssignAgent(locationId, model.OrganizationId.Value, model.AgentUserId, query.UserId, model.IsPrimary);
                        assignedAny = true;
                    }
                }

                //if (!string.IsNullOrWhiteSpace(model.SupportinAgentUserId))
                //{
                //    var supportAgent = await _agentRepository.GetAgentDetailByUserId(model.SupportinAgentUserId);
                //    if (supportAgent != null)
                //    {
                //        await AssignAgent(locationId, model.OrganizationId.Value, model.SupportinAgentUserId, query.UserId, false);
                //        assignedAny = true;
                //    }
                //}

                return assignedAny
                    ? (true, "Agent(s) assigned successfully.")
                    : (false, "No agents were assigned at this time.");
            }
            catch (Exception ex)
            {
                return (false, $"Location created but agent assignment failed: {ex.Message}");
            }
        }


        private async Task AssignAgent(
            long locationId,
            long organizationId,
            string userId,
            string onboardedBy,
            bool IsPrimary = false)
        {
            var args = new OrganizationAgentArgs
            {
                LocationId = locationId,
                OrganizationId = organizationId,
                UserId = userId,
                OnboardedById = onboardedBy,
                IsPrimary=IsPrimary,
                
            };

            await _agentRepository.UpdateOrganizationAgentLocation(args);
        }
     
        public async Task<OrganizationLocationModel> UpdateLocationAsync(EditLocationDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Location Id is required");
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                model.Id = query.Id;
                var response = await _organizationRepository.EditLocationAsync(model);

                return response.ViewMap();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<OrganizationLocationModel> CreateSubLocationAsync(CreateSubLocationDto model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

                model.CreatedByUserId = query.UserId;

                var response = await _organizationRepository.CreateSubLocationAsync(model);

                return response.ViewMap();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PagedListResponse<List<LocationListResponseDto>>> GetLocationAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetLocationsAsync(query);

        }
        public async Task<LocationAgentsResponseDto> GetLocationAgentsAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }


            return await _organizationRepository.GetLocationAgentsAsync(query.Id);

        }
        public async Task<LocationDashboardFullDto> GetFullDashboardAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetFullDashboardAsync(query.Id);

        }
        public async Task SyncLocationStockLevels(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            await FetchOrganizationDetails(query);
             await _organizationRepository.SyncLocationStockLevels();

        }
        public async Task<bool> ReassignPrimaryAgentAsync(ReassignPrimaryAgentDto dto)
        {
            
            return await _organizationRepository.ReassignPrimaryAgentAsync(dto);

        }
        public async Task<bool> RemoveAgentFromLocationAsync(RemoveAgentFromLocationDto dto)
        {

            return await _organizationRepository.RemoveAgentFromLocationAsync(dto);

        }
        public async Task<LocationResponseDto> GetLocationById(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetLocationByIdAsync(query.Id);

        }
        public async Task<PagedListResponse<List<AgentLocationListResponseDto>>> GetLocationsByAgentAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            //var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            //if (hasOrg != null)
            //{
            //    query.OrganizationId = hasOrg.OrganizaionId;
            //}

            return await _organizationRepository.GetLocationsByAgentAsync(query);

        }
       
        public async Task<bool> DeleteLocation(DeleteLocationDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Location Id is required");
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                model.UserId = query.UserId;
                model.Id = query.Id;
                await _organizationRepository.DeleteLocationAsync(model);
                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<OrganizationLocationModel> EditSubLocationAsync(EditSubLocationDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Location Id is required");
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                model.Id = query.Id;
                var response = await _organizationRepository.EditSubLocationAsync(model);

                return response.ViewMap();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<bool> DeleteSubLocationAsync(DeleteLocationDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Location Id is required");
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
                model.UserId = query.UserId;
                model.Id = query.Id;
                await _organizationRepository.DeleteSubLocationAsync(model);
                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PagedListResponse<List<LocationResponseDto>>> GetSubLocationsAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            //query.Id = query.Id <= 0 ? throw new BaseException(ExceptionType.BAD_REQUEST, "Parent Location Id is required") : query.Id;
            return await _organizationRepository.GetSubLocationsAsync(query);

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
        public async Task<LocationResponseDto> GetSubLocationByIdAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetSubLocationByIdAsync(query.Id);

        }
        public async Task<PagedListResponse<List<SubLocationListDto>>> GetSubLocationsByParentAsync(
   long parentLocationId,
   ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetSubLocationsByParentAsync(query.Id, query);

        }
        public async Task<LocationInventory> AddInventoryAsync(CreateInventoryDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Inventory Id is required");
                if (string.IsNullOrEmpty(query.businessCode)) throw new BaseException(ExceptionType.BAD_REQUEST, "Business Code is required");
                model.LocationId = query.Id;
                model.BusinessCode=query.businessCode;
                model.RecordedByUserId=query.UserId;
                var response = await _organizationRepository.AddInventoryAsync(model);

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<LocationInventory> EditInventoryAsync(EditInventoryDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Inventory Id is required");
                model.Id = query.Id;
                model.UpdatedByUserId = query.UserId;
                var response = await _organizationRepository.EditInventoryAsync(model);

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task DeleteInventory(DeleteLocationDto model, ListQueryParams query)
        {
            try
            {
                if (query.Id <= 0) throw new BaseException(ExceptionType.BAD_REQUEST, "Inventory Id is required");
                model.Id = query.Id;
                model.UserId = query.UserId;
                 await _organizationRepository.DeleteInventory(model);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PagedListResponse<List<InventoryListDto>>> GetInventoryByLocationAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetInventoryByLocationAsync(query.Id, query);

        }
        #endregion location

        public async Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            await FetchOrganizationDetails(query);
            return await _organizationRepository.GetRolesAsync(query);

        }

        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams query)
        {
            if (string.IsNullOrEmpty(query.businessCode)) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

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
          // await EnrichAgentAsync(query);
            var agent = await _organizationRepository.GetOrganizationAgentDetail(query);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent Detail not found");

            return new Response<OrganizationAgentDetailResponse>
            {
                Succeeded=true,
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
               return null;

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>>
    GetAgentTransactionHistory(ListQueryParams query)
        {
            await EnrichAgentAsync(query);
            var agent = await _organizationRepository.GetAgentTransactionHistory(query);

            if (agent == null)
                return null;

            return agent;
        }
        public async Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args)

        { 
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetOrganizationAgentPerformanceSummary(args);

            if (agent == null)
                return null;

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args)
        {
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetAgentSubscribers(args);

            if (agent == null)
                return null;

            return agent;
        }
        public async Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args)
        {
            await EnrichAgentAsync(args);
            var agent = await _organizationRepository.GetAgentInputAllocations(args);

            if (agent == null)
               return null;

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
                UserId = args.UserId,
                IsPrimary= args.IsPrimary,
                RoleId= model.RoleId.HasValue ? model.RoleId.Value : 0,
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
                if (model.LocationId > 0)
                {
                    await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);
                }
                if(model.RoleId > 0)
                {

                    await _agentRepository.UpdateOrganizationAgentRole(agentAssignment);
                }



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
                    ex.Message ?? "Unable to complete onboarding at the moment.");
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

                var agent = await CreateAgent(model, args, args.UserId);
                await AssignAgentToOrganization(model, args, args.UserId);

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

                await SendDefaultCredentialChange(registerUser.model, args.businessCode);

                await AssignAgentToOrganization(model, args, registerUser.model.Id);

                await EnsureWalletExists(registerUser.model, business);

                await _onboardingRepository.TrackUserRoleAssignment(registerUser.model.Id, args.userType);

                return new CreateAgentResponseModel
                {
                    UserId = args.UserId,
                    AgentId = agent.data.Id,
                    AgentUserId = registerUser.model.Id,
                    RequiresProfileCompletion = true,
                    Message= registerUser.message,
                    IsExisting=registerUser.IsExisting
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

            if (model.LocationId.HasValue &&  model.LocationId.Value > 0)
            {
                agentAssignment.LocationId = model.LocationId.Value;
                await _agentRepository.UpdateOrganizationAgentLocation(agentAssignment);
            }
            if (model.RoleId.HasValue && model.RoleId.Value > 0)
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

                await RunNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateFarmerResponseModel
                {
                    UserId = user.model.Id,
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
                await SendDefaultCredentialChange(user, args.businessCode);
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

                await _awsService.SendEmailAsync(
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
                await _sMSService.SendSmsAsync(
                    destination,
                    $"Your default pin is {model.Pin}. Kindly login and change this.",
                    businessCode
                );
            }
        }
        //private async Task GenerateAndSendOtp(OnboardingRequestModel model)
        //{
        //    var senderEmail = _configuration["Official:Email"];
        //    var otpTarget = !string.IsNullOrWhiteSpace(model.Email)
        //        ? OtpTarget.Email
        //        : OtpTarget.Phone;

        //    var destination = otpTarget == OtpTarget.Email
        //        ? model.Email
        //        : model.PhoneNumber;

        //    await _otpService.InvalidateActiveOtps(
        //        model.Id,
        //        otpTarget);

        //    var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
        //    {
        //        UserId = model.Id,
        //        Target = otpTarget,
        //        Destination = destination,
        //        Purpose = OtpPurpose.Onboarding,
        //        ExpiryMinutes = 5
        //    });

        //    // ------------------ Send OTP ------------------
        //    if (otpTarget == OtpTarget.Email)
        //    {
        //        //use aws
        //        await _awsService.SendEmailAsync(
        //            senderEmail,
        //            destination,
        //            "Confirm your account",
        //            $"Your verification code is {model..Code}. It expires in 5 minutes.",
        //            $"Your verification code is {otp.Code}. It expires in 5 minutes."
        //            );
        //        //await _sMSService.SendEmailAsync(
        //        //    destination,
        //        //    "Confirm your account",
        //        //    $"Your verification code is {otp.Code}. It expires in 5 minutes.");
        //    }
        //    else
        //    {
        //        await _sMSService.SendSmsAsync(
        //                otp.Destination,
        //                $"Your verification code is {otp.Code}. Expires in 5 minutes.");
        //    }
        //}

        private async Task<CreateUserResult> RegisterFarmerOrFail(
         CreateFarmerRequest model,
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
                    FullName=model.FullName
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
                HighestLevelOfEducation = model.HighestLevelOfEducation,
                
                
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
        public async Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args)
        {

            try
            {
                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.organizationCode);
                if (hasOrg != null)
                {
                    args.Id = hasOrg.OrganizaionId;
                }
                return await _organizationRepository.GetOrganizationFarmerDashboardAsync(args);
            }
            catch (Exception ex)
            {
                throw;
            }
         
        }
            public async Task<Response<OrganizationFarmerDetailResponse>> GetFarmerDetailsAsync(ListQueryParams query)
        {
            await EnrichFarmerAsync(query);
            var farmer = await _organizationRepository.GetFarmerDetailsAsync(query);


            if (farmer != null  && !string.IsNullOrEmpty(farmer.FarmerImage))
            {
              
                farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
            }
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

            var farmer = await _farmerRepository.GetFarmerByUserId(args.UserId, args.organizationCode);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "User not found.");

            args.Id = farmer.Id;

        }
        public async Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args)
        {
            try
            {

                return await _organizationRepository.GetOrganizationFarmerSalesSummary(args);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<PagedListResponse<List<OrganizationFarmerSalesHistor>>> FarmerSalesHistory(
     ListQueryParams request, CancellationToken cancellationToken)
        {
            try
            {
                request.SalesContext = "Organization";
                return await _salesRepository.FarmerSalesHistory(request, cancellationToken);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> DeactivateFarmerFromOrganization(AssignmentArgs args, string? message = null)
        {

            return await _farmerRepository.DeactivateFarmerFromOrganization(args, message);
        }

        #endregion FarmerMgt

        #region MembershipMgt
        public async Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(ListQueryParams args)
        {

            try
            {
                return await _organizationRepository.GetOrganizationMembershipDashboard(args);
            }
            catch (Exception ex)
            {
                throw;
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

        public async Task<PagedListResponse<List<OrganizationMemberShipResponseModel>>> GetMembershipsByOrganization(ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;

                var cooperative = await _organizationRepository.GetCooperativesByOrganization(query.organizationCode, query);
                if (cooperative == null) return new PagedListResponse<List<OrganizationMemberShipResponseModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<OrganizationMemberShipResponseModel>>
                {
                    Succeeded = true,
                    Data = cooperative.Data,
                    Count = cooperative.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Membership: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<CreateMembershipResponseModel> CreateMembershipUser(
   MemberShipRequestModel model,
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
                });

                if (string.IsNullOrEmpty(args.businessCode))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Business code is required");


                var dateTime = DateTimeOffset.UtcNow;

                var business = await GetBusinessOrFail(args.businessCode);

                var user = await RegisterMembershipLeaderOrFail(model, args);
                await SendDefaultCredentialChange(user.model, args.businessCode);

                var membership = await CreateMembershipOrFail(
                    model, args, business, user.model.Id, dateTime);

                model.Id = membership.data.Id;
                await RunMembershipNonCriticalSteps(
                    model, args, business, user.model, dateTime);

                return new CreateMembershipResponseModel
                {
                    UserId = user.model.Id,
                    MembershipId = membership.data.Id,
                    RequiresProfileCompletion = true,
                    Message = user.IsExisting
      ? user.message
      : $"User created successfully.",
                    IsExisting =user.IsExisting,

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
   
        public async Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args)
        {   

            return await _farmerRepository.AssignFarmerToCooperatives(args, args.Id);
        }
        public async Task<bool> RemoveFarmerFromCooperative(string userId, List<long> cooperativeId, string? organizationCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required.");
                var getFarmer = await _farmerRepository.GetFarmerByUserId(userId, organizationCode);
                if (getFarmer == null) { throw new BaseException(ExceptionType.BAD_REQUEST, "User Id is required."); }

                var profile = await _farmerRepository.RemoveFarmerFromCooperatives(getFarmer.Id, cooperativeId);

                if (profile == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Could not remove farmer from this membership.");

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

        private async Task RunMembershipNonCriticalSteps(
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


        private async Task<(bool status, MemberShipModel data)> CreateMembershipOrFail(
     MemberShipRequestModel model,
     OnBoardingArgs args,
     BusinessModel business,
     string userId,
     DateTimeOffset dateTime)
        {
            var agentIds = args.AgentIds?
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList() ?? new List<long>();

            return await _membershipRepository.CreateUpdateMembership(new MemberShipModel
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
                AgentIds = agentIds,
                BelongToBusinessCode = args.businessCode,
                OrganizationId = model.OrganizationId,
                OnboardedById = args.OnboardedByUserId,

                HasbeenAssignedAgent = agentIds.Any(),

                MembershipLeaderFirstName = model.MembershipLeaderFirstName,
                MembershipLeaderMiddleName = model.MembershipLeaderMiddleName,
                MembershipLeaderLastName = model.MembershipLeaderLastName,
                MembershipLeaderGender = model.MembershipLeaderGender,
                ContactPerson = $"{model.MembershipLeaderFirstName} {model.MembershipLeaderMiddleName} {model.MembershipLeaderLastName}"
            });
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

        #endregion MembershipMgt


        #region input
      
  

        private async Task EnrichFarmerQuery(ListQueryParams request)
        {
            var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId, request.organizationCode);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Farmer not found for the specified user.");

            request.Id = farmer.Id;
        }

        #endregion input
    }

}

