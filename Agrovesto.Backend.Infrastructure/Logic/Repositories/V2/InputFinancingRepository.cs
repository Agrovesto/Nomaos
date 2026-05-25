using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{

    public interface IInputFinancingRepository
    {
        Task<EopResponseDto> CreateEopAsync(CreateEopRequest request);
        Task<bool> CreateInputFinancingCategory(ProductCategoryDto dto);
        Task<bool> CreateManualInputPackageAsync(CreateInputPackageRequest request);
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
        public async Task<bool> CreateManualInputPackageAsync(CreateInputPackageRequest request)
        {
            if (request.Structure == InputPackageStructure.Physical &&
                (request.InputPackageItems == null || !request.InputPackageItems.Any()))
                throw new Exception("Physical package must have items");

            var items = request.InputPackageItems?.Select(x => new InputPackageItem
            {
                OrganizationProductId = x.OrganizationProductId,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost,
                UnitOfMeasure = x.UnitOfMeasure
            }).ToList() ?? new List<InputPackageItem>();

            var additionalCosts = request.AdditionalCosts?.Select(x => new AdditionalInputPackageCost
            {
                Name = x.Name,
                Description = x.Description,
                Amount = x.Amount,
                IsPerUnit= x.IsPerUnit,
                Quantity=x.Quantity,
                Type=x.Type
            }).ToList() ?? new List<AdditionalInputPackageCost>();
            var totalValue =
                items.Sum(x => x.Quantity * x.UnitCost) +
                additionalCosts.Sum(x => x.Amount);

            var repaymentValue = request.ApplyInterest
                ? totalValue + (totalValue * request.InterestRate)
                : totalValue;

            var commitment = request.CommitmentRequired
                ? (request.CommitmentType == CommitmentType.PercentageOfTotalValue
                    ? totalValue * request.CommitmentValue
                    : request.CommitmentValue)
                : 0;

            var package = new InputPackage
            {
                Name = request.Name,
                OrganizationId = request.OrganizationId,
                InputFinancingCategoryId = request.InputFinancingCategoryId,
                SubScriptionType= request.SubScriptionType,
                PackageStructure = request.Structure,

                InputPackageItems = items,
               
                CommitmentRequired = request.CommitmentRequired,
                CommitmentType = request.CommitmentType,
                CommitmentValue =request.CommitmentValue > 0 ? request.CommitmentValue : commitment,

                TotalPackageValue = request.TotalPackageValue > 0 ? request.TotalPackageValue : totalValue, // if totalvalue is supplied, TotalPackageValue = dto.TotalPackageValue, else calculate from items and additional costs
                UserWalletId = request.UserWalletId,

                AdditionalCosts = additionalCosts,                
          

                RepaymentMode = request.RepaymentMode,
                RepaymentValue = request.RepaymentValue > 0 ? request.RepaymentValue : repaymentValue,
                RepaymentPeriodInMonths = request.RepaymentPeriodInMonths,

                InterestRatePercentage = request.InterestRate,
                IsInterestApplied = request.ApplyInterest,

                Currency = request.Currency,

                IsGeneratedFromEop = false,
                Status = InputPackageStatus.Draft,
                Version = 1,
                CreatedOn = DateTimeOffset.UtcNow
            };

            _context.InputPackages.Add(package);
            await _context.SaveChangesAsync();

            return true;
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
                RepaymentPeriodInMonths = request.RepaymentPeriodInMonths,

                InterestRatePercentage = request.InterestRate,
                IsInterestApplied = request.ApplyInterest,
                ScheduleType = RepaymentScheduleType.HarvestLinked,

                EconomicsOfProductionId = eop.Id,
                IsGeneratedFromEop = true,

                Currency = request.Currency,

                Status = request.AutoApprove
                    ? InputPackageStatus.Published
                    : InputPackageStatus.Draft,

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

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status);

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
                    Status = x.Status,
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
                Status = package.Status,
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
