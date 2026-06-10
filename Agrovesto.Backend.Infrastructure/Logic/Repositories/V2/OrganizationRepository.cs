using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IOrganizationRepository
    {
        Task<LocationAgentsResponseDto> GetLocationAgentsAsync(long locationId);
        Task<bool> ReassignPrimaryAgentAsync(ReassignPrimaryAgentDto dto);
        Task<OrganizationLocation> CreateLocationAsync(CreateLocationDto model);
        Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data);
        Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model);
        Task<OrganizationLocation> CreateSubLocationAsync(CreateSubLocationDto dto);
        Task<OrganizationLocation> EditSubLocationAsync(EditSubLocationDto dto);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams args);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams args);
        Task<List<OrganizationMemberShipResponseModel>> GetCooperativesByOrganization(string organizationCode, ListQueryParams query);
        Task<OrganizationFarmerDetailResponse> GetFarmerDetailsAsync(ListQueryParams query);
        Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args);
        Task<PagedListResponse<List<LocationListResponseDto>>> GetLocationsAsync(ListQueryParams query);
        Task<LocationResponseDto> GetLocationByIdAsync(long id);
        Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args);
        Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args);
        Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(ListQueryParams args);
        Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(ListQueryParams query);
        Task<OrganizationRoleModel> GetRoleByIAsync(long Id);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<bool> ReassignAgentToLocation(OrganizationAgentArgs data);
        Task<OrganizationLocation> EditLocationAsync(EditLocationDto dto);
        Task DeleteLocationAsync(DeleteLocationDto dto);
        Task DeleteSubLocationAsync(DeleteLocationDto dto);
        Task<List<LocationResponseDto>> GetLocationTreeAsync(long organizationId);
        Task<PagedListResponse<List<LocationResponseDto>>> GetSubLocationsAsync(ListQueryParams query);
        Task<LocationResponseDto> GetSubLocationByIdAsync(long id);
        Task<PagedListResponse<List<SubLocationListDto>>> GetSubLocationsByParentAsync(long parentLocationId, ListQueryParams query);
        Task<bool> RemoveAgentFromLocationAsync(RemoveAgentFromLocationDto dto);
        Task<PagedListResponse<List<InventoryListDto>>> GetInventoryByLocationAsync(
    long locationId,
    ListQueryParams query);
        Task<LocationInventory> EditInventoryAsync(EditInventoryDto dto);
        Task<LocationInventory> AddInventoryAsync(CreateInventoryDto dto);
        Task<LocationDashboardFullDto> GetFullDashboardAsync(long organizationId);
        Task<ProductCategoryDto> GetCategoryById(long id);
        Task<PagedListResponse<List<ProductCategoryDto>>> GetCategories(ListQueryParams query);
        Task<OrganizationProductCategory> CreateCategory(ProductCategoryDto dto);
        Task<OrganizationProduct> CreateOrganizationProduct(OrganizationProductDto dto);
        Task<PagedListResponse<List<OrganizationProductDto>>> GetOrganizationProductList(ListQueryParams query);
        Task<OrganizationProductDto> GetorganizationProductById(long id);
        Task<bool> EditProductAsync(long id, EditOrganizationProductDto request);
        Task<bool> DeactivateProductAsync(long id);
        Task<List<InventoryTransactionDto>> GetInventoryTransactionsAsync(long inventoryId);
        Task DeleteInventory(DeleteLocationDto dto);
        Task SyncLocationStockLevels();
    }
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly DataContext _context;

        private readonly IDistributedCache _cache;
        public OrganizationRepository(DataContext context, IDistributedCache cache)
        {
            _context = context;

            _cache = cache;
        }
        #region agentviews
        //Rework this view to wire real data
        public async Task<PagedListResponse<List<OrganizationAgentActivityLog>>>
    GetAgentActivityLogs(
       ListQueryParams args)
        {

            var now = DateTimeOffset.Now;

            var data = new List<OrganizationAgentActivityLog>();
            //{
            //    new OrganizationAgentActivityLog
            //    {
            //        Id = 1,
            //        ActivityType = "Login",
            //        ActivityName = "Agent logged into system",
            //        TimeStamp = now.AddHours(-1)
            //    },
            //    new OrganizationAgentActivityLog
            //    {
            //        Id = 2,
            //        ActivityType = "Assignment",
            //        ActivityName = "Assigned to Location 8",
            //        TimeStamp = now.AddHours(-5)
            //    },
            //    new OrganizationAgentActivityLog
            //    {
            //        Id = 3,
            //        ActivityType = "Distribution",
            //        ActivityName = "Distributed fertilizer to farmer",
            //        TimeStamp = now.AddDays(-1)
            //    },
            //    new OrganizationAgentActivityLog
            //    {
            //        Id = 4,
            //        ActivityType = "Update",
            //        ActivityName = "Updated farmer subscription",
            //        TimeStamp = now.AddDays(-2)
            //    },
            //    new OrganizationAgentActivityLog
            //    {
            //        Id = 5,
            //        ActivityType = "Login",
            //        ActivityName = "Agent logged into mobile app",
            //        TimeStamp = now.AddDays(-3)
            //    }
            //};

            //if (!string.IsNullOrWhiteSpace(activityType))
            //{
            //    data = data
            //        .Where(x => x.ActivityType.Equals(activityType, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}
            if (!data.Any())
                return null;
            if (args.startDate.HasValue)
            {
                data = data
                    .Where(x => x.TimeStamp >= args.startDate.Value)
                    .ToList();
            }

            if (args.endDate.HasValue)
            {
                data = data
                    .Where(x => x.TimeStamp <= args.endDate.Value)
                    .ToList();
            }

            var totalCount = data.Count;

            var pagedData = data
                .OrderByDescending(x => x.TimeStamp)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentActivityLog>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>>
    GetAgentTransactionHistory(ListQueryParams args)
        {


            var data = new List<OrganizationAgentTransactionHistory>();
            //{
            //    new OrganizationAgentTransactionHistory
            //    {
            //        Id = 1,
            //        TransactionType = "Allocation",
            //        FarmerName = $"Farmer {args.Id}-A",
            //        FarmerUserId = $"FRM{args.Id}01",
            //        InputType = "Fertilizer",
            //        Quantity = 50 * factor,
            //        QuantityType = "Bags",
            //        Amount = 25000 * factor,
            //        Status = "Completed"
            //    },
            //    new OrganizationAgentTransactionHistory
            //    {
            //        Id = 2,
            //        TransactionType = "Distribution",
            //        FarmerName = $"Farmer {args.Id}-B",
            //        FarmerUserId = $"FRM{args.Id}02",
            //        InputType = "Seeds",
            //        Quantity = 30 * factor,
            //        QuantityType = "Kg",
            //        Amount = 12000 * factor,
            //        Status = factor % 2 == 0 ? "Pending" : "Completed"
            //    },
            //    new OrganizationAgentTransactionHistory
            //    {
            //        Id = 3,
            //        TransactionType = "Return",
            //        FarmerName = $"Farmer {args.Id}-C",
            //        FarmerUserId = $"FRM{args.Id}03",
            //        InputType = "Pesticide",
            //        Quantity = 10 * factor,
            //        QuantityType = "Litres",
            //        Amount = 5000 * factor,
            //        Status = "Failed"
            //    },
            //    new OrganizationAgentTransactionHistory
            //    {
            //        Id = 4,
            //        TransactionType = "Distribution",
            //        FarmerName = $"Farmer {args.Id}-D",
            //        FarmerUserId = $"FRM{args.Id}04",
            //        InputType = "Fertilizer",
            //        Quantity = 20 * factor,
            //        QuantityType = "Bags",
            //        Amount = 10000 * factor,
            //        Status = "Completed"
            //    }
            //};

            if (!data.Any())
                return null;

            //if (args.Status.HasValue)
            //{
            //    data = data
            //        .Where(x => x.Status.Equals(args.Status.Value, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            //if (!string.IsNullOrWhiteSpace(transactionType))
            //{
            //    data = data
            //        .Where(x => x.TransactionType.Equals(transactionType, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            var totalCount = data.Count;

            var pagedData = data
                .OrderByDescending(x => x.Id) // mimic real-world sorting (latest first)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentTransactionHistory>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (agent == null)
                return null;

            var orgAgents = await _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.AgentId == args.Id && x.IsActive)
                .ToListAsync();

            if (!orgAgents.Any())
                return null;

            var orgAgentIds = orgAgents.Select(x => x.Id).ToList();
            var subscribers = 0; //replace when there is data.

            var inputs = 0;//replace when there is data

            var totalEquityPaid = 0;// replace when there is data
            var productSold = 0;//replace when there is data
            return new OrganizationAgentPerformanceSummary
            {
                ProductSold = productSold,
                InputDisbursed = inputs,
                Subscribers = subscribers,
                TotalEquityPaid = totalEquityPaid,
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args)
        {


            var subscribers = new List<OrganizationAgentSubscriberModel>();
            //{
            //    new OrganizationAgentSubscriberModel
            //    {
            //        Id = 1,
            //        FarmerName = $"Farmer {args.Id }-A",
            //        FarmerUserId = $"FRM{args.Id }01",
            //        InputPackage = "Maize Package",
            //        InputPackageId = 101,
            //        EquityPaid = 10000 * factor,
            //        Status = true
            //    },
            //    new OrganizationAgentSubscriberModel
            //    {
            //        Id = 2,
            //        FarmerName = $"Farmer {args.Id }-B",
            //        FarmerUserId = $"FRM{args.Id }02",
            //        InputPackage = "Rice Package",
            //        InputPackageId = 102,
            //        EquityPaid = 15000 * factor,
            //        Status = factor % 2 == 0
            //    },
            //    new OrganizationAgentSubscriberModel
            //    {
            //        Id = 3,
            //        FarmerName = $"Farmer {args.Id }-C",
            //        FarmerUserId = $"FRM{args.Id }03",
            //        InputPackage = "Cassava Package",
            //        InputPackageId = 103,
            //        EquityPaid = 8000 * factor,
            //        Status = true
            //    }
            //};

            if (!subscribers.Any())
                return null;
            if (args.Status.HasValue)
            {
                subscribers = subscribers.Where(x => x.Status == args.Status.Value).ToList();
            }

            var totalCount = subscribers.Count;

            var pagedData = subscribers
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentSubscriberModel>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>>
    GetAgentInputAllocations(ListQueryParams args)
        {


            var data = new List<OrganizationAgentInputAllocationModel>();
            //{
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 1,
            //        InputName = "Fertilizer",
            //        Allocated = 100 * baseValue,
            //        Distributed = 60 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 2,
            //        InputName = "Seeds",
            //        Allocated = 80 * baseValue,
            //        Distributed = 30 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 3,
            //        InputName = "Pesticides",
            //        Allocated = 50 * baseValue,
            //        Distributed = 20 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 4,
            //        InputName = "Herbicides",
            //        Allocated = 30 * baseValue,
            //        Distributed = 10 * baseValue,
            //    }
            //};
            if (!data.Any())
                return null;
            data.ForEach(x => x.Remaining = x.Allocated - x.Distributed);

            int pageNumber = args.PageNumber > 0 ? args.PageNumber : 1;
            int pageSize = args.PageSize > 0 ? args.PageSize : 10;

            int totalCount = data.Count;

            var pagedData = data
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentInputAllocationModel>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }

        #endregion agentviews

        #region OrganizationProduct

       
        public async Task<OrganizationProductCategory> CreateCategory(ProductCategoryDto dto)
        {
            var exists = await _context.OrganizationProductCategories.AnyAsync(x =>
                x.CategoryName == dto.CategoryName &&
                x.BusinessCode == dto.BusinessCode);

            if (exists)
                throw new Exception("Product Category already exists.");


            var cat = new OrganizationProductCategory
            {
                BusinessCode = dto.BusinessCode,

                CategoryName = dto.CategoryName,
                IsActive = true,
                CreatedOn = DateTimeOffset.Now,
            };

            _context.OrganizationProductCategories.Add(cat);
            await _context.SaveChangesAsync();

            return cat;
        }
        public async Task<PagedListResponse<List<ProductCategoryDto>>> GetCategories(ListQueryParams query)
        {
            var baseQuery = _context.OrganizationProductCategories
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
                    CategoryName = x.CategoryName,
                    BusinessCode=x.BusinessCode
                  
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
        public async Task<ProductCategoryDto> GetCategoryById(long id)
        {
            var cat = await _context.OrganizationProductCategories
                           .Where(x => x.IsActive && x.Id == id)
                          
                           .FirstOrDefaultAsync();

            if (cat == null)
                throw new Exception("Product Category not found.");

            return new ProductCategoryDto
            {
                Id = cat.Id,
                CategoryName = cat.CategoryName,
                BusinessCode = cat.BusinessCode
            };
        }

        public async Task<OrganizationProduct> CreateOrganizationProduct(OrganizationProductDto dto)
        {
            var location = await _context.OrganizationProductCategories
               .FirstOrDefaultAsync(x => x.Id == dto.ProductCategoryId && x.IsActive);

            if (location == null)
                throw new Exception("Category not found.");

            var exists = await _context.OrganizationProducts.AnyAsync(x =>
                x.ProductName == dto.ProductName &&
                x.ProductCode == dto.ProductCode
                && x.OrganizationId == dto.OrganizationId
                && x.Season == dto.Season
                && x.OrganizationProductCategoryId == dto.ProductCategoryId
                && x.BusinessCode == dto.BusinessCode);

            if (exists)
                throw new Exception("Product already exists.");

            var prod = new OrganizationProduct
            {
                CreatedOn = DateTimeOffset.Now,
                OrganizationId = dto.OrganizationId,
                OrganizationProductCategoryId=dto.ProductCategoryId,
                ProductName=dto.ProductName,
                ProductCode=dto.ProductCode,
                Season= dto.Season,
                BusinessCode = dto.BusinessCode,
                IsActive = true,
                UnitType=dto.UnitType,
                MaximumOrderQuantity=dto.MaximumOrderQuantity,
                MinimumOrderQuantity=dto.MinimumOrderQuantity,
                Description=dto.Description
            };

            _context.OrganizationProducts.Add(prod);
            await _context.SaveChangesAsync();

            return prod;
        }
        public async Task<PagedListResponse<List<OrganizationProductDto>>> GetOrganizationProductList(
        ListQueryParams query)
        {
            var baseQuery = _context.OrganizationProducts
    .Where(li => li.OrganizationId == query.Id && li.IsActive);


            var totalCount = await baseQuery.CountAsync();


            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 50 : query.PageSize;

            var data = await baseQuery
     .OrderByDescending(li => li.CreatedOn)
     .Skip((pageNumber - 1) * pageSize)
     .Take(pageSize)
     .Select(li => new OrganizationProductDto
     {
         Id = li.Id,
         ProductCategory=li.OrganizationProductCategory.CategoryName,
         ProductCode = li.ProductCode,
         BusinessCode=li.BusinessCode,
         CreatedOn=li.CreatedOn,
         IsActive=li.IsActive,
         ProductCategoryId=li.OrganizationProductCategoryId,
         ProductName=li.ProductName,
         Season=li.Season,
         UnitType=li.UnitType,
         Description=li.Description,
         MaximumOrderQuantity=li.MaximumOrderQuantity,
         MinimumOrderQuantity=li.MinimumOrderQuantity
     })
     .ToListAsync();

            return new PagedListResponse<List<OrganizationProductDto>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<OrganizationProductDto> GetorganizationProductById(long id)
        {
            var product = await _context.OrganizationProducts
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new OrganizationProductDto
                {
                    Id = p.Id,
                    ProductCategory = p.OrganizationProductCategory.CategoryName,
                    ProductCode = p.ProductCode,
                    BusinessCode = p.BusinessCode,
                    CreatedOn = p.CreatedOn,
                    IsActive = p.IsActive,
                    ProductCategoryId = p.OrganizationProductCategoryId,
                    ProductName = p.ProductName,
                    Season = p.Season,
                    UnitType = p.UnitType,
                    Description=p.Description,
                    MaximumOrderQuantity=p.MaximumOrderQuantity,
                    MinimumOrderQuantity=p.MinimumOrderQuantity
                })
                .FirstOrDefaultAsync();

            return product;
        }

        public async Task<bool> EditProductAsync(long id, EditOrganizationProductDto request)
        {
            var product = await _context.OrganizationProducts
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                throw new KeyNotFoundException("Product not found or has been deactivated.");
            }


            if (!string.IsNullOrWhiteSpace(request.ProductName))
                product.ProductName = request.ProductName;

            if (!string.IsNullOrWhiteSpace(request.ProductCode))
                product.ProductCode = request.ProductCode;

            if (!string.IsNullOrWhiteSpace(request.BusinessCode))
                product.BusinessCode = request.BusinessCode;

            if (request.ProductCategoryId.HasValue)
                product.OrganizationProductCategoryId = request.ProductCategoryId.Value;

            if (!string.IsNullOrWhiteSpace(request.Season))
                product.Season = request.Season;

            if (!string.IsNullOrWhiteSpace(request.UnitType))
                product.UnitType = request.UnitType;
            if(request.MinimumOrderQuantity > 0)
            {
                product.MinimumOrderQuantity = request.MinimumOrderQuantity;
            }
            if(request.MaximumOrderQuantity > 0)
            {
                product.MaximumOrderQuantity = request.MaximumOrderQuantity;
            }
            if (!string.IsNullOrEmpty(request.Description))
                product.Description = request.Description;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> DeactivateProductAsync(long id)
        {
            var product = await _context.OrganizationProducts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                throw new KeyNotFoundException("Product not found.");
            }

            if (!product.IsActive)
            {
                return false;
            }

            product.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
        
        #endregion OrganizationProduct

        public async Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.Id <= 0)
                throw new ValidationException("Invalid Organization");

            var organizationId = args.Id;

            var result = await _context.Database
                .SqlQueryRaw<OrganizationDashboardResponse>(
        @"
SELECT
    (SELECT COUNT(DISTINCT AgentId)
     FROM OrganizationAgents
     WHERE OrganizationId = {0} AND IsActive = 1) AS TotalAgents,

    (SELECT COUNT(*)
     FROM OrganizationLocations
     WHERE OrganizationId = {0} AND IsActive = 1) AS TotalActiveLocations,

    CAST(0 AS decimal(18,2)) AS TotalEquityPaid,
 CAST(0 AS decimal(18,2)) AS TotalInputDisbursed,
    CAST(0 AS decimal(18,2)) AS TotalProduceSold
", organizationId)
                .FirstOrDefaultAsync();

            return result ?? new OrganizationDashboardResponse();
        }
        public async Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args)
        {
            var query = _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.LocationId == args.Id && x.IsActive);

            var totalCount = await query.CountAsync();

            var pagedOrgAgents = await query
                .OrderByDescending(x => x.Id)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            if (!pagedOrgAgents.Any())
            {
                return new PagedListResponse<List<LocationAgentDetailResponse>>
                {
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize,
                    Count = totalCount,
                    Data = new List<LocationAgentDetailResponse>()
                };
            }

            var orgAgentIds = pagedOrgAgents.Select(x => x.Id).ToList();
            var agentIds = pagedOrgAgents.Select(x => x.AgentId).ToList();

            var agents = await _context.Agents
                .AsNoTracking()
                .Where(x => agentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var roles = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => orgAgentIds.Contains(x.OrganizationAgentId))
                .GroupBy(x => x.OrganizationAgentId)
                .Select(g => new
                {
                    OrganizationAgentId = g.Key,
                    RoleId = g.Select(x => x.RoleId).FirstOrDefault()
                })
                .ToListAsync();

            var roleIds = roles.Select(x => x.RoleId).ToList();

            var roleDict = await _context.OrganizationRoles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RoleName);

            //// MOCK: Subscribers
            //var subscriberDict = pagedOrgAgents.ToDictionary(
            //    x => x.Id,
            //    x => new Random(x.Id.GetHashCode()).Next(5, 50)
            //);

            //// MOCK: Disbursed
            //var disbursedDict = pagedOrgAgents.ToDictionary(
            //    x => x.Id,
            //    x => (double)new Random(x.Id.GetHashCode()).Next(100, 1000)
            //);

            var result = pagedOrgAgents.Select(oa =>
            {
                var agent = agents.GetValueOrDefault(oa.AgentId);

                var roleEntry = roles.FirstOrDefault(r => r.OrganizationAgentId == oa.Id);

                var roleName = roleEntry != null && roleDict.ContainsKey(roleEntry.RoleId)
                    ? roleDict[roleEntry.RoleId]
                    : "N/A";

                return new LocationAgentDetailResponse
                {
                    AgentId = agent?.Id ?? 0,
                    AgentName = agent?.Name,
                    AgentRole = roleName,
                    TotalSubscribers = 0,//subscriberDict.GetValueOrDefault(oa.Id, 0),
                    TotalInputDisbursed = 0,//disbursedDict.GetValueOrDefault(oa.Id, 0),
                    IsActive = oa.IsActive
                };
            }).ToList();

            return new PagedListResponse<List<LocationAgentDetailResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }

        public async Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args)
        {
            var pageSize = args.PageSize <= 0 ? 10 : args.PageSize;
            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;

            var location = await _context.OrganizationLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (location == null)
                throw new NotFoundException("Location not found");

            // MOCK: Input types (since no DB yet)
            var mockInputs = new List<LocationInventoryBreakdownResponse>();
            //{
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 1,
            //        InputName = "Fertilizer",
            //        TotalStock = location.StockLevel * 0.5
            //    },
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 2,
            //        InputName = "Seeds",
            //        TotalStock = location.StockLevel * 0.3
            //    },
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 3,
            //        InputName = "Pesticides",
            //        TotalStock = location.StockLevel * 0.2
            //    }
            //};
            if (!mockInputs.Any())
                return null;

            // MOCK: Allocation per input (simulate assigned to agents)
            var allocationDict = mockInputs.ToDictionary(
                x => x.InputId,
                x => new Random((int)x.InputId * (int)location.Id).Next(10, 100)
            );

            //  Calculate allocation + available balance
            mockInputs.ForEach(x =>
            {
                x.QuantityAllocated = allocationDict.GetValueOrDefault(x.InputId, 0);

                //  Available = TotalStock - Allocated
                x.AvailableBalance = x.TotalStock - x.QuantityAllocated;

                // Prevent negative (just in case mock overshoots)
                if (x.AvailableBalance < 0)
                    x.AvailableBalance = 0;
            });

            var totalCount = mockInputs.Count;

            var pagedData = mockInputs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<LocationInventoryBreakdownResponse>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }

        public async Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args)
        {
            var pageSize = args.PageSize <= 0 ? 10 : args.PageSize;
            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;

            var location = await _context.OrganizationLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (location == null)
                throw new NotFoundException("Location not found");

            //  MOCK: Activity logs
            var mockLogs = new List<LocationActivityLogResponse>();
            //{
            //    new LocationActivityLogResponse
            //    {
            //        Id = 1,
            //        ActivityType = "Agent Assignment",
            //        Name = "Agent Assigned",
            //        Description = "Agent John Doe was assigned to this location",
            //        TimeStamp = DateTimeOffset.Now.AddDays(-1)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 2,
            //        ActivityType = "Input Disbursement",
            //        Name = "Fertilizer Distributed",
            //        Description = "50 units of fertilizer disbursed by Agent Jane",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-10)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 3,
            //        ActivityType = "Stock Update",
            //        Name = "Inventory Updated",
            //        Description = "Stock level updated after new supply",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-5)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 4,
            //        ActivityType = "Agent Update",
            //        Name = "Agent Reassigned",
            //        Description = "Agent moved to another location",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-2)
            //    }
            //};
            if (!mockLogs.Any())
                return null;

            // Optional filtering (by activity type if passed in query.Search)
            //if (!string.IsNullOrWhiteSpace(args.Search))
            //{
            //    mockLogs = mockLogs
            //        .Where(x => x.ActivityType.Contains(args.Search, StringComparison.OrdinalIgnoreCase) ||
            //                    x.Name.Contains(args.Search, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            // 📄 Pagination
            var totalCount = mockLogs.Count;

            var pagedData = mockLogs
                .OrderByDescending(x => x.TimeStamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<LocationActivityLogResponse>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<bool> ReassignAgentToLocation(OrganizationAgentArgs data)
        {
            var existingAgent = await _context.Agents
                .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (existingAgent == null)
                throw new Exception("Agent not found");

            if (!data.OrganizationId.HasValue)
                throw new Exception("OrganizationId is required");

            var existingAssignment = await _context.OrganizationAgents
                .FirstOrDefaultAsync(x =>
                    x.AgentId == existingAgent.Id &&
                    x.OrganizationId == data.OrganizationId &&
                    x.IsActive);

            if (existingAssignment == null)
            {
                await _context.OrganizationAgents.AddAsync(new OrganizationAgent
                {
                    AgentId = existingAgent.Id,
                    OrganizationId = data.OrganizationId.Value,
                    AssignedByUserId = data.OnboardedById,
                    AssignedOn = DateTimeOffset.Now,
                    LocationId = data.LocationId,
                    IsActive = true
                });
            }
            else
            {
                existingAssignment.LocationId = data.LocationId;
                existingAssignment.AssignedOn = DateTimeOffset.Now;
                existingAssignment.AssignedByUserId = data.OnboardedById;

                _context.OrganizationAgents.Update(existingAssignment);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == args.UserId);

            if (agent == null)
                return null;

            var orgAgents = await _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.AgentId == agent.Id && x.IsActive)
                .ToListAsync();

            if (orgAgents.Count == 0)
                return null;

            var orgAgentIds = orgAgents.Select(x => x.Id).ToList();

            var locationIds = orgAgents
                .Where(x => x.LocationId > 0)
                .Select(x => x.LocationId)
                .Distinct()
                .ToList();
            var locations = await (
                from loc in _context.OrganizationLocations.AsNoTracking()
                where locationIds.Contains(loc.Id)

                join lt in _context.LocationTypes.AsNoTracking()
                    on loc.LocationTypeId equals lt.Id into ltJoin
                from lt in ltJoin.DefaultIfEmpty()

                join st in _context.States.AsNoTracking()
                    on loc.StateId equals st.StateId into stJoin
                from st in stJoin.DefaultIfEmpty()

                join lga in _context.LGAs.AsNoTracking()
                    on loc.LgaId equals lga.LgaId into lgaJoin
                from lga in lgaJoin.DefaultIfEmpty()

                select new OrganizationLocationDto
                {
                    LocationId = loc.Id,
                    LocationName = loc.LocationName,

                    LocationType = lt != null ? lt.Type : null,
                    State = st != null ? st.State : null,
                    Lga = lga != null ? lga.Name : null
                }
            )
            .ToListAsync();

            var roles = await (
                from r in _context.OrganizationAgentRoles.AsNoTracking()
                where orgAgentIds.Contains(r.OrganizationAgentId)

                join rr in _context.OrganizationRoles.AsNoTracking()
                    on r.RoleId equals rr.Id

                select new OrganiationRoleDto
                {
                    RoleId = rr.Id,
                    RoleName = rr.RoleName
                }
            )
            .Distinct()
            .ToListAsync();
            return new OrganizationAgentDetailResponse
            {
                AgentId = agent.Id,
                UserId = agent.UserId,
                Name = agent.Name,
                Email = agent.Email,
                PhoneNumber = agent.PhoneNumber,
                State = agent.State,
                Lga = agent.Lga,
                Ward = agent.Ward,
                OnBoardedOn = agent.OnBoardedOn,

                Locations = locations,
                Roles = roles
            };
        }
        public async Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(
       ListQueryParams query)
        {
            var result = await
            (
                from m in _context.MemberShips.AsNoTracking()

                join om in _context.OrganizationMemberships.AsNoTracking()
                    on m.Id equals om.MembershipId

                join o in _context.Organizations.AsNoTracking()
                    on om.OrganizationId equals o.OrganizationId

                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on m.Id equals fc.CooperativeId into farmerJoin
                from fc in farmerJoin.DefaultIfEmpty()

                join f in _context.Farmers.AsNoTracking()
                    on fc.FarmerId equals f.Id into farmerDetailJoin
                from f in farmerDetailJoin.DefaultIfEmpty()

                where o.OrganizationCode == query.organizationCode

                group f by 1 into g
                select new OrganizationMembershipDashboardResponse
                {
                    TotalMemberships =
                        (from m2 in _context.MemberShips
                         join om2 in _context.OrganizationMemberships on m2.Id equals om2.MembershipId
                         join o2 in _context.Organizations on om2.OrganizationId equals o2.OrganizationId
                         where o2.OrganizationCode == query.organizationCode
                         select m2.Id).Distinct().Count(),

                    TotalFarmers = g.Count(x => x != null),

                    TotalVerifiedFarmers = g.Count(x => x != null && x.Level != FarmerLevel.Level1),

                    TotalUnverifiedFarmers = g.Count(x => x != null && x.Level == FarmerLevel.Level1),
                    TotalInputDisbursed = 0,
                    TotalRecoveryAmount = 0
                }
            )
            .FirstOrDefaultAsync();

            return result ?? new OrganizationMembershipDashboardResponse();
        }
        public async Task<List<OrganizationMemberShipResponseModel>> GetCooperativesByOrganization(
      string organizationCode,
      ListQueryParams query)
        {
            var result = await (
                from m in _context.MemberShips.AsNoTracking()

                join om in _context.OrganizationMemberships.AsNoTracking()
                    on m.Id equals om.MembershipId

                join o in _context.Organizations.AsNoTracking()
                    on om.OrganizationId equals o.OrganizationId

                join mt in _context.MemberShipTypes.AsNoTracking()
                    on m.MemberShipTypeId equals mt.Id into mtJoin
                from mt in mtJoin.DefaultIfEmpty()

                where o.OrganizationCode == organizationCode

                select new OrganizationMemberShipResponseModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Location = m.Location,
                    State = m.State,
                    Lga = m.Lga,
                    Ward = m.Ward,
                    OnBoardedOn = m.OnBoardedOn,

                    ContactPerson =
                        m.MembershipLeaderLastName + " " +
                        m.MembershipLeaderFirstName,

                    PhoneNumber = m.PhoneNumber,
                    UserId = m.UserId,

                    Tier = EnumExtensions.GetDisplayName(m.TierLevel),
                    MembershipType = mt != null ? mt.Name : null,

                    // ============================
                    // FARMER COUNTS (SAFE + CONSISTENT)
                    // ============================

                    TotalFarmers =
                        _context.FarmerCooperatives
                            .Where(fc => fc.CooperativeId == m.Id)
                            .Select(fc => fc.FarmerId)
                            .Distinct()
                            .Count(),

                    TotalVerifiedFarmers =
                        (from fc in _context.FarmerCooperatives
                         join f in _context.Farmers
                             on fc.FarmerId equals f.Id
                         where fc.CooperativeId == m.Id
                               && f.Level != FarmerLevel.Level1
                         select fc.FarmerId)
                        .Distinct()
                        .Count(),

                    TotalUnverifiedFarmers =
                        (
                            _context.FarmerCooperatives
                                .Where(fc => fc.CooperativeId == m.Id)
                                .Select(fc => fc.FarmerId)
                                .Distinct()
                                .Count()
                        )
                        -
                        (
                            from fc in _context.FarmerCooperatives
                            join f in _context.Farmers
                                on fc.FarmerId equals f.Id
                            where fc.CooperativeId == m.Id
                                  && f.Level != FarmerLevel.Level1
                            select fc.FarmerId
                        )
                        .Distinct()
                        .Count(),

                    HasbeenAssignedAgent =
                        _context.MemberShipAgents
                            .Any(a => a.MemberShipId == m.Id),

                    TotalInputReceived = 0,
                    RecoveryAmount = 0
                }
            )
            .OrderByDescending(x => x.OnBoardedOn)
            .ToListAsync();

            return result;
        }
      
        
        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
        {
            args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

            var baseQuery =
                from oa in _context.OrganizationAgents.AsNoTracking()
                where oa.OrganizationId == args.Id && oa.IsActive

                join a in _context.Agents.AsNoTracking()
                    on oa.AgentId equals a.Id

                group new { oa, a } by new
                {
                    a.Id,
                    a.UserId,
                    a.Name,
                    a.Email,
                    a.PhoneNumber,
                    a.State,
                    a.Lga,
                    a.Ward,
                    a.OnBoardedOn
                }
                into g

                select new OrganizationAgentResponse
                {
                    AgentId = g.Key.Id,
                    UserId = g.Key.UserId,
                    Name = g.Key.Name,
                    Email = g.Key.Email,
                    PhoneNumber = g.Key.PhoneNumber,
                    State = g.Key.State,
                    Lga = g.Key.Lga,
                    Ward = g.Key.Ward,
                    OnBoardedOn = g.Key.OnBoardedOn,

                    LocationCount = g
                        .Where(x => x.oa.LocationId > 0)
                        .Select(x => x.oa.LocationId)
                        .Distinct()
                        .Count(),

                    RoleCount =
                        (from r in _context.OrganizationAgentRoles
                         where g.Select(x => x.oa.Id).Contains(r.OrganizationAgentId)
                         select r.Id).Count(),

                    Subscribers = 0,
                    InputDisbursed = 0,
                    EquityPaid = 0,
                    ProductSold = 0
                };

            var totalCount = await baseQuery.CountAsync();

            var result = await baseQuery
                .OrderByDescending(x => x.AgentId)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            return new PagedListResponse<List<OrganizationAgentResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result,
                Succeeded=true,

            };
        }

        #region Location
        
        private async Task ValidateModules(List<long> moduleIds)
        {
            if (moduleIds.Count > 4)
                throw new Exception("Max 4 modules allowed.");

            var count = await _context.Modules.CountAsync(x => moduleIds.Contains(x.Id));

            if (count != moduleIds.Count)
                throw new Exception("Invalid modules.");
        }

        private async Task ValidateStorageCategories(List<long> categoryIds)
        {
            if (!categoryIds.Any()) return;

            var count = await _context.StorageCategories.CountAsync(x => categoryIds.Contains(x.Id));

            if (count != categoryIds.Count)
                throw new Exception("Invalid storage categories.");
        }

        private void ValidateTemperature(bool hasTemp, long? tempTypeId)
        {
            if (hasTemp && tempTypeId == null)
                throw new Exception("Temperature type required.");

            if (!hasTemp && tempTypeId != null)
                throw new Exception("Temperature type should be null.");
        }


        private List<LocationModule> MapModules(List<long> moduleIds)
        {
            return moduleIds.Select(x => new LocationModule
            {
                ModuleId = x
            }).ToList();
        }

        private List<LocationStorageCategory> MapCategories(List<long> categoryIds)
        {
            return categoryIds.Select(x => new LocationStorageCategory
            {
                StorageCategoryId = x
            }).ToList();
        }
        private async Task<bool> IsDescendant(long locationId, long potentialParentId)
        {
            var current = await _context.OrganizationLocations
                .FirstOrDefaultAsync(x => x.Id == potentialParentId);

            while (current != null)
            {
                if (current.ParentLocationId == locationId)
                    return true;

                if (current.ParentLocationId == null)
                    return false;

                current = await _context.OrganizationLocations
                    .FirstOrDefaultAsync(x => x.Id == current.ParentLocationId);
            }

            return false;
        }

        private async Task DeactivateChildren(long parentId, string userId, string reason)
        {
            var children = await _context.OrganizationLocations
                .Where(x => x.ParentLocationId == parentId && x.IsActive)
                .ToListAsync();

            foreach (var child in children)
            {
                child.IsActive = false;
                child.DeactivatedByUserId = userId;
                child.DeactivationReason = reason;

                await DeactivateChildren(child.Id, userId, reason);
            }
        }
        private LocationResponseDto MapToDto(OrganizationLocation x)
        {
            return new LocationResponseDto
            {
                IsActive = x.LocationStatus == "Active",
                Id = x.Id,
                LocationName = x.LocationName,

                LocationTypeId = x.LocationTypeId,
                LocationTypeName = x.LocationType?.Type,

                Capacity = x.Capacity,
                CapacityType = x.CapacityType,
                StockLevel = x.StockLevel,

                Address = x.Address,

                HasTemperatureControl = x.HasTemperatureControl,
                TemperatureControlType = x.TemperatureControlType?.Name,

                DateOfCommissioning = x.DateOfCommissioning,

                Modules = x.LocationModules
                    .Select(m => m.Module.Name)
                    .ToList(),

                StorageCategories = x.LocationStorageCategories
                    .Select(c => c.StorageCategory.Name)
                    .ToList(),

                ParentLocationId = x.ParentLocationId,
                LocationStatus = x.LocationStatus,
                Agents = x.OrganizationAgents
                        .Where(a => a.IsActive)
                        .Select(a => new AgentDto
                        {
                             AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,
                            IsPrimary=a.IsPrimary
                        })
                        .ToList(),
            };
        }
        //public async Task<OrganizationLocationModel> UpdateLocationAsync(long locationId, OrganizationLocationModel model)
        //{
        //    var existingLocation = await _context.OrganizationLocations
        //        .FirstOrDefaultAsync(x => x.Id == locationId);

        //    if (existingLocation == null)
        //        throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Location not found");

        //    var duplicate = await _context.OrganizationLocations
        //        .AnyAsync(x => x.LocationName == model.LocationName
        //        && x.OrganizationId==model.OrganizationId
        //        && x.StateId == model.StateId
        //        && x.LgaId == model.LgaId
        //        && x.Address == model.Address
        //        && x.Id != locationId);

        //    if (duplicate)
        //        throw new ValidationException("A location with this name already exists");
        //    if (model.LocationTypeId > 0)
        //        existingLocation.LocationTypeId = model.LocationTypeId;

        //    if (model.StateId > 0)
        //        existingLocation.StateId = model.StateId;

        //    if (model.LgaId > 0)
        //        existingLocation.LgaId = model.LgaId;

        //    if (!string.IsNullOrWhiteSpace(model.Address))
        //        existingLocation.Address = model.Address;

        //    if (model.Capacity > 0)
        //        existingLocation.Capacity = model.Capacity;

        //    if (!string.IsNullOrWhiteSpace(model.CapacityType))
        //        existingLocation.CapacityType = model.CapacityType;

        //    if (!string.IsNullOrWhiteSpace(model.LocationName))
        //        existingLocation.LocationName = model.LocationName;


        //    existingLocation.IsActive = model.IsActive;

        //    _context.OrganizationLocations.Update(existingLocation);
        //    await _context.SaveChangesAsync();

        //    return existingLocation.ViewMap();
        //}
        //public async Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model)
        //{
        //    var orgExist = await _context.OrganizationLocations
        //      .FirstOrDefaultAsync(x => x.LocationName == model.LocationName && x.OrganizationId == model.OrganizationId);

        //    if (orgExist != null)
        //    {
        //        return orgExist.ViewMap();
        //    }
        //    OrganizationLocation org = model.Map();
        //    org.IsActive = true;
        //    _context.Set<OrganizationLocation>().Add(org);
        //    await _context.SaveChangesAsync();
        //    var response = org.ViewMap();
        //    return response;
        //}
        //public async Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query)
        //{
        //    var locations = await _context.Set<OrganizationLocation>()
        //        .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id)
        //        .Include(x => x.LocationType)
        //        .Include(x => x.State)
        //        .Include(x => x.LGA)
        //        .ToListAsync();

        //    var locationIds = locations.Select(l => l.Id).ToList();

        //    var agentCounts = await _context.Set<OrganizationAgent>()
        //        .Where(a => locationIds.Contains(a.LocationId))
        //        .GroupBy(a => a.LocationId)
        //        .Select(g => new { LocationId = g.Key, Count = g.Count() })
        //        .ToListAsync();

        //    var result = locations.Select(loc =>
        //    {
        //        var assignedCount = agentCounts.FirstOrDefault(ac => ac.LocationId == loc.Id)?.Count ?? 0;

        //        return new OrganizationLocationResponseModel
        //        {
        //            Id = loc.Id,
        //            StateId = loc.StateId,
        //            LgaId = loc.LgaId,
        //            BusinessCode = loc.BusinessCode,
        //            OrganizationId = loc.OrganizationId,
        //            LocationName = loc.LocationName,
        //            LocationTypeId = loc.LocationTypeId,
        //            State = loc.State?.State,
        //            Lga = loc.LGA?.Name,
        //            LocationType = loc.LocationType?.Type,
        //            Address = loc.Address,
        //            IsActive = loc.IsActive,
        //            Capacity = loc.Capacity,
        //            TotalStock = loc.StockLevel,
        //            CurrentUsage = loc.Capacity - loc.StockLevel,
        //            CapacityType = loc.CapacityType,
        //            AssignedAgentCount = assignedCount.ToString()
        //        };
        //    }).ToList();

        //    return result;
        //}
        //public async Task<OrganizationLocationModel> GetLocationByIAsync(long Id)
        //{
        //    var loc = await _context.Set<OrganizationLocation>()
        //         .Include(x => x.LocationType)
        //        .Include(x => x.State)
        //        .Include(x => x.LGA)
        //        .FirstOrDefaultAsync(x => x.Id == Id);
        //    if (loc == null)
        //    {
        //        return null;
        //    }
        //    return new OrganizationLocationModel
        //    {
        //        Id = loc.Id,
        //        StateId = loc.StateId,
        //        LgaId = loc.LgaId,
        //        BusinessCode = loc.BusinessCode,
        //        OrganizationId = loc.OrganizationId,
        //        LocationName = loc.LocationName,
        //        LocationTypeId = loc.LocationTypeId,
        //        StateName = loc.State?.State,
        //        LgaName = loc.LGA?.Name,
        //        LocationTypeName = loc.LocationType?.Type,
        //        Address = loc.Address,
        //        IsActive = loc.IsActive,
        //        Capacity = loc.Capacity,
        //        Used = loc.StockLevel,
        //        Available = loc.Capacity - loc.StockLevel,
        //        CapacityType = loc.CapacityType
        //    };
        //}


        public async Task<OrganizationLocation> EditLocationAsync(EditLocationDto dto)
        {
            var location = await _context.OrganizationLocations
                .Include(x => x.LocationModules)
                .Include(x => x.LocationStorageCategories)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (location == null)
                throw new Exception("Location not found.");

            if (location.ParentLocationId != location.Id)
                throw new Exception("Sub location can only be edited inside its parent location.");


            var locationType = await _context.LocationTypes
                .FirstOrDefaultAsync(x => x.Id == location.LocationTypeId);

            if (!locationType.Type.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only warehouse can be edited here.");

            if (dto.HasTemperatureControl || dto.TemperatureControlTypeId > 0)
            {
                ValidateTemperature(
                    dto.HasTemperatureControl ,
                    dto.TemperatureControlTypeId > 0 ? dto.TemperatureControlTypeId : location.TemperatureControlTypeId
                );
            }

            if (dto.ModuleIds != null)
                await ValidateModules(dto.ModuleIds);

            if (dto.StorageCategoryIds != null)
                await ValidateStorageCategories(dto.StorageCategoryIds);

            if (!string.IsNullOrWhiteSpace(dto.LocationName))
                location.LocationName = dto.LocationName;

            if (dto.Capacity.HasValue)
                location.Capacity = dto.Capacity.Value;

            if (!string.IsNullOrWhiteSpace(dto.CapacityType))
                location.CapacityType = dto.CapacityType;

            if (dto.StateId.HasValue)
                location.StateId = dto.StateId.Value;

            if (dto.LgaId.HasValue)
                location.LgaId = dto.LgaId.Value;

            if (dto.WardId.HasValue)
                location.WardId = dto.WardId.Value;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                location.Address = dto.Address;


            if (dto.Latitude.HasValue)
                location.Latitude = dto.Latitude.Value;

            if (dto.Longitude.HasValue)
                location.Longitude = dto.Longitude.Value;


            if (dto.DateOfCommissioning.HasValue)
                location.DateOfCommissioning = dto.DateOfCommissioning.Value;

            if (dto.HasTemperatureControl)
            {
                location.HasTemperatureControl = dto.HasTemperatureControl;

                if (!dto.HasTemperatureControl)
                {
                    location.TemperatureControlTypeId = null;
                }
            }

            if (dto.TemperatureControlTypeId > 0)
            {
                location.TemperatureControlTypeId = dto.TemperatureControlTypeId;
            }

            if (dto.ModuleIds != null)
            {
                location.LocationModules.Clear();
                location.LocationModules = MapModules(dto.ModuleIds);
            }

            if (dto.StorageCategoryIds != null)
            {
                location.LocationStorageCategories.Clear();
                location.LocationStorageCategories = MapCategories(dto.StorageCategoryIds);
            }

            await _context.SaveChangesAsync();

            return location;
        }
        public async Task<OrganizationLocation> CreateLocationAsync(CreateLocationDto dto)
        {
            var locationType = await _context.LocationTypes
                .FirstOrDefaultAsync(x => x.Id == dto.LocationTypeId);

            if (locationType == null)
                throw new Exception("Invalid location type.");

            if (!locationType.Type.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only Warehouse can be created as root location.");

            var exists = await _context.OrganizationLocations.AnyAsync(x =>
                x.LocationName == dto.LocationName &&
                x.OrganizationId == dto.OrganizationId &&
                x.IsActive &&
                x.ParentLocationId == null);

            if (exists)
                throw new Exception("Warehouse already exists.");

            ValidateTemperature(dto.HasTemperatureControl, dto.TemperatureControlTypeId);
            await ValidateModules(dto.ModuleIds);
            await ValidateStorageCategories(dto.StorageCategoryIds);

            var location = new OrganizationLocation
            {
                OrganizationId = dto.OrganizationId.Value,
                BusinessCode = dto.BusinessCode,

                LocationName = dto.LocationName,
                LocationTypeId = dto.LocationTypeId,
                Longitude = dto.Longitude,
                Latitude = dto.Latitude,
                Capacity = dto.Capacity,
                CapacityType = dto.CapacityType,
                StockLevel = 0,
                LocationStatus = dto.LocationStatus,
                StateId = dto.StateId,
                LgaId = dto.LgaId,
                Address = dto.Address,
                OwnershipType=dto.OwnerShipType,
                WardId=dto.WardId,
                DateOfCommissioning = dto.DateOfCommissioning,

                HasTemperatureControl = dto.HasTemperatureControl,
                TemperatureControlTypeId = dto.HasTemperatureControl ? dto.TemperatureControlTypeId : null,

                IsActive = true,
                CreatedOn = DateTimeOffset.Now,
                CreatedByUserId = dto.CreatedByUserId,

                ParentLocationId = null
            };

            location.LocationModules = MapModules(dto.ModuleIds);
            location.LocationStorageCategories = MapCategories(dto.StorageCategoryIds);

            _context.OrganizationLocations.Add(location);
            await _context.SaveChangesAsync();

            location.ParentLocationId = location.Id;

            _context.OrganizationLocations.Update(location);
            await _context.SaveChangesAsync();

            return location;
        }

        public async Task<LocationDashboardFullDto> GetFullDashboardAsync(long organizationId)
        {
            var now = DateTimeOffset.UtcNow;
            var soon = now.AddDays(30);

            var locations = _context.OrganizationLocations
                .Where(x => x.OrganizationId == organizationId && x.IsActive);

            

            var totalWarehouses = await locations
                .CountAsync(x => x.ParentLocationId == x.Id);

            var totalSubWarehouses = await locations
                .CountAsync(x => x.ParentLocationId !=x.Id);


            var totalCapacity = await locations
                .SumAsync(x => (double?)x.Capacity) ?? 0;

            var totalStockLevel = await locations
                .SumAsync(x => (double?)x.StockLevel) ?? 0;

            var occupancyRate = totalCapacity == 0
                ? 0
                : (totalStockLevel / totalCapacity) * 100;

            var availableCapacity = totalCapacity - totalStockLevel;


            var agents = _context.OrganizationAgents
                .Where(x => x.OrganizationId == organizationId &&
                            x.LocationId != null);

            var activeAgents = await agents
                .CountAsync(x => x.IsActive);

            var inactiveAgents = await agents
                .CountAsync(x => !x.IsActive);

            var primaryAgents = await agents
                .CountAsync(x => x.IsPrimary && x.IsActive);

            var supportingAgents = await agents
                .CountAsync(x => !x.IsPrimary && x.IsActive);


            var inventories = _context.LocationInventories
                .Where(x => x.Location.OrganizationId == organizationId &&
                            x.IsActive);

            var totalInventoryRecords = await inventories.CountAsync();

            var totalInventoryQuantity = await inventories
                .SumAsync(x => (double?)x.Quantity) ?? 0;

            var expiringSoon = await inventories
                .CountAsync(x =>
                    x.ExpiryDate != null &&
                    x.ExpiryDate <= soon &&
                    x.ExpiryDate > now);

            var expired = await inventories
                .CountAsync(x =>
                    x.ExpiryDate != null &&
                    x.ExpiryDate <= now);

            var overCapacity = await locations
                .CountAsync(x => x.StockLevel > x.Capacity);


            var transactions = _context.InventoryTransactions
                .Where(x => x.OrganizationId == organizationId);

            var totalIncomingInventory = await transactions
                .Where(x =>
                    x.TransactionType == InventoryTransactionType.StockIn ||
                    x.TransactionType == InventoryTransactionType.TransferIn ||
                    x.TransactionType == InventoryTransactionType.AdjustmentIncrease ||
                    x.TransactionType == InventoryTransactionType.Returned)
                .SumAsync(x => (double?)x.Quantity) ?? 0;

            var totalOutgoingInventory = await transactions
                .Where(x =>
                    x.TransactionType == InventoryTransactionType.StockOut ||
                    x.TransactionType == InventoryTransactionType.TransferOut ||
                    x.TransactionType == InventoryTransactionType.AdjustmentDecrease ||
                    x.TransactionType == InventoryTransactionType.Damaged ||
                    x.TransactionType == InventoryTransactionType.Expired ||
                    x.TransactionType == InventoryTransactionType.Sold ||
                    x.TransactionType == InventoryTransactionType.Consumed)
                .SumAsync(x => (double?)x.Quantity) ?? 0;


            var topLocations = await locations
                .Select(x => new LocationUtilizationDto
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    Capacity = x.Capacity,
                    StockLevel = x.StockLevel,
                    UtilizationRate = x.Capacity == 0
                        ? 0
                        : (x.StockLevel / x.Capacity) * 100
                })
                .OrderByDescending(x => x.UtilizationRate)
                .Take(5)
                .ToListAsync();


            return new LocationDashboardFullDto
            {
                TotalWarehouses = totalWarehouses,
                TotalSubWarehouses = totalSubWarehouses,

                TotalCapacity = totalCapacity,
                TotalStockLevel = totalStockLevel,

                OccupancyRate = Math.Round(occupancyRate, 2),
                AvailableCapacity = availableCapacity,
                
                TotalInventoryRecords = totalInventoryRecords,
                TotalInventory = totalInventoryRecords,
                TotalInventoryQuantity = totalInventoryQuantity,

                TotalIncomingInventory = totalIncomingInventory,
                TotalOutgoingInventory = totalOutgoingInventory,

                ActiveAgents = activeAgents,
                InactiveAgents = inactiveAgents,
                //PrimaryAgents = primaryAgents,
                //SupportingAgents = supportingAgents,

                // health metrics
                //ExpiringSoonCount = expiringSoon,
                //ExpiredCount = expired,

                //OverCapacityLocations = overCapacity,

                //TopUtilizedLocations = topLocations
            };
        }
        public async Task SyncLocationStockLevels()
        {
            var locations = await _context.OrganizationLocations.ToListAsync();

            foreach (var location in locations)
            {
                location.StockLevel = await _context.LocationInventories
                    .Where(x => x.LocationId == location.Id && x.IsActive)
                    .SumAsync(x => x.Quantity);
            }

            await _context.SaveChangesAsync();
        }
        public async Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query)
        {
            var agentLocations = await _context.Set<OrganizationAgent>()
                .Where(a => a.AgentId == query.Id)
                .Select(a => a.LocationId)
                .ToListAsync();

            if (!agentLocations.Any())
                return new List<OrganizationAgentLocationResponseModel>();

            var locations = await _context.Set<OrganizationLocation>()
                .Where(l => agentLocations.Contains(l.Id))
                .Include(l => l.LocationType)
                .Include(l => l.State)
                .Include(l => l.LGA)
                .ToListAsync();

            var result = locations.Select(loc => new OrganizationAgentLocationResponseModel
            {
                Id = loc.Id,
                StateId = loc.StateId,
                LgaId = loc.LgaId,
                BusinessCode = loc.BusinessCode,
                OrganizationId = loc.OrganizationId,
                LocationName = loc.LocationName,
                LocationTypeId = loc.LocationTypeId,
                State = loc.State?.State,
                Lga = loc.LGA?.Name,
                LocationType = loc.LocationType?.Type,
                Address = loc.Address,
                IsActive = loc.IsActive,
                Capacity = loc.Capacity,
                Allocation = loc.StockLevel,
                AvailableCapacity = loc.Capacity - loc.StockLevel,
                CapacityType = loc.CapacityType
            }).ToList();

            return result;
        }
        public async Task<PagedListResponse<List<LocationListResponseDto>>> GetLocationsAsync(ListQueryParams query)
        {
            //var baseQuery = _context.OrganizationLocations
            //    .Where(x => x.OrganizationId == query.Id && x.IsActive)
            //    .AsQueryable();
            var baseQuery = _context.OrganizationLocations
    .Where(x =>
        x.OrganizationId == query.Id &&
        x.IsActive &&
        (
            x.ParentLocationId == null ||
            x.ParentLocationId == x.Id
        ))
    .AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Location))
            {
                baseQuery = baseQuery.Where(x => x.LocationName.Contains(query.Location));
            }
            if (!string.IsNullOrEmpty(query.LocationStatus))
            {
                baseQuery = baseQuery.Where(x => x.LocationStatus.ToLower().Contains(query.LocationStatus.ToLower()));
            }
            if (query.LocationTypeId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.LocationTypeId == query.LocationTypeId);
            }
            //if (query.HasPrimaryAgent == true)
            //{
            //    baseQuery = baseQuery.Where(x =>
            //        x.OrganizationAgents.Any(a => a.IsPrimary && a.IsActive));
            //}
            //if (!string.IsNullOrEmpty(query.AgentUserId))
            //{
            //    baseQuery = baseQuery.Where(x =>
            //        x.OrganizationAgents.Any(a => a.Agent.UserId == query.AgentUserId));
            //}
            var totalCount = await baseQuery.CountAsync();

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 100 : query.PageSize;

            var data = await baseQuery
                .OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LocationListResponseDto
                {
                    IsActive = x.LocationStatus == "Active",
                    // IsActive= x.IsActive,
                    Id = x.Id,
                    LocationName = x.LocationName,
                    Address = x.Address,
                    Capacity = x.Capacity,
                    CapacityType = x.CapacityType,
                    StockLevel = x.StockLevel,
                    LocationStatus=x.LocationStatus,
                    LocationTypeId = x.LocationTypeId,
                    LocationTypeName = x.LocationType.Type,
                    Agents = x.OrganizationAgents
                        .Where(a => a.IsActive)
                        .Select(a => new AgentDto
                        {
                            AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,
                            IsPrimary = a.IsPrimary
                        })
                        .ToList(),
                    PrimaryAgent = x.OrganizationAgents
                        .Where(a => a.IsPrimary)
                        .Select(a => new AgentDto
                        {
                            AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,
                            IsPrimary = true
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedListResponse<List<LocationListResponseDto>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<LocationResponseDto> GetLocationByIdAsync(long id)
        {
            var location = await _context.OrganizationLocations
                .Where(x => x.IsActive && x.Id == id)
                .Select(x => new LocationResponseDto
                {
                    Id = x.Id,
                    LocationName = x.LocationName,
                    Address = x.Address,
                                        Latitude = x.Latitude,
                                        Longitude=x.Longitude,
                                        LocationStatus=x.LocationStatus,
                                        OwnerShip=x.OwnershipType,

                    Capacity = x.Capacity,
                    CapacityType = x.CapacityType,
                    StockLevel = x.StockLevel,

                    LocationTypeId = x.LocationTypeId,
                    LocationTypeName = x.LocationType.Type,
                    

                    HasTemperatureControl = x.HasTemperatureControl,
                    TemperatureControlType = x.TemperatureControlType != null
                        ? x.TemperatureControlType.Name
                        : null,

                    DateOfCommissioning = x.DateOfCommissioning,

                    ParentLocationId = x.ParentLocationId,

                    Modules = x.LocationModules
                        .Select(m => m.Module.Name)
                        .ToList(),

                    StorageCategories = x.LocationStorageCategories
                        .Select(c => c.StorageCategory.Name)
                        .ToList(),

                    Agents = x.OrganizationAgents
                        .Where(a => a.IsActive)
                        .Select(a => new AgentDto
                        {
                            AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,
                            IsPrimary = a.IsPrimary
                        })
                        .ToList(),

                    PrimaryAgent = x.OrganizationAgents
                        .Where(a => a.IsPrimary && a.IsActive)
                        .Select(a => new AgentDto
                        {
                            AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,
                            IsPrimary = true
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (location == null)
                throw new Exception("Location not found.");

            return location;
        }
        public async Task<PagedListResponse<List<SubLocationListDto>>> GetSubLocationsByParentAsync(
    long parentLocationId,
    ListQueryParams query)
        {
            var baseQuery = _context.OrganizationLocations
                .Where(x => x.ParentLocationId == parentLocationId && x.Id != parentLocationId && x.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Location))
            {
                baseQuery = baseQuery.Where(x => x.LocationName.Contains(query.Location));
            }

            if (query.LocationTypeId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.LocationTypeId == query.LocationTypeId);
            }

            var totalCount = await baseQuery.CountAsync();

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 50 : query.PageSize;

            var data = await baseQuery
                .OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SubLocationListDto
                {
                    Id = x.Id,
                    LocationName = x.LocationName,
                    LocationType = x.LocationType.Type,

                    Capacity = x.Capacity,
                    CapacityType = x.CapacityType,

                    IsActive = x.LocationStatus == "Active",

                    AssignedAgentName = x.OrganizationAgents
                        .Where(a => a.IsPrimary && a.IsActive)
                        .Select(a => a.Agent.Name)
                        .FirstOrDefault(),

                    AssignedAgentUserId = x.OrganizationAgents
                        .Where(a => a.IsPrimary && a.IsActive)
                        .Select(a => a.Agent.UserId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedListResponse<List<SubLocationListDto>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        private async Task<bool> IsUnderWarehouse(long locationId)
        {
            var current = await _context.OrganizationLocations
                .Include(x => x.LocationType)
                .FirstOrDefaultAsync(x => x.Id == locationId);

            if (current != null)
            {
                //if (current.LocationType.Type == "Warehouse")
                //    return true;

                if (current.ParentLocationId != current.Id)
                    return false;

                //current = await _context.OrganizationLocations
                //    .Include(x => x.LocationType)
                //    .FirstOrDefaultAsync(x => x.Id == current.ParentLocationId);
            }

            return true;
        }
        public async Task<OrganizationLocation> CreateSubLocationAsync(CreateSubLocationDto dto)
        {
            var parent = await _context.OrganizationLocations
                .Include(x => x.LocationType)
                .FirstOrDefaultAsync(x => x.Id == dto.ParentLocationId);

            if (parent == null)
                throw new Exception("Parent not found.");

            if (!await IsUnderWarehouse(parent.Id))
                throw new Exception("Must belong to parent location.");

            var locationType = await _context.LocationTypes
                .FirstOrDefaultAsync(x => x.Id == dto.LocationTypeId);

            if (locationType == null)
                throw new Exception("Invalid location type.");

            //if (locationType.Type == "Warehouse")
            //    throw new Exception("Warehouse cannot be a sublocation.");

            var exists = await _context.OrganizationLocations.AnyAsync(x =>
                x.LocationName == dto.LocationName &&
                x.OrganizationId == parent.OrganizationId &&
                x.IsActive && 
                x.ParentLocationId == dto.ParentLocationId);

            if (exists)
                throw new Exception("Location already exists under this parent.");

            ValidateTemperature(dto.HasTemperatureControl, dto.TemperatureControlTypeId);
            await ValidateModules(dto.ModuleIds);
            await ValidateStorageCategories(dto.StorageCategoryIds);

            var location = new OrganizationLocation
            {
                OrganizationId = parent.OrganizationId,
                BusinessCode = parent.BusinessCode,

                LocationName = dto.LocationName,
                LocationTypeId = dto.LocationTypeId,
                OwnershipType = dto.OwnerShipType,
                Capacity = dto.Capacity,
                CapacityType = dto.CapacityType,
                StockLevel = 0,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                StateId = dto.StateId,
                LgaId = dto.LgaId,
                WardId=dto.WardId,
                Address = dto.Address,
                LocationStatus = dto.LocationStatus,
                DateOfCommissioning = dto.DateOfCommissioning,

                HasTemperatureControl = dto.HasTemperatureControl,
                TemperatureControlTypeId = dto.HasTemperatureControl ? dto.TemperatureControlTypeId : null,

                IsActive = true,
                CreatedOn = DateTimeOffset.Now,
                CreatedByUserId = dto.CreatedByUserId,

                ParentLocationId = parent.Id
            };

            location.LocationModules = MapModules(dto.ModuleIds);
            location.LocationStorageCategories = MapCategories(dto.StorageCategoryIds);

            _context.OrganizationLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }

        public async Task<OrganizationLocation> EditSubLocationAsync(EditSubLocationDto dto)
        {
            var location = await _context.OrganizationLocations
                .Include(x => x.LocationModules)
                .Include(x => x.LocationStorageCategories)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (location == null)
                throw new Exception("Location not found.");

            if (location.ParentLocationId == null)
                throw new Exception("Use EditLocation for root.");


            if (dto.NewParentLocationId.HasValue &&
                dto.NewParentLocationId != location.ParentLocationId)
            {
                var newParent = await _context.OrganizationLocations
                    .Include(x => x.LocationType)
                    .FirstOrDefaultAsync(x => x.Id == dto.NewParentLocationId);

                if (newParent == null)
                    throw new Exception("New parent not found.");

                if (!await IsUnderWarehouse(newParent.Id))
                    throw new Exception("Must belong to a parent location.");


                if (await IsDescendant(location.Id, newParent.Id))
                    throw new Exception("Cannot assign child as parent.");

                location.ParentLocationId = newParent.Id;
            }

            if (dto.HasTemperatureControl || dto.TemperatureControlTypeId > 0)
            {
                ValidateTemperature(
                    dto.HasTemperatureControl,
                    dto.TemperatureControlTypeId > 0 ? dto.TemperatureControlTypeId : location.TemperatureControlTypeId
                );
            }

            if (dto.ModuleIds != null)
                await ValidateModules(dto.ModuleIds);

            if (dto.StorageCategoryIds != null)
                await ValidateStorageCategories(dto.StorageCategoryIds);

            if (dto.Capacity.HasValue)
                location.Capacity = dto.Capacity.Value;

            if (!string.IsNullOrWhiteSpace(dto.CapacityType))
                location.CapacityType = dto.CapacityType;

            if (dto.StateId.HasValue)
                location.StateId = dto.StateId.Value;

            if (dto.LgaId.HasValue)
                location.LgaId = dto.LgaId.Value;

            if (dto.WardId.HasValue)
                location.WardId = dto.WardId.Value;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                location.Address = dto.Address;

            if (!string.IsNullOrWhiteSpace(dto.LocationName))
                location.LocationName = dto.LocationName;

            if (dto.Latitude.HasValue)
                location.Latitude = dto.Latitude.Value;

            if (dto.Longitude.HasValue)
                location.Longitude = dto.Longitude.Value;

            if (dto.DateOfCommissioning.HasValue)
                location.DateOfCommissioning = dto.DateOfCommissioning.Value;


            if (dto.HasTemperatureControl)
            {
                location.HasTemperatureControl = dto.HasTemperatureControl;

                if (!dto.HasTemperatureControl)
                {
                    location.TemperatureControlTypeId = null;
                }
            }

            if (dto.TemperatureControlTypeId > 0)
            {
                location.TemperatureControlTypeId = dto.TemperatureControlTypeId;
            }

            if (dto.ModuleIds != null)
            {
                location.LocationModules.Clear();
                location.LocationModules = MapModules(dto.ModuleIds);
            }

            if (dto.StorageCategoryIds != null)
            {
                location.LocationStorageCategories.Clear();
                location.LocationStorageCategories = MapCategories(dto.StorageCategoryIds);
            }


            await _context.SaveChangesAsync();

            return location;
        }
        public async Task DeleteLocationAsync(DeleteLocationDto dto)
        {
            var location = await _context.OrganizationLocations
                .Include(x => x.SubLocations)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (location == null)
                throw new Exception("Location not found.");

            if (location.ParentLocationId != null)
                throw new Exception("Use DeleteSubLocation for sublocations.");


            var hasActiveChildren = await _context.OrganizationLocations
                .AnyAsync(x => x.ParentLocationId == location.Id && x.IsActive);

            if (hasActiveChildren && !dto.ForceDelete)
                throw new Exception("Cannot delete location with active sublocations.");

            if (dto.ForceDelete)
            {
                await DeactivateChildren(location.Id, dto.UserId, dto.Reason);
            }

            location.LocationStatus = "Suspended";
            location.IsActive = false;
            location.DeactivatedByUserId = dto.UserId;
            location.DeactivationReason = dto.Reason;
            location.DeletedOn = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }
        public async Task DeleteSubLocationAsync(DeleteLocationDto dto)
        {
            var location = await _context.OrganizationLocations
                .Include(x => x.SubLocations)
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.ParentLocationId != dto.Id);

            if (location == null)
                throw new Exception("Location not found.");

            if (location.ParentLocationId == null)
                throw new Exception("Use DeleteLocation for root.");

            var hasActiveChildren = await _context.OrganizationLocations
                .AnyAsync(x => x.ParentLocationId == location.Id && x.IsActive);

            if (hasActiveChildren && !dto.ForceDelete)
                throw new Exception("Cannot delete sublocation with active children.");

            if (dto.ForceDelete)
            {
                await DeactivateChildren(location.Id, dto.UserId, dto.Reason);
            }
            location.LocationStatus = "Suspended";
            location.IsActive = false;
            location.DeactivatedByUserId = dto.UserId;
            location.DeactivationReason = dto.Reason;
            location.DeletedOn = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }
        public async Task<List<LocationResponseDto>> GetLocationTreeAsync(long organizationId)
        {
            var allLocations = await _context.OrganizationLocations
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .Include(x => x.LocationType)
                .Include(x => x.TemperatureControlType)
                .Include(x => x.LocationModules)
                    .ThenInclude(x => x.Module)
                .Include(x => x.LocationStorageCategories)
                    .ThenInclude(x => x.StorageCategory)
                .ToListAsync();

            var lookup = allLocations.ToLookup(x => x.ParentLocationId);

            List<LocationResponseDto> BuildTree(long? parentId)
            {
                return lookup[parentId]
                    .Select(loc =>
                    {
                        var dto = MapToDto(loc);
                        dto.Children = BuildTree(loc.Id);
                        return dto;
                    }).ToList();
            }

            return BuildTree(null);
        }
        public async Task<PagedListResponse<List<LocationResponseDto>>> GetSubLocationsAsync(ListQueryParams query)
        {
            var result = _context.OrganizationLocations
                .Where(x => x.Id != x.ParentLocationId && x.IsActive)
                .Include(x => x.LocationType)
                .Include(x => x.TemperatureControlType)
                .Include(x => x.LocationModules)
                    .ThenInclude(x => x.Module)
                .Include(x => x.LocationStorageCategories)
                    .ThenInclude(x => x.StorageCategory)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(query.Location))
            {
                result = result.Where(x => x.LocationName.Contains(query.Location));
            }

            if (query.LocationTypeId.HasValue)
            {
                result = result.Where(x => x.LocationTypeId == query.LocationTypeId);
            }

            //if (query.HasTemperatureControl.HasValue)
            //{
            //    result = result.Where(x => x.HasTemperatureControl == query.HasTemperatureControl);
            //}

            var totalCount = await result.CountAsync();

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var data = await result
              .OrderByDescending(x => x.CreatedOn)
              .Skip((pageNumber - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

            return new PagedListResponse<List<LocationResponseDto>>
            {
                Data = data.Select(MapToDto).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<bool> ReassignPrimaryAgentAsync(ReassignPrimaryAgentDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var locationExists = await _context.OrganizationLocations
                        .AnyAsync(x => x.Id == dto.LocationId && x.IsActive);

                    if (!locationExists)
                        throw new Exception("Location not found.");

                    var agent = await _context.Agents
                        .FirstOrDefaultAsync(x => x.UserId == dto.NewPrimaryAgentUserId);

                    if (agent == null)
                        throw new Exception("Agent not found.");

                    var assignments = await _context.OrganizationAgents
                        .Where(x => x.LocationId == dto.LocationId && x.IsActive)
                        .ToListAsync();

                    var currentPrimary = assignments
                        .FirstOrDefault(x => x.IsPrimary);

                    var newAgentAssignment = assignments
                        .FirstOrDefault(x => x.AgentId == agent.Id);

                    if (newAgentAssignment == null)
                    {
                        newAgentAssignment = new OrganizationAgent
                        {
                            AgentId = agent.Id,
                            LocationId = dto.LocationId,
                            OrganizationId = assignments.First().OrganizationId,
                            AssignedByUserId = dto.PerformedByUserId,
                            AssignedOn = DateTimeOffset.UtcNow,
                            IsActive = true,
                            IsPrimary = true
                        };

                        await _context.OrganizationAgents.AddAsync(newAgentAssignment);
                    }
                    else
                    {
                        newAgentAssignment.IsPrimary = true;
                        newAgentAssignment.AssignedOn = DateTimeOffset.UtcNow;
                    }

                    if (currentPrimary != null && currentPrimary.AgentId != agent.Id)
                    {
                        currentPrimary.IsPrimary = false;
                        _context.OrganizationAgents.Update(currentPrimary);
                    }

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
        public async Task<LocationAgentsResponseDto> GetLocationAgentsAsync(long locationId)
        {
            var result = await _context.OrganizationLocations
                .Where(x => x.Id == locationId && x.IsActive)
                .Select(x => new LocationAgentsResponseDto
                {
                    PrimaryAgent = x.OrganizationAgents
                        .Where(a => a.IsPrimary && a.IsActive)
                        .Select(a => new PrimaryAgentDto
                        {
                            AgentId = a.AgentId,
                            UserId = a.Agent.UserId,
                            Name = a.Agent.Name,

                            AssignedOn = a.AssignedOn,

                            AgentLevel =EnumExtensions.GetDisplayName(a.Agent.TierLevel), 
                            Status = a.IsActive ? "Active" : "Inactive"
                        })
                        .FirstOrDefault(),

                    OtherAgents = x.OrganizationAgents
                        .Where(a => !a.IsPrimary && a.IsActive)
                        .Select(a => new OtherAgentDto
                        {
                            AgentId = a.AgentId,
                            Name = a.Agent.Name,
                            UserId=a.Agent.UserId,
                            AssignedOn = a.AssignedOn,
                            Status = a.IsActive ? "Active" : "Inactive"
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (result == null)
                throw new Exception("Location not found.");

            return result;
        }
        public async Task<LocationResponseDto> GetSubLocationByIdAsync(long id)
        {


            var location = await _context.OrganizationLocations
                           .Where(x => x.IsActive && x.Id == id)
                           .Include(x => x.LocationType)
                           .Include(x => x.TemperatureControlType)
                           .Include(x => x.LocationModules)
                               .ThenInclude(x => x.Module)
                           .Include(x => x.LocationStorageCategories)
                               .ThenInclude(x => x.StorageCategory)
                           .FirstOrDefaultAsync();

            if (location == null)
                throw new Exception("Sub Location not found or has been deleted.");

            return MapToDto(location);
        }
        public async Task<bool> RemoveAgentFromLocationAsync(RemoveAgentFromLocationDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var agent = await _context.Agents
                        .FirstOrDefaultAsync(x => x.UserId == dto.UserId);

                    if (agent == null)
                        throw new Exception("Agent not found.");

                    var assignment = await _context.OrganizationAgents
                        .Where(x =>
                            x.AgentId == agent.Id &&
                            x.LocationId == dto.LocationId &&
                            x.IsActive)
                        .FirstOrDefaultAsync();

                    if (assignment == null)
                        throw new Exception("Agent is not assigned to this location.");

                    var allAgents = await _context.OrganizationAgents
                        .Where(x =>
                            x.LocationId == dto.LocationId &&
                            x.IsActive)
                        .ToListAsync();

                    if (assignment.IsPrimary)
                    {
                        var otherAgent = allAgents
                            .Where(x => x.AgentId != agent.Id)
                            .OrderByDescending(x => x.AssignedOn)
                            .FirstOrDefault();

                        if (otherAgent != null)
                        {
                            otherAgent.IsPrimary = true;

                            _context.OrganizationAgents.Update(otherAgent);
                        }
                        else
                        {
                            throw new Exception(
                                "Cannot remove the only primary agent. Assign another agent first.");
                        }
                    }
                    assignment.IsActive = false;
                    assignment.IsPrimary = false;
                    assignment.DeactivatedByUserId = dto.RemovedByUserId;
                    assignment.DeactivatedOn = DateTimeOffset.UtcNow;

                    _context.OrganizationAgents.Update(assignment);

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
        public async Task<LocationInventory> AddInventoryAsync(CreateInventoryDto dto)
        {
            var location = await _context.OrganizationLocations
                .FirstOrDefaultAsync(x => x.Id == dto.LocationId && x.IsActive);

            if (location == null)
                throw new Exception("Location not found.");

            if (location.StockLevel + dto.Quantity > location.Capacity)
                throw new Exception("Exceeds location capacity.");

            var productExist = await _context.OrganizationProducts
                .FirstOrDefaultAsync(x => x.Id == dto.ProductId && x.IsActive);

            if (productExist == null)
                throw new Exception("Product not found.");

            var batchNumber = dto.BatchNumber ?? $"BATCH-{DateTime.UtcNow.Ticks}";

            // Check if inventory already exists
            var inventory = await _context.LocationInventories
                .FirstOrDefaultAsync(x =>
                    x.LocationId == dto.LocationId &&
                    x.OrganizationProductId == dto.ProductId &&
                    x.BatchNumber == batchNumber &&
                    x.IsActive);

            decimal quantityBefore = 0;

            if (inventory == null)
            {
                inventory = new LocationInventory
                {
                    LocationId = dto.LocationId,
                    OrganizationProductId = dto.ProductId,
                    BatchNumber = batchNumber,
                    InventoryOwner = dto.InventoryOwner,
                    Quantity = dto.Quantity,
                    QuantityType = dto.QuantityType,
                    Description = dto.Description,
                    StorageDate = dto.StorageDate,
                    ExpiryDate = dto.ExpiryDate,
                    QualityGrade = dto.QualityGrade,
                    BusinessCode = dto.BusinessCode,
                    RecordedOn = DateTimeOffset.UtcNow,
                    RecordByUserId = dto.RecordedByUserId,
                    IsActive = true
                };

                _context.LocationInventories.Add(inventory);
            }
            else
            {
                quantityBefore = inventory.Quantity;

                inventory.Quantity += dto.Quantity;
            }

            var quantityAfter = quantityBefore + dto.Quantity;

            // CREATE TRANSACTION HERE
            var transaction = new InventoryTransaction
            {
                LocationInventory = inventory,
                OrganizationProductId = dto.ProductId,

                ToLocationId = dto.LocationId,

                TransactionType = InventoryTransactionType.StockIn,

                Quantity = dto.Quantity,
                QuantityType = dto.QuantityType,

                QuantityBefore = quantityBefore,
                QuantityAfter = quantityAfter,

                BatchNumber = batchNumber,

                Reason = "New stock added",

                PerformedByUserId = dto.RecordedByUserId,

                BusinessCode = dto.BusinessCode,

                TransactionDate = DateTimeOffset.UtcNow,
                OrganizationId = location.OrganizationId
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            // UPDATE LOCATION STOCK
            await UpdateLocationStockLevel(location.Id);

            await _context.SaveChangesAsync();

            return inventory;
        }
        public async Task DeleteInventory(DeleteLocationDto dto)
        {
            var inventory = await _context.LocationInventories
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive);

            if (inventory == null)
                throw new Exception("Inventory not found.");
            var location = await _context.OrganizationLocations
              .FirstOrDefaultAsync(x => x.Id == inventory.LocationId && x.IsActive);

            if (location == null)
                throw new Exception("Location not found.");

            var quantityBefore = inventory.Quantity;

            inventory.IsActive = false;
            inventory.DeletedByUserId = dto.UserId;
            inventory.DeleteReason = dto.Reason;
            inventory.DeletedOn = DateTimeOffset.UtcNow;
            inventory.Quantity = 0;

            var transaction = new InventoryTransaction
            {
                LocationInventoryId = inventory.Id,

                OrganizationProductId = inventory.OrganizationProductId,

                ToLocationId = inventory.LocationId,

                TransactionType = InventoryTransactionType.Delete,

                Quantity = 0,

                QuantityType = inventory.QuantityType,

                QuantityBefore = quantityBefore,

                QuantityAfter = 0,

                BatchNumber = inventory.BatchNumber,

                Reason = dto.Reason ?? "Inventory deleted",
                PerformedByUserId = dto.UserId,

                BusinessCode = inventory.BusinessCode,

                TransactionDate = DateTimeOffset.UtcNow,
                OrganizationId=location.OrganizationId,
                
                
            };
            _context.InventoryTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            await UpdateLocationStockLevel(location.Id);

            await _context.SaveChangesAsync();
        }

        public async Task<LocationInventory> EditInventoryAsync(EditInventoryDto dto)
        {
            var inventory = await _context.LocationInventories
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive);

            if (inventory == null)
                throw new Exception("Inventory not found.");

            var location = await _context.OrganizationLocations
                .FirstOrDefaultAsync(x => x.Id == inventory.LocationId && x.IsActive);

            if (location == null)
                throw new Exception("Location not found.");

            var quantityBefore = inventory.Quantity;

            decimal diff = 0;


            if (dto.Quantity.HasValue)
            {
                diff = dto.Quantity.Value - inventory.Quantity;

                if (location.StockLevel + diff > location.Capacity)
                    throw new Exception("Exceeds location capacity.");

                inventory.Quantity = dto.Quantity.Value;
            }


            if (!string.IsNullOrWhiteSpace(dto.QuantityType))
                inventory.QuantityType = dto.QuantityType;

            if (!string.IsNullOrWhiteSpace(dto.InventoryOwner))
                inventory.InventoryOwner = dto.InventoryOwner;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                inventory.Description = dto.Description;

            if (dto.StorageDate.HasValue)
                inventory.StorageDate = dto.StorageDate.Value;

            if (dto.ExpiryDate.HasValue)
                inventory.ExpiryDate = dto.ExpiryDate.Value;

            if (!string.IsNullOrWhiteSpace(dto.QualityGrade))
                inventory.QualityGrade = dto.QualityGrade;


            if (dto.Quantity.HasValue)
            {
                await UpdateLocationStockLevel(location.Id);
            }


            if (dto.Quantity.HasValue)
            {
                InventoryTransactionType transactionType;

                if (diff > 0)
                {
                    transactionType = InventoryTransactionType.AdjustmentIncrease;
                }
                else if (diff < 0)
                {
                    transactionType = InventoryTransactionType.AdjustmentDecrease;
                }
                else
                {
                    transactionType = InventoryTransactionType.Edit;
                }

                var transaction = new InventoryTransaction
                {
                    LocationInventoryId = inventory.Id,

                    OrganizationProductId = inventory.OrganizationProductId,

                    ToLocationId = inventory.LocationId,

                    TransactionType = transactionType,

                    Quantity = Math.Abs(diff),

                    QuantityType = inventory.QuantityType,

                    QuantityBefore = quantityBefore,

                    QuantityAfter = inventory.Quantity,

                    BatchNumber = inventory.BatchNumber,

                    Reason = dto.Reason ?? "Inventory edited",

                    PerformedByUserId = dto.UpdatedByUserId,

                    BusinessCode = inventory.BusinessCode,

                    TransactionDate = DateTimeOffset.UtcNow,
                    OrganizationId=location.OrganizationId
                };

                _context.InventoryTransactions.Add(transaction);
            }

            await _context.SaveChangesAsync();

            return inventory;
        }
        private async Task UpdateLocationStockLevel(long locationId)
        {
            var location = await _context.OrganizationLocations
                .FirstOrDefaultAsync(x => x.Id == locationId);

            if (location == null)
                return;

            location.StockLevel = await _context.LocationInventories
                .Where(x => x.LocationId == locationId && x.IsActive)
                .SumAsync(x => x.Quantity);
        }
        public async Task<PagedListResponse<List<InventoryListDto>>> GetInventoryByLocationAsync(
      long locationId,
      ListQueryParams query)
        {
            var baseQuery = _context.LocationInventories
                .Where(li =>
                    li.LocationId == locationId &&
                    li.IsActive &&
                    li.OrganizationProduct.IsActive);

            if (query.startDate.HasValue)
            {
                baseQuery = baseQuery.Where(li => li.StorageDate >= query.startDate.Value);
            }

            if (query.endDate.HasValue)
            {
                baseQuery = baseQuery.Where(li => li.StorageDate <= query.endDate.Value);
            }

            var totalCount = await baseQuery.CountAsync();

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 50 : query.PageSize;

            var data = await baseQuery
                .OrderByDescending(li => li.RecordedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(li => new InventoryListDto
                {
                    Id = li.Id,
                    ProductCode = li.OrganizationProduct.ProductCode,
                    BatchNumber = li.BatchNumber,
                    InventoryOwner = li.InventoryOwner,
                    Quantity = li.Quantity,
                    QuantityType = li.QuantityType,
                    StorageDate = li.StorageDate,
                    ExpiryDate = li.ExpiryDate,
                    QualityGrade = li.QualityGrade
                })
                .ToListAsync();

            return new PagedListResponse<List<InventoryListDto>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<InventoryTransactionDto>>
    GetInventoryTransactionsAsync(long inventoryId)
        {
            return await _context.InventoryTransactions
                .Where(x => x.LocationInventoryId == inventoryId)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new InventoryTransactionDto
                {
                    Id = x.Id,
                    TransactionType =EnumExtensions.GetDisplayName(x.TransactionType),
                    Quantity = x.Quantity,
                    QuantityBefore = x.QuantityBefore,
                    QuantityAfter = x.QuantityAfter,
                    BatchNumber = x.BatchNumber,
                    Reason = x.Reason,
                    TransactionDate = x.TransactionDate
                })
                .ToListAsync();
        }
       
        
        #endregion Location


        public async Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model)
        {
            var orgExist = await _context.OrganizationRoles
              .FirstOrDefaultAsync(x => x.RoleName == model.RoleName && x.OrganizationId == model.OrganizationId);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            OrganizationRole org = model.Map();
            org.CreatedOn = DateTimeOffset.Now;
            _context.Set<OrganizationRole>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query)
        {
            var org = await _context.Set<OrganizationRole>()
                .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id).ToListAsync();

            return org.Select(x => x.ViewMap()).ToList();
        }
        public async Task<OrganizationRoleModel> GetRoleByIAsync(long Id)
        {
            var org = await _context.Set<OrganizationRole>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
        }

        #region FarmerMgt
        public async Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var farmer = await _context.Farmers
                        .FirstOrDefaultAsync(f => f.UserId == data.UserId);

                    if (farmer == null)
                    {
                        farmer = data.Map();
                        await _context.Farmers.AddAsync(farmer);
                    }
                    else
                    {
                        await UpdateFarmerProfileAsync(data.UserId, new FarmerUpdateModel
                        {
                            Name = data.Name,
                            PhoneNumber = data.PhoneNumber,
                            Email = data.Email,
                            Gender = data.Gender,
                            State = data.State,
                            Lga = data.Lga,
                            Location = data.Location,
                            Ward = data.Ward,
                            BVN = data.BVN,
                            NIN = data.NIN,
                            FarmerCategory = data.FarmerCategory,
                            HighestLevelOfEducation = data.HighestLevelOfEducation,
                            YearsInAgric = data.YearsInAgric,
                            DateOfBirth = data.DateOfBirth
                        });
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        farmer = await _context.Farmers
                            .FirstOrDefaultAsync(f => f.UserId == data.UserId);
                    }

                    // ===========================
                    // ORGANIZATION
                    // ===========================
                    if (!string.IsNullOrEmpty(data.OrganizationCode))
                    {
                        var exists = await _context.FarmerOrganizations
                            .AnyAsync(x => x.FarmerId == farmer.Id &&
                                           x.OrganizationCode == data.OrganizationCode);

                        if (!exists)
                        {
                            await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                            {
                                FarmerId = farmer.Id,
                                OrganizationCode = data.OrganizationCode,
                                IsActive = true,
                                OnboardedByUserId = data.OnboardedByUserId
                            });
                        }
                    }

                    // ===========================
                    // COOPERATIVES (BULK SAFE)
                    // ===========================
                    if (data.CooperativeIds != null)
                    {
                        var existing = await _context.FarmerCooperatives
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.CooperativeId).ToHashSet();
                        var incoming = data.CooperativeIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.CooperativeId));

                        await _context.FarmerCooperatives.AddRangeAsync(
                            toAdd.Select(id => new FarmerCooperative
                            {
                                FarmerId = farmer.Id,
                                CooperativeId = id,
                                IsActive = true,
                                OnboardedByUserId = data.OnboardedByUserId,
                                JoinedOn = DateTimeOffset.UtcNow
                            }));

                        _context.FarmerCooperatives.RemoveRange(toRemove);
                    }

                    // ===========================
                    // AGENTS (BULK SAFE)
                    // ===========================
                    if (data.AgentIds != null)
                    {
                        var existing = await _context.FarmerAgents
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.AgentId).ToHashSet();
                        var incoming = data.AgentIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.AgentId));

                        await _context.FarmerAgents.AddRangeAsync(
                            toAdd.Select(id => new FarmerAgent
                            {
                                FarmerId = farmer.Id,
                                AgentId = id,
                                AssignedByUserId = data.OnboardedByUserId,
                                AssignedOn = DateTimeOffset.UtcNow,
                                IsActive = true,
                            }));

                        _context.FarmerAgents.RemoveRange(toRemove);
                    }

                    // ===========================
                    // VALUE CHAIN
                    // ===========================
                    if (data.SelectedItems != null)
                    {
                        await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, farmer.Map());
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_Farmers_UserId") == true;
        }
        //public async Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data)
        //{
        //    var strategy = _context.Database.CreateExecutionStrategy();

        //    return await strategy.ExecuteAsync(async () =>
        //    {
        //        using var transaction = await _context.Database.BeginTransactionAsync();

        //        try
        //        {
        //            var farmer = await _context.Farmers
        //                .FirstOrDefaultAsync(f => f.UserId == data.UserId);

        //            if (farmer != null)
        //            {
        //                await UpdateFarmerProfileAsync(data.UserId, new FarmerUpdateModel
        //                {
        //                    DateOfBirth = data.DateOfBirth,
        //                    State = data.State,
        //                    Lga = data.Lga,
        //                    Name = data.Name,
        //                    PhoneNumber = data.PhoneNumber,
        //                    Email = data.Email,
        //                    Gender = data.Gender,
        //                    Location = data.Location,
        //                    Ward = data.Ward,
        //                    BVN = data.BVN,
        //                    NIN = data.NIN,
        //                    IsNinVerified = data.IsNinVerified,
        //                    FarmerCategory = data.FarmerCategory,
        //                    FarmerImage = data.FarmerImage,
        //                    HighestLevelOfEducation = data.HighestLevelOfEducation,
        //                    YearsInAgric = data.YearsInAgric
        //                });
        //            }
        //            else
        //            {
        //                farmer = data.Map();
        //                await _context.Farmers.AddAsync(farmer);
        //            }

        //            await _context.SaveChangesAsync();

        //            // ===========================
        //            // ORGANIZATION LINK
        //            // ===========================
        //            if (!string.IsNullOrEmpty(data.OrganizationCode))
        //            {
        //                bool exists = await _context.FarmerOrganizations
        //                    .AnyAsync(x => x.FarmerId == farmer.Id && x.OrganizationCode == data.OrganizationCode);

        //                if (!exists)
        //                {
        //                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
        //                    {
        //                        FarmerId = farmer.Id,
        //                        OrganizationCode = data.OrganizationCode,
        //                        OnboardedByUserId = data.OnboardedByUserId
        //                    });
        //                }
        //            }

        //            // ===========================
        //            // COOPERATIVES
        //            // ===========================
        //            if (data.CooperativeIds != null)
        //            {
        //                var existing = await _context.FarmerCooperatives
        //                    .Where(x => x.FarmerId == farmer.Id)
        //                    .ToListAsync();

        //                var incoming = data.CooperativeIds.Distinct().ToList();

        //                var toAdd = incoming
        //                    .Where(id => !existing.Any(e => e.CooperativeId == id))
        //                    .ToList();

        //                var toRemove = existing
        //                    .Where(e => !incoming.Contains(e.CooperativeId))
        //                    .ToList();

        //                foreach (var id in toAdd)
        //                {
        //                    await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
        //                    {
        //                        FarmerId = farmer.Id,
        //                        CooperativeId = id,
        //                        OnboardedByUserId = data.OnboardedByUserId,
        //                        JoinedOn = DateTimeOffset.Now
        //                    });
        //                }

        //                _context.FarmerCooperatives.RemoveRange(toRemove);
        //            }

        //            // ===========================
        //            // AGENTS
        //            // ===========================
        //            if (data.AgentIds != null)
        //            {
        //                var existing = await _context.FarmerAgents
        //                    .Where(x => x.FarmerId == farmer.Id)
        //                    .ToListAsync();

        //                var incoming = data.AgentIds.Distinct().ToList();

        //                var toAdd = incoming
        //                    .Where(id => !existing.Any(e => e.AgentId == id))
        //                    .ToList();

        //                var toRemove = existing
        //                    .Where(e => !incoming.Contains(e.AgentId))
        //                    .ToList();

        //                foreach (var id in toAdd)
        //                {
        //                    await _context.FarmerAgents.AddAsync(new FarmerAgent
        //                    {
        //                        FarmerId = farmer.Id,
        //                        AgentId = id,
        //                        AssignedByUserId = data.OnboardedByUserId,
        //                        AssignedOn = DateTimeOffset.Now
        //                    });
        //                }

        //                _context.FarmerAgents.RemoveRange(toRemove);
        //            }

        //            // ===========================
        //            // VALUE CHAIN
        //            // ===========================
        //            if (data.SelectedItems != null)
        //            {
        //                await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);
        //            }

        //            await _context.SaveChangesAsync();
        //            await transaction.CommitAsync();

        //            return (true, farmer.Map());
        //        }
        //        catch
        //        {
        //            await transaction.RollbackAsync();
        //            throw;
        //        }
        //    });
        //}

        public async Task<FarmerModel> UpdateFarmerProfileAsync(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                return null;

            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farmer == null) return null;

            var isModified = false;

            if (update.Name != null && update.Name != farmer.Name) { farmer.Name = update.Name; isModified = true; }
            if (update.Location != null && update.Location != farmer.Location) { farmer.Location = update.Location; isModified = true; }
            if (update.State != null && update.State != farmer.State) { farmer.State = update.State; isModified = true; }
            if (update.Lga != null && update.Lga != farmer.Lga) { farmer.Lga = update.Lga; isModified = true; }
            if (update.Ward != null && update.Ward != farmer.Ward) { farmer.Ward = update.Ward; isModified = true; }
            if (update.PhoneNumber != null && update.PhoneNumber != farmer.PhoneNumber) { farmer.PhoneNumber = update.PhoneNumber; isModified = true; }
            if (update.Email != null && update.Email != farmer.Email) { farmer.Email = update.Email; isModified = true; }
            if (update.Gender != null && update.Gender != farmer.Gender) { farmer.Gender = update.Gender; isModified = true; }
            if (update.DateOfBirth != null && update.DateOfBirth != farmer.DateOfBirth) { farmer.DateOfBirth = update.DateOfBirth; isModified = true; }
            // Note: entity Farmer does not persist some DTO fields (FarmerImage, OrganizationCode, NIN, BVN, IsNinVerified).
            // Those are stored in other tables (FarmerImages, FarmerOrganizations, SubAccounts) and should be updated via their respective repositories/services.
            if (update.FarmerCategory != null && update.FarmerCategory != farmer.FarmerCategory) { farmer.FarmerCategory = update.FarmerCategory; isModified = true; }
            if (update.YearsInAgric.HasValue && update.YearsInAgric.Value != farmer.YearsInAgric) { farmer.YearsInAgric = update.YearsInAgric.Value; isModified = true; }
            if (update.HighestLevelOfEducation != null && update.HighestLevelOfEducation != farmer.HighestLevelOfEducation) { farmer.HighestLevelOfEducation = update.HighestLevelOfEducation; isModified = true; }
            if (update.SelectedItems != null)
            {
                await UpdateFarmerValueChainItems(farmer.Id, update.SelectedItems);
                isModified = true;
            }
            if (!string.IsNullOrEmpty(update.OrganizationCode))
            {
                bool exists = await _context.FarmerOrganizations.AnyAsync(x =>
                    x.FarmerId == farmer.Id &&
                    x.OrganizationCode == update.OrganizationCode);

                if (!exists)
                {
                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                    {
                        FarmerId = farmer.Id,
                        OrganizationCode = update.OrganizationCode,
                        OnboardedByUserId = farmer.OnboardedByUserId
                    });
                }
            }
            if (update.CooperativeIds != null)
            {
                var existing = _context.FarmerCooperatives.Where(fc => fc.FarmerId == farmer.Id);
                _context.FarmerCooperatives.RemoveRange(existing);
                foreach (var coopId in update.CooperativeIds.Distinct())
                {
                    await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                    {
                        FarmerId = farmer.Id,
                        CooperativeId = coopId,
                        OnboardedByUserId = farmer.OnboardedByUserId,
                        JoinedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (update.AgentIds != null)
            {
                var existingAgents = _context.FarmerAgents.Where(fa => fa.FarmerId == farmer.Id);
                _context.FarmerAgents.RemoveRange(existingAgents);
                foreach (var agentId in update.AgentIds.Distinct())
                {
                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agentId,
                        AssignedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(update.NIN) || !string.IsNullOrWhiteSpace(update.BVN))
            {
                var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == farmer.UserId);
                if (subAccount != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != subAccount.NIN)
                    {
                        subAccount.NIN = update.NIN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != subAccount.Bvn)
                    {
                        subAccount.Bvn = update.BVN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }
                }
                else
                {
                    var newSub = new SubAccount
                    {
                        UserId = farmer.UserId,
                        NIN = update.NIN,
                        Bvn = update.BVN,
                        CreatedOn = DateTimeOffset.UtcNow
                    };
                    await _context.SubAccounts.AddAsync(newSub);
                    isModified = true;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == farmer.UserId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != user.NIN)
                    {
                        user.NIN = update.NIN;
                        _context.Users.Update(user);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != user.BVN)
                    {
                        user.BVN = update.BVN;
                        _context.Users.Update(user);
                        isModified = true;
                    }
                }
            }

            if (isModified)
            {
                _context.Farmers.Update(farmer);
                await _context.SaveChangesAsync();
            }

            return farmer.Map();
        }

        private async Task UpdateFarmerValueChainItems(long farmerId, List<ValueChainSelectionDto> valueChainSelections)
        {
            var existingItems = await _context.FarmerValueChainItems
                .Where(x => x.FarmerId == farmerId)
                .ToListAsync();

            var incomingItems = valueChainSelections
                .SelectMany(v => v.Items.Select(item => new { v.ValueChainType, ItemName = item }))
                .ToList();

            foreach (var incoming in incomingItems)
            {
                bool exists = existingItems.Any(e =>
                    e.ValueChainType == incoming.ValueChainType &&
                    e.ItemName == incoming.ItemName);

                if (!exists)
                {
                    _context.FarmerValueChainItems.Add(new FarmerValueChainItem
                    {
                        FarmerId = farmerId,
                        ValueChainType = incoming.ValueChainType,
                        ItemName = incoming.ItemName
                    });
                }
            }

            var toRemove = existingItems
                .Where(e => !incomingItems.Any(i =>
                    i.ValueChainType == e.ValueChainType &&
                    i.ItemName == e.ItemName))
                .ToList();

            _context.FarmerValueChainItems.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
        }

        private IQueryable<BaseUserDto> GetOrganizationBaseUsers(string orgCode)
        {
            return _context.UserOrganizations
                .AsNoTracking()
                .Where(uo => uo.OrganizationCode == orgCode && uo.IsActive)
                .Join(_context.Users.AsNoTracking(),
                    uo => uo.UserId,
                    u => u.Id,
                    (uo, u) => new BaseUserDto
                    {
                        UserId = u.Id,
                        CreatedOn = u.CreatedOn
                    })
                .GroupBy(x => x.UserId)
                .Select(g => new BaseUserDto
                {
                    UserId = g.Key,
                    CreatedOn = g.Max(x => x.CreatedOn)
                });
        }
        public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(
    ListQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(args.organizationCode))
                throw new ValidationException("Organization code is required");

            args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

            var skip = (args.PageNumber - 1) * args.PageSize;


            var query =
                from fo in _context.FarmerOrganizations.AsNoTracking()

                where fo.OrganizationCode == args.organizationCode
                      && fo.IsActive

                join f in _context.Farmers.AsNoTracking()
                    on fo.FarmerId equals f.Id

                join u in _context.Users.AsNoTracking()
                    on f.UserId equals u.Id

                select new OrganizationFarmerResponse
                {
                    UserId = u.Id,

                    FarmerId = f.Id,

                    Name = f.Name ?? u.Name,

                    PhoneNumber = u.PhoneNumber,

                    Email = u.Email,

                    Gender = f.Gender,

                    State = f.State,

                    Lga = f.Lga,

                    Ward = f.Ward,

                    Location = f.Location,

                    FarmerCategory = f.FarmerCategory,

                    Level = EnumExtensions.GetDisplayName(
                        (FarmerLevel)f.Level),

                    CreatedOn = u.CreatedOn,

                    FarmerOnboardingStatus =
                        u.FarmerOnboardingStatus.ToString(),

                    IsPendingOnboarding = false,

                    IsActive = fo.IsActive
                };


            var totalCount = await query.CountAsync();


            var data = await query
                .OrderByDescending(x => x.CreatedOn)
                .Skip(skip)
                .Take(args.PageSize)
                .ToListAsync();


            return new PagedListResponse<List<OrganizationFarmerResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = data
            };
        }
        /** public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(ListQueryParams args)
         {
             if (string.IsNullOrWhiteSpace(args.organizationCode))
                 throw new ValidationException("Organization code is required");

             args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
             args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

             var skip = (args.PageNumber - 1) * args.PageSize;
             var orgCode = args.organizationCode;

             var baseUsers = GetOrganizationBaseUsers(orgCode);

             var query =
                 from b in baseUsers
                 join u in _context.Users.AsNoTracking()
                     on b.UserId equals u.Id

                 join f in _context.Farmers.AsNoTracking()
                     on u.Id equals f.UserId into fj
                 from f in fj.DefaultIfEmpty()

                 join fo in _context.FarmerOrganizations.AsNoTracking()
                     .Where(x => x.OrganizationCode == orgCode 
                     && x.IsActive
                     )
                     on f.Id equals fo.FarmerId into foj
                 from fo in foj.DefaultIfEmpty()

                 select new OrganizationFarmerResponse
                 {
                     UserId = u.Id,
                     FarmerId = f != null ? f.Id : 0,
                     Name = f != null ? (f.Name ?? u.Name) : u.Name,
                     PhoneNumber = u.PhoneNumber,
                     Email = u.Email,
                     Gender = f != null ? f.Gender : null,
                     State = f != null ? f.State : null,
                     Lga = f != null ? f.Lga : null,
                     Ward = f != null ? f.Ward : null,
                     Location = f != null ? f.Location : null,
                     FarmerCategory = f != null ? f.FarmerCategory : null,
                     Level = f != null
                         ? EnumExtensions.GetDisplayName((FarmerLevel)f.Level)
                         : "Pending",
                     CreatedOn = b.CreatedOn,
                     FarmerOnboardingStatus = u.FarmerOnboardingStatus.ToString(),
                     IsPendingOnboarding = fo == null || !fo.IsActive,
                     IsActive = fo != null && fo.IsActive
                 };

             var totalCount = await baseUsers.CountAsync();

             var data = await query
                 .OrderByDescending(x => x.CreatedOn)
                 .Skip(skip)
                 .Take(args.PageSize)
                 .ToListAsync();

             return new PagedListResponse<List<OrganizationFarmerResponse>>
             {
                 PageNumber = args.PageNumber,
                 PageSize = args.PageSize,
                 Count = totalCount,
                 Data = data
             };
         }
      
         **/

        public async Task<OrganizationFarmerDetailResponse> GetFarmerDetailsAsync(ListQueryParams query)
        {
            if (string.IsNullOrWhiteSpace(query.organizationCode))
                throw new ValidationException("Organization code is required");

            var farmerData = await (
                from fo in _context.FarmerOrganizations
                join f in _context.Farmers on fo.FarmerId equals f.Id
                join u in _context.Users on f.UserId equals u.Id into userJoin
                from u in userJoin.DefaultIfEmpty()
                where fo.OrganizationCode == query.organizationCode
                      && f.Id == query.Id
                select new { Farmer = f, User = u }
            ).FirstOrDefaultAsync();

            if (farmerData == null)
                throw new NotFoundException("Farmer not found in this organization");

            var farmer = farmerData.Farmer;
            var user = farmerData.User;

            var farmerImage = await _context.FarmerImages
                .Where(x => x.FarmerUserId == farmer.UserId)
                .Select(x => x.IMageKey)
                .FirstOrDefaultAsync();

            var valueChainItems = await _context.FarmerValueChainItems
                .Where(x => x.FarmerId == farmer.Id)
                .Select(x => new FarmerValueChainItemDto
                {
                    ValueChainType = x.ValueChainType,
                    ItemName = x.ItemName
                })
                .ToListAsync();

            var farms = await _context.Farms
                .Where(f => f.FarmerId == farmer.Id)
                .ToListAsync();

            var agents = await (
                from fa in _context.FarmerAgents
                join a in _context.Agents on fa.AgentId equals a.Id
                where fa.FarmerId == farmer.Id
                select new OrganizationFarmerAgentDto
                {
                    AgentId = a.Id,
                    AgentName = a.Name,
                    AgentUserId = a.UserId,
                    AssignedOn = fa.AssignedOn,
                    Active = a.IsActive
                })
                .ToListAsync();

            var memberships = await (
                from fc in _context.FarmerCooperatives
                join c in _context.MemberShips on fc.CooperativeId equals c.Id
                where fc.FarmerId == farmer.Id
                select new OrganizationFarmerCooperativeDto
                {
                    MembershipId = c.Id,
                    MembershipName = c.Name,
                    JoinedOn = fc.JoinedOn,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return new OrganizationFarmerDetailResponse
            {
                FarmerId = farmer.Id,
                UserId = farmer.UserId,
                Name = farmer.Name,
                PhoneNumber = farmer.PhoneNumber,
                Email = farmer.Email,
                Gender = farmer.Gender,
                Dob = farmer.DateOfBirth,
                BVN = user?.BVN,
                NIN = user?.NIN,
                FarmerImage = farmerImage,

                State = farmer.State,
                Lga = farmer.Lga,
                Ward = farmer.Ward,
                Location = farmer.Location,

                FarmerCategory = farmer.FarmerCategory,
                Level = EnumExtensions.GetDisplayName(farmer.Level),
                CreatedOn = farmer.OnBoardedOn,

                YearsInAgric = farmer.YearsInAgric,
                FarmSize = farms.Sum(x => x.FarmSize),

                ValueChainItems = valueChainItems,
                Agents = agents,
                Memberships = memberships
            };
        }
        public async Task<OrganizationFarmerDashboardResponse>
    GetOrganizationFarmerDashboardAsync(ListQueryParams args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (string.IsNullOrWhiteSpace(args.organizationCode))
                throw new ValidationException("Invalid Organization");

            var orgCode = args.organizationCode;

            var farmerQuery =
                from fo in _context.FarmerOrganizations.AsNoTracking()

                where fo.OrganizationCode == orgCode

                join f in _context.Farmers.AsNoTracking()
                    on fo.FarmerId equals f.Id

                join u in _context.Users.AsNoTracking()
                    on f.UserId equals u.Id

                select new
                {
                    UserId = u.Id,
                    FarmerId = f.Id,
                    IsActive = fo.IsActive,
                    HasCompleteProfile =u.FarmerOnboardingStatus != FarmerOnboardingStatus.Completed
                };


            var totalFarmers = await farmerQuery
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();


            var activeFarmers = await farmerQuery
                .Where(x => x.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();


            var incompleteProfile = await farmerQuery
                .Where(x => !x.HasCompleteProfile)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();


            return new OrganizationFarmerDashboardResponse
            {
                TotalFarmers = activeFarmers,//totalFarmers,

                ActiveFarmers = activeFarmers,

                //IncompleteProfile = incompleteProfile,

                FarmersWithLoans = 0,

                RecoveryRate = 0
            };
        }
    

        public async Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args)
        {
            var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == args.UserId)
                .AsNoTracking();

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new OrganizationFarmerSalesSummary
                {
                    TotalVolumeSold = g.Sum(x => (decimal?)x.QuantitySoldToday) ?? 0,
                    TotalRevenue = g.Sum(x => (decimal?)x.TotalPriceSold) ?? 0,
                    AverageSalesValue = g.Average(x => (decimal?)x.TotalPriceSold) ?? 0
                })
                .FirstOrDefaultAsync();

            return summary ?? new OrganizationFarmerSalesSummary
            {
                TotalVolumeSold = 0,
                TotalRevenue = 0,
                AverageSalesValue = 0
            };
        }


        #endregion FarmerMgt
    }
}
