using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Events;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
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
        Task<EopResponseDto> CreateEopAsync(CreateEopRequest request);
        Task<bool> CreateInputFinancingCategory(ProductCategoryDto dto);
        Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, InputPackageConfigArgs args);
        Task<bool> DeactivatePackageAsync(long packageId, long organizationId);
        Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request);
        Task<EopDto> GetEopByIdAsync(long id);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetInputFinancingCategories(ListQueryParams query);
        Task<ProductCategoryDto> GetInputFinancingCategoryById(long id);
        Task<InputPackageDto> GetPackageByIdAsync(long id);
        Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query);
        Task<bool> ReactivatePackageAsync(long packageId, long organizationId);
        Task<bool> RemoveItemFromPackage(long packageId, long itemId);
        Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs args);
        Task<bool> UpdateInputPackageConfig(long packageId, UpdateInputPackageConfigRequest request, InputPackageConfigArgs args);
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
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (request == null)
                        throw new Exception("Request cannot be null");

                    var isPhysical = request.PackageStructure == InputPackageStructure.Physical;

                    var requestItems = request.InputPackageItems ?? new List<CreateInputPackageItemRequest>();
                    var requestAdditionalCosts = request.AdditionalCosts ?? new List<CreateAdditionalCostRequest>();

                    if (isPhysical)
                    {
                        if (!requestItems.Any())
                            throw new Exception("Physical package must have items");
                    }
                    else
                    {
                        if (requestItems.Any())
                            throw new Exception("Non-physical package cannot have items");
                    }

                    var items = isPhysical
                        ? requestItems.Select(x => new InputPackageItem
                        {
                            OrganizationProductId = x.OrganizationProductId,
                            Quantity = x.Quantity,
                            UnitCost = x.UnitCost
                        }).ToList()
                        : new List<InputPackageItem>();

                    var additionalCosts = requestAdditionalCosts?.Select(x => new AdditionalInputPackageCost
                    {
                        Name = x.Name,
                        Description = x.Description,
                        Amount = x.Amount,
                        IsPerUnit = x.IsPerUnit,
                        Quantity = x.Quantity,
                        Type = x.Type
                    }).ToList() ?? new List<AdditionalInputPackageCost>();

                    var additionalCostsTotal = additionalCosts.Sum(x => x.Amount);

                    decimal itemsTotal = isPhysical
                        ? items.Sum(x => x.Quantity * x.UnitCost)
                        : request.TotalPackageValue;

                    if (isPhysical &&
                        request.TotalPackageValue > 0 &&
                        Math.Round(request.TotalPackageValue, 2) != Math.Round(itemsTotal, 2))
                        throw new Exception("TotalPackageValue mismatch");



                    var totalPackageValue = itemsTotal;
                    var totalFinancingValue = totalPackageValue + additionalCostsTotal;

                    if(request.TotalFinancingValue > 0 && Math.Round(request.TotalFinancingValue, 2) != Math.Round(totalFinancingValue, 2))
                        throw new Exception("TotalFinancingValue mismatch");

                    var commitment = request.CommitmentRequired
                       ? (request.CommitmentType == CommitmentType.PercentageOfTotalValue
                           ? totalPackageValue * (request.CommitmentValue / 100m)
                           : request.CommitmentValue)
                       : 0;


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
                        TotalFinancingValue = request.TotalFinancingValue > 0 ? request.TotalFinancingValue : totalFinancingValue,
                        CommitmentRequired = request.CommitmentRequired,
                        CommitmentType = request.CommitmentType,
                        CommitmentValue = commitment,
                        CommitmentPaymentStartDate = request.CommitmentPaymentStartDate,
                        CommitmentPaymentEndDate = request.CommitmentPaymentEndDate,
                        UserWalletId = request.UserWalletId,
                        WalletProviderId = request.PaybackWalletId,
                        RepaymentFrequency = request.RepaymentFrequency,
                        RepaymentDuration = request.RepaymentDuration,
                        RepaymentStartDate = request.RepaymentStartDate,
                        RepaymentEndDate = request.RepaymentEndDate,
                        RepaymentGracePeriod = request.RepaymentGracePeriod,
                        RepaymentMode = request.RepaymentMode,
                        RepaymentValue = request.RepaymentValue,
                        RiskCategory = request.RiskCategory,
                        IsSelectedWalletProvisioned = args.isSelectedWalletProvisioned,
                        Currency = request.Currency,
                        IsGeneratedFromEop = false,
                        Version = 1,
                        CreatedOn = DateTimeOffset.UtcNow,
                        IsActive = true
                    };

                    _context.InputPackages.Add(package);
                    await _context.SaveChangesAsync();

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
                catch(Exception ex)
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
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (request == null)
                        throw new Exception("Request cannot be null");

                    var package = await _context.InputPackages
                        .FirstOrDefaultAsync(x => x.Id == packageId
                                               && x.OrganizationId == args.organizationId);

                    if (package == null)
                        throw new Exception("Package not found");

                    if (request.CommitmentPaymentStartDate >= request.CommitmentPaymentEndDate)
                        throw new Exception("Invalid commitment payment date range");

                    if (request.RepaymentStartDate >= request.RepaymentEndDate)
                        throw new Exception("Invalid repayment date range");

                    if (request.RepaymentGracePeriod < request.RepaymentStartDate)
                        throw new Exception("Grace period cannot be before repayment start date");

                    if (!package.IsActive)
                        throw new Exception("Cannot update configuration for inactive package");

                    package.CommitmentPaymentStartDate = request.CommitmentPaymentStartDate;
                    package.CommitmentPaymentEndDate = request.CommitmentPaymentEndDate;

                    package.RepaymentFrequency = request.RepaymentFrequency;
                    package.RepaymentDuration = request.RepaymentDuration;

                    package.RepaymentStartDate = request.RepaymentStartDate;
                    package.RepaymentEndDate = request.RepaymentEndDate;
                    package.RepaymentGracePeriod = request.RepaymentGracePeriod;

                    package.UpdatedOn = DateTimeOffset.UtcNow;
                    package.UpdatedByUserId = args.createdByUserId;

                    await _context.SaveChangesAsync();

                    var scheduleRequest = new RepaymentEngineRequest
                    {
                        PackageId = package.Id,
                        Frequency = request.RepaymentFrequency,
                        Duration = request.RepaymentDuration,
                        StartDate = request.RepaymentStartDate,
                        EndDate = request.RepaymentEndDate,
                        GracePeriod = request.RepaymentGracePeriod,
                        TotalAmount = package.TotalFinancingValue
                    };

                    var schedule = _repaymentEngine.GenerateSchedule(scheduleRequest);
                    _context.RepaymentSchedules.AddRange(schedule);

                    await _context.SaveChangesAsync();
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
            var query = _context.InputPackages
                .Include(x => x.InputFinancingCategory)
                
                .Where(x => x.OrganizationId == request.OrganizationId
               // && x.IsActive
                );

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new InputPackageListModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    FinancingCategory= x.InputFinancingCategory.Name,
                    InputTarget= x.InputTarget,
                    TotalFinancingValue = x.TotalFinancingValue,
                    CommitmentValue = x.CommitmentValue,
                    RepaymentModel= EnumExtensions.GetDisplayName(x.RepaymentMode),
                    RiskCategory= EnumExtensions.GetDisplayName(x.RiskCategory),
                    Subscribers= 0,//update on subscribers added
                    IsActive= x.IsActive,
                    
                })
                .ToListAsync();

            return new PagedResult<InputPackageListModel>
            {
                Items = items,
                Count = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        public async Task<InputPackageDto> GetPackageByIdAsync(long id)
        {
            var package = await _context.InputPackages
                .Include(x=> x.InputFinancingCategory)
                .Include(x => x.InputPackageItems)
                .ThenInclude(x => x.OrganizationProduct)
                .Include(x => x.AdditionalCosts)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (package == null)
                return null;

            return new InputPackageDto
            {
                IsActive= package.IsActive,
               WalletProviderId= package.WalletProviderId ?? 0,
                Id = package.Id,
                Name = package.Name,
                IsRepaymentWalletConfigured =package.IsSelectedWalletProvisioned,
                SubscriptionType= EnumExtensions.GetDisplayName(package.SubScriptionType),
                InputSeason=package.InputSeason,
                InputTarget=package.InputTarget,
                FinancingCategory=package.InputFinancingCategory.Name,
                FinancialStructure = new InputPackageFinancingStructureDto
                {
                    TotalFinancingValue = package.TotalFinancingValue,
                    CommitmentRequired = package.CommitmentRequired,
                    CommitmentValue = package.CommitmentValue,                   
                    RepaymentEndDate=package.RepaymentEndDate,
                    RepaymentGracePeriod=package.RepaymentGracePeriod,
                    CommitmentPaymentStartDate=package.CommitmentPaymentStartDate,
                    CommitmentPaymentEndDate=package.CommitmentPaymentEndDate,
                    RepaymentModel= EnumExtensions.GetDisplayName(package.RepaymentMode),
                    RepaymentStartDate=package.RepaymentStartDate,
                    RepaymentValue= package.RepaymentValue,
                    RepaymentPeriod = GetRepaymentPeriod(package.RepaymentFrequency, package.RepaymentDuration),
                    RiskCategory = EnumExtensions.GetDisplayName(package.RiskCategory)
                },
                PackageInputs = package.InputPackageItems.Select(i => new InputPackageBreakdownDto
                {
                    OrganizationProductId = i.OrganizationProductId,
                    InputId=i.Id,
                    InputType = i.OrganizationProduct.ProductName,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    TotalCost = i.Quantity * i.UnitCost,
                    
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
                .FirstOrDefaultAsync(x => x.Id == packageId);

            EnsurePackageIsEditable(package);

            var item = await _context.InputPackageItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.InputPackageId == packageId);

            if (item == null)
                throw new Exception("Item not found in package");

            _context.InputPackageItems.Remove(item);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> ReplacePackageItemAsync(
        ReplacePackageItemRequest request,
        ReplacePackageItemRequestArgs args)
        {
            var package = await _context.InputPackages
                .FirstOrDefaultAsync(x => x.Id == args.PackageId);

            EnsurePackageIsEditable(package);

            var item = await _context.InputPackageItems
                .FirstOrDefaultAsync(x => x.Id == args.InputId && x.InputPackageId == args.PackageId);

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

            await _context.SaveChangesAsync();

            return true;
        }
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

        public async Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request)
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
