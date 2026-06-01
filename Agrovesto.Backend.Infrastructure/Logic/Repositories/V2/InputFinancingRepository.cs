using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Events;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
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
        Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request);
        Task<EopDto> GetEopByIdAsync(long id);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetInputFinancingCategories(ListQueryParams query);
        Task<ProductCategoryDto> GetInputFinancingCategoryById(long id);
        Task<InputPackageDto> GetPackageByIdAsync(long id);
        Task<PagedResult<InputPackageDto>> GetPackagesAsync(InputPackageFilterRequest request);
    }
    public class InputFinancingRepository : IInputFinancingRepository
    {
        private readonly DataContext _context;

        public InputFinancingRepository(DataContext context)
        {
            _context = context;
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
                IsActive= true,
                UnitOfMeasure=request.UnitOfMeasure
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

                    if (isPhysical)
                    {
                        if (request.InputPackageItems == null || !request.InputPackageItems.Any())
                            throw new Exception("Physical package must have items");
                    }
                    else
                    {
                        if (request.InputPackageItems != null && request.InputPackageItems.Any())
                            throw new Exception("Non-physical package cannot have items");
                    }

                    var items = isPhysical
                        ? request.InputPackageItems.Select(x => new InputPackageItem
                        {
                            OrganizationProductId = x.OrganizationProductId,
                            Quantity = x.Quantity,
                            UnitCost = x.UnitCost
                        }).ToList()
                        : new List<InputPackageItem>();

                    var additionalCosts = request.AdditionalCosts?.Select(x => new AdditionalInputPackageCost
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
                        request.TotalPackageValue != itemsTotal)
                        throw new Exception("TotalPackageValue mismatch");

                    var totalPackageValue = itemsTotal;
                    var totalFinancingValue = totalPackageValue + additionalCostsTotal;

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
                        TotalFinancingValue = totalFinancingValue,
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
                        CreatedOn = DateTimeOffset.UtcNow
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

        //   public async Task<bool> CreateInputFinancingPackageConfig(
        //CreateInputPackageRequest request, InputPackageConfigArgs args)
        //   {
        //       if (request == null)
        //           throw new Exception("Request cannot be null");

        //       var isPhysical = request.PackageStructure == InputPackageStructure.Physical;

        //       if (isPhysical)
        //       {
        //           if (request.InputPackageItems == null || !request.InputPackageItems.Any())
        //               throw new Exception("Physical package must have items");
        //       }
        //       else
        //       {
        //           if (request.InputPackageItems != null && request.InputPackageItems.Any())
        //               throw new Exception("Non-physical package cannot have items");
        //       }

        //       var items = isPhysical
        //           ? request.InputPackageItems.Select(x => new InputPackageItem
        //           {
        //               OrganizationProductId = x.OrganizationProductId,
        //               Quantity = x.Quantity,
        //               UnitCost = x.UnitCost
        //           }).ToList()
        //           : new List<InputPackageItem>();

        //       var additionalCosts = request.AdditionalCosts?.Select(x => new AdditionalInputPackageCost
        //       {
        //           Name = x.Name,
        //           Description = x.Description,
        //           Amount = x.Amount,
        //           IsPerUnit = x.IsPerUnit,
        //           Quantity = x.Quantity,
        //           Type = x.Type
        //       }).ToList() ?? new List<AdditionalInputPackageCost>();

        //       var additionalCostsTotal = additionalCosts.Sum(x => x.Amount);

        //       decimal itemsTotal = 0;

        //       if (isPhysical)
        //       {
        //           itemsTotal = items.Sum(x => x.Quantity * x.UnitCost);

        //           if (request.TotalPackageValue > 0 &&
        //               Math.Round(request.TotalPackageValue, 2) != Math.Round(itemsTotal, 2))
        //           {
        //               throw new Exception(
        //                   $"TotalPackageValue mismatch. Expected {itemsTotal}, got {request.TotalPackageValue}");
        //           }
        //       }
        //       else
        //       {
        //           if (request.TotalPackageValue <= 0)
        //               throw new Exception("Monetary package must have TotalPackageValue > 0");

        //           itemsTotal = request.TotalPackageValue;
        //       }

        //       var totalPackageValue = itemsTotal;


        //       var totalFinancingValue = totalPackageValue + additionalCostsTotal;

        //       var commitment = request.CommitmentRequired
        //           ? (request.CommitmentType == CommitmentType.PercentageOfTotalValue
        //               ? totalPackageValue * request.CommitmentValue  //change to totalFinancingValue * request.CommitmentValue if commitment should be calculated on total financing value instead of total package value
        //               : request.CommitmentValue)
        //           : 0;

        //       var package = new InputPackage
        //       {
        //           OrganizationId = args.organizationId,
        //           CreatedByUserId = args.createdByUserId,
        //           Name = request.Name,

        //           InputFinancingCategoryId = request.InputFinancingCategoryId,
        //           SubScriptionType = request.SubScriptionType,
        //           InputTarget = request.InputTarget,
        //           InputSeason = request.InputSeason,

        //           PackageStructure = request.PackageStructure,

        //           InputPackageItems = items,
        //           AdditionalCosts = additionalCosts,

        //           TotalPackageValue = totalPackageValue,
        //           AdditionalCostAmount = additionalCostsTotal,

        //           TotalFinancingValue = totalFinancingValue,

        //           CommitmentRequired = request.CommitmentRequired,
        //           CommitmentType = request.CommitmentType,
        //           CommitmentValue = commitment,

        //           CommitmentPaymentStartDate = request.CommitmentPaymentStartDate,
        //           CommitmentPaymentEndDate = request.CommitmentPaymentEndDate.HasValue ? request.CommitmentPaymentEndDate.Value : null,

        //           UserWalletId = request.UserWalletId.HasValue ? request.UserWalletId.Value : null,
        //           WalletProviderId = request.PaybackWalletId,


        //           RepaymentFrequency = request.RepaymentFrequency,
        //           RepaymentDuration = request.RepaymentDuration,
        //           RepaymentStartDate = request.RepaymentStartDate.HasValue ? request.RepaymentStartDate.Value : null,
        //           RepaymentEndDate = request.RepaymentEndDate.HasValue ? request.RepaymentEndDate.Value : null,
        //           RepaymentGracePeriod = request.RepaymentGracePeriod.HasValue ? request.RepaymentGracePeriod.Value : null,
        //           RepaymentMode = request.RepaymentMode,
        //           RepaymentValue = request.RepaymentValue,

        //           RiskCategory = request.RiskCategory,

        //           IsSelectedWalletProvisioned = args.isSelectedWalletProvisioned,
        //           Currency = request.Currency,

        //           IsGeneratedFromEop = false,
        //           Version = 1,
        //           CreatedOn = DateTimeOffset.UtcNow
        //       };


        //       _context.InputPackages.Add(package);
        //       await _context.SaveChangesAsync();

        //       return true;
        //   }

        //update package configuration
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
                TotalCount = totalCount,
                Page = request.Page,
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
        public async Task<PagedResult<InputPackageDto>> GetPackagesAsync(InputPackageFilterRequest request)
        {
            var query = _context.InputPackages
                .Where(x => x.OrganizationId == request.OrganizationId && x.IsActive);

            //if (request.Status.HasValue)
            //    query = query.Where(x => x.IsActive == request.Status);

            if (request.IsGeneratedFromEop.HasValue)
                query = query.Where(x => x.IsGeneratedFromEop == request.IsGeneratedFromEop);

            if (request.CategoryId.HasValue)
                query = query.Where(x => x.InputFinancingCategoryId == request.CategoryId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new InputPackageDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    TotalPackageValue = x.TotalPackageValue,
                    RepaymentValue = x.RepaymentValue,
                  //  Status = x.Status,
                    IsGeneratedFromEop = x.IsGeneratedFromEop,
                    Currency = x.Currency
                })
                .ToListAsync();

            return new PagedResult<InputPackageDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        public async Task<InputPackageDto> GetPackageByIdAsync(long id)
        {
            var package = await _context.InputPackages
                .Include(x => x.InputPackageItems)
                .Include(x => x.AdditionalCosts)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (package == null)
                throw new Exception("Package not found");

            return new InputPackageDto
            {
                Id = package.Id,
                Name = package.Name,
                TotalPackageValue = package.TotalPackageValue,
                RepaymentValue = package.RepaymentValue,
               // Status = package.Status,
                IsGeneratedFromEop = package.IsGeneratedFromEop,
                Currency = package.Currency,

                Items = package.InputPackageItems.Select(i => new InputPackageItemDto
                {
                    OrganizationProductId = i.OrganizationProductId,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost
                }).ToList(),

                AdditionalCosts = package.AdditionalCosts.Select(c => new AdditionalCostDto
                {
                    Name = c.Name,
                    Amount = c.Amount
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
