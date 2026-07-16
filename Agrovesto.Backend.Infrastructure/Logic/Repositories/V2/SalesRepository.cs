using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Sales;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface ISalesRepository
    {
        Task<List<FarmerSalesAnalyticsModel>> GetFarmerSalesAnalytics(
            string farmerUserId,
            string groupBy, // "day", "week", "month"
            DateTimeOffset? startDate,
            DateTimeOffset? endDate);
        Task<PagedListResponse<List<OrganizationFarmerSalesHistor>>> FarmerSalesHistory(
           ListQueryParams request, CancellationToken cancellationToken);
        Task<byte[]> ExportFarmerSalesToCsv(string farmerUserId);
        Task<byte[]> ExportFarmerSalesToExcel(string farmerUserId);
    }
    public class SalesRepository : ISalesRepository
    {
        private readonly DataContext _context;
        public SalesRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<FarmerSalesAnalyticsModel>> GetFarmerSalesAnalytics(
    string farmerUserId,
    string groupBy, // "day", "week", "month"
    DateTimeOffset? startDate,
    DateTimeOffset? endDate)
        {
            var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == farmerUserId
                // && x.IsActive
                );

            if (startDate.HasValue)
                query = query.Where(x => x.TransactionActualDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.TransactionActualDate <= endDate.Value);

            // 🔥 Grouping logic
            switch (groupBy?.ToLower())
            {
                case "week":
                    return await query
                        .GroupBy(x => new
                        {
                            Year = x.TransactionActualDate.Year,
                            Week = EF.Functions.DateDiffWeek(DateTimeOffset.MinValue, x.TransactionActualDate)
                        })
                        .Select(g => new FarmerSalesAnalyticsModel
                        {
                            Period = $"Week {g.Key.Week}",
                            TotalSales = g.Sum(x => (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit),
                            TotalQuantity = g.Sum(x => x.QuantitySoldToday),
                            Transactions = g.Count()
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync();

                case "month":
                    return await query
                        .GroupBy(x => new { x.TransactionActualDate.Year, x.TransactionActualDate.Month })
                        .Select(g => new FarmerSalesAnalyticsModel
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                            TotalSales = g.Sum(x => (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit),
                            TotalQuantity = g.Sum(x => x.QuantitySoldToday),
                            Transactions = g.Count()
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync();

                default: // day
                    return await query
                        .GroupBy(x => x.TransactionActualDate.Date)
                        .Select(g => new FarmerSalesAnalyticsModel
                        {
                            Period = g.Key.ToString(),
                            TotalSales = g.Sum(x => (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit),
                            TotalQuantity = g.Sum(x => x.QuantitySoldToday),
                            Transactions = g.Count()
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync();
            }
        }
        public async Task<PagedListResponse<List<OrganizationFarmerSalesHistor>>> FarmerSalesHistory(
     ListQueryParams request, CancellationToken cancellationToken)
        {
            var query = _context.FarmerSalesTracker
                .AsNoTracking()
                .Where(x => x.FarmerUserId == request.UserId);

            if (request.startDate.HasValue)
                query = query.Where(x => x.TransactionActualDate >= request.startDate.Value);

            if (request.endDate.HasValue)
                query = query.Where(x => x.TransactionActualDate <= request.endDate.Value);

            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                if (Enum.TryParse<PaymentStatus>(request.PaymentStatus, true, out var paymentStatus))
                    query = query.Where(x => x.PaymentStatus == paymentStatus);
                else
                    throw new ArgumentException("Invalid payment status value.");
            }

            if (!string.IsNullOrWhiteSpace(request.SalesContext))
            {
                if (Enum.TryParse<SaleContextType>(request.SalesContext, true, out var contextType))
                    query = query.Where(x => x.ContextType == contextType);
                else
                    throw new ArgumentException("Invalid sales context value.");
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Guard against invalid page numbers
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            // If no data, return empty list immediately
            if (totalCount == 0)
            {
                return new PagedListResponse<List<OrganizationFarmerSalesHistor>>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Count = 0,
                    Data = new List<OrganizationFarmerSalesHistor>()
                };
            }

            var data = await query
                .OrderByDescending(x => x.TransactionActualDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new OrganizationFarmerSalesHistor 
                { 
                    Date = x.TransactionActualDate, 
                    Quantity = x.QuantitySoldToday, 
                    UnitPrice = x.PriceOfLowestUnit, 
                    TotalValue = (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit, 
                    ProductSold = x.CropSold, 
                    PaymentStatus = EnumExtensions.GetDisplayName(x.PaymentStatus) 
                }).ToListAsync(cancellationToken);

            return new PagedListResponse<List<OrganizationFarmerSalesHistor>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = data
            };
        }
        public async Task<byte[]> ExportFarmerSalesToCsv(string farmerUserId)
        {
            var data = await _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == farmerUserId
                //&& x.IsActive
                )
                .OrderByDescending(x => x.TransactionActualDate)
                .ToListAsync();

            var builder = new StringBuilder();

            builder.AppendLine("Date,Quantity,UnitPrice,TotalValue,PaymentStatus");

            foreach (var x in data)
            {
                var total = (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit;

                builder.AppendLine($"{x.TransactionActualDate:yyyy-MM-dd},{x.QuantitySoldToday},{x.PriceOfLowestUnit},{total},{EnumExtensions.GetDisplayName(x.PaymentStatus)}");
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        public async Task<byte[]> ExportFarmerSalesToExcel(string farmerUserId)
        {
            var data = await _context.FarmerSalesTracker
              .Where(x => x.FarmerUserId == farmerUserId
              //&& x.IsActive
                )
                .ToListAsync();

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Sales");

            // Header
            sheet.Cells[1, 1].Value = "Date";
            sheet.Cells[1, 2].Value = "Quantity";
            sheet.Cells[1, 3].Value = "Unit Price";
            sheet.Cells[1, 4].Value = "Total Value";
            sheet.Cells[1, 5].Value = "Payment Status";

            // Data
            for (int i = 0; i < data.Count; i++)
            {
                var row = i + 2;
                var x = data[i];

                sheet.Cells[row, 1].Value = x.TransactionActualDate;
                sheet.Cells[row, 2].Value = x.QuantitySoldToday;
                sheet.Cells[row, 3].Value = x.PriceOfLowestUnit;
                sheet.Cells[row, 4].Value = (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit;
                sheet.Cells[row, 5].Value = EnumExtensions.GetDisplayName(x.PaymentStatus);
            }

            return package.GetAsByteArray();
        }
    }
    //public class GetFarmerSalesHistoryHandler
    //: IRequestHandler<GetFarmerSalesHistoryQuery, PagedListResponse<List<OrganizationFarmerSalesHistor>>>
    //{
    //    private readonly DataContext _context;

    //    public GetFarmerSalesHistoryHandler(DataContext context)
    //    {
    //        _context = context;
    //    }

    //    public async Task<PagedListResponse<List<OrganizationFarmerSalesHistor>>> Handle(
    //        GetFarmerSalesHistoryQuery request,
    //        CancellationToken cancellationToken)
    //    {
    //        var query = _context.FarmerSalesTracker
    //            .AsNoTracking()
    //            .Where(x => x.FarmerUserId == request.FarmerUserId 
    //            //&& x.IsActive
    //            );

    //        if (request.StartDate.HasValue)
    //            query = query.Where(x => x.TransactionActualDate >= request.StartDate.Value);

    //        if (request.EndDate.HasValue)
    //            query = query.Where(x => x.TransactionActualDate <= request.EndDate.Value);

    //        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
    //            query = query.Where(x => x.PaymentStatus == request.PaymentStatus);

    //        var totalCount = await query.CountAsync(cancellationToken);

    //        var data = await query
    //            .OrderByDescending(x => x.TransactionActualDate)
    //            .Skip((request.PageNumber - 1) * request.PageSize)
    //            .Take(request.PageSize)
    //            .Select(x => new OrganizationFarmerSalesHistor
    //            {
    //                Date = x.TransactionActualDate,
    //                Quantity = x.QuantitySoldToday,
    //                UnitPrice = x.PriceOfLowestUnit,
    //                TotalValue = (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit,
    //                ProductSold = (decimal)x.QuantitySoldToday * x.PriceOfLowestUnit,
    //                PaymentStatus = EnumExtensions.GetDisplayName(x.PaymentStatus)
    //            })
    //            .ToListAsync(cancellationToken);

    //        return new PagedListResponse<List<OrganizationFarmerSalesHistor>>
    //        {
    //            PageNumber = request.PageNumber,
    //            PageSize = request.PageSize,
    //            Count = totalCount,
    //            Data = data
    //        };
    //    }
    //}
    //public class GetFarmerSalesHistoryQuery : IRequest<PagedListResponse<List<OrganizationFarmerSalesHistor>>>
    //{
    //    public string FarmerUserId { get; set; }
    //    public DateTimeOffset? StartDate { get; set; }
    //    public DateTimeOffset? EndDate { get; set; }
    //    public string PaymentStatus { get; set; }
    //    public int PageNumber { get; set; }
    //    public int PageSize { get; set; }
    //}


}

