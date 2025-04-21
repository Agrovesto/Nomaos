using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
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
            var query = from t in _context.FarmerSalesTracker
                        join f in _context.Farmers on t.FarmerUserId equals f.UserId
                        where f.BelongToCooperativeId == cooperativeId
                        select new { Transaction = t, Farmer = f };

            // Apply filters
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(x => x.Transaction.SalesType == transactionType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.Transaction.CollectionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.Transaction.CollectionDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.Farmer.Location == location);
            }
            if (query.Count() == 0) { return null; }
            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Paginated result
            var items = await query
                .OrderByDescending(x => x.Transaction.CollectionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Transaction)
                .ToListAsync();

            return new PagedListResponse<List<FarmerSalesTrackerModel>>
            {
                Data = items.Select(x=>x.Map()).ToList(),
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
            var query = _context.FarmerSalesTracker
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
