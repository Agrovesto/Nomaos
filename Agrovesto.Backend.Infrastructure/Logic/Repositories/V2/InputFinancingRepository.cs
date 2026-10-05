using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Events;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{

    public interface IInputFinancingRepository
    {
        Task<bool> AddSubscriberAsync(AddSubscriberRequest request, InputSubscriptionRequestArgs args);
        Task<bool> ApproveSubscriptionAsync(ApproveSubscriptionRequest request, InputSubscriptionRequestArgs args);
        Task<EopResponseDto> CreateEopAsync(CreateEopRequest request);
        Task<bool> CreateInputFinancingCategory(ProductCategoryDto dto);
        Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, InputPackageConfigArgs args);
        Task<bool> DeactivatePackageAsync(long packageId, long organizationId);
        Task<bool> DisburseSubscriptionAsync(DisburseSubscriptionRequest request, InputSubscriptionRequestArgs args);
        Task<EopDto> GetEopByIdAsync(long id);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request);
        Task<PagedResult<FinancingBreakdownDto>> GetFinancingBreakdownAsync(
        string subscriberUserId,
        int pageNumber,
        int pageSize);
        Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(long subscriptionId);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetInputFinancingCategories(ListQueryParams query);
        Task<ProductCategoryDto> GetInputFinancingCategoryById(long id);
        Task<InputFinancingDashboardDto> GetInputFinancingOverviewAsync(SubscriptionQueryFilter filter);
        Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(string subscriberUserId);
        Task<LoanDetailDto> GetLoanDetailsAsync(long subscriptionId);
        Task<InputPackageDto> GetPackageByIdAsync(long id);
        Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query);
        Task<PagedResult<RepaymentBreakdownDto>> GetRepaymentBreakdownAsync(long subscriptionId, int pageNumber, int pageSize);
        Task<PagedResult<InputObligationDto>> GetSubscriptionObligationsAsync(SubscriptionQueryFilter filter, int pageNumber, int pageSize);
        Task<PagedResult<SubscriptionDetailDto>> GetSubscriptionsAsync(SubscriptionQueryFilter filter, int pageNumber, int pageSize);
        Task<bool> ReactivatePackageAsync(long packageId, long organizationId);
        Task<bool> RecordCommitmentPaymentAsync(RecordCommitmentPaymentRequest request, InputSubscriptionRequestArgs args);
        Task<bool> RecordSubscriptionRepaymentAsync(RecordRepaymentRequest request);
        Task<bool> RemoveItemFromPackage(long packageId, long itemId);
        Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs args);
        Task<bool> UpdateInputPackageConfig(long packageId, UpdateInputPackageConfigRequest request, InputPackageConfigArgs args);

        Task<byte[]> ExportSubscriptionsAsync(SubscriptionQueryFilter filter, ExportFormat format);
        Task<SubscriptionFullDetailDto> FetchSubscriptionForReview(SubscriptionQueryFilter filter);
        Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId);
        Task<CommitmentReminderDto?> GetCommitmentReminderAsync(long subscriptionId);
        Task<SubscriptionReviewResponseDto> GetSubscriptionDetail(SubscriptionQueryFilter filter);
        Task<long> RequestInputDisbursementApprovalAsync(long subscriptionId, string? reference, string userId, string subscriberUserId);
        Task<decimal> GetTotalCommitmentPaidAsync(long subscriptionId);
        Task<bool> RecordSuccessfulPaymentAsync(long paymentId, PaymentConfirmationRequest request, string performedBy);
        Task FlagPendingBankTransferAsync(long paymentId);
        Task FlagPendingCashApprovalAsync(long paymentId);
        Task<long> CreatePendingPaymentAsync(CreateCommitmentPaymentRequest request);
        Task<bool> AddItemToPackageAsync(long packageId, AddPackageItemRequest request);
        Task<bool> AddAdditionalCostToPackageAsync(long packageId, AddAdditionalCostRequest request);
        Task<bool> RemoveAdditionalCostFromPackageAsync(long packageId, long additionalCostId);
        Task<bool> UpdateAdditionalCostAsync(UpdateAdditionalCostRequest request, UpdateAdditionalCostArgs args);
        Task<bool> UpdateInputPackageWallets(long organizationId, long walletId, long providerId);
        Task<bool> ApproveInputDisbursementRequestAsync(ApproveDisbursementRequestDto request, InputSubscriptionRequestArgs args);
        Task<decimal> GetAvailableBalanceAsync(long locationId);
        Task DeductAsync(long locationId, decimal amount, string referenceNumber, string agentUserId);
        Task<decimal> GetTotalRepaymentPaidAsync(long subscriptionId);
        Task AddCreditBalanceAsync(long subscriptionId, decimal amount, string reference);
        Task<decimal> GetUnconsumedCreditBalanceAsync(long subscriptionId);
        Task<RepaymentReminderDto?> GetRepaymentReminderAsync(long subscriptionId);
        Task<List<OverdueRepaymentReminderDto>> GetOverdueSchedulesAcrossSubscriptionsAsync();
        Task<List<RepaymentScheduleItemDto>> GetRepaymentScheduleAsync(long subscriptionId);
        Task<PagedResult<RepaymentLedgerEntryDto>> GetRepaymentLedgerAsync(long subscriptionId, int pageNumber, int pageSize);
        Task<FarmerSubscriptionOverviewDto> GetAgentSubscriptionOverview(SubscriptionQueryFilter filter);
        Task<bool> RemoveSubscriptionAsync(InputSubscriptionRequestArgs args, CancellationToken cancellationToken = default);
        Task<EditRepaymentPlanResult> EditRepaymentPlanAsync(long packageId, UpdateRepaymentPlanRequest request, InputPackageConfigArgs args);
        Task<PerUserTypeFinancingOverviewCountDto> GetFinancingOverViewPerSubscriptionType(SubscriptionQueryFilter filter);
    }
    public class InputFinancingRepository : IInputFinancingRepository
    {
        private readonly DataContext _context;
        private readonly IRepaymentEngine _repaymentEngine;
        public InputFinancingRepository(DataContext context, IRepaymentEngine repaymentEngine)
        {
            _context = context;
            _repaymentEngine = repaymentEngine;
        }

        public async Task<bool> CreateInputFinancingCategory(ProductCategoryDto dto)
        {
            var exists = await _context.InputFinancingCategories.AnyAsync(x =>
                x.Name == dto.CategoryName &&
                x.BusinessCode == dto.BusinessCode);

            if (exists)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Product Category already exists.");


            var cat = new InputFinancingCategory
            {
                BusinessCode = dto.BusinessCode,
                Description = dto.Description,

                Name = dto.CategoryName,
                IsActive = true,
                CreatedOn = DateTimeOffset.Now,
            };

            _context.InputFinancingCategories.Add(cat);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<PagedListResponse<List<ProductCategoryDto>>> GetInputFinancingCategories(ListQueryParams query)
        {
            var baseQuery = _context.InputFinancingCategories
                .Where(x => x.BusinessCode == query.businessCode && x.IsActive)
                .AsQueryable();


            var totalCount = await baseQuery.CountAsync();

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 100 : query.PageSize;

            var data = await baseQuery
                .OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductCategoryDto
                {
                    Id = x.Id,
                    CategoryName = x.Name,
                    BusinessCode = x.BusinessCode

                })
                .ToListAsync();

            return new PagedListResponse<List<ProductCategoryDto>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<ProductCategoryDto> GetInputFinancingCategoryById(long id)
        {
            var cat = await _context.InputFinancingCategories
                           .Where(x => x.IsActive && x.Id == id)

                           .FirstOrDefaultAsync();

            if (cat == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Product Category not found.");

            return new ProductCategoryDto
            {
                Id = cat.Id,
                CategoryName = cat.Name,
                BusinessCode = cat.BusinessCode
            };
        }

        public async Task<bool> UpdateInputPackageWallets(long organizationId, long walletId, long providerId)
        {
            var package = await _context.InputPackages
                .Where(x => x.OrganizationId == organizationId
                &&
                x.WalletProviderId == providerId
                && !x.IsSelectedWalletProvisioned).ToListAsync();

            if (!package.Any()) return false;

            foreach (var p in package)
            {
                p.UserWalletId = walletId;
                p.IsSelectedWalletProvisioned = true;
                p.WalletProviderId = providerId;
            }
            await _context.SaveChangesAsync();

            return true;
        }

        private static void ValidateRepaymentRequest(bool repaymentRequired, decimal? repaymentValue)
        {
            if (!repaymentRequired && repaymentValue.HasValue)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Cannot specify a repayment value when RepaymentRequired is false, please set RepaymentRequired to true or remove the repayment value");
        }

        private static void ValidateCommitmentRequest(bool commitmentRequired, decimal? commitmentValue)
        {
            if (!commitmentRequired && commitmentValue.HasValue)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Cannot specify a commitment value when CommitmentRequired is false, please set CommitmentRequired to true or remove the commitment value");
        }


        private static List<InputPackageItem> BuildPackageItems(
            InputPackageStructure structure,
            List<CreateInputPackageItemRequest> requestItems)
        {
            var isPhysical = structure == InputPackageStructure.Physical;

            if (isPhysical && !requestItems.Any())
                throw new BaseException(ExceptionType.BAD_REQUEST, "Physical package must have items");

            if (!isPhysical && requestItems.Any())
                throw new BaseException(ExceptionType.BAD_REQUEST, "Non-physical package cannot have items");

            return isPhysical
                ? requestItems.Select(x => new InputPackageItem
                {
                    OrganizationProductId = x.OrganizationProductId,
                    Quantity = x.Quantity,
                    UnitCost = x.UnitCost
                }).ToList()
                : new List<InputPackageItem>();
        }

        private static List<AdditionalInputPackageCost> BuildAdditionalCosts(
            List<CreateAdditionalCostRequest> requestCosts)
        {
            return requestCosts.Select(x => new AdditionalInputPackageCost
            {
                Name = x.Name,
                Description = x.Description,
                Amount = x.Amount,
                IsPerUnit = x.IsPerUnit,
                Quantity = x.Quantity,
                Type = x.Type
            }).ToList();
        }


        private static (decimal totalPackageValue, decimal totalFinancingValue) CalculateTotals(
            InputPackageStructure structure,
            decimal requestTotalPackageValue,
            decimal requestTotalFinancingValue,
            List<InputPackageItem> items,
            List<AdditionalInputPackageCost> additionalCosts)
        {
            var isPhysical = structure == InputPackageStructure.Physical;
            var additionalCostsTotal = additionalCosts.Sum(x => x.Amount);

            decimal itemsTotal = isPhysical
                ? items.Sum(x => x.Quantity * x.UnitCost)
                : requestTotalPackageValue;

            if (isPhysical &&
                requestTotalPackageValue > 0 &&
                Math.Round(requestTotalPackageValue, 2) != Math.Round(itemsTotal, 2))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "TotalPackageValue mismatch");
            }

            var totalPackageValue = itemsTotal;
            var totalFinancingValue = totalPackageValue + additionalCostsTotal;

            if (requestTotalFinancingValue > 0 &&
                Math.Round(requestTotalFinancingValue, 2) != Math.Round(totalFinancingValue, 2))
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "TotalFinancingValue mismatch");
            }

            return (totalPackageValue, requestTotalFinancingValue > 0 ? requestTotalFinancingValue : totalFinancingValue);
        }


        private static CommitmentPlan? BuildCommitmentPlan(
            bool commitmentRequired,
            CommitmentType? commitmentType,
            decimal? requestCommitmentValue,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            decimal totalPackageValue)
        {
            if (!commitmentRequired)
                return null;

            if (!commitmentType.HasValue)
            {
                throw new ValidationException("Commitment type is required when commitment is required");
            }
            if (!requestCommitmentValue.HasValue)
            {
                throw new ValidationException("Commitment requestCommitmentValue is required when commitment is required");
            }

            var commitmentValue = commitmentType == CommitmentType.PercentageOfTotalValue
                ? totalPackageValue * (requestCommitmentValue / 100m)
                : requestCommitmentValue;

            var plan = new CommitmentPlan
            {
                CommitmentType = commitmentType.Value,
                CommitmentValue = commitmentValue.Value,
                CommitmentPaymentStartDate = startDate,
                CommitmentPaymentEndDate = endDate,
                CommitmentPercentage =commitmentType == CommitmentType.PercentageOfTotalValue ? requestCommitmentValue.Value : 0
            };

            ValidateCommitmentPlan(plan); // reused from AddSubscriberAsync
            return plan;
        }

        private static RepaymentPlan? BuildRepaymentPlan(
            bool repaymentRequired,
            RepaymentFrequency? frequency,
            int? duration,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            DateTimeOffset? gracePeriod,
            InputPackageRepaymentMode? mode,
            decimal? repaymentValue,
            RiskCategory riskCategory)
        {
            if (!repaymentRequired)
                return null;
            if(!frequency.HasValue)
            {
                throw new ValidationException("Repayment frequency is required when repayment is required");
            }
            if (!mode.HasValue)
            {
                throw new ValidationException("Repayment mode is required when repayment is required");
            }
            if (!duration.HasValue)
            {
                throw new ValidationException("Repayment duration is required when repayment is required");
            }
            if (!repaymentValue.HasValue)
            {
                throw new ValidationException("Repayment repaymentValue is required when repayment is required");
            }
            var plan = new RepaymentPlan
            {
                RepaymentFrequency = frequency.Value,
                RepaymentDuration = duration.Value,
                RepaymentStartDate = startDate,
                RepaymentEndDate = endDate,
                RepaymentGracePeriod = gracePeriod,
                RepaymentMode = mode.Value,
                RepaymentValue = repaymentValue.Value,
                RiskCategory = riskCategory
            };

            ValidateRepaymentPlan(plan); // reused from AddSubscriberAsync
            return plan;
        }

        public async Task<bool> CreateInputFinancingPackageConfig(
     CreateInputPackageRequest request,
     InputPackageConfigArgs args)
        {
            if (request == null)
                throw new Exception("Request cannot be null");

            ValidateRepaymentRequest(request.RepaymentRequired, request.RepaymentValue);
            ValidateCommitmentRequest(request.CommitmentRequired, request.CommitmentValue);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var requestItems = request.InputPackageItems ?? new List<CreateInputPackageItemRequest>();
                    var requestAdditionalCosts = request.AdditionalCosts ?? new List<CreateAdditionalCostRequest>();

                    var items = BuildPackageItems(request.PackageStructure, requestItems);
                    var additionalCosts = BuildAdditionalCosts(requestAdditionalCosts);

                    var (totalPackageValue, totalFinancingValue) = CalculateTotals(
                        request.PackageStructure,
                        request.TotalPackageValue,
                        request.TotalFinancingValue,
                        items,
                        additionalCosts);

                    var package = new InputPackage
                    {
                        OrganizationId = args.organizationId,
                        CreatedByUserId = args.createdByUserId,
                        Name = request.Name,
                        InputFinancingCategoryId = request.InputFinancingCategoryId,
                        SubScriptionType = request.SubScriptionType,
                        InputTarget = request.InputTarget,
                        InputSeason = request.InputSeason,
                        PackageStructure = request.PackageStructure,

                        InputPackageItems = items,
                        AdditionalCosts = additionalCosts,

                        TotalPackageValue = totalPackageValue,
                        AdditionalCostAmount = additionalCosts.Sum(x => x.Amount),
                        TotalFinancingValue = totalFinancingValue,

                        CommitmentRequired = request.CommitmentRequired,
                        RepaymentRequired = request.RepaymentRequired,

                        UserWalletId = request.UserWalletId,
                        WalletProviderId = request.PaybackWalletId > 0 ? request.PaybackWalletId : null,

                        IsSelectedWalletProvisioned = args.isSelectedWalletProvisioned,

                        Currency = request.Currency,
                        IsGeneratedFromEop = false,
                        Version = 1,
                        CreatedOn = DateTimeOffset.UtcNow,
                        IsActive = true,
                        BusinessCode = args.BusinessCode,

                        CommitmentPlan = BuildCommitmentPlan(
                            request.CommitmentRequired,
                            request.CommitmentType.HasValue ? request.CommitmentType.Value : null,
                            request.CommitmentValue.HasValue ? request.CommitmentValue.Value : null,
                            request.CommitmentPaymentStartDate,
                            request.CommitmentPaymentEndDate,
                            totalPackageValue),

                        RepaymentPlan = BuildRepaymentPlan(
                            request.RepaymentRequired,
                            request.RepaymentFrequency.HasValue ? request.RepaymentFrequency.Value : null,
                            request.RepaymentDuration.HasValue ? request.RepaymentDuration.Value : null,
                            request.RepaymentStartDate,
                            request.RepaymentEndDate,
                            request.RepaymentGracePeriod,
                            request.RepaymentModel.HasValue ? request.RepaymentModel.Value : null,
                            request.RepaymentValue,
                            request.RiskCategory)
                    };

                    _context.InputPackages.Add(package);

                    var outboxEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(InputPackageCreatedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new InputPackageCreatedEvent
                        {
                            PackageId = package.Id,
                            OrganizationId = package.OrganizationId,
                            CreatedByUserId = package.CreatedByUserId
                        })
                    };

                    _context.OutboxMessages.Add(outboxEvent);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        private static void ApplyIfPresent<T>(T? value, Action<T> setter) where T : struct
        {
            if (value.HasValue)
                setter(value.Value);
        }
        public async Task<bool> UpdateInputPackageConfig(
     long packageId,
     UpdateInputPackageConfigRequest request,
     InputPackageConfigArgs args)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var package = await _context.InputPackages
                        .Include(x => x.RepaymentPlan)
                        .Include(x => x.CommitmentPlan)
                        .Include(x => x.InputPackageItems)
                        .Include(x => x.AdditionalCosts)
                        .FirstOrDefaultAsync(x =>
                            x.Id == packageId &&
                            x.OrganizationId == args.organizationId);

                    if (package == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

                    if (!package.IsActive)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Cannot update configuration for inactive package");

                    await EnsurePackageIsEditable(package);

                    ApplyIfPresent(request.CommitmentRequired, v => package.CommitmentRequired = v);
                    ApplyIfPresent(request.RepaymentRequired, v => package.RepaymentRequired = v);

                    if (!package.CommitmentRequired && request.CommitmentValue.HasValue && request.CommitmentValue.Value > 0)
                        throw new BaseException(ExceptionType.BAD_REQUEST,
                            "Cannot specify a commitment value when CommitmentRequired is false, please set CommitmentRequired to true or remove the commitment value");

                    if (!package.RepaymentRequired && request.RepaymentValue.HasValue && request.RepaymentValue.Value > 0)
                        throw new BaseException(ExceptionType.BAD_REQUEST,
                            "Cannot specify a repayment value when RepaymentRequired is false, please set RepaymentRequired to true or remove the repayment value");

                    if (package.CommitmentRequired)
                    {
                        var plan = package.CommitmentPlan ?? new CommitmentPlan { InputPackageId = package.Id };

                        ApplyIfPresent(request.CommitmentType, v => plan.CommitmentType = v);
                        ApplyIfPresent(request.CommitmentPaymentStartDate, v => plan.CommitmentPaymentStartDate = v);
                        ApplyIfPresent(request.CommitmentPaymentEndDate, v => plan.CommitmentPaymentEndDate = v);

                        if (request.CommitmentValue.HasValue)
                        {
                            plan.CommitmentValue = plan.CommitmentType == CommitmentType.PercentageOfTotalValue
                                ? package.TotalPackageValue * (request.CommitmentValue.Value / 100m)
                                : request.CommitmentValue.Value;

                            plan.CommitmentPercentage = plan.CommitmentType == CommitmentType.PercentageOfTotalValue
                                ? request.CommitmentValue.Value
                                : 0m;
                        }

                        ValidateCommitmentPlan(plan);
                        package.CommitmentPlan = plan;
                    }
                    else if (package.CommitmentPlan != null)
                    {
                        _context.CommitmentPlans.Remove(package.CommitmentPlan);
                        package.CommitmentPlan = null;
                    }
                    ApplyIfPresent(request.UserWalletId, v => package.UserWalletId = v);
                    if (args.isSelectedWalletProvisioned)
                    {
                        package.IsSelectedWalletProvisioned = true;
                    }

                    if (package.RepaymentRequired)
                    {
                        var plan = package.RepaymentPlan ?? new RepaymentPlan { InputPackageId = package.Id };

                        ApplyIfPresent(request.RepaymentFrequency, v => plan.RepaymentFrequency = v);
                        ApplyIfPresent(request.RepaymentDuration, v => plan.RepaymentDuration = v);
                        ApplyIfPresent(request.RepaymentStartDate, v => plan.RepaymentStartDate = v);
                        ApplyIfPresent(request.RepaymentEndDate, v => plan.RepaymentEndDate = v);
                        ApplyIfPresent(request.RepaymentGracePeriod, v => plan.RepaymentGracePeriod = v);
                        ApplyIfPresent(request.RepaymentModel, v => plan.RepaymentMode = v);
                        ApplyIfPresent(request.RepaymentValue, v => plan.RepaymentValue = v);
                        ApplyIfPresent(request.RiskCategory, v => plan.RiskCategory = v);
                       

                        ValidateRepaymentPlan(plan);
                        package.RepaymentPlan = plan;
                    }
                    else if (package.RepaymentPlan != null)
                    {
                        _context.RepaymentPlans.Remove(package.RepaymentPlan);
                        package.RepaymentPlan = null;
                    }

                    package.UpdatedOn = DateTimeOffset.UtcNow;
                    package.UpdatedByUserId = args.createdByUserId;

                    var outboxEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(InputPackageConfigurationUpdatedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new InputPackageConfigurationUpdatedEvent
                        {
                            PackageId = package.Id,
                            OrganizationId = package.OrganizationId
                        })
                    };

                    _context.OutboxMessages.Add(outboxEvent);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams request)
        {
            if (request.PageNumber <= 0) request.PageNumber = 1;
           request.PageSize = 10;

            var query = _context.InputPackages
                .Include(x => x.RepaymentPlan)
                .Include(x => x.CommitmentPlan)
                .Include(x => x.InputFinancingCategory)
                .Include(x => x.Subscriptions)

                .Where(x => x.OrganizationId == request.OrganizationId);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    FinancingCategory = x.InputFinancingCategory.Name,
                    x.InputTarget,
                    x.TotalFinancingValue,
                    CommitmentValue = x.CommitmentPlan != null ? x.CommitmentPlan.CommitmentValue : 0,
                    RepaymentMode = x.RepaymentPlan != null ? x.RepaymentPlan.RepaymentMode : (InputPackageRepaymentMode?)null,
                    RiskCategory = x.RepaymentPlan != null ? x.RepaymentPlan.RiskCategory : (RiskCategory?)null,
                    x.IsActive,
                    x.SubScriptionType,
                    x.Subscriptions
                })
                .ToListAsync();

            return new PagedResult<InputPackageListModel>
            {
                Data = data.Select(x => new InputPackageListModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    FinancingCategory = x.FinancingCategory,
                    InputTarget = x.InputTarget,
                    TotalFinancingValue = x.TotalFinancingValue,
                    CommitmentValue = x.CommitmentValue,
                    RepaymentModel = x.RepaymentMode.HasValue ? EnumExtensions.GetDisplayName(x.RepaymentMode.Value) : null,
                    RiskCategory = x.RiskCategory.HasValue ? EnumExtensions.GetDisplayName(x.RiskCategory.Value) : null,
                    Subscribers = x.Subscriptions.Count(),
                    IsActive = x.IsActive,
                    SubscriberType = EnumExtensions.GetDisplayName(x.SubScriptionType)
                }).ToList(),

                Count = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        private static DateTimeOffset GetDueDate(DateTimeOffset start, RepaymentFrequency frequency, int installmentNumber)
        {
            return frequency switch
            {
                RepaymentFrequency.Daily => start.AddDays(installmentNumber),
                RepaymentFrequency.Weekly => start.AddDays(7 * installmentNumber),

                RepaymentFrequency.Monthly => EndOfMonth(start.AddMonths(installmentNumber - 1)),
                RepaymentFrequency.Quarterly => EndOfMonth(start.AddMonths(3 * (installmentNumber - 1))),

                RepaymentFrequency.Yearly => EndOfMonth(start.AddYears(installmentNumber-1)),
                RepaymentFrequency.NA => throw new BaseException(ExceptionType.OPERATION_FAILED, "Repayment frequency is not set"),
                _ => throw new NotSupportedException($"Repayment frequency '{installmentNumber}' is not supported.")

            };
        }
        private static List<RepaymentScheduleDto> GenerateRepaymentScheduleBrekadown(RepaymentPlan plan)
        {
            var schedule = new List<RepaymentScheduleDto>();

            if (plan == null || !plan.RepaymentStartDate.HasValue || plan.RepaymentDuration <= 0)
                return schedule;

            int duration = plan.RepaymentDuration;
            var start = plan.RepaymentStartDate.Value;

            decimal baseAmount = Math.Floor(plan.RepaymentValue / duration * 100m) / 100m;
            decimal lastAmount = plan.RepaymentValue - (baseAmount * (duration - 1));

            for (int i = 1; i <= duration; i++)
            {
                var dueDate = GetDueDate(start, plan.RepaymentFrequency, i);
                var amount = (i == duration) ? lastAmount : baseAmount;
                var grace = plan.RepaymentGracePeriod.HasValue
                    ? EndOfMonth(dueDate.AddMonths(1))
                    : (DateTimeOffset?)null;

                schedule.Add(new RepaymentScheduleDto
                {
                    InstallmentNumber = i,
                    DueDate = dueDate,
                    AmountToPay = amount,
                    GracePeriodDeadline = grace
                });
            }

            return schedule;
        }
        public async Task<InputPackageDto> GetPackageByIdAsync(long id)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputFinancingCategory)
                .Include(x => x.InputPackageItems)
                    .ThenInclude(x => x.OrganizationProduct)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .Include(x => x.RepaymentPlan)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (package == null)
                return null;

            var commitment = package.CommitmentPlan;
            var repayment = package.RepaymentPlan;

           

            var repaymentBreakdown =  GenerateRepaymentScheduleBrekadown(
                repayment
                );

            return new InputPackageDto
            {
                IsActive = package.IsActive,
                WalletProviderId = package.WalletProviderId ?? 0,

                Id = package.Id,
                Name = package.Name,
                IsRepaymentWalletConfigured = package.IsSelectedWalletProvisioned,

                SubscriptionType = EnumExtensions.GetDisplayName(package.SubScriptionType),
                InputSeason = package.InputSeason,
                InputTarget = package.InputTarget,

                FinancingCategory = package.InputFinancingCategory?.Name,

                FinancialStructure = new InputPackageFinancingStructureDto
                {
                    TotalFinancingValue = package.TotalFinancingValue,
                    CommitmentRequired = package.CommitmentRequired,

                    CommitmentValue = commitment?.CommitmentValue ?? 0,

                    CommitmentPaymentStartDate = commitment?.CommitmentPaymentStartDate,
                    CommitmentPaymentEndDate = commitment?.CommitmentPaymentEndDate,
                    CommitmentPercentage = commitment?.CommitmentPercentage ?? 0,
                    RepaymentStartDate = repayment?.RepaymentStartDate,
                    RepaymentEndDate = repayment?.RepaymentEndDate,
                    RepaymentGracePeriod = repayment?.RepaymentGracePeriod,

                    RepaymentModel = repayment != null
                        ? EnumExtensions.GetDisplayName(repayment.RepaymentMode)
                        : null,

                    RiskCategory = repayment != null
                        ? EnumExtensions.GetDisplayName(repayment.RiskCategory)
                        : null,

                    RepaymentValue = repayment?.RepaymentValue ?? 0,

                    RepaymentPeriod = repayment != null
                        ? GetRepaymentPeriod(repayment.RepaymentFrequency, repayment.RepaymentDuration)
                        : null
                },

                PackageInputs = package.InputPackageItems.Select(i => new InputPackageBreakdownDto
                {
                    OrganizationProductId = i.OrganizationProductId,
                    InputId = i.Id,

                    InputType = i.OrganizationProduct?.ProductName,

                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    TotalCost = i.Quantity * i.UnitCost
                }).ToList(),

                AdditionalCosts = package.AdditionalCosts.Select(c => new AdditionalCostDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Amount = c.Amount
                }).ToList(),

                RepaymentBreakdown = repaymentBreakdown
            };
        }

        public async Task<PagedResult<RepaymentBreakdownDto>> GetPackageRepaymentBreakdownAsync(
        long packageId,
        int pageNumber = 1,
        int pageSize = 20)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _context.InputPackageSubscriptionPayments
                .Where(x => x.PaymentType == PaymentType.Repayment
                    && x.Subscription.InputPackageId == packageId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.PaidOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RepaymentBreakdownDto
                {
                    Id = x.Id,
                    SubscriptionId = x.SubscriptionId,
                    AmountPaid = x.AmountPaid,
                    Date = x.PaidOn
                })
                .ToListAsync();

            return new PagedResult<RepaymentBreakdownDto>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                //TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        public async Task<bool> DeactivatePackageAsync(long packageId, long organizationId)
        {
            var package = await _context.InputPackages
                .FirstOrDefaultAsync(x => x.Id == packageId
                                       && x.OrganizationId == organizationId);

            if (package == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

            package.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> ReactivatePackageAsync(long packageId, long organizationId)
        {
            var package = await _context.InputPackages
                .FirstOrDefaultAsync(x => x.Id == packageId
                                       && x.OrganizationId == organizationId);

            if (package == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

            package.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> RemoveItemFromPackage(long packageId, long itemId)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == packageId);

            await EnsurePackageIsEditable(package);

            var item = package.InputPackageItems
                .FirstOrDefault(x => x.Id == itemId);

            if (item == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Item not found in package");

            _context.InputPackageItems.Remove(item);

            // recalc AFTER mutation
            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> ReplacePackageItemAsync(
        ReplacePackageItemRequest request,
        ReplacePackageItemRequestArgs args)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == args.PackageId);

            await EnsurePackageIsEditable(package);

            var item = package.InputPackageItems
                .FirstOrDefault(x => x.Id == args.InputId);

            if (item == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Package item not found");

            var productExists = await _context.OrganizationProducts
                .AnyAsync(x => x.Id == request.OrganizationProductId
                            && x.OrganizationId == args.OrganizationId);

            if (!productExists)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid product for this organization");

            item.OrganizationProductId = request.OrganizationProductId;
            item.Quantity = request.Quantity;
            item.UnitOfMeasure = request.UnitOfMeasure;
            item.UnitCost = request.UnitCost;
            item.OverrideCost = null;

            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> AddItemToPackageAsync(
        long packageId,
        AddPackageItemRequest request)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == packageId);

            await EnsurePackageIsEditable(package);

            var product = await _context.OrganizationProducts
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationProductId
                                        && x.OrganizationId == package.OrganizationId);

            if (product == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid product for this organization");

            var newItem = new InputPackageItem
            {
                InputPackageId = packageId,
                OrganizationProductId = request.OrganizationProductId,
                Quantity = request.Quantity,
                UnitCost = request.UnitCost,
                UnitOfMeasure = request.UnitOfMeasure
            };

            _context.InputPackageItems.Add(newItem);

            // recalc AFTER adding
            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddAdditionalCostToPackageAsync(
        long packageId,
        AddAdditionalCostRequest request)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == packageId);

            await EnsurePackageIsEditable(package);

            var additionalCost = new AdditionalInputPackageCost
            {
                InputPackageId = packageId,
                Name = request.Name,
                Description = request.Description,
                Amount = request.Amount,
                IsPerUnit = request.IsPerUnit,
                Quantity = request.Quantity,
                Type = request.Type
            };

            _context.AdditionalInputPackageCosts.Add(additionalCost);

            package.AdditionalCosts.Add(additionalCost);

            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> RemoveAdditionalCostFromPackageAsync(
        long packageId,
        long additionalCostId)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == packageId);

            await EnsurePackageIsEditable(package);

            var additionalCost = package.AdditionalCosts
                .FirstOrDefault(x => x.Id == additionalCostId);

            if (additionalCost == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Additional cost not found in package");

            _context.AdditionalInputPackageCosts.Remove(additionalCost);

            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateAdditionalCostAsync(
        UpdateAdditionalCostRequest request,
        UpdateAdditionalCostArgs args)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .FirstOrDefaultAsync(x => x.Id == args.PackageId);

            await EnsurePackageIsEditable(package);

            var additionalCost = package.AdditionalCosts
                .FirstOrDefault(x => x.Id == args.AdditionalCostId);

            if (additionalCost == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Additional cost not found in package");

            additionalCost.Name = request.Name;
            additionalCost.Description = request.Description;
            additionalCost.Amount = request.Amount;
            additionalCost.IsPerUnit = request.IsPerUnit;
            additionalCost.Quantity = request.Quantity;
            additionalCost.Type = request.Type;

            RecalculatePackageFinancials(package);

            await _context.SaveChangesAsync();

            return true;
        }

        private void RecalculatePackageFinancials(InputPackage package)
        {
            var isPhysical = package.PackageStructure == InputPackageStructure.Physical;

            var itemsTotal = isPhysical
                ? package.InputPackageItems.Sum(x => x.Quantity * x.UnitCost)
                : package.TotalPackageValue;

            var additionalCostsTotal = package.AdditionalCosts.Sum(x => x.Amount);

            package.TotalPackageValue = itemsTotal;
            package.AdditionalCostAmount = additionalCostsTotal;
            package.TotalFinancingValue = itemsTotal + additionalCostsTotal;

            if (package.CommitmentPlan != null &&
                package.CommitmentPlan.CommitmentType == CommitmentType.PercentageOfTotalValue)
            {
                package.CommitmentPlan.CommitmentValue =
                    itemsTotal * (package.CommitmentPlan.CommitmentPercentage / 100m);
            }
        }

        #region Input Subscriber
        private static void ValidateCommitmentPlan(CommitmentPlan? commitment)
        {
            if (commitment == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "This package requires a commitment plan, please configure the commitment plan for this package");

            var missing = new List<string>();

            if (commitment.CommitmentType == default)
                missing.Add("commitment type");

            if (commitment.CommitmentValue <= 0)
                missing.Add("commitment value");


            if (commitment.CommitmentPaymentStartDate != null &&
                commitment.CommitmentPaymentEndDate != null &&
                commitment.CommitmentPaymentEndDate <= commitment.CommitmentPaymentStartDate)
                missing.Add("commitment payment end date must be after the start date");

            if (commitment.CommitmentType == CommitmentType.PercentageOfTotalValue && commitment.CommitmentValue <= 0)
                missing.Add("commitment percentage");

            if (missing.Any())
                throw new BaseException(ExceptionType.OPERATION_FAILED,
                    $"Commitment plan is not fully configured, please configure the following before subscribing: {string.Join(", ", missing)}");
        }

        private static void ValidateRepaymentPlan(RepaymentPlan? repayment)
        {
            if (repayment == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "This package requires a repayment plan, please configure the repayment plan for this package");

            var missing = new List<string>();

            if (repayment.RepaymentValue <= 0)
                missing.Add("repayment value");

            if (repayment.RepaymentDuration <= 0)
                missing.Add("repayment duration");

            if (repayment.RepaymentFrequency == default)
                missing.Add("repayment frequency");

            if (repayment.RepaymentMode == default)
                missing.Add("repayment mode");

            if (repayment.RiskCategory == default)
                missing.Add("risk category");


            if (repayment.RepaymentStartDate != null &&
                repayment.RepaymentEndDate != null &&
                repayment.RepaymentEndDate <= repayment.RepaymentStartDate)
                missing.Add("repayment end date must be after the start date");

            if (repayment.RepaymentGracePeriod == null)
                missing.Add("repayment grace period");


            if (repayment.RepaymentDuration > 0 &&
            repayment.RepaymentFrequency != default &&
            repayment.RepaymentStartDate.HasValue &&
            repayment.RepaymentEndDate.HasValue)
            {
                var expectedLastDueDate = GetInstallmentDueDate(
                    repayment.RepaymentDuration,
                    repayment.RepaymentStartDate.Value,
                    repayment.RepaymentFrequency);

                if (expectedLastDueDate.Date != repayment.RepaymentEndDate.Value.Date)
                {
                    missing.Add(
                         $"Repayment schedule mismatch: with a {repayment.RepaymentFrequency} frequency and duration of " +
                        $"{repayment.RepaymentDuration}, starting {repayment.RepaymentStartDate.Value:yyyy-MM-dd}, the final " +
                        $"installment falls on {expectedLastDueDate:yyyy-MM-dd}, not the configured end date " +
                        $"{repayment.RepaymentEndDate.Value:yyyy-MM-dd}. Adjust the duration, frequency, start date, or end date " +
                        "so they agree.");
                }
            }

            if (missing.Any())
                throw new BaseException(ExceptionType.OPERATION_FAILED,
                    $"Repayment plan is not fully configured or has errors, please fix the following: {string.Join(", ", missing)}");

            
        }

        public async Task<bool> AddSubscriberAsync(
        AddSubscriberRequest request,
        InputSubscriptionRequestArgs args)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var package = await _context.InputPackages
             .Include(x => x.Organization)
             .Include(x => x.CommitmentPlan)
             .Include(x => x.RepaymentPlan)
             .FirstOrDefaultAsync(x => x.Id == request.PackageId);

                    if (package == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

                    if (!package.IsActive)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Only active packages can accept subscribers");

                    if (package.SubScriptionType == InputSubScriptionType.Individual &&
                        request.SubscriberType != InputSubScriptionType.Individual)
                    {
                        throw new BaseException(ExceptionType.BAD_REQUEST, "This package does not allow group subscriptions");
                    }

                    if (package.SubScriptionType == InputSubScriptionType.Individual)
                    {
                        var isFarmer = await _context.Farmers.FirstOrDefaultAsync(x => x.UserId == request.SubscriberUserId);
                        if (isFarmer == null)
                            throw new BaseException(ExceptionType.BAD_REQUEST, "This package does not allow group subscriptions, Subscriber not a farmer");

                        if (package.Organization != null)
                        {
                            var farmerBelongsToOrg = await _context.FarmerOrganizations
                                .AnyAsync(fo =>
                                    fo.FarmerId == isFarmer.Id &&
                                    fo.OrganizationCode == package.Organization.OrganizationCode);

                            if (!farmerBelongsToOrg)
                                throw new BaseException(
                                    Core.Enums.ExceptionType.ACCESS_DENIED,
                                    "Subscriber does not belong to the organization this package belongs to");
                        }
                    }
                    else if (request.SubscriberType == InputSubScriptionType.MemberShip)
                    {
                        var membership = await _context.MemberShips
                            .FirstOrDefaultAsync(x => x.UserId == request.SubscriberUserId);

                        if (membership == null)
                            throw new BaseException(ExceptionType.BAD_REQUEST, "Subscriber is not a valid membership/cooperative");

                        if (package.Organization != null)
                        {
                            var membershipBelongsToOrg = await _context.OrganizationMemberships
                                .AnyAsync(mo =>
                                    mo.MembershipId == membership.Id &&
                                    mo.OrganizationId == package.Organization.OrganizationId);

                            if (!membershipBelongsToOrg)
                                throw new BaseException(
                                    Core.Enums.ExceptionType.ACCESS_DENIED,
                                    "Membership does not belong to the organization this package belongs to");
                        }
                    }

                    var alreadySubscribed = await _context.InputPackageSubscriptions
                        .AnyAsync(x =>
                            x.InputPackageId == request.PackageId &&
                            x.SubscriberUserId == request.SubscriberUserId &&
                            x.IsActive);

                    if (alreadySubscribed)
                    {
                        throw new BaseException(
                            Core.Enums.ExceptionType.ALREADY_EXIST,
                            "Subscriber already added to this package");
                    }

                    var repayment = package.RepaymentPlan;
                    var commitment = package.CommitmentPlan;



                    if (package.CommitmentRequired)
                        ValidateCommitmentPlan(commitment);
                    if (package.RepaymentRequired)
                        ValidateRepaymentPlan(repayment);

                    var subscription = new InputPackageSubscription
                    {
                        InputPackageId = package.Id,
                        SubscriberUserId = request.SubscriberUserId,
                        SubscriberType = request.SubscriberType,
                        AddedByUserId = args.UserId,
                        OrganizationId = args.OrganizationId,
                        Status = SubscriptionStatus.PendingApproval,
                        IsActive = true,
                        SubscribedOn = DateTimeOffset.UtcNow,

                        TotalFinancingValue = package.TotalFinancingValue,

                        CommitmentRequired = package.CommitmentRequired,
                        CommitmentValue = commitment?.CommitmentValue ?? 0,
                        CommitmentPaymentStartDate = commitment?.CommitmentPaymentStartDate,
                        CommitmentPaymentEndDate = commitment?.CommitmentPaymentEndDate,

                        RepaymentFrequency = repayment?.RepaymentFrequency ?? default,
                        RepaymentDuration = repayment?.RepaymentDuration ?? 0,
                        RepaymentStartDate = repayment?.RepaymentStartDate,
                        RepaymentEndDate = repayment?.RepaymentEndDate,
                        RepaymentGracePeriod = repayment?.RepaymentGracePeriod,
                        RepaymentMode = repayment?.RepaymentMode ?? default,
                        RepaymentValue = repayment?.RepaymentValue ?? 0,

                        BusinessCode = args.BusinessCode
                    };

                    _context.InputPackageSubscriptions.Add(subscription);
                    await _context.SaveChangesAsync();
                    if (package.RepaymentRequired)
                    {
                        var repaymentPlan = package.RepaymentPlan;

                        var schedules = GenerateRepaymentSchedules(
                            subscription.Id,
                            repaymentPlan.RepaymentStartDate!.Value,
                            repaymentPlan.RepaymentEndDate!.Value,
                            repaymentPlan.RepaymentGracePeriod,
                            repaymentPlan.RepaymentDuration,
                            repaymentPlan.RepaymentFrequency,
                            repaymentPlan.RepaymentValue);

                        _context.RepaymentSchedules.AddRange(schedules);
                    }
                    // OUTBOX EVENT
                    var outboxEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(InputPackageSubscriberAddedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new InputPackageSubscriberAddedEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = package.Id,
                            SubscriberUserId = request.SubscriberUserId,
                            Status = SubscriptionStatus.PendingApproval.ToString(),
                            SubscriberType = request.SubscriberType.ToString(),
                            CommitmentRequired = package.CommitmentRequired,
                            CommitmentValue = commitment?.CommitmentValue ?? 0,
                            OccurredOn = DateTime.UtcNow
                        })
                    };

                    _context.OutboxMessages.Add(outboxEvent);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> RemoveSubscriptionAsync(
        InputSubscriptionRequestArgs args,
        CancellationToken cancellationToken = default)
        {
            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x =>
                    x.InputPackageId == args.PackageId &&
                    x.Id == args.SubscriptionId &&
                    x.IsActive,
                    cancellationToken);

            if (subscription == null)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "Subscription not found or already removed");
            }

            subscription.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static List<RepaymentSchedule> GenerateRepaymentSchedules(
         long subscriptionId,
         DateTimeOffset repaymentStartDate,
         DateTimeOffset repaymentEndDate,
         DateTimeOffset? repaymentGracePeriod,
         int repaymentDuration,
         RepaymentFrequency repaymentFrequency,
         decimal repaymentValue)
        {
            if (repaymentDuration <= 0)
                throw new ArgumentException("Repayment duration must be greater than zero.");

            var schedules = new List<RepaymentSchedule>();

            decimal installmentAmount = Math.Floor(repaymentValue / repaymentDuration * 100m) / 100m;
            decimal lastInstallmentAmount = repaymentValue - (installmentAmount * (repaymentDuration - 1));

            int? graceMonthOffset = null;
            int? graceDayOffset = null;

            if (repaymentGracePeriod.HasValue)
            {
                if (repaymentFrequency is RepaymentFrequency.Monthly or RepaymentFrequency.Quarterly or RepaymentFrequency.Yearly)
                {
                    graceMonthOffset = ((repaymentGracePeriod.Value.Year - repaymentEndDate.Year) * 12)
                                        + (repaymentGracePeriod.Value.Month - repaymentEndDate.Month);
                }
                else
                {
                    graceDayOffset = (int)(repaymentGracePeriod.Value.Date - repaymentEndDate.Date).TotalDays;
                }
            }

            for (int installment = 1; installment <= repaymentDuration; installment++)
            {
                var dueDate = GetInstallmentDueDate(installment, repaymentStartDate, repaymentFrequency);
                var amount = (installment == repaymentDuration) ? lastInstallmentAmount : installmentAmount;

                DateTimeOffset? graceDeadline = graceMonthOffset.HasValue
                    ? EndOfMonth(dueDate.AddMonths(graceMonthOffset.Value))
                    : graceDayOffset.HasValue
                        ? dueDate.AddDays(graceDayOffset.Value)
                        : null;

                schedules.Add(new RepaymentSchedule
                {
                    InputPackageSubscriptionId = subscriptionId,
                    InstallmentNumber = installment,
                    DueDate = dueDate,
                    GracePeriodDueDate = graceDeadline,
                    Amount = amount,
                    Balance = amount,
                    AmountPaid = 0,
                    PaidOn = null,
                    IsPaid = false,
                    IsWithinGracePeriod = false,
                    IsOverdue = false,
                    PenaltyAccrued = 0,
                    PenaltyPaid = 0,
                    Status = RepaymentScheduleStatus.Pending
                });
            }

            if (schedules[^1].DueDate.Date != repaymentEndDate.Date)
            {
                throw new BaseException(ExceptionType.OPERATION_FAILED,
                    $"Repayment schedule mismatch: computed last due date {schedules[^1].DueDate:yyyy-MM-dd} " +
                    $"does not match configured repayment end date {repaymentEndDate:yyyy-MM-dd}. " +
                    "Check RepaymentDuration against RepaymentStartDate/RepaymentEndDate.");
            }

            return schedules;
        }

        private static DateTimeOffset GetInstallmentDueDate(
            int installmentNumber,
            DateTimeOffset repaymentStartDate,
            RepaymentFrequency repaymentFrequency)
        {
            int step = installmentNumber - 1;

            return repaymentFrequency switch
            {
                RepaymentFrequency.Daily => repaymentStartDate.AddDays(step),
                RepaymentFrequency.Weekly => repaymentStartDate.AddDays(7 * step),
                RepaymentFrequency.Monthly => EndOfMonth(repaymentStartDate.AddMonths(step)),
                RepaymentFrequency.Quarterly => EndOfMonth(repaymentStartDate.AddMonths(3 * step)),
                RepaymentFrequency.Yearly => EndOfMonth(repaymentStartDate.AddYears(step)),
                RepaymentFrequency.NA => throw new BaseException(ExceptionType.OPERATION_FAILED, "Repayment frequency is not set"),
                _ => throw new NotSupportedException($"Repayment frequency '{repaymentFrequency}' is not supported.")
            };
        }

        private static DateTimeOffset EndOfMonth(DateTimeOffset date)
        {
            int lastDay = DateTime.DaysInMonth(date.Year, date.Month);
            return new DateTimeOffset(date.Year, date.Month, lastDay, date.Hour, date.Minute, date.Second, date.Offset);
        }

        private static void SoftDeleteSchedule(RepaymentSchedule schedule, string deletedByUserId, string reason, long? supersededByScheduleId = null)
        {
            schedule.IsDeleted = true;
            schedule.DeletedOn = DateTimeOffset.UtcNow;
            schedule.DeletedByUserId = deletedByUserId;
            schedule.DeletionReason = reason;
            schedule.SupersededByScheduleId = supersededByScheduleId;
        }
        public async Task<EditRepaymentPlanResult> EditRepaymentPlanAsync(
      long packageId,
      UpdateRepaymentPlanRequest request,
      InputPackageConfigArgs args)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var package = await _context.InputPackages
                        .Include(x => x.RepaymentPlan)
                        .Include(x => x.Subscriptions.Where(s => s.IsActive))
                            .ThenInclude(s => s.RepaymentSchedules)
                        .FirstOrDefaultAsync(x =>
                            x.Id == packageId &&
                            x.OrganizationId == args.organizationId);

                    if (package == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

                    if (!package.RepaymentRequired || package.RepaymentPlan == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "This package does not have a repayment plan configured");

                    await EnsurePackageIsEditable(package);

                    var plan = package.RepaymentPlan;

                    ApplyIfPresent(request.RepaymentFrequency, v => plan.RepaymentFrequency = v);
                    ApplyIfPresent(request.RepaymentDuration, v => plan.RepaymentDuration = v);
                    ApplyIfPresent(request.RepaymentStartDate, v => plan.RepaymentStartDate = v);
                    ApplyIfPresent(request.RepaymentEndDate, v => plan.RepaymentEndDate = v);
                    ApplyIfPresent(request.RepaymentGracePeriod, v => plan.RepaymentGracePeriod = v);
                    ApplyIfPresent(request.RepaymentModel, v => plan.RepaymentMode = v);
                    ApplyIfPresent(request.RepaymentValue, v => plan.RepaymentValue = v);
                    ApplyIfPresent(request.RiskCategory, v => plan.RiskCategory = v);

                    ValidateRepaymentPlan(plan);

                    var result = new EditRepaymentPlanResult();
                    var activeSubscriptions = package.Subscriptions.ToList();

                    if (!activeSubscriptions.Any())
                    {
                       
                        result.Success = true;
                        result.SubscribersRegenerated = 0;
                    }
                    else
                    {
                        foreach (var subscription in activeSubscriptions)
                        {
                            var activeSchedules = subscription.RepaymentSchedules
                                .Where(s => !s.IsDeleted)
                                .ToList();

                            bool hasPayments = activeSchedules.Any(s => s.AmountPaid > 0 || s.IsPaid);

                            if (hasPayments)
                            {
                                // Don't touch schedules with payment history 
                                result.SubscribersRequiringManualReview.Add(subscription.Id);
                                continue;
                            }

                            subscription.RepaymentFrequency = plan.RepaymentFrequency;
                            subscription.RepaymentDuration = plan.RepaymentDuration;
                            subscription.RepaymentStartDate = plan.RepaymentStartDate;
                            subscription.RepaymentEndDate = plan.RepaymentEndDate;
                            subscription.RepaymentGracePeriod = plan.RepaymentGracePeriod;
                            subscription.RepaymentMode = plan.RepaymentMode;
                            subscription.RepaymentValue = plan.RepaymentValue;

                            var newSchedules = GenerateRepaymentSchedules(
                                subscription.Id,
                                plan.RepaymentStartDate!.Value,
                                plan.RepaymentEndDate!.Value,
                                plan.RepaymentGracePeriod,
                                plan.RepaymentDuration,
                                plan.RepaymentFrequency,
                                plan.RepaymentValue);

                            _context.RepaymentSchedules.AddRange(newSchedules);

                            await _context.SaveChangesAsync();

                            foreach (var oldSchedule in activeSchedules)
                            {
                                var replacement = newSchedules
                                    .FirstOrDefault(s => s.InstallmentNumber == oldSchedule.InstallmentNumber);

                                SoftDeleteSchedule(
                                    oldSchedule,
                                    args.createdByUserId,
                                    "Repayment plan edited — schedule regenerated",
                                    replacement?.Id);
                            }

                            result.SubscribersRegenerated++;
                        }

                        result.Success = true;
                    }

                    package.UpdatedOn = DateTimeOffset.UtcNow;
                    package.UpdatedByUserId = args.createdByUserId;

                    var outboxEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(RepaymentPlanUpdatedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new RepaymentPlanUpdatedEvent
                        {
                            PackageId = package.Id,
                            OrganizationId = package.OrganizationId,
                            SubscribersRegenerated = result.SubscribersRegenerated,
                            SubscribersRequiringManualReview = result.SubscribersRequiringManualReview
                        })
                    };

                    _context.OutboxMessages.Add(outboxEvent);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }


        public async Task<bool> ApproveSubscriptionAsync(ApproveSubscriptionRequest request, InputSubscriptionRequestArgs args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var subscription = await _context.InputPackageSubscriptions
                        .Include(x => x.InputPackage)
                        .FirstOrDefaultAsync(x => x.Id == request.SubscriptionId && x.SubscriberUserId == args.SubscriberUserId);

                    if (subscription == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

                    if (subscription.Status != SubscriptionStatus.PendingApproval)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Only pending approval subscriptions can be processed");

                    if (request.ApprovalState == ApprovalDecision.Rejected)
                    {
                        if (string.IsNullOrWhiteSpace(request.RejectionReason))
                            throw new BaseException(ExceptionType.BAD_REQUEST, "Rejection reason is required");

                        subscription.Status = SubscriptionStatus.Rejected;
                        subscription.RejectedByUserId = args.UserId;
                        subscription.RejectedOn = DateTimeOffset.UtcNow;
                        subscription.RejectionReason = request.RejectionReason;
              
                        _context.InputPackageSubscriptions.Update(subscription);

                        await _context.SaveChangesAsync();

                        var rejectEvent = new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            EventType = nameof(InputPackageSubscriptionRejectedEvent),
                            CreatedOn = DateTime.UtcNow,
                            IsProcessed = false,
                            Payload = JsonSerializer.Serialize(new InputPackageSubscriptionRejectedEvent
                            {
                                SubscriptionId = subscription.Id,
                                PackageId = subscription.InputPackageId,
                                SubscriberUserId = subscription.SubscriberUserId,
                                RejectionReason = request.RejectionReason
                            })
                        };

                        _context.OutboxMessages.Add(rejectEvent);

                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return true;
                    }

                    subscription.Status = subscription.InputPackage.CommitmentRequired
                        ? SubscriptionStatus.PendingCommitment  
                        : SubscriptionStatus.PendingDisbursement;

                    subscription.ApprovedByUserId = args.UserId;
                    subscription.ApprovedOn = DateTimeOffset.UtcNow;

                    _context.InputPackageSubscriptions.Update(subscription);

                    await _context.SaveChangesAsync();

                    var approveEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(InputPackageSubscriptionApprovedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new InputPackageSubscriptionApprovedEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = subscription.InputPackageId,
                            SubscriberUserId = subscription.SubscriberUserId,
                            RequiresCommitment = subscription.InputPackage.CommitmentRequired
                        })
                    };

                    _context.OutboxMessages.Add(approveEvent);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<decimal> GetTotalCommitmentPaidAsync(
        long subscriptionId)
        {
            return await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;
        }

        public async Task<long> CreatePendingPaymentAsync(CreateCommitmentPaymentRequest request)
        {
            var payment = new InputPackageSubscriptionPayment
            {
                SubscriptionId = request.SubscriptionId,
                PaymentType = request.PaymentType,
                AmountPaid = request.Amount,
                PaymentChannel = request.PaymentChannel,
                Status = TransactionStatus.Pending,
                Reference = request.Reference,
                ExternalReference = request.ExternalReference,
                WalletProviderId = request.WalletProviderId,
                CreatedOn = DateTimeOffset.UtcNow,
                RecordedByUserId = request.RecordedByUserId
            };

            _context.InputPackageSubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();

            return payment.Id;
        }
        public async Task FlagPendingBankTransferAsync(long paymentId)
        {
            var payment = await _context.InputPackageSubscriptionPayments
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            payment.Status = TransactionStatus.AwaitingReconciliation;

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "BankTransferPendingVerification",
                Payload = JsonSerializer.Serialize(new { paymentId })
            });

            await _context.SaveChangesAsync();
        }
        public async Task FlagPendingCashApprovalAsync(long paymentId)
        {
            var payment = await _context.InputPackageSubscriptionPayments
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            payment.Status = TransactionStatus.AwaitingConfirmation;

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "CashPaymentPendingApproval",
                Payload = JsonSerializer.Serialize(new { paymentId })
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> RecordSuccessfulPaymentAsync(
        long paymentId,
        PaymentConfirmationRequest request, string performedBy)
        {
            var payment = await _context.InputPackageSubscriptionPayments
                .Include(x => x.Subscription)
                .ThenInclude(x => x.InputPackage)
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Payment record not found");

            if (payment.Status == TransactionStatus.Successful)
                return true;

            if (payment.Status == TransactionStatus.Failed)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Cannot confirm failed payment");

            payment.Status = request.Status;
            payment.ExternalReference = request.ExternalReference;
            payment.TransactionResponse = request.ResponsePayload;
            payment.PaidOn = DateTimeOffset.UtcNow;

            if (request.Status != TransactionStatus.Successful)
            {
                await _context.SaveChangesAsync();
                return false;
            }
            await _context.SaveChangesAsync();
            var subscription = payment.Subscription;

            switch (payment.PaymentType)
            {
                case PaymentType.Commitment:
                    await ApplyCommitmentPaymentEffectsAsync(subscription, payment, performedBy);
                    break;

                case PaymentType.Repayment:
                    await ApplyRepaymentEffectsAsync(subscription, payment, performedBy);
                    break;

                default:
                    throw new Exception($"Unhandled payment type '{payment.PaymentType}'");
            }

           

            return true;
        }

        private async Task ApplyCommitmentPaymentEffectsAsync(
        InputPackageSubscription subscription,
        InputPackageSubscriptionPayment payment,
        string performedBy)
        {
            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var fullyPaid = totalPaid >= subscription.CommitmentValue;

            if (fullyPaid 
                //&& subscription.Status == SubscriptionStatus.PendingCommitment
                )
            {
                subscription.Status = SubscriptionStatus.CommitmentPaid;

                //await RequestInputDisbursementApprovalAsync(subscription.Id,
                //    payment.ExternalReference, performedBy, subscription.SubscriberUserId);

                _context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = nameof(CommitmentFullyPaidEvent),
                    CreatedOn = DateTime.UtcNow,
                    IsProcessed = false,
                    Payload = JsonSerializer.Serialize(
                        new CommitmentFullyPaidEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = subscription.InputPackageId,
                            TotalPaid = totalPaid
                        })
                });
            }

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(CommitmentPaymentRecordedEvent),
                CreatedOn = DateTime.UtcNow,
                IsProcessed = false,
                Payload = JsonSerializer.Serialize(
                    new CommitmentPaymentRecordedEvent
                    {
                        SubscriptionId = subscription.Id,
                        SubscriberUserId = subscription.SubscriberUserId,
                        OrganizationId = subscription.OrganizationId,
                        AmountPaid = payment.AmountPaid,
                        OutstandingBalance = subscription.CommitmentValue - totalPaid,
                        FullyPaid = fullyPaid
                    })
            });

            await _context.SaveChangesAsync();
        }

        private async Task ApplyRepaymentEffectsAsync(
        InputPackageSubscription subscription,
        InputPackageSubscriptionPayment payment,
        string performedBy)
        {
            var totalRepaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var fullyRepaid = totalRepaid >= subscription.RepaymentValue;
            if (fullyRepaid 
                //&& (subscription.Status == SubscriptionStatus.PendingRepayment || subscription.Status ==
                //SubscriptionStatus.Disbursed || subscription.Status == SubscriptionStatus.Active)
                )
            {
                subscription.Status = SubscriptionStatus.Completed;

                _context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = nameof(RepaymentFullyPaidEvent),
                    CreatedOn = DateTime.UtcNow,
                    IsProcessed = false,
                    Payload = JsonSerializer.Serialize(
                        new RepaymentFullyPaidEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = subscription.InputPackageId,
                            TotalRepaid = totalRepaid
                        })
                });
            }

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(RepaymentRecordedEvent),
                CreatedOn = DateTime.UtcNow,
                IsProcessed = false,
                Payload = JsonSerializer.Serialize(
                    new RepaymentRecordedEvent
                    {
                        SubscriptionId = subscription.Id,
                        SubscriberUserId = subscription.SubscriberUserId,
                        OrganizationId = subscription.OrganizationId,
                        Amount = payment.AmountPaid,
                        RemainingAmount = subscription.RepaymentValue - totalRepaid,
                        IsCompleted = fullyRepaid
                    })
            });

            await _context.SaveChangesAsync();

        }

        public async Task<decimal> GetTotalRepaymentPaidAsync(long subscriptionId)
        {
            return await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;
        }

        public async Task<bool> RecordCommitmentPaymentAsync(
        RecordCommitmentPaymentRequest request,
        InputSubscriptionRequestArgs args)
        {
            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SubscriptionId &&
                    x.SubscriberUserId == args.SubscriberUserId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            // ⚠️ IMPORTANT:
            // Do NOT re-validate wallet, amounts, or recompute totals here
            // That already happened upstream

            var paymentExists = await _context.InputPackageSubscriptionPayments
                .AnyAsync(x =>
                    x.Reference == request.Reference);

            if (paymentExists)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Duplicate payment reference");

            var payment = new InputPackageSubscriptionPayment
            {
                SubscriptionId = subscription.Id,
                PaymentType = PaymentType.Commitment,
                AmountPaid = request.Amount,
                PaymentChannel = request.PaymentChannel,
                Status = TransactionStatus.Successful,
                Reference = request.Reference,
                ExternalReference = request.ExternalReference,
                PaidOn = DateTimeOffset.UtcNow
            };

            _context.InputPackageSubscriptionPayments.Add(payment);

            await _context.SaveChangesAsync();

            // ⚠️ ONLY minimal state transition (optional here)
            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            if (totalPaid >= subscription.CommitmentValue)
            {
                subscription.Status = SubscriptionStatus.PendingDisbursement;

                _context.InputPackageSubscriptions.Update(subscription);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task AddCreditBalanceAsync(long subscriptionId, decimal amount, string reference)
        {
            _context.SubscriptionCreditBalance.Add(new SubscriptionCreditBalance
            {
                SubscriptionId = subscriptionId,
                Amount = amount,
                Reference = reference,
                CreatedOn = DateTimeOffset.UtcNow,
                IsConsumed = false
            });

            await _context.SaveChangesAsync();
        }
        public async Task<decimal> GetUnconsumedCreditBalanceAsync(long subscriptionId)
        {
            return await _context.SubscriptionCreditBalance
                .Where(x => x.SubscriptionId == subscriptionId && !x.IsConsumed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
        }
        public async Task<CommitmentReminderDto?> GetCommitmentReminderAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                .ThenInclude(x => x.Organization)
                .Include(x => x.SubscriberUser)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            if (!subscription.InputPackage.CommitmentRequired)
            {
                return new CommitmentReminderDto
                {
                    RequiresReminder = false
                };
            }


            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var required = subscription.CommitmentValue;
            var outstanding = required - totalPaid;

            if (outstanding <= 0)
            {
                return new CommitmentReminderDto
                {
                    SubscriptionId = subscription.Id,
                    SubscriberUserId = subscription.SubscriberUserId,
                    TotalCommitment = required,
                    AmountPaid = totalPaid,
                    OutstandingAmount = 0,
                    RequiresReminder = false,
                    Organization = new OrganizationDto
                    {
                        OrganizationName = subscription.InputPackage.Organization.CompanyName
                    }
                };
            }


            var user = subscription.SubscriberUser;

            var email = user?.Email;
            var phone = user?.PhoneNumber;


            string message;

            if (totalPaid == 0)
            {
                message =
                    $"You have an outstanding commitment of ₦{required:N0} " +
                    $"for the {subscription.InputPackage.Name} package. " +
                    $"Kindly make your commitment payment to proceed with input release.";
            }
            else
            {
                message =
                    $"You have an outstanding commitment balance of ₦{outstanding:N0} " +
                    $"for the {subscription.InputPackage.Name} package. " +
                    $"You have paid ₦{totalPaid:N0} of ₦{required:N0}. " +
                    $"Please complete payment to proceed with input release.";
            }


            return new CommitmentReminderDto
            {
                SubscriptionId = subscription.Id,
                SubscriberUserId = subscription.SubscriberUserId,

                TotalCommitment = required,
                AmountPaid = totalPaid,
                OutstandingAmount = outstanding,

                RequiresReminder = true,

                Message = message,

                Email = email,
                PhoneNumber = phone
            };
        }
        public async Task<RepaymentReminderDto?> GetRepaymentReminderAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                .ThenInclude(x => x.Organization)
                .Include(x => x.SubscriberUser)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            //if (subscription.Status != SubscriptionStatus.PendingRepayment)
            //{
            //    return new RepaymentReminderDto
            //    {
            //        RequiresReminder = false
            //    };
            //}

            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var creditBalance = await GetUnconsumedCreditBalanceAsync(subscription.Id);

            var required = subscription.RepaymentValue;

            var outstanding = required - totalPaid;

            if (outstanding <= 0)
            {
                return new RepaymentReminderDto
                {
                    SubscriptionId = subscription.Id,
                    SubscriberUserId = subscription.SubscriberUserId,
                    TotalRepayment = required,
                    AmountPaid = totalPaid,
                    CreditBalance = creditBalance,
                    OutstandingAmount = 0,
                    RequiresReminder = false,
                    RequiresReconciliation = false,
                    Organization = new OrganizationDto
                    {
                        OrganizationName = subscription.InputPackage.Organization.CompanyName
                    }
                };
            }

            var user = subscription.SubscriberUser;
            var email = user?.Email;
            var phone = user?.PhoneNumber;

            var coveredByCredit = creditBalance >= outstanding;

            string message;

            if (coveredByCredit)
            {
                message =
                    $"Your repayment of ₦{required:N0} for the {subscription.InputPackage.Name} package " +
                    $"is covered by a credit balance of ₦{creditBalance:N0}. " +
                    $"Please reach out to your organization to reconcile and close out your subscription.";
            }
            else if (totalPaid == 0 && creditBalance == 0)
            {
                message =
                    $"You have an outstanding repayment of ₦{required:N0} " +
                    $"for the {subscription.InputPackage.Name} package. " +
                    $"Kindly make your repayment via cash or produce to clear your balance.";
            }
            else
            {
                message =
                    $"You have an outstanding repayment balance of ₦{outstanding:N0} " +
                    $"for the {subscription.InputPackage.Name} package. " +
                    $"You have repaid ₦{totalPaid:N0}" +
                    (creditBalance > 0 ? $" and have a credit of ₦{creditBalance:N0} that will reduce what you still need to pay. " : ". ") +
                    $"Please complete repayment to close out your subscription.";
            }

            return new RepaymentReminderDto
            {
                SubscriptionId = subscription.Id,
                SubscriberUserId = subscription.SubscriberUserId,

                TotalRepayment = required,
                AmountPaid = totalPaid,
                CreditBalance = creditBalance,
                OutstandingAmount = outstanding,

                RequiresReminder = true,
                RequiresReconciliation = coveredByCredit,

                Message = message,

                Email = email,
                PhoneNumber = phone
            };
        }
        public async Task<List<RepaymentScheduleItemDto>> GetRepaymentScheduleAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var schedules = await _context.RepaymentSchedules
                .Where(x => x.InputPackageSubscriptionId == subscriptionId)
                .OrderBy(x => x.InstallmentNumber)
                .ToListAsync();

            if (!schedules.Any())
                return new List<RepaymentScheduleItemDto>();

            var today = DateTimeOffset.UtcNow;
            var result = new List<RepaymentScheduleItemDto>();
            var hasChanges = false;

            foreach (var schedule in schedules)
            {
                var dto = EvaluateSchedule(schedule, today, out var changed);
                if (changed) hasChanges = true;
                result.Add(dto);
            }

            if (hasChanges)
                await _context.SaveChangesAsync();

            return result;
        }
        private RepaymentScheduleItemDto EvaluateSchedule(RepaymentSchedule schedule, DateTimeOffset today, out bool changed)
        {
            changed = false;

            var isPastDue = schedule.DueDate < today;
            var isFullySatisfied = schedule.IsPaid || schedule.AmountPaid >= schedule.Amount;

            RepaymentScheduleStatus status;

            if (isFullySatisfied)
            {
                status = RepaymentScheduleStatus.Paid;

                if (schedule.IsOverdue)
                {
                    schedule.IsOverdue = false;
                    changed = true;
                }
            }
            else if (isPastDue)
            {
                if (schedule.IsWithinGracePeriod)
                {
                    // Late, but tolerated for now — visually distinct from a hard Overdue,
                    // but still worth reminding about since money is still owed.
                    status = RepaymentScheduleStatus.InGraceperiod;
                }
                else
                {
                    status = RepaymentScheduleStatus.Overdue;

                    if (!schedule.IsOverdue)
                    {
                        schedule.IsOverdue = true;
                        changed = true;
                    }
                }
            }
            else
            {
                status = schedule.AmountPaid > 0
                    ? RepaymentScheduleStatus.PartiallyPaid
                    : RepaymentScheduleStatus.Pending;

                if (schedule.IsOverdue)
                {
                    schedule.IsOverdue = false;
                    changed = true;
                }
            }

            var outstanding = Math.Max(schedule.Amount - schedule.AmountPaid, 0);
            var penaltyOutstanding = Math.Max(schedule.PenaltyAccrued - schedule.PenaltyPaid, 0);

            var requiresReminder =
                status == RepaymentScheduleStatus.Overdue ||
                status == RepaymentScheduleStatus.InGraceperiod;

            return new RepaymentScheduleItemDto
            {
                ScheduleId = schedule.Id,
                InstallmentNumber = schedule.InstallmentNumber,
                DueDate = schedule.DueDate,

                ExpectedAmount = schedule.Amount,
                AmountPaid = schedule.AmountPaid,
                OutstandingAmount = outstanding,

                PenaltyAccrued = schedule.PenaltyAccrued,
                PenaltyPaid = schedule.PenaltyPaid,
                PenaltyOutstanding = penaltyOutstanding,

                IsWithinGracePeriod = schedule.IsWithinGracePeriod,

                Status = status,
                RequiresReminder = requiresReminder
            };
        }
        public async Task<PagedResult<RepaymentLedgerEntryDto>> GetRepaymentLedgerAsync(
        long subscriptionId, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 5;

            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var payments = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .Select(x => new
                {
                    x.Reference,
                    x.PaymentChannel,
                    x.AmountPaid,
                    x.PaidOn,
                    x.WalletProviderId
                })
                .ToListAsync();

            if (!payments.Any())
            {
                return new PagedResult<RepaymentLedgerEntryDto>
                {
                    Data = new List<RepaymentLedgerEntryDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Count = 0
                };
            }

            // ─── Batch-resolve wallet provider display names for cash entries ───
            var walletProviderIds = payments
                .Where(x => x.PaymentChannel == PaymentChannel.Wallet && x.WalletProviderId.HasValue)
                .Select(x => x.WalletProviderId!.Value)
                .Distinct()
                .ToList();

            var walletProviderNames = new Dictionary<long, string>();
            if (walletProviderIds.Any())
            {
                var wallets = await _context.UserWallets
                    .Include(x => x.WalletProvider)
                    .Where(x => walletProviderIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var wallet in wallets)
                    walletProviderNames[wallet.Id] = wallet.WalletProvider?.Name ?? wallet.WalletProvider?.Code ?? "Wallet";
            }

            // ─── Batch-resolve produce names for produce entries via matching inventory transactions ───
            var produceReferences = payments
                .Where(x => x.PaymentChannel == PaymentChannel.Produce)
                .Select(x => x.Reference)
                .Distinct()
                .ToList();

            var produceNamesByReference = new Dictionary<string, string>();
            if (produceReferences.Any())
            {
                var inventoryTransactions = await _context.InventoryTransactions
                    .Include(x => x.OrganizationProduct)
                    .Where(x =>
                        x.SubscriptionId == subscriptionId &&
                        produceReferences.Contains(x.ReferenceNumber))
                    .ToListAsync();

                foreach (var txn in inventoryTransactions)
                    produceNamesByReference[txn.ReferenceNumber] = txn.OrganizationProduct?.ProductName ?? "Produce";
            }

            // ─── Group by reference — a single repayment "event" may in future span multiple
            // channel rows sharing one reference (a hybrid cash+produce payment). Today's
            // write path only ever produces one row per reference, so this collapses to
            // single-row groups now, but stays forward-compatible without a shape change later. ───
            var grouped = payments
                .GroupBy(x => x.Reference)
                .Select(g =>
                {
                    var channelsInGroup = g.Select(x => x.PaymentChannel).Distinct().ToList();
                    var totalAmount = g.Sum(x => x.AmountPaid);
                    var earliestDate = g.Min(x => x.PaidOn) ?? DateTimeOffset.UtcNow;

                    string type;
                    string channelDisplay;

                    if (channelsInGroup.Count > 1)
                    {
                        type = "Hybrid";

                        var parts = g.Select(row => row.PaymentChannel == PaymentChannel.Wallet
                            ? (row.WalletProviderId.HasValue && walletProviderNames.TryGetValue(row.WalletProviderId.Value, out var wName)
                                ? $"Wallet ({wName})"
                                : "Wallet")
                            : (produceNamesByReference.TryGetValue(row.Reference, out var pName) ? pName : "Produce"))
                            .Distinct()
                            .ToList();

                        channelDisplay = string.Join(" + ", parts);
                    }
                    else if (channelsInGroup[0] == PaymentChannel.Wallet)
                    {
                        type = "Cash";
                        var walletId = g.First().WalletProviderId;
                        channelDisplay = walletId.HasValue && walletProviderNames.TryGetValue(walletId.Value, out var wName)
                            ? $"Wallet ({wName})"
                            : "Wallet";
                    }
                    else
                    {
                        type = "Produce";
                        channelDisplay = produceNamesByReference.TryGetValue(g.Key, out var pName) ? pName : "Produce";
                    }

                    return new { Reference = g.Key, Type = type, Channel = channelDisplay, Amount = totalAmount, Date = earliestDate };
                })
                .OrderBy(x => x.Date)
                .ToList();

            // ─── Compute running balance across the FULL chronological history first —
            // balance-after depends on true payment order, not on which page is requested. ───
            decimal runningPaid = 0m;
            var fullLedger = new List<RepaymentLedgerEntryDto>();

            foreach (var entry in grouped)
            {
                runningPaid += entry.Amount;

                fullLedger.Add(new RepaymentLedgerEntryDto
                {
                    Date = entry.Date,
                    Type = entry.Type,
                    Channel = entry.Channel,
                    Amount = entry.Amount,
                    BalanceAfter = Math.Max(subscription.RepaymentValue - runningPaid, 0),
                    Reference = entry.Reference
                });
            }

            var totalCount = fullLedger.Count;

            // Ledger view reads most-recent-first; balances were computed chronologically above.
            var pagedItems = fullLedger
                .OrderByDescending(x => x.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<RepaymentLedgerEntryDto>
            {
                Data = pagedItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount
            };
        }

        public async Task<List<OverdueRepaymentReminderDto>> GetOverdueSchedulesAcrossSubscriptionsAsync()
        {
            var today = DateTimeOffset.UtcNow;

            var subscriptions = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                .Include(x => x.SubscriberUser)
                .Include(x => x.RepaymentSchedules)
                .Where(x => x.Status == SubscriptionStatus.PendingRepayment)
                .ToListAsync();

            var reminders = new List<OverdueRepaymentReminderDto>();
            var hasChanges = false;

            foreach (var subscription in subscriptions)
            {
                var schedules = subscription.RepaymentSchedules
                    .OrderBy(x => x.InstallmentNumber)
                    .ToList();

                foreach (var schedule in schedules)
                {
                    var dto = EvaluateSchedule(schedule, today, out var changed);
                    if (changed) hasChanges = true;

                    if (!dto.RequiresReminder)
                        continue;

                    reminders.Add(new OverdueRepaymentReminderDto
                    {
                        SubscriptionId = subscription.Id,
                        SubscriberUserId = subscription.SubscriberUserId,
                        Email = subscription.SubscriberUser?.Email,
                        PhoneNumber = subscription.SubscriberUser?.PhoneNumber,
                        PackageName = subscription.InputPackage.Name,
                        Schedule = dto
                    });
                }
            }

            if (hasChanges)
                await _context.SaveChangesAsync();

            return reminders;
        }
        public async Task<long> RequestInputDisbursementApprovalAsync(
        long subscriptionId,
        string? reference,
        string userId, string subscriberuserId)
        {
            if(string.IsNullOrEmpty(subscriberuserId))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscriber user ID is required");

            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.SubscriberUserId == subscriberuserId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var allowedStatuses = new[] { SubscriptionStatus.PendingDisbursement, SubscriptionStatus.CommitmentPaid };
            if (!allowedStatuses.Contains(subscription.Status))
                throw new Exception($"Current status does not allow for input release request. Current subscription status: {subscription.Status}");

            var blockingDisbursementStatuses = new[] { DisbursementStatus.PendingApproval, DisbursementStatus.Approved };

            var existingDisbursement = await _context.InputSubscriptionDisbursements
                .FirstOrDefaultAsync(x => x.SubscriptionId == subscriptionId
                    && blockingDisbursementStatuses.Contains(x.Status));

            if (existingDisbursement != null)
            {
                existingDisbursement.RequestedAmount = subscription.TotalFinancingValue;
                existingDisbursement.RequestByUserId = userId;
                existingDisbursement.RequestForUserId = subscriberuserId;
                existingDisbursement.RequestCreatedOn = DateTimeOffset.Now;
                existingDisbursement.Reference = reference;
                existingDisbursement.Status = DisbursementStatus.PendingApproval;

                subscription.Status = SubscriptionStatus.PendingReleaseApproval;
                await _context.SaveChangesAsync();

                return existingDisbursement.Id;
            }

            var request = new InputSubscriptionDisbursement
            {
                SubscriptionId = subscriptionId,
                RequestedAmount = subscription.TotalFinancingValue,
                RequestByUserId = userId,
                RequestForUserId = subscriberuserId,
                RequestCreatedOn = DateTimeOffset.Now,
                Reference = reference,
                Status = DisbursementStatus.PendingApproval,
            };
            _context.InputSubscriptionDisbursements.Add(request);

            subscription.Status = SubscriptionStatus.PendingReleaseApproval;
            await _context.SaveChangesAsync();

            return request.Id;
        }
        public async Task<bool> ApproveInputDisbursementRequestAsync(
        ApproveDisbursementRequestDto request,
        InputSubscriptionRequestArgs args)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid request");

            var query = _context.InputSubscriptionDisbursements
                .Include(x => x.Subscription)
                    .ThenInclude(x => x.InputPackage)
                        .ThenInclude(x => x.RepaymentPlan)
                .AsQueryable();

            query = query.Where(x => x.Subscription.SubscriberUserId == args.SubscriberUserId
                && x.SubscriptionId == args.SubscriptionId);

            var disbursementRequest = await query
                .OrderByDescending(x => x.RequestCreatedOn)
                .FirstOrDefaultAsync();

            if (disbursementRequest == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Disbursement request not found");

            if (disbursementRequest.Status != DisbursementStatus.PendingApproval)
                throw new Exception($"Request already processed. Current disbursement status: {disbursementRequest.Status}");
            if(request.ApprovalState == ApprovalDecision.Approved)
            {
                disbursementRequest.Status = DisbursementStatus.Approved;
                disbursementRequest.ApprovedByUserId = args.UserId;
                disbursementRequest.ApprovedOn = DateTimeOffset.UtcNow;
                disbursementRequest.Subscription.Status = SubscriptionStatus.PendingDisbursement;
                disbursementRequest.ApprovedAmount = request.Amount.HasValue ? request.Amount.Value : disbursementRequest.RequestedAmount;

            }else if(request.ApprovalState == ApprovalDecision.Rejected)
            {
                disbursementRequest.Status = DisbursementStatus.Rejected;
                disbursementRequest.RejectedByUserId = args.UserId;
                disbursementRequest.RejectedOn = DateTimeOffset.UtcNow;
                disbursementRequest.Subscription.Status = SubscriptionStatus.Rejected;
                disbursementRequest.RejectionReason = "Disbursement Rejected by Organization";
            }
            else
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid approval state");
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<decimal> GetAvailableBalanceAsync(long locationId)
        {
            return await _context.LocationInventories
                .Where(x =>
                    x.LocationId == locationId &&
                    x.LocationInventoryType == LocationInventoryType.Monetary &&
                    x.IsActive == true &&
                    x.DeletedOn == null)
                .SumAsync(x => x.TotalAmount);
        }

        public async Task DeductAsync(
        long locationId,
        decimal amount,
        string referenceNumber,
        string agentUserId)
        {
            if (amount <= 0)
                throw new DisbursementException("Deduction amount must be greater than zero.");

            var monetaryRows = await _context.LocationInventories
        .Where(x =>
            x.LocationId == locationId &&
            x.LocationInventoryType == LocationInventoryType.Monetary &&
            x.IsActive == true &&
            x.DeletedOn == null &&
            x.TotalAmount > 0)
        .OrderBy(x => x.RecordedOn)
        .ToListAsync();

            decimal totalAvailable = monetaryRows.Sum(x => x.TotalAmount);

            if (totalAvailable < amount)
                throw new DisbursementException(
                    $"Insufficient funds at this location. " +
                    $"Available: {totalAvailable:N2}, Requested: {amount:N2}.");

            decimal remaining = amount;

            foreach (var row in monetaryRows)
            {
                if (remaining <= 0) break;

                decimal deductFromThisRow = Math.Min(row.TotalAmount, remaining);

                row.TotalAmount -= deductFromThisRow;
                remaining -= deductFromThisRow;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    FromLocationId = locationId,
                    ToLocationId = locationId,
                    OrganizationProductId = null,          // monetary — no product
                    Amount = deductFromThisRow,
                    QuantityAfter = row.TotalAmount,
                    TransactionType = InventoryTransactionType.Disbursement,
                    ReferenceNumber = referenceNumber,
                    TransactionDate = DateTimeOffset.UtcNow,
                    PerformedByUserId = agentUserId,
                });
            }

        }

        public async Task<bool> DisburseSubscriptionAsync(
        DisburseSubscriptionRequest request,
        InputSubscriptionRequestArgs args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var subscription = await _context.InputPackageSubscriptions
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.InputPackageItems)
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.RepaymentPlan)
                        .Include(x => x.InputPackage)
                            .ThenInclude(x => x.AdditionalCosts)
                        .FirstOrDefaultAsync(x =>
                            x.Id == args.SubscriptionId &&
                            x.SubscriberUserId == args.SubscriberUserId);

                    if (subscription == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

                    var approvedRequest = await _context.InputSubscriptionDisbursements
                        .FirstOrDefaultAsync(x =>
                            x.SubscriptionId == args.SubscriptionId &&
                            x.Status == DisbursementStatus.Approved);

                    if (approvedRequest == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Disbursement not approved yet");

                    if (subscription.Status != SubscriptionStatus.PendingDisbursement)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not ready for disbursement");

                    if (subscription.InputPackage.RepaymentPlan == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Repayment plan is required for disbursement");

                    var alreadyDisbursed = await _context.InputSubscriptionDisbursements
                        .AnyAsync(x => x.SubscriptionId == subscription.Id);

                    if (alreadyDisbursed)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription already disbursed");

                    decimal disbursedAmount = 0m;

                    var additionalCosts = subscription.InputPackage.AdditionalCosts
                        ?? new List<AdditionalInputPackageCost>();

                    decimal paidToUserTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.PaidOutToUser)
                        .Sum(x => x.Amount);

                    decimal vendorPaidTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.VendorPaid)
                        .Sum(x => x.Amount);

                    decimal internalTotal = additionalCosts
                        .Where(x => x.AdditionalCostTreatmentType == AdditionalCostTreatmentType.Internal)
                        .Sum(x => x.Amount);
                    if (subscription.InputPackage.PackageStructure == InputPackageStructure.Physical)
                    {
                        decimal itemsTotal = 0m;

                        foreach (var item in subscription.InputPackage.InputPackageItems)
                        {
                            var inventory = await _context.LocationInventories
                                .Include(x => x.OrganizationProduct)
                                .FirstOrDefaultAsync(x =>
                                    x.OrganizationProductId == item.OrganizationProductId);

                            if (inventory == null)
                                throw new Exception($"Product not found: {item.OrganizationProductId}");

                            if (inventory.TotalAmount < item.Quantity)
                                throw new Exception($"Insufficient inventory for {inventory.OrganizationProduct.ProductName}");

                            inventory.TotalAmount -= item.Quantity;

                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                OrganizationProductId = inventory.OrganizationProductId,
                                Quantity = item.Quantity,
                                QuantityBefore = inventory.TotalAmount + item.Quantity,
                                QuantityAfter = inventory.TotalAmount,
                                TransactionType = InventoryTransactionType.Disbursement,
                                ReferenceNumber = request.Reference,
                                SubscriptionId = subscription.Id,
                                TransactionDate = DateTimeOffset.UtcNow
                            });

                            itemsTotal += item.Quantity * item.UnitCost;
                        }

                        await _context.SaveChangesAsync();

                        disbursedAmount =
                            itemsTotal +
                            paidToUserTotal +
                            vendorPaidTotal;
                    }

                    else
                    {
                        disbursedAmount = subscription.TotalFinancingValue;
                    }

                    approvedRequest.Status = DisbursementStatus.Disbursed;
                    approvedRequest.DisbursedOn = DateTimeOffset.UtcNow;
                    approvedRequest.ApprovedAmount = disbursedAmount;
                    approvedRequest.DisbursedByUserId = args.UserId;
                    approvedRequest.Reference = request.Reference;

                    subscription.Status = SubscriptionStatus.Active;
                    subscription.DisbursedOn = DateTimeOffset.UtcNow;

                    var plan = subscription.InputPackage.RepaymentPlan;

                    var schedule = _repaymentEngine.GenerateSchedule(
                        new RepaymentEngineRequest
                        {
                            InputSubscriptionId = subscription.Id,
                            Frequency = plan.RepaymentFrequency,
                            Duration = plan.RepaymentDuration,
                            StartDate = plan.RepaymentStartDate,
                            EndDate = plan.RepaymentEndDate,
                            GracePeriod = plan.RepaymentGracePeriod,
                            TotalAmount = disbursedAmount
                        });

                    _context.RepaymentSchedules.AddRange(
                        schedule.Select(x => new RepaymentSchedule
                        {
                            InputPackageSubscriptionId = subscription.Id,
                            InstallmentNumber = x.InstallmentNumber,
                            DueDate = x.DueDate,
                            Amount = x.Amount,
                            IsWithinGracePeriod = x.IsWithinGracePeriod,
                            IsPaid = false
                        })
                    );

                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(SubscriptionDisbursedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new SubscriptionDisbursedEvent
                        {
                            SubscriptionId = subscription.Id,
                            PackageId = subscription.InputPackageId,
                            Amount = disbursedAmount
                        })
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> RecordSubscriptionRepaymentAsync(RecordRepaymentRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. GET SUBSCRIPTION
                    var subscription = await _context.InputPackageSubscriptions
                        .FirstOrDefaultAsync(x => x.Id == request.SubscriptionId && x.SubscriberUserId == request.SubscriberUserId);


                    if (subscription == null)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

                    if (subscription.Status != SubscriptionStatus.Active)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Repayment is only allowed for active subscriptions");

                    // 2. GET OUTSTANDING SCHEDULES
                    var schedules = await _context.RepaymentSchedules
                        .Where(x => x.InputPackageSubscriptionId == subscription.Id && !x.IsPaid)
                        .OrderBy(x => x.DueDate)
                        .ToListAsync();

                    if (!schedules.Any())
                        throw new BaseException(ExceptionType.BAD_REQUEST, "No outstanding repayment schedules found");

                    // 3. CALCULATE OUTSTANDING VALUES (FOR ALLOCATION ONLY)
                    var outstandingPenalty = schedules.Sum(x => x.PenaltyAccrued);
                    var outstandingPrincipal = schedules.Sum(x => x.Amount - x.AmountPaid);

                    var allocationEngine = new RepaymentAllocationEngine();

                    var allocation = allocationEngine.Allocate(
                        request.Amount,
                        outstandingPenalty,
                        0,
                        outstandingPrincipal);

                    // 4. APPLY PAYMENT TO SCHEDULES (PRINCIPAL FLOW)
                    decimal remaining = request.Amount;

                    foreach (var schedule in schedules)
                    {
                        if (remaining <= 0)
                            break;

                        var outstanding = schedule.Amount - schedule.AmountPaid;

                        if (outstanding <= 0)
                            continue;

                        if (remaining >= outstanding)
                        {
                            schedule.AmountPaid += outstanding;
                            schedule.IsPaid = true;
                            schedule.PaidOn = DateTimeOffset.UtcNow;

                            remaining -= outstanding;
                        }
                        else
                        {
                            schedule.AmountPaid += remaining;
                            remaining = 0;
                        }

                        _context.RepaymentSchedules.Update(schedule);
                    }

                    // 5. CREATE PAYMENT RECORD
                    var payment = new InputPackageSubscriptionPayment
                    {
                        SubscriptionId = subscription.Id,
                        PaymentType = PaymentType.Repayment,
                        AmountPaid = request.Amount,
                        PaymentChannel = request.PaymentChannel,
                        Status = TransactionStatus.Successful,
                        Reference = request.Reference,
                        ExternalReference = request.ExternalReference,
                        PaidOn = DateTimeOffset.UtcNow
                    };

                    _context.InputPackageSubscriptionPayments.Add(payment);

                    await _context.SaveChangesAsync();

                    // 6. CREATE ALLOCATION LEDGER (IMPORTANT)
                    _context.SubscriptionPaymentAllocations.Add(new SubscriptionPaymentAllocation
                    {
                        InputPackageSubscriptionPaymentId = payment.Id,
                        PenaltyPortion = allocation.PenaltyPortion,
                        InterestPortion = allocation.InterestPortion,
                        PrincipalPortion = allocation.PrincipalPortion
                    });

                    // 7. CHECK COMPLETION
                    var allPaid = await _context.RepaymentSchedules
                        .Where(x => x.InputPackageSubscriptionId == subscription.Id)
                        .AllAsync(x => x.IsPaid);

                    if (allPaid)
                    {
                        subscription.Status = SubscriptionStatus.Completed;
                        subscription.CompletedOn = DateTimeOffset.UtcNow;
                    }

                    _context.InputPackageSubscriptions.Update(subscription);

                    await _context.SaveChangesAsync();

                    var outboxEvent = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        EventType = nameof(RepaymentRecordedEvent),
                        CreatedOn = DateTime.UtcNow,
                        IsProcessed = false,
                        Payload = JsonSerializer.Serialize(new RepaymentRecordedEvent
                        {
                            SubscriptionId = subscription.Id,
                            Amount = request.Amount,
                            RemainingAmount = remaining,
                            IsCompleted = subscription.Status == SubscriptionStatus.Completed
                        })
                    };

                    _context.OutboxMessages.Add(outboxEvent);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task<IQueryable<InputPackageSubscription>> BuildSubscriptionQuery(
        SubscriptionQueryFilter filter)
        {
            var query = _context.InputPackageSubscriptions
                .Where(x => x.BusinessCode == filter.BusinessCode)
                //.Include(x => x.InputPackage)
                //.ThenInclude(x => x.CommitmentPlan)
                //.Include(x => x.InputPackage)
                //.ThenInclude(x => x.RepaymentPlan)
                //.Include(x => x.SubscriberUser)
                //.Include(x => x.AddedByUser)
                .AsNoTracking()
                .AsQueryable()
                ;

            query = ApplyFilterIfPresent(filter, query);


            // Agent scope filtering
            query = await ApplyFilterForAgentRole(filter, query);


            // Membership scope filtering
            query = await ApplyFilterForMemberShipRole(filter, query);

            // Farmer scope filtering
            query = await ApplyFilterForFarmerRole(filter, query);

            if (filter.ViewType.HasValue)
            {
                switch (filter.ViewType.Value)
                {
                    case SubscriptionView.PendingApproval:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingApproval);
                        break;


                    case SubscriptionView.PendingCommitment:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingCommitment );
                        break;

                    case SubscriptionView.Approved:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.Approved);
                        break;

                    case SubscriptionView.CommitmentPaid:

                        query = query.Where(x =>
                           x.Status == SubscriptionStatus.CommitmentPaid);
                        break;

                    case SubscriptionView.InputReleaseRequest:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingReleaseApproval);
                        break;

                    case SubscriptionView.PendingDisbursement:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingDisbursement);
                        break;

                    case SubscriptionView.Active:
                    case SubscriptionView.InputReleased:
                        query = query.Where(x =>
                           x.Status == SubscriptionStatus.Disbursed ||
                            x.Status == SubscriptionStatus.PartiallyDisbursed ||
                             x.Status == SubscriptionStatus.Active
                            );
                        break;

                    case SubscriptionView.Completed:

                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.Completed
                            || (!x.InputPackage.RepaymentRequired && x.Status == SubscriptionStatus.Disbursed)
                            || (!x.InputPackage.RepaymentRequired && x.Status == SubscriptionStatus.Active)

                            );
                        break;

                    case SubscriptionView.Defaulted:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.Defaulted);
                        break;

                    case SubscriptionView.Rejected:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.Rejected);
                        break;
                }
            }


            if (filter.HasPaidCommitment.HasValue)
            {
                if (filter.HasPaidCommitment.Value)
                {
                    var paidCommitmentIds = _context.InputPackageSubscriptionPayments
    .Where(p => p.PaymentType == PaymentType.Commitment && p.Status == TransactionStatus.Successful)
    .GroupBy(p => p.SubscriptionId)
    .Select(g => new { SubscriptionId = g.Key, Total = g.Sum(p => p.AmountPaid) });

                    query = query.Join(paidCommitmentIds, x => x.Id, p => p.SubscriptionId, (x, p) => new { x, p })
                        .Where(xp => xp.x.CommitmentValue > 0 && xp.p.Total >= xp.x.CommitmentValue)
                        .Select(xp => xp.x);
                }
                else
                {
                    query = query.Where(x =>
                    x.CommitmentValue > 0 &&
                        _context.InputPackageSubscriptionPayments
                        .Where(p =>
                            p.SubscriptionId == x.Id &&
                            p.PaymentType == PaymentType.Commitment &&
                            p.Status == TransactionStatus.Successful)
                        .Sum(p => (decimal?)p.AmountPaid) < x.CommitmentValue);
                }
            }


            return query;
        }

        private async Task<IQueryable<InputPackageSubscription>> ApplyFilterForFarmerRole(SubscriptionQueryFilter filter, IQueryable<InputPackageSubscription> query)
        {
            if (filter.CurrentLoggedInUserRole == UserType.Farmer.ToString())
            {
                var farmerId = await _context.Farmers
                    .Where(x => x.UserId == filter.UserId)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (filter.OrganizationId.HasValue)
                {
                    var organizationFarmer = await _context.FarmerOrganizations
                        .Where(x => x.OrganizationCode == filter.OrganizationCode && x.FarmerId == farmerId)
                        .Select(x => x.FarmerId)
                        .FirstOrDefaultAsync();

                    if (organizationFarmer == null)
                    {
                        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Farmer not found in Organization");
                    }

                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerOrganizations.Any(fa =>
                             fa.FarmerId == organizationFarmer &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))

                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerOrganizations.Any(fa =>
                             fa.FarmerId == farmerId &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))
                    );
                }
            }

            return query;
        }
        private async Task<IQueryable<InputPackageSubscription>> ApplyFilterForMemberShipRole(SubscriptionQueryFilter filter, IQueryable<InputPackageSubscription> query)
        {
            if (filter.CurrentLoggedInUserRole == UserType.MemberShip.ToString())
            {
                var memberShipId = await _context.MemberShips
                    .Where(x => x.UserId == filter.UserId)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (filter.OrganizationId.HasValue)
                {
                    var organizationMembership = await _context.OrganizationMemberships
                        .Where(x => x.OrganizationId == filter.OrganizationId && x.MembershipId == memberShipId)
                        .Select(x => x.MembershipId)
                        .FirstOrDefaultAsync();

                    if (organizationMembership == null)
                    {
                        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Membership not found in Organization");
                    }

                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerCooperatives.Any(fa =>
                             fa.CooperativeId == organizationMembership &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))

                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerCooperatives.Any(fa =>
                             fa.CooperativeId == memberShipId &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))
                    );
                }
            }

            return query;
        }

        private async Task<IQueryable<InputPackageSubscription>> ApplyFilterForAgentRole(SubscriptionQueryFilter filter, IQueryable<InputPackageSubscription> query)
        {
            if (filter.CurrentLoggedInUserRole == UserType.Agent.ToString())
            {
                var agentId = await _context.Agents
                    .Where(x => x.UserId == filter.UserId)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (filter.OrganizationId.HasValue)
                {
                    var organizationAgent = await _context.OrganizationAgents
                        .Where(x => x.OrganizationId == filter.OrganizationId && x.AgentId == agentId)
                        .Select(x => x.AgentId)
                        .FirstOrDefaultAsync();

                    if (organizationAgent == null)
                    {
                        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Agent not found in Organization");
                    }

                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerAgents.Any(fa =>
                             fa.AgentId == organizationAgent &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))
                        ||
                        (x.SubscriberType == InputSubScriptionType.MemberShip &&
                         _context.MemberShipAgents.Any(ma =>
                             ma.AgentId == organizationAgent &&
                             _context.MemberShips.Any(m =>
                                 m.Id == ma.MemberShipId &&
                                 m.UserId == x.SubscriberUserId)))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.SubscriberType == InputSubScriptionType.Individual &&
                         _context.FarmerAgents.Any(fa =>
                             fa.AgentId == agentId &&
                             _context.Farmers.Any(f =>
                                 f.Id == fa.FarmerId &&
                                 f.UserId == x.SubscriberUserId)))
                        ||
                        (x.SubscriberType == InputSubScriptionType.MemberShip &&
                         _context.MemberShipAgents.Any(ma =>
                             ma.AgentId == agentId &&
                             _context.MemberShips.Any(m =>
                                 m.Id == ma.MemberShipId &&
                                 m.UserId == x.SubscriberUserId)))
                    );
                }
            }

            return query;
        }

        private static IQueryable<InputPackageSubscription> ApplyFilterIfPresent(SubscriptionQueryFilter filter, IQueryable<InputPackageSubscription> query)
        {
            if (!string.IsNullOrEmpty(filter.SubscriberUserId))
                query = query.Where(x => x.SubscriberUserId == filter.SubscriberUserId);


            if (filter.PackageId.HasValue)
                query = query.Where(x => x.InputPackageId == filter.PackageId);


            if (filter.OrganizationId.HasValue)
                query = query.Where(x => x.OrganizationId == filter.OrganizationId);


            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status);


            if (filter.IsActiveOnly == true)
                query = query.Where(x => x.IsActive);
            return query;
        }

        private async Task<IQueryable<InputPackageSubscription>> ApplyAgentScope(
        IQueryable<InputPackageSubscription> query,
        string userId, long? organizationId)
        {
            var agentId = await _context.Agents
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (organizationId.HasValue)
            {
                var organizationAgent = await _context.OrganizationAgents
                    .Where(x => x.OrganizationId == organizationId.Value && x.AgentId == agentId)
                    .Select(x => x.AgentId)
                    .FirstOrDefaultAsync();

                if (organizationAgent == 0)
                {
                    throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Agent not found in Organization");
                }

                return query.Where(x =>
                    (x.SubscriberType == InputSubScriptionType.Individual &&
                     _context.FarmerAgents.Any(fa =>
                         fa.AgentId == organizationAgent &&
                         _context.Farmers.Any(f =>
                             f.Id == fa.FarmerId &&
                             f.UserId == x.SubscriberUserId)))
                    ||
                    (x.SubscriberType == InputSubScriptionType.MemberShip &&
                     _context.MemberShipAgents.Any(ma =>
                         ma.AgentId == organizationAgent &&
                         _context.MemberShips.Any(m =>
                             m.Id == ma.MemberShipId &&
                             m.UserId == x.SubscriberUserId)))
                );
            }

            return query.Where(x =>
                (x.SubscriberType == InputSubScriptionType.Individual &&
                 _context.FarmerAgents.Any(fa =>
                     fa.AgentId == agentId &&
                     _context.Farmers.Any(f =>
                         f.Id == fa.FarmerId &&
                         f.UserId == x.SubscriberUserId)))
                ||
                (x.SubscriberType == InputSubScriptionType.MemberShip &&
                 _context.MemberShipAgents.Any(ma =>
                     ma.AgentId == agentId &&
                     _context.MemberShips.Any(m =>
                         m.Id == ma.MemberShipId &&
                         m.UserId == x.SubscriberUserId)))
            );
        }
        private async Task<IQueryable<InputPackageSubscription>> ApplyMembershipScope(
        IQueryable<InputPackageSubscription> query,
        string userId, long? organizationId)
        {
            var membershipId = await _context.MemberShips
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (organizationId.HasValue)
            {
                var organizationMembership = await _context.OrganizationMemberships
                    .Where(x => x.OrganizationId == organizationId.Value && x.MembershipId == membershipId)
                    .Select(x => x.MembershipId)
                    .FirstOrDefaultAsync();

                if (organizationMembership == 0)
                {
                    throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Membership not found in Organization");
                }

                return query.Where(x =>
                    (x.SubscriberType == InputSubScriptionType.Individual &&
                     _context.FarmerCooperatives.Any(fa =>
                         fa.CooperativeId == organizationMembership &&
                         _context.Farmers.Any(f =>
                             f.Id == fa.FarmerId &&
                             f.UserId == x.SubscriberUserId)))
                    
                );
            }

            return query.Where(x =>
                (x.SubscriberType == InputSubScriptionType.Individual &&
                 _context.FarmerCooperatives.Any(fa =>
                     fa.CooperativeId == membershipId &&
                     _context.Farmers.Any(f =>
                         f.Id == fa.FarmerId &&
                         f.UserId == x.SubscriberUserId)))
          
            );
        }
     
        public async Task<FarmerSubscriptionOverviewDto> GetAgentSubscriptionOverview(
        SubscriptionQueryFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.UserId))
            {
                throw new ArgumentException("UserId is required to resolve subscription overview.", nameof(filter));
            }

            var baseQuery = _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                
                .Where(x => x.BusinessCode == filter.BusinessCode)
                .AsQueryable();

            if (filter.CurrentLoggedInUserRole == UserType.Farmer.ToString())
            {
                return await BuildFarmerOverview(baseQuery, filter);
            }

            if (filter.CurrentLoggedInUserRole == UserType.Agent.ToString())
            {
                baseQuery = await ApplyAgentScope(baseQuery, filter.UserId!, filter.OrganizationId);
            }
            else if (filter.CurrentLoggedInUserRole == UserType.MemberShip.ToString())
            {
                baseQuery = await ApplyMembershipScope(baseQuery, filter.UserId!, filter.OrganizationId);
            }

            var statusCounts = await baseQuery
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            long CountFor(SubscriptionStatus status) =>
                statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

            return new FarmerSubscriptionOverviewDto
            {
                TotalSubscriptions = statusCounts.Sum(s => s.Count),
                PendingApproval = CountFor(SubscriptionStatus.PendingApproval),
                PendingCommitment = CountFor(SubscriptionStatus.PendingCommitment)
                                     + CountFor(SubscriptionStatus.Approved),
                InputsReleased = CountFor(SubscriptionStatus.Disbursed),
                Active = CountFor(SubscriptionStatus.Active)
            };
        }
        private async Task<FarmerSubscriptionOverviewDto> BuildFarmerOverview(
    IQueryable<InputPackageSubscription> baseQuery,
    SubscriptionQueryFilter filter)
        {
            var farmerQuery = baseQuery
                .Where(x => x.SubscriberUserId == filter.UserId);

            var repayableStatuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Disbursed };

            var activePackageCount = await farmerQuery
                .CountAsync(x => x.Status == SubscriptionStatus.Active);

            var repayableQuery = farmerQuery.Where(x => repayableStatuses.Contains(x.Status));

            var payment = await repayableQuery
       .Select(x => new
       {
           x.Id,
           x.InputPackage.RepaymentRequired,
           x.TotalFinancingValue,
           TotalRepaid = _context.InputPackageSubscriptionPayments
               .Where(p =>
                   p.SubscriptionId == x.Id &&
                   p.Status == TransactionStatus.Successful &&
                   p.PaymentType == PaymentType.Repayment)
               .Sum(p => (decimal?)p.AmountPaid) ?? 0m
       })
       .ToListAsync();

            var totalRepaid = payment.Sum(x => x.TotalRepaid);

            var outstanding = payment
                .Where(x => x.RepaymentRequired)
                .Sum(x => x.TotalFinancingValue - x.TotalRepaid);
            var repayableSubscriptionIds = payment.Select(x => x.Id).ToList();

            var nextDueDate = await _context.RepaymentSchedules
                .Where(x => repayableSubscriptionIds.Contains(x.InputPackageSubscriptionId) && !x.IsPaid)
                .OrderBy(x => x.DueDate)
                .Select(x => (DateTimeOffset?)x.DueDate)
                .FirstOrDefaultAsync();
            return new FarmerSubscriptionOverviewDto
            {
                ActivePackages = activePackageCount,
                TotalOutstanding = outstanding,
                NextDueDate = nextDueDate.HasValue
                    ? nextDueDate.Value.ToString("dd-MMM-yyyy") // or whatever format you use elsewhere
                    : "NA",
                TotalRepaid = totalRepaid
            };
        }

        public async Task<PagedResult<SubscriptionDetailDto>> GetSubscriptionsAsync(
    SubscriptionQueryFilter filter,
    int pageNumber,
    int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
           pageSize = 10;

            var query = await BuildSubscriptionQuery(filter);
            var count = await query.CountAsync();

            if (count == 0)
            {
                return new PagedResult<SubscriptionDetailDto>
                {
                    Data = [],
                    Count = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var pagedIds = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.Id, x.CommitmentRequired, x.CommitmentValue })
                .ToListAsync();

            var subscriptionIds = pagedIds.Select(x => x.Id).ToList();

            var commitmentTotals = await GetPaymentTotalsAsync(subscriptionIds, PaymentType.Commitment);
            var repaymentTotals = await GetPaymentTotalsAsync(subscriptionIds, PaymentType.Repayment);
            var disbursedDates = await GetDisbursedDatesAsync(subscriptionIds);
            var nextDueDates = await GetNextDueDatesAsync(subscriptionIds);

            var rawItems = await _context.InputPackageSubscriptions
                .Where(x => subscriptionIds.Contains(x.Id))
                .AsNoTracking()
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.CommitmentPlan)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.RepaymentPlan)
                .Include(x => x.SubscriberUser)
                .Include(x => x.AddedByUser)
                .Include(x => x.Organization)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.InputPackageId,
                    x.SubscriberUserId,
                    x.SubscriberType,
                    x.CommitmentRequired,
                    x.CommitmentValue,
                    x.TotalFinancingValue,
                    x.SubscribedOn,
                    x.Status,
                    SubscriberName = x.SubscriberUser == null
                        ? x.SubscriberUserId
                        : !string.IsNullOrWhiteSpace(x.SubscriberUser.Name)
                            ? x.SubscriberUser.Name
                            : !string.IsNullOrWhiteSpace(x.SubscriberUser.PhoneNumber)
                                ? x.SubscriberUser.PhoneNumber
                                : x.SubscriberUser.Email,
                    CreatedBy = x.AddedByUser == null
                        ? x.AddedByUserId
                        : !string.IsNullOrWhiteSpace(x.AddedByUser.Name)
                            ? x.AddedByUser.Name
                            : !string.IsNullOrWhiteSpace(x.AddedByUser.PhoneNumber)
                                ? x.AddedByUser.PhoneNumber
                                : x.AddedByUser.Email,
                    Location = x.Organization != null ? x.Organization.CompanyName : null,
                    PackageName = x.InputPackage.Name,
                    x.RejectedOn,
                    RepaymentMethod = x.InputPackage.RepaymentPlan != null
                        ? EnumExtensions.GetDisplayName(x.InputPackage.RepaymentPlan.RepaymentMode)
                        : null,
                    x.RiskLevel,
                    TotalRepaymentValue = x.InputPackage.RepaymentPlan != null
                        ? x.TotalFinancingValue
                        : 0m,
                    RepaymentRequired = x.InputPackage.RepaymentRequired,
                    CommitmentPaymentDeadline = x.InputPackage.CommitmentPlan != null
                        ? x.InputPackage.CommitmentPlan.CommitmentPaymentEndDate
                        : (DateTimeOffset?)null,
                    RepaymentDuration = x.InputPackage.RepaymentPlan != null
                        ? GetRepaymentPeriod(
                            x.InputPackage.RepaymentPlan.RepaymentFrequency,
                            x.InputPackage.RepaymentPlan.RepaymentDuration)
                        : null,
                    x.ApprovedOn,
                    Season = x.InputPackage.InputSeason
                })
                .ToListAsync();

            //TODO: improvement on query when viewtype is for release request, the request date from input disbursement table

            var items = rawItems
                .Select(x => MapToDto(x, commitmentTotals, repaymentTotals, disbursedDates, nextDueDates))
                .ToList();

            return new PagedResult<SubscriptionDetailDto>
            {
                Data = items,
                Count = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

       

        private async Task<Dictionary<long, decimal>> GetPaymentTotalsAsync(
            List<long> subscriptionIds,
            PaymentType paymentType)
        {
            return await _context.InputPackageSubscriptionPayments
                .AsNoTracking()
                .Where(p =>
                    subscriptionIds.Contains(p.SubscriptionId) &&
                    p.PaymentType == paymentType &&
                    p.Status == TransactionStatus.Successful)
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    Total = g.Sum(p => (decimal?)p.AmountPaid) ?? 0m
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.Total);
        }

        private async Task<Dictionary<long, DateTimeOffset?>> GetDisbursedDatesAsync(List<long> subscriptionIds)
        {
            return await _context.InputSubscriptionDisbursements
                .AsNoTracking()
                .Where(p => subscriptionIds.Contains(p.SubscriptionId) &&
                            p.Status == DisbursementStatus.Disbursed)
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    DisbursedOn = (DateTimeOffset?)g.Max(x => x.DisbursedOn)
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.DisbursedOn);
        }

        private async Task<Dictionary<long, DateTimeOffset?>> GetNextDueDatesAsync(List<long> subscriptionIds)
        {
            var today = DateTimeOffset.UtcNow;

            return await _context.RepaymentSchedules
                .AsNoTracking()
                .Where(x => subscriptionIds.Contains(x.InputPackageSubscriptionId) &&
                            !x.IsPaid &&
                            x.DueDate >= today)
                .GroupBy(x => x.InputPackageSubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    NextDueDate = (DateTimeOffset?)g.Min(x => x.DueDate)
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.NextDueDate);
        }

        private static string GetCommitmentStatus(bool commitmentRequired, decimal commitmentValue, decimal commitmentPaid)
        {
            if (!commitmentRequired || commitmentValue <= 0)
                return "Not Required";

            return commitmentPaid >= commitmentValue ? "Paid" : "Pending";
        }

        private static decimal GetCommitmentBalance(bool commitmentRequired, decimal commitmentValue, decimal commitmentPaid)
        {
            if (!commitmentRequired || commitmentValue <= 0)
                return 0m;

            return Math.Max(commitmentValue - commitmentPaid, 0m);
        }

        private static decimal GetRoi(bool repaymentRequired, decimal totalRepaymentValue, decimal totalFinancingValue)
        {
            if (!repaymentRequired || totalRepaymentValue <= 0)
                return 0m;

            return Math.Max(totalRepaymentValue - totalFinancingValue, 0m);
        }

        private static SubscriptionDetailDto MapToDto(
            dynamic x,
            Dictionary<long, decimal> commitmentTotals,
            Dictionary<long, decimal> repaymentTotals,
            Dictionary<long, DateTimeOffset?> disbursedDates,
            Dictionary<long, DateTimeOffset?> nextDueDates)
        {
            long id = x.Id;
            bool commitmentRequired = x.CommitmentRequired;
            decimal commitmentValue = x.CommitmentValue;
            decimal totalRepaymentValue = x.TotalRepaymentValue;
            decimal totalFinancingValue = x.TotalFinancingValue;
            bool repaymentRequired = x.RepaymentRequired;

            var commitmentPaid = commitmentTotals.GetValueOrDefault(id, 0m);
            var repaymentPaid = repaymentTotals.GetValueOrDefault(id, 0m);
            var repaymentBalance = totalRepaymentValue <= 0
                ? 0m
                : Math.Max(totalRepaymentValue - repaymentPaid, 0m);

            var nextDueDate = repaymentRequired
        ? nextDueDates.GetValueOrDefault(id)
        : null;

            return new SubscriptionDetailDto
            {
                PackageId = x.InputPackageId,
                SubscriptionId = id,
                SubscriberUserId = x.SubscriberUserId,
                SubscriberName = x.SubscriberName,
                CreatedBy = x.CreatedBy,
                SubscriptionType = x.SubscriberType,
                Location = x.Location,
                PackageName = x.PackageName,
                CommitmentValue = commitmentValue,
                TotalFinancingValue = totalFinancingValue,
                SubscribedOn = x.SubscribedOn,
                SubscriptionStatus = x.Status,
                SubscriptionStatusName = EnumExtensions.GetDisplayName(x.Status),
                CommitmentStatus = GetCommitmentStatus(commitmentRequired, commitmentValue, commitmentPaid),
                RejectedOn = x.RejectedOn,
                SubscriptionTypeName = EnumExtensions.GetDisplayName(x.SubscriberType),
                CommitmentPaid = commitmentPaid,
                CommitmentBalance = GetCommitmentBalance(commitmentRequired, commitmentValue, commitmentPaid),
                RiskLevel = EnumExtensions.GetDisplayName(x.RiskLevel),
                RepaymentBalance = repaymentBalance,
                RepaymentMethod = x.RepaymentMethod,
                ROI = GetRoi(repaymentRequired, totalRepaymentValue, totalFinancingValue),
                RepaymentDuration = x.RepaymentDuration,
                ApprovedOn = x.ApprovedOn,
                CommitmentPaymentDeadline = x.CommitmentPaymentDeadline,
                Season = x.Season,
                DisbursedOn = disbursedDates.GetValueOrDefault(id),
                NextDueDate = nextDueDates.GetValueOrDefault(id)
            };
        }

        public async Task<PerUserTypeFinancingOverviewCountDto> GetFinancingOverViewPerSubscriptionType(
       SubscriptionQueryFilter filter)
        {
            var totalFinancedValue = 0m;
            var totalSubscription = 0;
            var subscriptionsQuery = _context.InputPackageSubscriptions
            
                .Where(x => x.BusinessCode == filter.BusinessCode);

            if (filter.OrganizationId.HasValue)
            {
                subscriptionsQuery = subscriptionsQuery
                    .Where(x => x.OrganizationId == filter.OrganizationId.Value);
            }
            if(filter.SubscriptionType.HasValue)
            {
                subscriptionsQuery = subscriptionsQuery
                    .Where(x => x.SubscriberType == filter.SubscriptionType.Value);
                if(filter.SubscriptionType.Value == InputSubScriptionType.Individual)
                {
                    totalFinancedValue = await subscriptionsQuery
                        .Where(x=>x.SubscriberType == InputSubScriptionType.Individual)
                        .SumAsync(x => (decimal?)x.TotalFinancingValue) ?? 0m;

                    totalSubscription = await subscriptionsQuery
                        .Where(x => x.SubscriberType == InputSubScriptionType.Individual)
                        .CountAsync();
                }
                else if (filter.SubscriptionType.Value == InputSubScriptionType.MemberShip)
                {
                    totalFinancedValue = await subscriptionsQuery
                        .Where(x => x.SubscriberType == InputSubScriptionType.MemberShip)
                        .SumAsync(x => (decimal?)x.TotalFinancingValue) ?? 0m;
                    totalSubscription = await subscriptionsQuery
                        .Where(x => x.SubscriberType == InputSubScriptionType.MemberShip)
                        .CountAsync();
                }
            }
            else
            {
                totalFinancedValue = await subscriptionsQuery
                      .Where(x => x.SubscriberType == InputSubScriptionType.Individual)
                        .SumAsync(x => (decimal?)x.TotalFinancingValue) ?? 0m;
                totalSubscription = await subscriptionsQuery
                          .CountAsync();

            }


            return new PerUserTypeFinancingOverviewCountDto
            {
                TotalFinancedValue = totalFinancedValue,
                TotalFinance= totalSubscription
            };
        }


        public async Task<InputFinancingDashboardDto> GetInputFinancingOverviewAsync(
        SubscriptionQueryFilter filter)
        {
            var subscriptionsQuery = _context.InputPackageSubscriptions
                .Where(x => x.BusinessCode == filter.BusinessCode);

            if (filter.OrganizationId.HasValue)
            {
                subscriptionsQuery = subscriptionsQuery
                    .Where(x => x.OrganizationId == filter.OrganizationId.Value);
            }

            var subscriptionIds = await subscriptionsQuery
                .Select(x => x.Id)
                .ToListAsync();

            var totalFinancedValue = await subscriptionsQuery
                .SumAsync(x => (decimal?)x.TotalFinancingValue) ?? 0m;



            var totalActivePackages = await _context.InputPackages
      .Where(p =>
          p.BusinessCode == filter.BusinessCode &&
          p.IsActive)
      .Where(p =>
          !filter.OrganizationId.HasValue ||
          p.OrganizationId == filter.OrganizationId.Value)
      .CountAsync();

            var totalRepaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    subscriptionIds.Contains(x.SubscriptionId) &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var totalCommitmentPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    subscriptionIds.Contains(x.SubscriptionId) &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Commitment)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var totalOutstanding = totalFinancedValue - totalRepaid;

            var recoveryRate = totalFinancedValue == 0
                ? 0
                : (totalRepaid / totalFinancedValue) * 100m;

            return new InputFinancingDashboardDto
            {
                TotalFinancedValue = totalFinancedValue,
                TotalAmountRepaid = totalRepaid,
                TotalCommitted = totalCommitmentPaid,
                TotalOutstandingBalance = totalOutstanding,
                RecoveryRate = Math.Round(recoveryRate, 2),
                TotalActivePackages = totalActivePackages
            };
        }


        
        public async Task<SubscriptionFullDetailDto> FetchSubscriptionForReview(SubscriptionQueryFilter filter)
        {
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                    .ThenInclude(p => p.CommitmentPlan)
                .Include(x => x.InputPackage)
                    .ThenInclude(p => p.RepaymentPlan)
                .Include(x => x.SubscriberUser)
                .Include(x => x.AddedByUser)
                .FirstOrDefaultAsync(x => x.Id == filter.SubscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var package = subscription.InputPackage;
            var orgId = subscription.OrganizationId;

            string? location = null;
            string? verificationStatus = null;


            if (subscription.SubscriberType == InputSubScriptionType.Individual)
            {
                var farmer = await _context.Farmers
                    .FirstOrDefaultAsync(x => x.UserId == subscription.SubscriberUserId);

                if (farmer != null)
                {
                    location = farmer.Location;

                    var farmerOrg = await _context.FarmerOrganizations
                        .FirstOrDefaultAsync(x =>
                            x.FarmerId == farmer.Id &&
                            x.OrganizationCode == filter.OrganizationCode);

                    verificationStatus = farmerOrg == null
                        ? "Not Enrolled"
                        : (farmerOrg.IsActive ? "Verified" : "Inactive");
                }
            }
            else if (subscription.SubscriberType == InputSubScriptionType.MemberShip)
            {
                var member = await _context.MemberShips
                    .FirstOrDefaultAsync(x => x.UserId == subscription.SubscriberUserId);

                if (member != null)
                {
                    location = member.Location;

                    var memberOrg = await _context.OrganizationMemberships
                        .FirstOrDefaultAsync(x =>
                            x.MembershipId == member.Id &&
                            x.OrganizationId == orgId);

                    verificationStatus = memberOrg == null
                        ? "Not Enrolled"
                        : (memberOrg.IsActive ? "Verified" : "Inactive");
                }
            }


            var commitmentPlan = package?.CommitmentPlan;
            var repaymentPlan = package?.RepaymentPlan;
            var RepaymentPeriod = repaymentPlan != null
                       ? GetRepaymentPeriod(repaymentPlan.RepaymentFrequency, repaymentPlan.RepaymentDuration)
                       : null;

            return new SubscriptionFullDetailDto
            {
                Subscriber = new SubscriberInfoDto
                {
                    Name = subscription.SubscriberUser?.Name
                           ?? subscription.SubscriberUser?.Email
                           ?? subscription.SubscriberUserId,

                    Type = EnumExtensions.GetDisplayName(subscription.SubscriberType),

                    Agent = subscription.AddedByUser?.Name
                            ?? subscription.AddedByUserId,

                    Location = location,

                    VerificationStatus = verificationStatus,
                    EnrollmentDate = subscription.SubscribedOn,
                },

                Package = new PackageInfoDto
                {
                    PackageName = package?.Name,

                    TotalValue = package?.TotalFinancingValue ?? 0,

                    CommitmentRequired = package?.CommitmentRequired ?? false,

                    CommitmentValue = commitmentPlan?.CommitmentValue ?? 0,
                    RepaymentValue = repaymentPlan.RepaymentValue,

                    RepaymentDuration = RepaymentPeriod,//repaymentPlan?.RepaymentDuration ?? 0,

                    RepaymentModel = EnumExtensions.GetDisplayName(repaymentPlan?.Frequency),

                    RiskCategory = EnumExtensions.GetDisplayName(repaymentPlan?.RiskCategory)
                }
            };
        }

        public async Task<SubscriptionReviewResponseDto> GetSubscriptionDetail(SubscriptionQueryFilter filter)
        {
            //viewtype 5, 10, 6

            var deliveryInfo = new DeliveryInformation();
            if (filter.SubscriptionId <= 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid SubscriptionId provided");

            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.SubscriberUser)
                .Include(x => x.AddedByUser)
                .Include(x => x.InputPackage)
                    .ThenInclude(p => p.CommitmentPlan)
                .Include(x => x.InputPackage)
                    .ThenInclude(p => p.RepaymentPlan)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.Organization)
                .FirstOrDefaultAsync(x => x.Id == filter.SubscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            if (filter.ViewType.HasValue &&
        !SubscriptionViewValidator.Matches(filter.ViewType.Value, subscription))
            {
                throw new BaseException (ExceptionType.BAD_REQUEST,
                    $"The supplied filter '{filter.ViewType.Value}' does not match this subscription's current status '{subscription.Status}'."
                    );
            }
            var package = subscription.InputPackage
                ?? throw new Exception("Package not found for subscription");

            var org = package.Organization;

            var packageItems = await _context.InputPackageItems
                .Where(x => x.InputPackageId == package.Id)
                .Include(x => x.OrganizationProduct)
                .ToListAsync();

            var additionalCosts = await _context.AdditionalInputPackageCosts
                .Where(x => x.InputPackageId == package.Id)
                .ToListAsync();

            string? location = null;
            string? lga = null;
            string? state = null;
            string? verificationStatus = null;

            if (subscription.SubscriberType == InputSubScriptionType.Individual)
            {
                var farmer = await _context.Farmers
                    .FirstOrDefaultAsync(x => x.UserId == subscription.SubscriberUserId);

                if (farmer != null)
                {
                    location = farmer.Location;
                    state = farmer.State;
                    lga = farmer.Lga;
                    var farmerOrg = await _context.FarmerOrganizations
                        .FirstOrDefaultAsync(x =>
                            x.FarmerId == farmer.Id &&
                            x.OrganizationCode == org.OrganizationCode);

                    verificationStatus = farmerOrg == null
                        ? "Not Enrolled"
                        : farmerOrg.IsActive ? "Verified" : "Inactive";
                }
            }
            else if (subscription.SubscriberType == InputSubScriptionType.MemberShip)
            {
                var member = await _context.MemberShips
                    .FirstOrDefaultAsync(x => x.UserId == subscription.SubscriberUserId);

                if (member != null)
                {
                    location = member.Location;
                    state = member.State;
                    lga = member.Lga;
                    var memberOrg = await _context.OrganizationMemberships
                        .FirstOrDefaultAsync(x =>
                            x.MembershipId == member.Id &&
                            x.OrganizationId == org.OrganizationId);

                    verificationStatus = memberOrg == null
                        ? "Not Enrolled"
                        : memberOrg.IsActive ? "Verified" : "Inactive";
                }
            }
            var allowedStatuses = new[] { SubscriptionView.Active, SubscriptionView.InputReleased , 
                SubscriptionView.Completed, SubscriptionView.Defaulted };
            if (allowedStatuses.Contains(filter.ViewType.Value))
            {
                var disbursement = await _context.InputSubscriptionDisbursements
                    .Where(x => x.SubscriptionId == subscription.Id)
                    .OrderByDescending(x => x.DisbursedOn)
                    .FirstOrDefaultAsync();
                if (disbursement != null)
                {
                    deliveryInfo.AmountDisbursed = disbursement.ApprovedAmount;
                }
            }
            string? agentName = null;
            string? warehouseAssigned = null;
            if (subscription.SubscriberType == InputSubScriptionType.Individual)
            {
                var agentInfo = await (
                    from fa in _context.FarmerAgents
                    join f in _context.Farmers on fa.FarmerId equals f.Id
                    join a in _context.Agents on fa.AgentId equals a.Id
                    join u in _context.Users on a.UserId equals u.Id
                    join oa in _context.OrganizationAgents
                        on new { AgentId = a.Id, OrganizationId = org.OrganizationId }
                        equals new { AgentId = oa.AgentId, OrganizationId = oa.OrganizationId }
                        into oaJoin
                    from oa in oaJoin.DefaultIfEmpty()
                    where f.UserId == subscription.SubscriberUserId
                    select new
                    {
                        AgentName = u.Name ?? u.PhoneNumber ?? u.Email,
                        WarehouseAssigned = oa != null ? oa.Location != null ? oa.Location.LocationName: null : null,
                        warehouseId = oa != null ? oa.LocationId : null,
                        warehouseState = oa != null ? oa.Location != null ? oa.Location.State : null : null,
                        warehouseLga = oa != null ? oa.Location != null ? oa.Location.LGA : null : null,
                        warehouseWard = oa != null ? oa.Location != null ? oa.Location.Ward : null : null,
                        warehouseLocation = oa != null ? oa.Location != null ? oa.Location.Address : null : null

                    }
                ).FirstOrDefaultAsync();

                agentName = agentInfo?.AgentName;
                warehouseAssigned = agentInfo?.WarehouseAssigned;
                deliveryInfo.Warehouse = agentInfo?.WarehouseAssigned;
                deliveryInfo.Location = agentInfo?.warehouseLocation;
                deliveryInfo.AgentName= agentInfo?.AgentName;

            }
            else if (subscription.SubscriberType == InputSubScriptionType.MemberShip)
            {
                var agentInfo = await (
                    from ma in _context.MemberShipAgents
                    join m in _context.MemberShips on ma.MemberShipId equals m.Id
                    join a in _context.Agents on ma.AgentId equals a.Id
                    join u in _context.Users on a.UserId equals u.Id
                    join oa in _context.OrganizationAgents
                        on new { AgentId = a.Id, OrganizationId = org.OrganizationId }
                        equals new { AgentId = oa.AgentId, OrganizationId = oa.OrganizationId }
                        into oaJoin
                    from oa in oaJoin.DefaultIfEmpty()
                    where m.UserId == subscription.SubscriberUserId
                    select new
                    {
                        AgentName = u.Name ?? u.PhoneNumber ?? u.Email,
                        WarehouseAssigned = oa != null
                            ? oa.Location != null
                                ? oa.Location.LocationName
                                : null
                            : null,
                        warehouseId = oa != null ? oa.LocationId : null,
                        warehouseState = oa != null ? oa.Location != null ? oa.Location.State : null : null,
                        warehouseLga = oa != null ? oa.Location != null ? oa.Location.LGA : null : null,
                        warehouseWard = oa != null ? oa.Location != null ? oa.Location.Ward : null : null,
                        warehouseLocation = oa != null ? oa.Location != null ? oa.Location.Address : null : null
                    }
                ).FirstOrDefaultAsync();

                agentName = agentInfo?.AgentName;
                warehouseAssigned = agentInfo?.WarehouseAssigned;
                deliveryInfo.Warehouse = agentInfo?.WarehouseAssigned;
                deliveryInfo.Location = agentInfo?.warehouseLocation;
                deliveryInfo.AgentName = agentInfo?.AgentName;
            }

            var paymentTotals = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.Status == TransactionStatus.Successful &&
                    (x.PaymentType == PaymentType.Commitment ||
                     x.PaymentType == PaymentType.Repayment))
                .GroupBy(x => x.PaymentType)
                .Select(g => new
                {
                    PaymentType = g.Key,
                    Total = g.Sum(x => (decimal?)x.AmountPaid) ?? 0m
                })
                .ToListAsync();

            var commitmentPaid = paymentTotals
                .FirstOrDefault(x => x.PaymentType == PaymentType.Commitment)?.Total ?? 0m;
            var repaymentPaid = paymentTotals
                .FirstOrDefault(x => x.PaymentType == PaymentType.Repayment)?.Total ?? 0m;

            var rawRepaymentHistory = await _context.InputPackageSubscriptionPayments
                .Include(x => x.RecordedByUser)
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .OrderByDescending(x => x.PaidOn)
                .Select(x => new
                {
                    x.PaidOn,
                    x.AmountPaid,
                    x.PaymentChannel,
                    RecordedBy = x.RecordedByUser != null
                        ? !string.IsNullOrWhiteSpace(x.RecordedByUser.Name)
                            ? x.RecordedByUser.Name
                            : x.RecordedByUser.Email
                        : null
                })
                .ToListAsync();

            var rawcommitmentPaymentHistory = await _context.InputPackageSubscriptionPayments
                .Include(x => x.RecordedByUser)
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .OrderByDescending(x => x.PaidOn)
                .Select(x => new
                {
                    x.PaidOn,
                    x.AmountPaid,
                    x.PaymentChannel,
                    RecordedBy = x.RecordedByUser != null
                        ? !string.IsNullOrWhiteSpace(x.RecordedByUser.Name)
                            ? x.RecordedByUser.Name
                            : x.RecordedByUser.Email
                        : null
                })
                .ToListAsync();

            var repaymentPeriod = package.RepaymentPlan != null
                ? GetRepaymentPeriod(
                    package.RepaymentPlan.RepaymentFrequency,
                    package.RepaymentPlan.RepaymentDuration)
                : null;

            var commitmentRequired = package.CommitmentPlan?.CommitmentValue ?? 0m;
            var commitmentOutstanding = commitmentRequired - commitmentPaid;

            var repaymentValue = package.RepaymentRequired && package.RepaymentPlan != null
                ? package.RepaymentPlan.RepaymentValue
                : 0m;

            var repaymentOutstanding = Math.Max(repaymentValue - repaymentPaid, 0m);

            var commitmentDueDate = package.CommitmentPlan?.CommitmentPaymentEndDate;
            string? commitmentDaysLeft = null;

            if (commitmentDueDate.HasValue)
            {
                var isCommitmentPaid = commitmentPaid >= commitmentRequired && commitmentRequired > 0;
                if (isCommitmentPaid)
                {
                    commitmentDaysLeft = "Paid";
                }
                else
                {
                    var days = (commitmentDueDate.Value.Date - DateTimeOffset.UtcNow.Date).Days;
                    commitmentDaysLeft = days <= 0 ? "Overdue" : $"{days} day(s) left";
                }
            }

            var repaymentEndDate = package.RepaymentPlan?.RepaymentEndDate;
            string? repaymentDaysLeft = null;

            if (repaymentEndDate.HasValue)
            {
                var isRepaymentPaid = repaymentPaid >= repaymentValue && repaymentValue > 0;
                if (isRepaymentPaid)
                {
                    repaymentDaysLeft = "Paid";
                }
                else
                {
                    var days = (repaymentEndDate.Value.Date - DateTimeOffset.UtcNow.Date).Days;
                    repaymentDaysLeft = days <= 0 ? "Overdue" : $"{days} day(s) left";
                }
            }

            var commitmentPlanDisplay = package.CommitmentPlan != null
            ? EnumExtensions.GetDisplayName(package.CommitmentPlan.CommitmentType)
            : "NA";

            var repaymentModelDisplay = package.RepaymentPlan != null
                ? EnumExtensions.GetDisplayName(package.RepaymentPlan.RepaymentMode)
                : "NA";

            var riskCategoryDisplay = package.RepaymentPlan != null
                ? EnumExtensions.GetDisplayName(package.RepaymentPlan.RiskCategory)
                : "NA";

            return new SubscriptionReviewResponseDto
            {
                SubscriptionId = subscription.Id,
                Subscriber = new SubscriberInfoDto
                {
                    UserId = subscription.SubscriberUserId,
                    Name = subscription.SubscriberUser?.Name
                        ?? subscription.SubscriberUser?.PhoneNumber
                        ?? subscription.SubscriberUser?.Email
                        ?? subscription.SubscriberUserId,
                    PhoneNumber = subscription.SubscriberUser?.PhoneNumber,
                    Type = EnumExtensions.GetDisplayName(subscription.SubscriberType),
                    Agent = agentName,
                    Location = location,
                    VerificationStatus = verificationStatus,
                    EnrollmentDate = subscription.SubscribedOn,
                    Package = package.Name,
                    WarehouseAssigned = warehouseAssigned,
                    Lga = lga,
                    State = state
                },

                Package = new PackageInfoDto
                {
                    PackageTarget = package.InputTarget,
                    PackageName = package.Name,
                    TotalValue = package.TotalFinancingValue,
                    CommitmentRequired = package.CommitmentRequired,
                    CommitmentValue = package.CommitmentPlan?.CommitmentValue ?? 0m,
                    RepaymentValue = package.RepaymentPlan?.RepaymentValue ?? 0m,
                    RepaymentDuration = repaymentPeriod,
                    Season = package.InputSeason,
                    RepaymentStartDate = package.RepaymentPlan?.RepaymentStartDate,
                    RepaymentEndDate = package.RepaymentPlan?.RepaymentEndDate,
                    CommitmentPlan = commitmentPlanDisplay,
                    RepaymentModel = repaymentModelDisplay,
                    RiskCategory = riskCategoryDisplay
                },

                FinancingSnapshot = new FinancingSnapshotDto
                {
                    TotalFinancedValue = subscription.TotalFinancingValue,
                    CommitmentPaid = commitmentPaid,
                    CommitmentAmountRequired = commitmentRequired,
                    CommitmentOutstandingBalance = commitmentOutstanding,
                    CommitmentRequired = package.CommitmentRequired,
                    CommitmentDaysLeft = commitmentDaysLeft,
                    TotalRepaidAmount = repaymentPaid,
                    TotalRepaymentRequired = repaymentValue,
                    RepaymentRequired = package.RepaymentRequired,
                    TotalRepaymentBalance = repaymentOutstanding,
                    RepaymentDaysLeft = repaymentDaysLeft,
                    RepaymentDuration = repaymentPeriod,
                    RepaymentModel = repaymentModelDisplay,
                    RiskCategory = riskCategoryDisplay
                },

                DeliveryDetails= deliveryInfo,
                InputBreakdown = packageItems
                    .Select(x => new InputBreakdownDto
                    {
                        InputName = x.OrganizationProduct?.ProductName,
                        UnitPrice = x.UnitCost,
                        Quantity = x.Quantity,
                        TotalCost = x.UnitCost * x.Quantity
                    }).ToList(),

                AdditionalCost = additionalCosts
                    .Select(x => new AdditionalCostDto
                    {
                        Name = x.Name,
                        Amount = x.Amount
                    }).ToList(),

                RepaymentHistory = rawRepaymentHistory
                    .Select(x => new RepaymentHistoryDto
                    {
                        PaymentDate = x.PaidOn,
                        AmountPaid = x.AmountPaid,
                        PaymentMethod = x.PaymentChannel.ToString(),
                        RecordedBy = x.RecordedBy
                    }).ToList(),

                CommitmentPaymentHistory = rawcommitmentPaymentHistory
                    .Select(x => new CommitmentHistoryDto
                    {
                        PaymentDate = x.PaidOn,
                        AmountPaid = x.AmountPaid,
                        PaymentMethod = x.PaymentChannel.ToString(),
                        RecordedBy = x.RecordedBy
                    }).ToList()
            };

        }

        public async Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId)
        {
            var document = await _context.SubscriptionDocuments
                .FirstOrDefaultAsync(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.DocumentType == DocumentType.CommissionLetter);

            if (document == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Commission letter not found for this subscription");

            if (string.IsNullOrWhiteSpace(document.FilePath))
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid document file path");

            if (!File.Exists(document.FilePath))
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "File no longer exists on server storage");

            var fileBytes = await File.ReadAllBytesAsync(document.FilePath);

            var fileName = string.IsNullOrWhiteSpace(document.FileName)
                ? $"commission_letter_{subscriptionId}.pdf"
                : document.FileName;

            return (fileBytes, fileName);
        }
        public async Task<PagedResult<InputObligationDto>> GetSubscriptionObligationsAsync(
        SubscriptionQueryFilter filter,
        int pageNumber,
        int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

            var query = await BuildSubscriptionQuery(filter);

            var count = await query.CountAsync();

            if (count == 0)
            {
                return new PagedResult<InputObligationDto>
                {
                    Data = [],
                    Count = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var subscriptions = await query
                .Include(x => x.InputPackage)
                .Include(x => x.RepaymentSchedules)
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var subscriptionIds = subscriptions.Select(x => x.Id).ToList();

            var paymentsLookup = await _context.InputPackageSubscriptionPayments
                .Where(p =>
                    subscriptionIds.Contains(p.SubscriptionId) &&
                    p.Status == TransactionStatus.Successful &&
                    p.PaymentType == PaymentType.Repayment)
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    TotalPaid = g.Sum(x => x.AmountPaid)
                })
                .ToListAsync();

            var paymentMap = paymentsLookup
                .ToDictionary(x => x.SubscriptionId, x => x.TotalPaid);

            var items = subscriptions.Select(sub =>
        {
            var totalCost = sub.TotalFinancingValue;

            paymentMap.TryGetValue(sub.Id, out var amountPaid);

            var remainingBalance = totalCost - amountPaid;

            var nextDueDate = sub.RepaymentSchedules
                .Where(x => !x.IsPaid)
                .OrderBy(x => x.DueDate)
                .Select(x => (DateTimeOffset?)x.DueDate)
                .FirstOrDefault();

            var progress = totalCost == 0
                ? 0
                : (amountPaid / totalCost) * 100m;

            return new InputObligationDto
            {
                Name = sub.InputPackage?.Name,
                TotalCost = totalCost,
                AmountPaid = amountPaid,
                RemainingBalance = remainingBalance,
                Date = nextDueDate,
                RepaymentProgress = Math.Round(progress, 2)
            };
        }).ToList();

            return new PagedResult<InputObligationDto>
            {
                Data = items,
                Count = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<LoanDetailDto> GetLoanDetailsAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.InputPackageItems)
                        .ThenInclude(x => x.OrganizationProduct)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.CommitmentPlan)
                .Include(x => x.InputPackage)
                    .ThenInclude(x => x.RepaymentPlan)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Subscription not found");

            var package = subscription.InputPackage;
            var repayment = package.RepaymentPlan;

            var inputs = package.PackageStructure == InputPackageStructure.Physical
                ? package.InputPackageItems.Select(x => new InputBreakdownDto
                {
                    InputName = x.OrganizationProduct?.ProductName ?? "Unknown",
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitCost,
                    TotalCost = x.Quantity * x.UnitCost
                }).ToList()
                : new List<InputBreakdownDto>();

            var installments = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .OrderBy(x => x.PaidOn)
                .Select(x => new InstallmentDto
                {
                    PaymentDate = x.PaidOn,
                    Amount = x.AmountPaid,
                    Reference = x.Reference
                })
                .ToListAsync();

            var schedules = await _context.RepaymentSchedules
                .Where(x => x.InputPackageSubscriptionId == subscriptionId)
                .OrderBy(x => x.InstallmentNumber)
                .Select(x => new RepaymentScheduleDto
                {
                    InstallmentNumber = x.InstallmentNumber,
                    DueDate = x.DueDate,
                    AmountToPay = x.Amount,
                    GracePeriodDeadline = x.GracePeriodDueDate
                })
                .ToListAsync();

            return new LoanDetailDto
            {
                LoanName = package.Name,
                LoanAmount = subscription.TotalFinancingValue,
                DateIssued = subscription.DisbursedOn ?? subscription.SubscribedOn,

                DueDate = repayment?.RepaymentEndDate,

                InterestRate = repayment?.InterestRate ?? 0,

                Inputs = inputs,
                InstallmentsPaid = installments,
                RepaymentSchedule = schedules
            };
        }
        public async Task<PagedResult<FinancingBreakdownDto>> GetFinancingBreakdownAsync(
        string subscriberUserId,
        int pageNumber,
        int pageSize)
        {
            if (string.IsNullOrWhiteSpace(subscriberUserId))
                throw new ArgumentException("SubscriberUserId is required");

            var query = _context.InputPackageSubscriptions
                .AsNoTracking()
                .Include(x => x.InputPackage)
                .Where(x => x.SubscriberUserId == subscriberUserId);

            var count = await query.CountAsync();

            var subscriptions = await query
                .OrderByDescending(x => x.SubscribedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var subscriptionIds = subscriptions.Select(x => x.Id).ToList();

            var payments = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    subscriptionIds.Contains(x.SubscriptionId) &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .GroupBy(x => x.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    TotalPaid = g.Sum(x => x.AmountPaid)
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.TotalPaid);

            var schedules = await _context.RepaymentSchedules
                .Where(x => subscriptionIds.Contains(x.InputPackageSubscriptionId))
                .GroupBy(x => x.InputPackageSubscriptionId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.OrderBy(x => x.DueDate).FirstOrDefault()
                );
            var items = subscriptions.Select(sub =>
            {
                payments.TryGetValue(sub.Id, out var paid);
                schedules.TryGetValue(sub.Id, out var firstSchedule);

                var total = sub.TotalFinancingValue;

                var percentage = total == 0
                    ? 0
                    : (paid / total) * 100m;

                string status =
                    paid <= 0
                        ? "Unpaid"
                        : paid >= total
                            ? "Paid"
                            : "Partially Paid";

                return new FinancingBreakdownDto
                {
                    LoanName = sub.InputPackage?.Name,
                    Amount = total,
                    DateIssued = sub.DisbursedOn ?? sub.SubscribedOn,
                    DueDate = firstSchedule?.DueDate,
                    RepaymentPercentage = Math.Round(percentage, 2),
                    Status = status
                };
            }).ToList();

            return new PagedResult<FinancingBreakdownDto>
            {
                Data = items,
                Count = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(string subscriberUserId)
        {
            if (string.IsNullOrWhiteSpace(subscriberUserId))
                throw new ArgumentException("SubscriberUserId is required");

            var subscriptions = await _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(x => x.SubscriberUserId == subscriberUserId)
                .Select(x => new
                {
                    x.Id,
                    x.TotalFinancingValue,
                    x.Status
                })
                .ToListAsync();

            if (!subscriptions.Any())
            {
                return new InputLoanSummaryDto();
            }

            var subscriptionIds = subscriptions.Select(x => x.Id).ToList();

            var totalLoanIssued = subscriptions.Sum(x => x.TotalFinancingValue);

            var activeStatuses = new[]
            {
        SubscriptionStatus.Active,
        SubscriptionStatus.PendingDisbursement,
        SubscriptionStatus.PendingCommitment,
        SubscriptionStatus.PendingApproval
    };

            var totalActiveLoansAmount = subscriptions
                .Where(x => activeStatuses.Contains(x.Status))
                .Sum(x => x.TotalFinancingValue);

            var totalRepaidAmount = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    subscriptionIds.Contains(x.SubscriptionId) &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

            var totalOutstanding = totalLoanIssued - totalRepaidAmount;


            var totalActiveLoans = subscriptions.Count(x => activeStatuses.Contains(x.Status));

            var totalRepaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    subscriptionIds.Contains(x.SubscriptionId) &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;


            var totalOutstandingBalance = await _context.RepaymentSchedules
                .Where(x =>
                    subscriptionIds.Contains(x.InputPackageSubscriptionId) &&
                    !x.IsPaid)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            return new InputLoanSummaryDto
            {
                TotalLoansIssued = totalLoanIssued,
                TotalRepaid = totalRepaid,
                TotalActiveLoans = totalActiveLoans,
                TotalActiveLoansAmount = totalActiveLoansAmount,
                TotalRepaidAmount = totalRepaidAmount,
                TotalOutstanding = totalOutstanding,
                TotalOutstandingBalance = totalOutstandingBalance
            };
        }

        public async Task<PagedResult<RepaymentBreakdownDto>> GetRepaymentBreakdownAsync(
        long subscriptionId,
        int pageNumber,
        int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            var subscriptionExists = await _context.InputPackageSubscriptions
                .AnyAsync(x => x.Id == subscriptionId);

            if (!subscriptionExists)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var totalCount = await _context.RepaymentSchedules
                .Where(x => x.InputPackageSubscriptionId == subscriptionId)
                .CountAsync();

            var schedules = await _context.RepaymentSchedules
                .Where(x => x.InputPackageSubscriptionId == subscriptionId)
                .OrderBy(x => x.DueDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var payments = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .OrderBy(x => x.PaidOn)
                .ToListAsync();

            decimal paymentPool = 0;
            int paymentIndex = 0;

            var result = new List<RepaymentBreakdownDto>();

            foreach (var schedule in schedules)
            {
                decimal installmentAmount = schedule.Amount;
                decimal paid = 0;

                while (paymentPool > 0 && paid < installmentAmount)
                {
                    var needed = installmentAmount - paid;
                    var applied = Math.Min(paymentPool, needed);

                    paid += applied;
                    paymentPool -= applied;
                }

                while (paid < installmentAmount && paymentIndex < payments.Count)
                {
                    var payment = payments[paymentIndex];
                    var remainingPaymentAmount = payment.AmountPaid;

                    var needed = installmentAmount - paid;
                    var applied = Math.Min(remainingPaymentAmount, needed);

                    paid += applied;
                    remainingPaymentAmount -= applied;

                    if (remainingPaymentAmount > 0)
                        paymentPool += remainingPaymentAmount;

                    paymentIndex++;
                }

                var balance = installmentAmount - paid;

                result.Add(new RepaymentBreakdownDto
                {
                    Date = schedule.DueDate,
                    LoanAmount = installmentAmount,
                    AmountPaid = paid,
                    MethodOfPayment = payments.FirstOrDefault()?.PaymentChannel.ToString() ?? "N/A",
                    BalanceRemaining = balance < 0 ? 0 : balance
                });
            }

            return new PagedResult<RepaymentBreakdownDto>
            {
                Data = result,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Subscription not found");

            var totalExpectedRepaymentValue = await _context.RepaymentSchedules
                .Where(x => x.InputPackageSubscriptionId == subscriptionId)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.Status == TransactionStatus.Successful &&
                    x.PaymentType == PaymentType.Repayment)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

            var totalOutstanding = totalExpectedRepaymentValue - totalPaid;

            if (totalOutstanding < 0)
                totalOutstanding = 0;

            var repaymentPercentage = totalExpectedRepaymentValue == 0
                ? 0
                : (totalPaid / totalExpectedRepaymentValue) * 100m;

            return new FinancingRepaymentSummaryDto
            {
                TotalExpectedRepaymentValue = totalExpectedRepaymentValue,
                TotalPaid = totalPaid,
                TotalOutstanding = totalOutstanding,
                RepaymentPercentage = Math.Round(repaymentPercentage, 2)
            };
        }

        public async Task<byte[]> ExportSubscriptionsAsync(
        SubscriptionQueryFilter filter,
        ExportFormat format)
        {

            var baseQuery = await BuildSubscriptionQuery(filter);
            baseQuery = baseQuery.AsNoTracking();


            var subscriptionIds = await baseQuery
                .Select(x => x.Id)
                .ToListAsync();

            if (!subscriptionIds.Any())
                return format switch
                {
                    ExportFormat.Excel => ExportToExcel([]),
                    ExportFormat.Pdf => ExportToPdf([]),
                    _ => throw new NotSupportedException("Format not supported")
                };

            var paymentTotals = await _context.InputPackageSubscriptionPayments
                .AsNoTracking()
                .Where(p =>
                    subscriptionIds.Contains(p.SubscriptionId) &&
                    p.Status == TransactionStatus.Successful)
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    CommitmentPaid = g
                        .Where(x => x.PaymentType == PaymentType.Commitment)
                        .Sum(x => (decimal?)x.AmountPaid) ?? 0m
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.CommitmentPaid);


            var rawData = await baseQuery
                .Select(x => new
                {
                    x.Id,
                    x.InputPackageId,
                    x.SubscriberUserId,
                    x.AddedByUserId,
                    x.SubscriberType,
                    x.CommitmentValue,
                    x.CommitmentRequired,
                    x.TotalFinancingValue,
                    x.SubscribedOn,
                    x.Status,
                    SubscriberName = x.SubscriberUser == null
                        ? x.SubscriberUserId
                        : !string.IsNullOrWhiteSpace(x.SubscriberUser.Name)
                            ? x.SubscriberUser.Name
                            : !string.IsNullOrWhiteSpace(x.SubscriberUser.PhoneNumber)
                                ? x.SubscriberUser.PhoneNumber
                                : x.SubscriberUser.Email,
                    CreatedBy = x.AddedByUser == null
                        ? x.AddedByUserId
                        : !string.IsNullOrWhiteSpace(x.AddedByUser.Name)
                            ? x.AddedByUser.Name
                            : !string.IsNullOrWhiteSpace(x.AddedByUser.PhoneNumber)
                                ? x.AddedByUser.PhoneNumber
                                : x.AddedByUser.Email,
                    Location = x.Organization != null ? x.Organization.CompanyName : null,
                    PackageName = x.InputPackage.Name,
                })
                .ToListAsync();


            var data = rawData.Select(x =>
            {
                var commitmentPaid = paymentTotals.GetValueOrDefault(x.Id, 0m);
                var commitmentStatus = !x.CommitmentRequired || x.CommitmentValue <= 0
                    ? "Not Required"
                    : commitmentPaid >= x.CommitmentValue
                        ? "Paid"
                        : "Pending";

                return new SubscriptionDetailDto
                {
                    PackageId = x.InputPackageId,
                    SubscriptionId = x.Id,
                    SubscriberUserId = x.SubscriberUserId,
                    SubscriberName = x.SubscriberName,
                    CreatedBy = x.CreatedBy,
                    SubscriptionType = x.SubscriberType,
                    Location = x.Location,
                    PackageName = x.PackageName,
                    CommitmentValue = x.CommitmentValue,
                    TotalFinancingValue = x.TotalFinancingValue,
                    SubscribedOn = x.SubscribedOn,
                    SubscriptionStatus = x.Status,
                    SubscriptionStatusName = EnumExtensions.GetDisplayName(x.Status),
                    CommitmentStatus = commitmentStatus
                };
            }).ToList();


            return format switch
            {
                ExportFormat.Excel => ExportToExcel(data),
                ExportFormat.Pdf => ExportToPdf(data),
                _ => throw new NotSupportedException("Format not supported")
            };
        }

        private IQueryable<InputPackageSubscription> ApplyFilters(
        IQueryable<InputPackageSubscription> query,
        SubscriptionQueryFilter filter)
        {
            if (!string.IsNullOrEmpty(filter.SubscriberUserId))
                query = query.Where(x => x.SubscriberUserId == filter.SubscriberUserId);

            if (filter.PackageId.HasValue)
                query = query.Where(x => x.InputPackageId == filter.PackageId);

            if (filter.OrganizationId.HasValue)
                query = query.Where(x => x.OrganizationId == filter.OrganizationId);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status);

            if (filter.IsActiveOnly == true)
                query = query.Where(x => x.IsActive);

            if (filter.ViewType.HasValue)
            {
                switch (filter.ViewType.Value)
                {
                    case SubscriptionView.PendingApproval:
                        query = query.Where(x => x.Status == SubscriptionStatus.PendingApproval);
                        break;

                    case SubscriptionView.PendingCommitment:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingCommitment ||
                            x.Status == SubscriptionStatus.Approved);
                        break;

                    case SubscriptionView.Active:
                        query = query.Where(x => x.Status == SubscriptionStatus.Active);
                        break;

                    case SubscriptionView.Completed:
                        query = query.Where(x => x.Status == SubscriptionStatus.Completed);
                        break;

                    case SubscriptionView.Defaulted:
                        query = query.Where(x => x.Status == SubscriptionStatus.Defaulted);
                        break;
                }
            }

            return query;
        }
        private byte[] ExportToExcel(List<SubscriptionDetailDto> data)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Subscriptions");

            // HEADER
            ws.Cell(1, 1).Value = "Subscription Id";
            ws.Cell(1, 2).Value = "Subscriber";
            ws.Cell(1, 3).Value = "Package";
            ws.Cell(1, 4).Value = "Commitment";
            ws.Cell(1, 5).Value = "Created By";
            ws.Cell(1, 6).Value = "Total Financing";
            //  ws.Cell(1, 7).Value = "Subscribed On";
            ws.Cell(1, 7).Value = "Status";

            var row = 2;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.SubscriptionId;
                ws.Cell(row, 2).Value = item.SubscriberName;
                ws.Cell(row, 3).Value = item.PackageName;

                ws.Cell(row, 4).Value = item.CommitmentValue;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(row, 5).Value = item.CreatedBy;

                ws.Cell(row, 6).Value = item.TotalFinancingValue;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

                //ws.Cell(row, 7).Value = item.SubscribedOn;
                //ws.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd";

                ws.Cell(row, 7).Value = item.SubscriptionStatus.ToString();

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        private byte[] ExportToPdf(List<SubscriptionDetailDto> data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);   // ID
                            columns.RelativeColumn();     // Subscriber
                            columns.RelativeColumn();     // Type
                            columns.RelativeColumn();     // CreatedBy
                            columns.RelativeColumn();     // Location
                            columns.RelativeColumn();     // Package
                            columns.ConstantColumn(100);   // Commitment
                            columns.ConstantColumn(120);   // Total Finance
                            columns.ConstantColumn(120);   // Date
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("ID");
                            header.Cell().Text("Subscriber");
                            header.Cell().Text("Type");
                            header.Cell().Text("Created By");
                            header.Cell().Text("Location");
                            header.Cell().Text("Package");
                            header.Cell().Text("Commitment");
                            header.Cell().Text("Total Finance");
                            header.Cell().Text("Date Submitted");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.SubscriptionId.ToString());
                            table.Cell().Text(item.SubscriberName);
                            table.Cell().Text(EnumExtensions.GetDisplayName(item.SubscriptionType));
                            table.Cell().Text(item.CreatedBy ?? "-");
                            table.Cell().Text(item.Location ?? "-");
                            table.Cell().Text(item.PackageName);

                            table.Cell().Text(item.CommitmentValue.ToString("N2"));
                            table.Cell().Text(item.TotalFinancingValue.ToString("N2"));

                            table.Cell().Text(item.SubscribedOn.HasValue ? item.SubscribedOn.Value.ToString("yyyy-MM-dd") : null);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }  
        #endregion

        private async Task EnsurePackageIsEditable(InputPackage package)
        {
            if (package == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Package not found");

            if (!package.IsActive)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "This package is not editable because it is deactivated.");
            }

            var hasSubscribers = await _context.InputPackageSubscriptions
                .AnyAsync(x => x.InputPackageId == package.Id);

            if (hasSubscribers)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "This package is no longer editable because it already has subscribers.");
            }
        }
        private static string GetRepaymentPeriod(RepaymentFrequency frequency, int duration)
        {
            var unit = frequency switch
            {
                RepaymentFrequency.Daily => "day",
                RepaymentFrequency.Weekly => "week",
                RepaymentFrequency.Monthly => "month",
                RepaymentFrequency.Quarterly => "quarter",
                RepaymentFrequency.Yearly => "year",
                _ => "Not Set"
            };

            if (frequency == RepaymentFrequency.Weekly)
            {
                return $"{duration * 2} weeks";
            }

            var plural = duration > 1 && frequency != RepaymentFrequency.Weekly ? "s" : "";

            return $"{duration} {unit}{plural}";
        }

        private decimal CalculateRepayment(decimal baseValue, bool applyInterest, decimal rate)
        {
            if (!applyInterest)
                return baseValue;

            return baseValue + (baseValue * rate);
        }
        private decimal CalculateCommitment(
        decimal total,
        CommitmentType type,
        decimal value,
        bool required)
        {
            if (!required)
                return 0;

            return type switch
            {
                CommitmentType.StaticFee => value,
                CommitmentType.PercentageOfTotalValue => total * value,
                _ => value
            };
        }

        private RiskCategory CalculateRisk(EconomicsOfProduction eop)
        {
            var profit = eop.ExpectedYield * eop.ExpectedMarketPrice - eop.TotalCostOfProduction;

            if (profit <= 0)
                return RiskCategory.High;

            if (profit < eop.TotalCostOfProduction * 0.25m)
                return RiskCategory.Medium;

            return RiskCategory.Low;
        }

        #region EOP to Package Generation
        public async Task<EopResponseDto> CreateEopAsync(CreateEopRequest request)
        {
            if (request.CostItems == null || !request.CostItems.Any())
                throw new BaseException(ExceptionType.BAD_REQUEST, "EOP must have at least one cost item");

            var costItems = request.CostItems.Select(x => new EopCostItem
            {
                Name = x.Name,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost,
                OrganizationProductId = x.OrganizationProductId
            }).ToList();

            var totalCost = costItems.Sum(x => x.Quantity * x.UnitCost);

            var eop = new EconomicsOfProduction
            {
                Name = request.Name,
                Crop = request.Crop,
                Season = request.Season,
                LandSize = request.LandSize,
                ExpectedYield = request.ExpectedYield,
                ExpectedMarketPrice = request.ExpectedMarketPrice,
                TotalCostOfProduction = totalCost,
                OrganizationId = request.OrganizationId,
                CostItems = costItems,
                CreatedOn = DateTime.UtcNow,
                IsActive = true,
                UnitOfMeasure = request.UnitOfMeasure
            };

            _context.EconomicsOfProductions.Add(eop);
            await _context.SaveChangesAsync();

            return new EopResponseDto
            {
                Id = eop.Id,
                Name = eop.Name,
                Crop = eop.Crop,
                TotalCostOfProduction = eop.TotalCostOfProduction,

                CostItems = eop.CostItems.Select(c => new EopCostItemResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Quantity = c.Quantity,
                    UnitCost = c.UnitCost,
                    TotalCost = c.TotalCost
                }).ToList()
            };
        }
        public async Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request)
        {
            var query = _context.EconomicsOfProductions
                .Where(x => x.OrganizationId == request.OrganizationId && x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Crop))
                query = query.Where(x => x.Crop.Contains(request.Crop));

            if (!string.IsNullOrWhiteSpace(request.Season))
                query = query.Where(x => x.Season.Contains(request.Season));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new EopDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Crop = x.Crop,
                    Season = x.Season,
                    LandSize = x.LandSize,
                    ExpectedYield = x.ExpectedYield,
                    ExpectedMarketPrice = x.ExpectedMarketPrice,
                    TotalCostOfProduction = x.TotalCostOfProduction
                })
                .ToListAsync();

            return new PagedResult<EopDto>
            {
                Data = items,
                Count = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<EopDto> GetEopByIdAsync(long id)
        {
            var eop = await _context.EconomicsOfProductions
                .Include(x => x.CostItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (eop == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "EOP not found");

            return new EopDto
            {
                Id = eop.Id,
                Name = eop.Name,
                Crop = eop.Crop,
                Season = eop.Season,
                LandSize = eop.LandSize,
                ExpectedYield = eop.ExpectedYield,
                ExpectedMarketPrice = eop.ExpectedMarketPrice,
                TotalCostOfProduction = eop.TotalCostOfProduction,
                CostItems = eop.CostItems.Select(c => new EopCostItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Quantity = c.Quantity,
                    UnitCost = c.UnitCost,
                    OrganizationProductId = c.OrganizationProductId
                }).ToList()
            };
        }

        private void ValidateEop(EconomicsOfProduction eop)
        {
            if (eop.ExpectedYield <= 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid expected yield");

            if (eop.ExpectedMarketPrice <= 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid market price");

            if (!eop.CostItems.Any(x => x.OrganizationProductId != null))
                throw new BaseException(ExceptionType.BAD_REQUEST, "EOP must contain at least one input item");
        }
        #endregion EOP to Package Generation
    }
}
