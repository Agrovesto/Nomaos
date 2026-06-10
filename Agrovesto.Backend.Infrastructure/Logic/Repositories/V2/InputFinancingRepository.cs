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
using Amazon.SimpleNotificationService.Model;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        //Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request);
        Task<EopDto> GetEopByIdAsync(long id);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request);
        Task<PagedResult<FinancingBreakdownDto>> GetFinancingBreakdownAsync(
   string subscriberUserId,
   int pageNumber,
   int pageSize);
        Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(long subscriptionId);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetInputFinancingCategories(ListQueryParams query);
        Task<ProductCategoryDto> GetInputFinancingCategoryById(long id);
        Task<InputFinancingDashboardDto> GetInputFinancingDashboardAsync(SubscriptionQueryFilter filter);
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
        Task<long> RequestInputDisbursementApprovalAsync(long subscriptionId, decimal? requestedAmount, string? reference, InputSubscriptionRequestArgs args);
        Task<decimal> GetTotalCommitmentPaidAsync(long subscriptionId);
       // Task<bool> RecordSuccessfulPaymentAsync(RecordCommitmentPaymentRequest request, InputSubscriptionRequestArgs args);
        Task<bool> RecordSuccessfulPaymentAsync(long paymentId, PaymentConfirmationRequest request);
        Task FlagPendingBankTransferAsync(long paymentId);
        Task FlagPendingCashApprovalAsync(long paymentId);
        Task<long> CreatePendingPaymentAsync(CreateCommitmentPaymentRequest request);
        Task<bool> AddItemToPackageAsync(long packageId, AddPackageItemRequest request);
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
                throw new Exception("Product Category already exists.");


            var cat = new InputFinancingCategory
            {
                BusinessCode = dto.BusinessCode,
                Description=dto.Description,
                
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
                throw new Exception("Product Category not found.");

            return new ProductCategoryDto
            {
                Id = cat.Id,
                CategoryName = cat.Name,
                BusinessCode = cat.BusinessCode
            };
        }

        /**
         * 
         *InputPackage (template)
        ↓
Subscription (real customer participation)
        ↓
RepaymentSchedule (per subscriber tracking OR linked schedule)
         */

        public async Task<bool> CreateInputFinancingPackageConfig(
        CreateInputPackageRequest request,
        InputPackageConfigArgs args)
        {
            if (request == null)
                throw new Exception("Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var isPhysical = request.PackageStructure == InputPackageStructure.Physical;

                    var requestItems = request.InputPackageItems ?? new List<CreateInputPackageItemRequest>();
                    var requestAdditionalCosts = request.AdditionalCosts ?? new List<CreateAdditionalCostRequest>();

                    if (isPhysical && !requestItems.Any())
                        throw new Exception("Physical package must have items");

                    if (!isPhysical && requestItems.Any())
                        throw new Exception("Non-physical package cannot have items");


                    var items = isPhysical
                        ? requestItems.Select(x => new InputPackageItem
                        {
                            OrganizationProductId = x.OrganizationProductId,
                            Quantity = x.Quantity,
                            UnitCost = x.UnitCost
                        }).ToList()
                        : new List<InputPackageItem>();


                    var additionalCosts = requestAdditionalCosts.Select(x => new AdditionalInputPackageCost
                    {
                        Name = x.Name,
                        Description = x.Description,
                        Amount = x.Amount,
                        IsPerUnit = x.IsPerUnit,
                        Quantity = x.Quantity,
                        Type = x.Type
                    }).ToList();


                    var additionalCostsTotal = additionalCosts.Sum(x => x.Amount);

                    decimal itemsTotal = isPhysical
                        ? items.Sum(x => x.Quantity * x.UnitCost)
                        : request.TotalPackageValue;

                    if (isPhysical &&
                        request.TotalPackageValue > 0 &&
                        Math.Round(request.TotalPackageValue, 2) != Math.Round(itemsTotal, 2))
                    {
                        throw new Exception("TotalPackageValue mismatch");
                    }

                    var totalPackageValue = itemsTotal;
                    var totalFinancingValue = totalPackageValue + additionalCostsTotal;

                    if (request.TotalFinancingValue > 0 &&
                        Math.Round(request.TotalFinancingValue, 2) != Math.Round(totalFinancingValue, 2))
                    {
                        throw new Exception("TotalFinancingValue mismatch");
                    }


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
                        AdditionalCostAmount = additionalCostsTotal,
                        TotalFinancingValue = request.TotalFinancingValue > 0
                            ? request.TotalFinancingValue
                            : totalFinancingValue,

                        CommitmentRequired = request.CommitmentRequired,
                        RepaymentRequired = request.RepaymentRequired,

                        UserWalletId = request.UserWalletId,
                        WalletProviderId = request.PaybackWalletId,

                        IsSelectedWalletProvisioned = args.isSelectedWalletProvisioned,

                        Currency = request.Currency,
                        IsGeneratedFromEop = false,
                        Version = 1,
                        CreatedOn = DateTimeOffset.UtcNow,
                        IsActive = true,
                        BusinessCode = args.BusinessCode
                    };

                    if (request.CommitmentRequired)
                    {
                        var commitmentValue =
                            request.CommitmentType == CommitmentType.PercentageOfTotalValue
                                ? totalPackageValue * (request.CommitmentValue / 100m)
                                : request.CommitmentValue;

                        package.CommitmentPlan = new CommitmentPlan
                        {
                            CommitmentType = request.CommitmentType,
                            CommitmentValue = commitmentValue,
                            CommitmentPaymentStartDate = request.CommitmentPaymentStartDate,
                            CommitmentPaymentEndDate = request.CommitmentPaymentEndDate
                        };
                    }

                    if (request.RepaymentRequired)
                    {
                        package.RepaymentPlan = new RepaymentPlan
                        {
                            RepaymentFrequency = request.RepaymentFrequency,
                            RepaymentDuration = request.RepaymentDuration,
                            RepaymentStartDate = request.RepaymentStartDate,
                            RepaymentEndDate = request.RepaymentEndDate,
                            RepaymentGracePeriod = request.RepaymentGracePeriod,
                            RepaymentMode = request.RepaymentMode,
                            RepaymentValue = request.RepaymentValue,
                            RiskCategory = request.RiskCategory
                        };
                    }

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
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> UpdateInputPackageConfig(
        long packageId,
        UpdateInputPackageConfigRequest request,
        InputPackageConfigArgs args)
        {
            if (request == null)
                throw new Exception("Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var package = await _context.InputPackages
                        .Include(x => x.CommitmentPlan)
                        .Include(x => x.RepaymentPlan)
                        .FirstOrDefaultAsync(x =>
                            x.Id == packageId &&
                            x.OrganizationId == args.organizationId);

                    if (package == null)
                        throw new Exception("Package not found");

                    if (!package.IsActive)
                        throw new Exception("Cannot update configuration for inactive package");

                    if (request.CommitmentRequired)
                    {
                        if (request.CommitmentPaymentStartDate >= request.CommitmentPaymentEndDate)
                            throw new Exception("Invalid commitment payment date range");
                    }

                    
                    if (request.RepaymentRequired)
                    {
                        if (request.RepaymentStartDate >= request.RepaymentEndDate)
                            throw new Exception("Invalid repayment date range");

                        if (request.RepaymentGracePeriod < request.RepaymentStartDate)
                            throw new Exception("Grace period cannot be before repayment start date");
                    }


                    package.CommitmentRequired = request.CommitmentRequired;
                    package.RepaymentRequired = request.RepaymentRequired;


                    if (request.CommitmentRequired)
                    {
                        var commitmentValue =
                            request.CommitmentType == CommitmentType.PercentageOfTotalValue
                                ? package.TotalPackageValue * (request.CommitmentValue / 100m)
                                : request.CommitmentValue;


                        if (package.CommitmentPlan == null)
                        {
                            package.CommitmentPlan = new CommitmentPlan
                            {
                                InputPackageId = package.Id
                            };
                        }

                        package.CommitmentPlan.CommitmentType = request.CommitmentType;
                        package.CommitmentPlan.CommitmentValue = commitmentValue;
                        package.CommitmentPlan.CommitmentPaymentStartDate = request.CommitmentPaymentStartDate;
                        package.CommitmentPlan.CommitmentPaymentEndDate = request.CommitmentPaymentEndDate;
                    }
                    else
                    {
                        // REMOVE EXISTING PLAN IF DISABLED
                        if (package.CommitmentPlan != null)
                        {
                            _context.CommitmentPlans.Remove(package.CommitmentPlan);
                        }
                    }

                    if (request.RepaymentRequired)
                    {
                        if (package.RepaymentPlan == null)
                        {
                            package.RepaymentPlan = new RepaymentPlan
                            {
                                InputPackageId = package.Id
                            };
                        }

                        package.RepaymentPlan.RepaymentFrequency = request.RepaymentFrequency;
                        package.RepaymentPlan.RepaymentDuration = request.RepaymentDuration;

                        package.RepaymentPlan.RepaymentStartDate = request.RepaymentStartDate;
                        package.RepaymentPlan.RepaymentEndDate = request.RepaymentEndDate;
                        package.RepaymentPlan.RepaymentGracePeriod = request.RepaymentGracePeriod;

                        package.RepaymentPlan.RepaymentMode = request.RepaymentMode;
                        package.RepaymentPlan.RepaymentValue = request.RepaymentValue;
                        package.RepaymentPlan.RiskCategory = request.RiskCategory;
                    }
                    else
                    {
                        // REMOVE EXISTING PLAN IF DISABLED
                        if (package.RepaymentPlan != null)
                        {
                            _context.RepaymentPlans.Remove(package.RepaymentPlan);
                        }
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
            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, request.PageSize);

            var query = _context.InputPackages
                .Where(x => x.OrganizationId == request.OrganizationId);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
                    x.SubScriptionType
                })
                .ToListAsync();

            return new PagedResult<InputPackageListModel>
            {
                Items = data.Select(x => new InputPackageListModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    FinancingCategory = x.FinancingCategory,
                    InputTarget = x.InputTarget,
                    TotalFinancingValue = x.TotalFinancingValue,
                    CommitmentValue = x.CommitmentValue,
                    RepaymentModel = x.RepaymentMode.HasValue ? EnumExtensions.GetDisplayName(x.RepaymentMode.Value) : null,
                    RiskCategory = x.RiskCategory.HasValue ? EnumExtensions.GetDisplayName(x.RiskCategory.Value) : null,
                    Subscribers = 0,
                    IsActive = x.IsActive,
                    SubscriberType = EnumExtensions.GetDisplayName(x.SubScriptionType)
                }).ToList(),

                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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
                    Name = c.Name,
                    Amount = c.Amount
                }).ToList()
            };
        }
        public async Task<bool> DeactivatePackageAsync(long packageId, long organizationId)
        {
            var package = await _context.InputPackages
                .FirstOrDefaultAsync(x => x.Id == packageId
                                       && x.OrganizationId == organizationId);

            if (package == null)
                throw new Exception("Package not found");

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
                throw new Exception("Package not found");

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

            EnsurePackageIsEditable(package);

            var item = package.InputPackageItems
                .FirstOrDefault(x => x.Id == itemId);

            if (item == null)
                throw new Exception("Item not found in package");

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

            EnsurePackageIsEditable(package);

            var item = package.InputPackageItems
                .FirstOrDefault(x => x.Id == args.InputId);

            if (item == null)
                throw new Exception("Package item not found");

            var productExists = await _context.OrganizationProducts
                .AnyAsync(x => x.Id == request.OrganizationProductId
                            && x.OrganizationId == args.OrganizationId);

            if (!productExists)
                throw new Exception("Invalid product for this organization");

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

            EnsurePackageIsEditable(package);

            var product = await _context.OrganizationProducts
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationProductId
                                        && x.OrganizationId == package.OrganizationId);

            if (product == null)
                throw new Exception("Invalid product for this organization");

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
        private void RecalculatePackageFinancials(InputPackage package)
        {
            var isPhysical = package.PackageStructure == InputPackageStructure.Physical;

            var itemsTotal = isPhysical
                ? package.InputPackageItems.Sum(x => x.Quantity * x.UnitCost)
                : package.TotalPackageValue;

            var additionalCostsTotal = package.AdditionalCosts.Sum(x => x.Amount);

            package.TotalPackageValue = itemsTotal;
            package.AdditionalCostAmount = additionalCostsTotal;

            var totalFinancingValue = itemsTotal + additionalCostsTotal;

            package.TotalFinancingValue = totalFinancingValue;

            // Optional: if commitments depend on total value, recompute here too
            if (package.CommitmentPlan != null)
            {
                if (package.CommitmentPlan.CommitmentType == CommitmentType.PercentageOfTotalValue)
                {
                    package.CommitmentPlan.CommitmentValue =
                        itemsTotal * (package.CommitmentPlan.CommitmentValue / 100m);
                }
            }
        }



        /**
         * 
         *1. Subscriber Added
   → PendingApproval

2. Financier Approves
   → PendingCommitment (if commitment required)

3. Commitment Paid
   → EligibleForDisbursement

4. Disbursement Happens
   → Active
   → Generate Repayment Schedule HERE

5. Repayment Tracking Begins
   → Due / Overdue / Paid tracking

6. Completed
   → fully paid

7. Defaulted
   → missed repayment obligations
         * */

        #region Input Subscriber
        public async Task<bool> AddSubscriberAsync(
        AddSubscriberRequest request,
        InputSubscriptionRequestArgs args)
        {
            if (request == null)
                throw new Exception("Request cannot be null");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var package = await _context.InputPackages
                        .Include(x => x.CommitmentPlan)
                        .Include(x => x.RepaymentPlan)
                        .FirstOrDefaultAsync(x => x.Id == request.PackageId);

                    if (package == null)
                        throw new Exception("Package not found");

                    if (!package.IsActive)
                        throw new Exception("Only active packages can accept subscribers");

                    if (package.SubScriptionType == InputSubScriptionType.Individual &&
                        request.SubscriberType != InputSubScriptionType.Individual)
                    {
                        throw new Exception("This package does not allow group subscriptions");
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

                    if (package.RepaymentRequired && repayment == null)
                        throw new Exception("Repayment plan is required for subscription");

                    if (package.CommitmentRequired && commitment == null)
                        throw new Exception("Commitment plan is required for subscription");

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
                        
                        BusinessCode = args.BusinessCode
                    };

                    _context.InputPackageSubscriptions.Add(subscription);

                    await _context.SaveChangesAsync();

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
                        throw new Exception("Subscription not found");

                    if (subscription.Status != SubscriptionStatus.PendingApproval)
                        throw new Exception("Only pending approval subscriptions can be processed");

                    if (request.ApprovalState == ApprovalDecision.Rejected)
                    {
                        if (string.IsNullOrWhiteSpace(request.RejectionReason))
                            throw new Exception("Rejection reason is required");

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
                        ? SubscriptionStatus.PendingCommitment   //this should be approved
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
                PaymentType = PaymentType.Commitment,
                AmountPaid = request.Amount,
                PaymentChannel = request.PaymentChannel,
                Status = TransactionStatus.Pending,
                Reference = request.Reference,
                ExternalReference = request.ExternalReference,
                WalletProviderId = request.WalletProviderId,
                CreatedOn = DateTimeOffset.UtcNow
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
      PaymentConfirmationRequest request)
        {
            var payment = await _context.InputPackageSubscriptionPayments
                .Include(x => x.Subscription)
                .ThenInclude(x => x.InputPackage)
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment == null)
                throw new Exception("Payment record not found");

            if (payment.Status == TransactionStatus.Successful)
                return true;

            if (payment.Status == TransactionStatus.Failed)
                throw new Exception("Cannot confirm failed payment");

            payment.Status = request.Status;
            payment.ExternalReference = request.ExternalReference;
            payment.TransactionResponse = request.ResponsePayload;
            payment.PaidOn = DateTimeOffset.UtcNow;

            if (request.Status != TransactionStatus.Successful)
            {
                await _context.SaveChangesAsync();
                return false;
            }

            var subscription = payment.Subscription;

            /*
             * Recalculate total successful commitment payments
             */
            var totalPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var fullyPaid =
                totalPaid >= subscription.CommitmentValue;

            /*
             * Move subscription to next stage
             */
            if (fullyPaid &&
                subscription.Status == SubscriptionStatus.PendingCommitment)
            {
                subscription.Status =
                    SubscriptionStatus.PendingDisbursement;

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

            /*
             * General payment recorded event
             */
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
                        SubscriberUserId =
                            subscription.SubscriberUserId,

                        OrganizationId =
                            subscription.OrganizationId,

                        AmountPaid = payment.AmountPaid,

                        OutstandingBalance =
                            subscription.CommitmentValue - totalPaid,

                        FullyPaid = fullyPaid
                    })
            });

            await _context.SaveChangesAsync();

            return true;
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
                throw new Exception("Subscription not found");

            // ⚠️ IMPORTANT:
            // Do NOT re-validate wallet, amounts, or recompute totals here
            // That already happened upstream

            var paymentExists = await _context.InputPackageSubscriptionPayments
                .AnyAsync(x =>
                    x.Reference == request.Reference);

            if (paymentExists)
                throw new Exception("Duplicate payment reference");

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
        /**
         public async Task<bool> RecordCommitmentPaymentAsync(RecordCommitmentPaymentRequest request, InputSubscriptionRequestArgs args)
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
                        throw new Exception("Subscription not found");

                    if (subscription.Status != SubscriptionStatus.PendingCommitment)
                        throw new Exception("Subscription is not in commitment stage");

                    if (!subscription.InputPackage.CommitmentRequired)
                        throw new Exception("Commitment not required for this package");

                   
                    var totalPaid = await _context.InputPackageSubscriptionPayments
                        .Where(x =>
                            x.SubscriptionId == subscription.Id &&
                            x.PaymentType == PaymentType.Commitment &&
                            x.Status == TransactionStatus.Successful)
                        .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

                    var newTotal = totalPaid + request.Amount;

                    if (newTotal > subscription.CommitmentValue)
                        throw new Exception("Commitment payment exceeds required amount");

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

                    var updatedTotal = await _context.InputPackageSubscriptionPayments
                        .Where(x =>
                            x.SubscriptionId == subscription.Id &&
                            x.PaymentType == PaymentType.Commitment &&
                            x.Status == TransactionStatus.Successful)
                        .SumAsync(x => x.AmountPaid);

                    bool fullyPaid = updatedTotal >= subscription.CommitmentValue;

                    if (fullyPaid)
                    {
                        subscription.Status = SubscriptionStatus.PendingDisbursement;

                        var outboxEvent = new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            EventType = nameof(CommitmentFullyPaidEvent),
                            CreatedOn = DateTime.UtcNow,
                            IsProcessed = false,
                            Payload = JsonSerializer.Serialize(new CommitmentFullyPaidEvent
                            {
                                SubscriptionId = subscription.Id,
                                PackageId = subscription.InputPackageId,
                                TotalPaid = updatedTotal
                            })
                        };

                        _context.OutboxMessages.Add(outboxEvent);
                    }

                    _context.InputPackageSubscriptions.Update(subscription);

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
        **/
        public async Task<CommitmentReminderDto?> GetCommitmentReminderAsync(long subscriptionId)
        {
            var subscription = await _context.InputPackageSubscriptions
                .Include(x => x.InputPackage)
                .Include(x => x.SubscriberUser) 
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new Exception("Subscription not found");

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
                    RequiresReminder = false
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

        public async Task<long> RequestInputDisbursementApprovalAsync(
            long subscriptionId,
            decimal? requestedAmount,
            string? reference,
            InputSubscriptionRequestArgs args)
        {
            var subscription = await _context.InputPackageSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (subscription == null)
                throw new Exception("Subscription not found");

            if (subscription.Status != SubscriptionStatus.PendingDisbursement)
                throw new Exception("Not ready for disbursement");
            var request = new InputSubscriptionDisbursement
            {
                SubscriptionId = subscriptionId,
                RequestedAmount = subscription.TotalFinancingValue,
                RequestedByUserId = args.UserId,
                RequestCreatedOn= DateTimeOffset.Now,
                Reference= reference,
                Status = DisbursementStatus.PendingApproval,
            };
            _context.InputSubscriptionDisbursements.Add(request);

            await _context.SaveChangesAsync();

            return request.Id;
        }
        public async Task<bool> ApproveInputDisbursementRequestAsync(
            ApproveDisbursementRequestDto request,
            string approvedByUserId)
        {
            if (request == null)
                throw new Exception("Invalid request");

            var query = _context.InputSubscriptionDisbursements
                .Include(x => x.Subscription)
                    .ThenInclude(x => x.InputPackage)
                        .ThenInclude(x => x.RepaymentPlan)
                .AsQueryable();

            if (request.RequestId.HasValue)
            {
                query = query.Where(x => x.Id == request.RequestId.Value);
            }
            else if (request.SubscriptionId.HasValue)
            {
                query = query.Where(x => x.SubscriptionId == request.SubscriptionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.Reference))
            {
                query = query.Where(x => x.Reference == request.Reference);
            }
            else
            {
                throw new Exception("No valid identifier provided");
            }

            var disbursementRequest = await query.FirstOrDefaultAsync();

            if (disbursementRequest == null)
                throw new Exception("Disbursement request not found");

            if (disbursementRequest.Status != DisbursementStatus.PendingApproval)
                throw new Exception("Request already processed");

            disbursementRequest.Status = DisbursementStatus.Approved;
            disbursementRequest.ApprovedByUserId = approvedByUserId;
            disbursementRequest.ApprovedOn = DateTimeOffset.UtcNow;
            disbursementRequest.Subscription.Status = SubscriptionStatus.PendingDisbursement;
            disbursementRequest.ApprovedAmount = request.Amount.HasValue ? request.Amount.Value : disbursementRequest.RequestedAmount;
            
            await _context.SaveChangesAsync();

            return true;
        }
        //for this disbursement, the input might be physical commodity based on 
        //the input package configuration, so to disburse that kind of input, we 
        //need to deduct the disbursed input package items from the organization product in
        // the warehouse so as to track when there is no more inventory to fulfil disbursement
        //however, if it is just monetary, then you can log the disbursement amount.
        //rework this disburse subscription to cater for that

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
                            x.Id == request.SubscriptionId &&
                            x.SubscriberUserId == args.SubscriberUserId);

                    if (subscription == null)
                        throw new Exception("Subscription not found");

                    var approvedRequest = await _context.InputSubscriptionDisbursements
                        .FirstOrDefaultAsync(x =>
                            x.SubscriptionId == request.SubscriptionId &&
                            x.Status == DisbursementStatus.Approved);

                    if (approvedRequest == null)
                        throw new Exception("Disbursement not approved yet");

                    if (subscription.Status != SubscriptionStatus.PendingDisbursement)
                        throw new Exception("Subscription not ready for disbursement");

                    if (subscription.InputPackage.RepaymentPlan == null)
                        throw new Exception("Repayment plan is required for disbursement");

                    var alreadyDisbursed = await _context.InputSubscriptionDisbursements
                        .AnyAsync(x => x.SubscriptionId == subscription.Id);

                    if (alreadyDisbursed)
                        throw new Exception("Subscription already disbursed");

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

                            if (inventory.Quantity < item.Quantity)
                                throw new Exception($"Insufficient inventory for {inventory.OrganizationProduct.ProductName}");

                            inventory.Quantity -= item.Quantity;

                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                OrganizationProductId = inventory.OrganizationProductId,
                                Quantity = item.Quantity,
                                QuantityBefore = inventory.Quantity + item.Quantity,
                                QuantityAfter = inventory.Quantity,
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
                        throw new Exception("Subscription not found");

                    if (subscription.Status != SubscriptionStatus.Active)
                        throw new Exception("Repayment is only allowed for active subscriptions");

                    // 2. GET OUTSTANDING SCHEDULES
                    var schedules = await _context.RepaymentSchedules
                        .Where(x => x.InputPackageSubscriptionId == subscription.Id && !x.IsPaid)
                        .OrderBy(x => x.DueDate)
                        .ToListAsync();

                    if (!schedules.Any())
                        throw new Exception("No outstanding repayment schedules found");

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
                        subscription.Status = SubscriptionStatus.RepaymentCompleted;
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
                            IsCompleted = subscription.Status == SubscriptionStatus.RepaymentCompleted
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


        #endregion
        private IQueryable<InputPackageSubscription> BuildSubscriptionQueryObligation(
        SubscriptionQueryFilter filter)
        {
            var query = _context.InputPackageSubscriptions
                .Where(x=>x.BusinessCode == filter.BusinessCode)
                .Include(x => x.InputPackage)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SubscriberUserId))
            {
                query = query.Where(x => x.SubscriberUserId == filter.SubscriberUserId);
            }
            if (filter.PackageId.HasValue)
            {
                query = query.Where(x => x.InputPackageId == filter.PackageId);
            }
            if (filter.OrganizationId.HasValue)
            {
                query = query.Where(x => x.OrganizationId == filter.OrganizationId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            if (filter.HasPaidCommitment.HasValue)
            {
                if (filter.HasPaidCommitment.Value)
                {
                    query = query.Where(x =>
                        _context.InputPackageSubscriptionPayments
                            .Where(p =>
                                p.SubscriptionId == x.Id &&
                                p.PaymentType == PaymentType.Commitment &&
                                p.Status == TransactionStatus.Successful)
                            .Sum(p => (decimal?)p.AmountPaid) >= x.CommitmentValue);
                }
                else
                {
                    query = query.Where(x =>
                        _context.InputPackageSubscriptionPayments
                            .Where(p =>
                                p.SubscriptionId == x.Id &&
                                p.PaymentType == PaymentType.Commitment &&
                                p.Status == TransactionStatus.Successful)
                            .Sum(p => (decimal?)p.AmountPaid) < x.CommitmentValue);
                }
            }

            if (filter.IsActiveOnly == true)
            {
                query = query.Where(x => x.Status == SubscriptionStatus.Active);
            }

            return query;
        }
        private IQueryable<InputPackageSubscription> BuildSubscriptionQuery(
        SubscriptionQueryFilter filter)
        {
            var query = _context.InputPackageSubscriptions
                .Where(x => x.BusinessCode == filter.BusinessCode)
                .Include(x => x.InputPackage)
                .Include(x => x.SubscriberUser)
                .Include(x => x.AddedByUser)
                .AsQueryable();

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
                    case SubscriptionView.Approved:
                        query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingCommitment || x.Status == SubscriptionStatus.Approved);
                            break;
                    case SubscriptionView.PendingDisbursement:
                         query = query.Where(x =>
                            x.Status == SubscriptionStatus.PendingDisbursement 
                            //||
                           // x.Status == SubscriptionStatus.Approved
                            );
                        break;

                    //case SubscriptionView.Approved:
                    //    query = query.Where(x =>
                    //       x.Status == SubscriptionStatus.Approved);
                    //    break;
                    case SubscriptionView.Active:
                        query = query.Where(x => x.Status == SubscriptionStatus.Active);
                        break;

                    case SubscriptionView.Completed:
                        query = query.Where(x => x.Status == SubscriptionStatus.RepaymentCompleted);
                        break;

                    case SubscriptionView.Defaulted:
                        query = query.Where(x => x.Status == SubscriptionStatus.Defaulted);
                        break;

                }
            }

            if (filter.HasPaidCommitment.HasValue)
            {
                if (filter.HasPaidCommitment.Value)
                {
                    query = query.Where(x =>
                        _context.InputPackageSubscriptionPayments
                            .Where(p =>
                                p.SubscriptionId == x.Id &&
                                p.PaymentType == PaymentType.Commitment &&
                                p.Status == TransactionStatus.Successful)
                            .Sum(p => (decimal?)p.AmountPaid) >= x.CommitmentValue);
                }
                else
                {
                    query = query.Where(x =>
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
        public async Task<InputFinancingDashboardDto> GetInputFinancingDashboardAsync(
        SubscriptionQueryFilter filter)
        {
            var subscriptionsQuery = _context.InputPackageSubscriptions
                .Where(x => x.BusinessCode == filter.BusinessCode);

            // Apply organization filter if provided
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

            //var totalCommitted = await subscriptionsQuery
            //    .Where(x => x.CommitmentRequired)
            //    .SumAsync(x => (decimal?)x.CommitmentValue) ?? 0m;

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
        public async Task<PagedResult<SubscriptionDetailDto>> GetSubscriptionsAsync(
        SubscriptionQueryFilter filter,
        int pageNumber,
        int pageSize)
        {
            var query = BuildSubscriptionQuery(filter);

            var count = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SubscriptionDetailDto
                {
                    PackageId= x.InputPackageId,
                    SubscriptionId =x.Id,
                    SubscriberUserId= x.SubscriberUserId,
                    SubscriberName =
    x.SubscriberUser == null
        ? x.SubscriberUserId
        : !string.IsNullOrWhiteSpace(x.SubscriberUser.Name)
            ? x.SubscriberUser.Name
            : !string.IsNullOrWhiteSpace(x.SubscriberUser.PhoneNumber)
                ? x.SubscriberUser.PhoneNumber
                : x.SubscriberUser.Email,

                    CreatedBy =
    x.AddedByUser == null
        ? x.AddedByUserId
        : !string.IsNullOrWhiteSpace(x.AddedByUser.Name)
            ? x.AddedByUser.Name
            : !string.IsNullOrWhiteSpace(x.AddedByUser.PhoneNumber)
                ? x.AddedByUser.PhoneNumber
                : x.AddedByUser.Email,
                    SubscriptionType = x.SubscriberType,
                    Location = x.Organization != null ? x.Organization.CompanyName : null,

                    PackageName = x.InputPackage.Name,

                    CommitmentValue = x.CommitmentValue,
                    TotalFinancingValue = x.TotalFinancingValue,
                    SubscribedOn = x.SubscribedOn,
                    SubscriptionStatus = x.Status,
                    SubscriptionStatusName= EnumExtensions.GetDisplayName(x.Status),
                    CommitmentStatus = !x.CommitmentRequired || x.CommitmentValue <= 0
                                       ? "Not Required"
                                        : ((_context.InputPackageSubscriptionPayments
                .Where(p =>
                    p.SubscriptionId == x.Id &&
                    p.PaymentType == PaymentType.Commitment &&
                    p.Status == TransactionStatus.Successful)
                .Sum(p => (decimal?)p.AmountPaid) ?? 0m) >= x.CommitmentValue)
            ? "Paid"
            : "Pending"
                })
                .ToListAsync();

            return new PagedResult<SubscriptionDetailDto>
            {
                Items = items,
                Count = count,
                PageNumber = pageNumber,
                PageSize = pageSize
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
                throw new Exception("Subscription not found");

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

                    RepaymentDuration = RepaymentPeriod,//repaymentPlan?.RepaymentDuration ?? 0,

                    RepaymentModel =EnumExtensions.GetDisplayName(repaymentPlan?.Frequency),

                    RiskCategory =EnumExtensions.GetDisplayName(repaymentPlan?.RiskCategory)
                }
            };
        }
        public async Task<SubscriptionReviewResponseDto> GetSubscriptionDetail(
       SubscriptionQueryFilter filter
            )
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
                throw new Exception("Subscription not found");

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
                        : farmerOrg.IsActive
                            ? "Verified"
                            : "Inactive";
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
                        : memberOrg.IsActive
                            ? "Verified"
                            : "Inactive";
                }
            }
            var RepaymentPeriod = package.RepaymentPlan != null
                        ? GetRepaymentPeriod(package.RepaymentPlan.RepaymentFrequency, package.RepaymentPlan.RepaymentDuration)
                        : null;
            var repayment = package.RepaymentPlan;

            var subscriberInfo = new SubscriberInfoDto
            {
                Name = subscription.SubscriberUser?.Name
                       ?? subscription.SubscriberUser?.Email
                       ?? subscription.SubscriberUserId,

                Type = EnumExtensions.GetDisplayName(subscription.SubscriberType),

                Agent = subscription.AddedByUser?.Name
                        ?? subscription.AddedByUserId,

                Location = location,

                VerificationStatus = verificationStatus,

                EnrollmentDate = subscription.SubscribedOn
            };

            var response = new SubscriptionReviewResponseDto
            {
                Subscriber = subscriberInfo
            };

        //    if (
                //filter.ViewType == SubscriptionView.PendingCommitment ||
               // filter.ViewType == SubscriptionView.Approved ||
      //          filter.ViewType == SubscriptionView.Active)
    //        {
                response.Package = new PackageInfoDto
                {
                    PackageName = package?.Name,

                    TotalValue = package?.TotalFinancingValue ?? 0m,

                    CommitmentRequired = package?.CommitmentRequired ?? false,

                    CommitmentValue =
                        package?.CommitmentPlan?.CommitmentValue ?? 0m,

                    RepaymentDuration = RepaymentPeriod,
                        //package?.RepaymentPlan?.RepaymentDuration ?? 0,

                    RepaymentModel =
                        package?.RepaymentPlan?.RepaymentFrequency.ToString(),

                    RiskCategory =
                        package?.RepaymentPlan?.RiskCategory.ToString()
                };

//                return response;
  //          }
            var commitmentPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Commitment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var repaymentPaid = await _context.InputPackageSubscriptionPayments
                .Where(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.PaymentType == PaymentType.Repayment &&
                    x.Status == TransactionStatus.Successful)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

            var commitmentRequired =
                package?.CommitmentPlan?.CommitmentValue ?? 0m;

            var commitmentOutstanding =
                commitmentRequired - commitmentPaid;

            
            var repaymentValue =
  package.RepaymentRequired && package.RepaymentPlan != null
      ? package.RepaymentPlan.RepaymentValue
      : 0m;

            var repaymentOutstanding = repaymentValue - repaymentPaid;

            if (repaymentOutstanding < 0)
            {
                repaymentOutstanding = 0;
            }

            var commitmentDueDate =
                package?.CommitmentPlan?.CommitmentPaymentEndDate;

            int? commitmentDaysLeft = null;

            if (commitmentDueDate.HasValue)
            {
                commitmentDaysLeft =
                    (commitmentDueDate.Value.Date -
                     DateTimeOffset.UtcNow.Date).Days;
            }

            var repaymentEndDate =
                package?.RepaymentPlan?.RepaymentEndDate;

            int? repaymentDaysLeft = null;

            if (repaymentEndDate.HasValue)
            {
                repaymentDaysLeft =
                    (repaymentEndDate.Value.Date -
                     DateTimeOffset.UtcNow.Date).Days;
            }

            response.FinancingSnapshot = new FinancingSnapshotDto
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
                RepaymentDuration = RepaymentPeriod,
                RepaymentModel = EnumExtensions.GetDisplayName(package.RepaymentPlan?.RepaymentFrequency),

                RiskCategory =EnumExtensions.GetDisplayName(package?.RepaymentPlan?.RiskCategory)

            };

            response.RepaymentHistory =
                await _context.InputPackageSubscriptionPayments
                    .Include(x => x.RecordedByUser)
                    .Where(x =>
                        x.SubscriptionId == subscription.Id &&
                        x.PaymentType == PaymentType.Repayment &&
                        x.Status == TransactionStatus.Successful)
                    .OrderByDescending(x => x.PaidOn)
                    .Select(x => new RepaymentHistoryDto
                    {
                        PaymentDate = x.PaidOn,

                        AmountPaid = x.AmountPaid,

                        PaymentMethod = x.PaymentChannel.ToString(),

                        RecordedBy =
                            x.RecordedByUser != null
                                ? (!string.IsNullOrWhiteSpace(x.RecordedByUser.Name)
                                    ? x.RecordedByUser.Name
                                    : x.RecordedByUser.Email)
                                : null
                    })
                    .ToListAsync();

            return response;
        }
        public async Task<(byte[] FileBytes, string FileName)> DownloadCommissionLetterAsync(long subscriptionId)
        {
            var document = await _context.SubscriptionDocuments
                .FirstOrDefaultAsync(x =>
                    x.SubscriptionId == subscriptionId &&
                    x.DocumentType == DocumentType.CommissionLetter);

            if (document == null)
                throw new Exception("Commission letter not found for this subscription");

            if (string.IsNullOrWhiteSpace(document.FilePath))
                throw new Exception("Invalid document file path");

            if (!File.Exists(document.FilePath))
                throw new Exception("File no longer exists on server storage");

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
            var query = BuildSubscriptionQueryObligation(filter);

            var count = await query.CountAsync();

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
                    .Select(x => x.DueDate)
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
                Items = items,
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
                throw new Exception("Subscription not found");

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
                    InstallmentNo = x.InstallmentNumber,
                    DueDate = x.DueDate,
                    ExpectedAmount = x.Amount,
                    IsPaid = x.IsPaid
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
                Items = items,
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
                TotalRepaid=totalRepaid,
                TotalActiveLoans=totalActiveLoans,
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
                throw new Exception("Subscription not found");

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

                // Apply leftover pool first
                while (paymentPool > 0 && paid < installmentAmount)
                {
                    var needed = installmentAmount - paid;
                    var applied = Math.Min(paymentPool, needed);

                    paid += applied;
                    paymentPool -= applied;
                }

                // Consume payments
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
                Items = result,
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
                throw new Exception("Subscription not found");

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

        #region Input Subscription reporting

        public async Task<byte[]> ExportSubscriptionsAsync(
    SubscriptionQueryFilter filter,
    ExportFormat format)
        {
            var baseQuery = _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(x => x.BusinessCode == filter.BusinessCode)
                .Include(x => x.InputPackage)
                .Include(x => x.SubscriberUser)
                .Include(x => x.AddedByUser)
                .AsQueryable();
baseQuery = ApplyFilters(baseQuery, filter);
            var payments = _context.InputPackageSubscriptionPayments
                .AsNoTracking()
                .Where(p => p.Status == TransactionStatus.Successful)
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    CommitmentPaid = g
                        .Where(x => x.PaymentType == PaymentType.Commitment)
                        .Sum(x => (decimal?)x.AmountPaid) ?? 0m
                });

            var data = await (
                from sub in baseQuery
                join pay in payments
                    on sub.Id equals pay.SubscriptionId into payGroup
                from p in payGroup.DefaultIfEmpty()
                select new SubscriptionDetailDto
                {
                    PackageId = sub.InputPackageId,
                    SubscriptionId = sub.Id,
                    SubscriberUserId = sub.SubscriberUserId,

                    SubscriberName =
                        sub.SubscriberUser == null
                            ? sub.SubscriberUserId
                            : !string.IsNullOrWhiteSpace(sub.SubscriberUser.Name)
                                ? sub.SubscriberUser.Name
                                : !string.IsNullOrWhiteSpace(sub.SubscriberUser.PhoneNumber)
                                    ? sub.SubscriberUser.PhoneNumber
                                    : sub.SubscriberUser.Email,

                    CreatedBy =
                        sub.AddedByUser == null
                            ? sub.AddedByUserId
                            : !string.IsNullOrWhiteSpace(sub.AddedByUser.Name)
                                ? sub.AddedByUser.Name
                                : !string.IsNullOrWhiteSpace(sub.AddedByUser.PhoneNumber)
                                    ? sub.AddedByUser.PhoneNumber
                                    : sub.AddedByUser.Email,

                    SubscriptionType = sub.SubscriberType,
                    Location = sub.Organization != null ? sub.Organization.CompanyName : null,
                    PackageName = sub.InputPackage.Name,
                    CommitmentValue = sub.CommitmentValue,
                    TotalFinancingValue = sub.TotalFinancingValue,
                    SubscribedOn = sub.SubscribedOn,
                    SubscriptionStatus = sub.Status,

                    CommitmentStatus =
                        (p.CommitmentPaid >= sub.CommitmentValue)
                            ? "Paid"
                            : "Pending"
                })
                .ToListAsync();

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
                        query = query.Where(x => x.Status == SubscriptionStatus.RepaymentCompleted);
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

                            table.Cell().Text(item.SubscribedOn.HasValue ?  item.SubscribedOn.Value.ToString("yyyy-MM-dd") : null);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }  /**
         * PendingApproval
→ PendingCommitment
→ PendingDisbursement
→ Active
→ Completed
→ Defaulted
         **/
        #endregion
        private void EnsurePackageIsEditable(InputPackage package)
        {
            if (package == null)
                throw new Exception("Package not found");

            if (!package.IsActive)
            {
                throw new Exception("This package is not editable because it is deactivated.");
            }
        }
        private static string GetRepaymentPeriod(RepaymentFrequency frequency, int duration)
        {
            var unit = frequency switch
            {
                RepaymentFrequency.Weekly => "week",
                RepaymentFrequency.BiWeekly => "2 weeks",
                RepaymentFrequency.Monthly => "month",
                RepaymentFrequency.Quarterly => "quarter",
                RepaymentFrequency.Annually => "year",
                _ => "period"
            };

            // handle special case for BiWeekly to avoid "6 2 weeks"
            if (frequency == RepaymentFrequency.BiWeekly)
            {
                return $"{duration * 2} weeks";
            }

            var plural = duration > 1 && frequency != RepaymentFrequency.BiWeekly ? "s" : "";

            return $"{duration} {unit}{plural}";
        }

        #region EOP to Package Generation
        public async Task<EopResponseDto> CreateEopAsync(CreateEopRequest request)
        {
            if (request.CostItems == null || !request.CostItems.Any())
                throw new Exception("EOP must have at least one cost item");

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
        /**public async Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request)
        {
            var eop = await _context.EconomicsOfProductions
                .Include(x => x.CostItems)
                .FirstOrDefaultAsync(x => x.Id == request.EconomicsOfProductionId);

            if (eop == null)
                throw new Exception("EOP not found");

            if (!eop.CostItems.Any())
                throw new Exception("EOP has no cost items");

            // 🔒 Validate completeness
            ValidateEop(eop);

            var packageItems = new List<InputPackageItem>();
            var additionalCosts = new List<AdditionalInputPackageCost>();

            foreach (var cost in eop.CostItems)
            {
                var total = cost.Quantity * cost.UnitCost;

                if (cost.OrganizationProductId.HasValue)
                {
                    packageItems.Add(new InputPackageItem
                    {
                        OrganizationProductId = cost.OrganizationProductId.Value,
                        Quantity = cost.Quantity,
                        UnitCost = cost.UnitCost,
                        UnitOfMeasure = "Auto"
                    });
                }
                else
                {
                    additionalCosts.Add(new AdditionalInputPackageCost
                    {
                        Name = cost.Name,
                        Amount = total
                    });
                }
            }

            var totalInputCost = packageItems.Sum(x => x.Quantity * x.UnitCost);
            var totalAdditionalCost = additionalCosts.Sum(x => x.Amount);

            var totalPackageValue = totalInputCost + totalAdditionalCost;

            var repaymentValue = CalculateRepayment(
                totalPackageValue,
                request.ApplyInterest,
                request.InterestRate
            );

            var commitmentAmount = CalculateCommitment(
                totalPackageValue,
                request.CommitmentType,
                request.CommitmentValue,
                request.CommitmentRequired
            );

            var risk = CalculateRisk(eop);

            var package = new InputPackage
            {
                Name = eop.Name,
                OrganizationId = request.OrganizationId,
                InputFinancingCategoryId = request.CategoryId,
                SubScriptionType = request.SubscriptionType,

                PackageStructure = InputPackageStructure.Physical,

                InputPackageItems = packageItems,
                AdditionalCosts = additionalCosts,

                TotalPackageValue = totalPackageValue,
                RepaymentValue = repaymentValue,

                CommitmentRequired = request.CommitmentRequired,
                CommitmentType = request.CommitmentType,
                CommitmentValue = commitmentAmount,

                RiskCategory = risk,
                RepaymentMode = request.RepaymentMode,
                RepaymentDuration = request.RepaymentPeriodInMonths,

                InterestRatePercentage = request.InterestRate,
                IsInterestApplied = request.ApplyInterest,
                ScheduleType = RepaymentScheduleType.HarvestLinked,

                EconomicsOfProductionId = eop.Id,
                IsGeneratedFromEop = true,

                Currency = request.Currency,

                //Status = request.AutoApprove
                //    ? InputPackageStatus.Published
                //    : InputPackageStatus.Draft,

                Version = 1,
                CreatedOn = DateTimeOffset.UtcNow
            };

            _context.InputPackages.Add(package);
            await _context.SaveChangesAsync();

            return true;
        }
        **/
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
                Items = items,
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
                throw new Exception("EOP not found");

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
                throw new Exception("Invalid expected yield");

            if (eop.ExpectedMarketPrice <= 0)
                throw new Exception("Invalid market price");

            if (!eop.CostItems.Any(x => x.OrganizationProductId != null))
                throw new Exception("EOP must contain at least one input item");
        }
        #endregion EOP to Package Generation
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
    }
}
