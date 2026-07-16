using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.Dashboard;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class FarmerTransactionRepository : IFarmerTransactionRepository
    {
        private readonly DataContext _context;
        public FarmerTransactionRepository(DataContext context)
        {
            _context = context;

        }
        public async Task<FarmerSalesSummaryDto?> GetSalesSummary(
    ListQueryParams args)
        {
            var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == args.UserId)
                .AsNoTracking();

            var summary = await query
                .GroupBy(x => 1) 
                .Select(g => new FarmerSalesSummaryDto
                {
                    TotalVolumeSold = g.Sum(x => (decimal?)x.QuantitySoldToday) ?? 0,
                    TotalRevenue = g.Sum(x => (decimal?)x.TotalPriceSold) ?? 0,
                    AverageSalesValue = g.Average(x => (decimal?)x.TotalPriceSold) ?? 0
                })
                .FirstOrDefaultAsync();

            return summary; 
        }
        public async Task<PagedListResponse<List<FarmerSalesHistoryModel>>?> GetSalesHistory(
    ListQueryParams args)
        {
            var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == args.UserId)
                .AsNoTracking(); 
         
            if (args.startDate.HasValue)
                query = query.Where(x => x.CollectionDate >= args.startDate.Value);

            if (args.endDate.HasValue)
                query = query.Where(x => x.CollectionDate <= args.endDate.Value);


            var totalCount = await query.CountAsync();

            if (totalCount == 0)
                return null;

            var data = await query
                .OrderByDescending(x => x.CollectionDate)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .Select(x => new FarmerSalesHistoryModel
                {
                    Id = x.Id,
                    UserId = x.FarmerUserId,
                    ProductSold = x.CropSold,
                    Quantity = x.QuantitySoldToday,
                    UnitPrice = x.CropPricePerUnitSold,
                    Date = x.TransactionActualDate,
                    TotalAmount = x.TotalPriceSold,
                    PaymentStatus = EnumExtensions.GetDisplayName(x.PaymentStatus)
                })
                .ToListAsync();

            return new PagedListResponse<List<FarmerSalesHistoryModel>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = args.PageNumber,
                PageSize = args.PageSize
            };
        }
        #region Report
        public async Task<List<FarmerSalesTrackerModel>> FilterFarmerTransactionForDownload(
string? businessCode,
string? farmerId,
string? state,
string? marketDay,
string? marketName,
string? cropName,
DateTime? startDate,
DateTime? endDate)
        {
            var query = _context.FarmerSalesTracker.AsQueryable();

            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(x => x.BelongToBusinessCode == businessCode);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(x => x.MarketState == state);
            }

            if (!string.IsNullOrWhiteSpace(marketDay))
            {
                query = query.Where(x => x.MarketDay == marketDay);
            }

            if (!string.IsNullOrWhiteSpace(marketName))
            {
                query = query.Where(x => x.MarketName == marketName);
            }
            if (!string.IsNullOrWhiteSpace(cropName))
            {
                query = query.Where(x => x.CropSold == cropName);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.TransactionActualDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.TransactionActualDate <= endDate.Value);
            }

            var farmers = await query.OrderByDescending(x=>x.TransactionActualDate).ToListAsync();

            if (farmers == null || !farmers.Any())
            {
                throw new Exception("No record found");
            }

            return farmers.Select(x => x.Map()).ToList();
        }

        #endregion

        //TODO: get farmer transactions by userId
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmer(string farmerId, int pageNumber = 1,
         int pageSize = 10,
         string? transactionType = null,
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null)
        {
               var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == farmerId)
                .AsQueryable();

            // Apply filters dynamically
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.MarketLocation == location);
            }
            if(query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            // Apply pagination and ordering
            var pagedResult = await query
                .OrderByDescending(x => x.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //TODO: log farmer transaction
        public async Task<(bool status, FarmerSalesTrackerModel data)> LogTransaction(FarmerSalesTrackerModel data)
        {
            var entity = data.Map();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return (true, entity.Map());
        }
        //TODO: get farmer transactions by business
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByBusiness(string businessCode, int pageNumber = 1,
         int pageSize = 10,
         string? transactionType = null,
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null)
        {
            var query =  _context.FarmerSalesTracker
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();

            // Apply filters dynamically
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.MarketLocation == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            // Apply pagination and ordering
            var pagedResult = await query
                .OrderByDescending(x => x.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //TODO: get all farmertransaction by agent
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByAgent(string agentId, int pageNumber = 1,
         int pageSize = 10,
         string? transactionType = null,
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null)
        {
            var query =  _context.FarmerSalesTracker
                .Where(x => x.CollectionOfficerId == agentId)
               .AsQueryable();

            // Apply filters dynamically
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.MarketLocation == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            // Apply pagination and ordering
            var pagedResult = await query
                .OrderByDescending(x => x.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        //TODO: get all farmertransaction by cooperative

        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerSalesTransactionsByCooperativeAsync(
           long cooperativeId,
           int pageNumber = 1,
           int pageSize = 10,
           string? transactionType = null,
           DateTimeOffset? startDate = null,
           DateTimeOffset? endDate = null,
           string? location = null)
        {
            // Base query: join sales transactions to farmers via FarmerCooperatives
            var query =
                from t in _context.FarmerSalesTracker.AsNoTracking()
                join f in _context.Farmers.AsNoTracking()
                    on t.FarmerUserId equals f.UserId

                // Join FarmerCooperatives to handle multiple cooperatives
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on f.Id equals fc.FarmerId
                where fc.CooperativeId == cooperativeId
                select new { Transaction = t, Farmer = f };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(transactionType))
                query = query.Where(x => x.Transaction.SalesType == transactionType);

            if (startDate.HasValue)
                query = query.Where(x => x.Transaction.CollectionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Transaction.CollectionDate <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(x => x.Farmer.Location == location);

            var totalCount = await query.CountAsync();
            if (totalCount == 0)
            {
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Data = new List<FarmerSalesTrackerModel>(),
                    Count = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            // Apply pagination
            var items = await query
                .OrderByDescending(x => x.Transaction.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Transaction)
                .ToListAsync();

            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = items.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //TODO: get all farmertransaction
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactions(
      int pageNumber = 1,
      int pageSize = 10,
      string? transactionType = null,
      DateTimeOffset? startDate = null,
      DateTimeOffset? endDate = null,
      string? location = null)
        {
            //var query = _context.FarmerSalesTracker

            //    .AsQueryable();
            var query = from sale in _context.FarmerSalesTracker
                        join farmer in _context.Farmers
                            on sale.FarmerUserId equals farmer.UserId
                        select new { sale, farmer };
            // Apply filters dynamically
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.sale.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.sale.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.sale.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.sale.MarketLocation == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            

            var pagedResult = await query
                .OrderByDescending(x => x.sale.TransactionActualDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).
                Select(x=> new FarmerSalesTrackerModel
                {
                    BelongToBusinessCode=x.sale.BelongToBusinessCode,
                    CollectionDate=x.sale.CollectionDate,
                    CollectionOfficerId=x.sale.CollectionOfficerId,
                    CostOfPackagingMaterial=x.sale.CostOfPackagingMaterial,
                    CropPricePerUnitSold=x.sale.CropPricePerUnitSold,
                    FarmerUserId=x.sale.FarmerUserId,
                    IntendedPriceToSellPerUnit=x.sale.IntendedPriceToSellPerUnit,
                    IsCropPackaged=x.sale.IsCropPackaged,
                    IsCropTransportedToMarket=x.sale.IsCropTransportedToMarket,
                    LowestUnitQuntity=x.sale.LowestUnitQuntity,
                    MarketLga=x.sale.MarketLga,
                    MeansOfTransport=x.sale.MeansOfTransport,
                    ModeOfPayment=x.sale.ModeOfPayment,
                    OtherCost=x.sale.OtherCost,
                    PackagingType=x.sale.PackagingType,
                    PriceOfLowestUnit=x.sale.PriceOfLowestUnit,
                    QuantityBroughtToMarket=x.sale.QuantityBroughtToMarket,
                    QuantitySoldToday=x.sale.QuantitySoldToday,
                    SalesTypeName=x.sale.SalesType,
                    SoldCropUnitTypeName=x.sale.SoldCropUnitType,
                    supervisorId=x.sale.supervisorId,
                    TransportCost=x.sale.TransportCost,
                    UnitTypeName=x.sale.UnitType,
                    Id = x.sale.Id,
                    CropSold = x.sale.CropSold,
                    TotalPriceSold = x.sale.TotalPriceSold,
                    MarketName = x.sale.MarketName,
                    MarketDay = x.sale.MarketDay,
                    MarketState = x.sale.MarketState,
                    MarketLocation = x.sale.MarketLocation,
                    TotalTimeSpentInMarket = x.sale.TotalTimeSpentInMarket,
                    TransactionActualDate = x.sale.CollectionDate,
                    FarmerName = x.farmer.Name
                })
                .ToListAsync();
            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = pagedResult,//pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }



        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionsByBusinessCode(
            string businessCode,
  int pageNumber = 1,
  int pageSize = 10,
  string? transactionType = null,
  DateTimeOffset? startDate = null,
  DateTimeOffset? endDate = null,
  string? location = null)
        {
            var query = _context.FarmerSalesTracker
                .Where(t=>t.BelongToBusinessCode == businessCode)
                .AsQueryable();

            // Apply filters dynamically
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.MarketLocation == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            // Apply pagination and ordering
            var pagedResult = await query
                .OrderByDescending(x => x.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }


        //TODO: calculate total transaction volume
        //TODO: calculate total transaction volumen based on transaction type
        //TODO: calculate transaction performance (daily, weekly, monthly, yearly)

        //TODO: get most transacted product
        //TODO: get most transacted product by farmer
        //TODO: get most transacted product by location

    }
}
