using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.Dashboard;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class DashboardManager : IDashboardManager
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IBusinessRepository _businessRepository;

        private readonly ILogger<DashboardManager> _log;
        public DashboardManager(IDashboardRepository dashboardRepository, IBusinessRepository businessRepository,
            ILogger<DashboardManager> log)
        {
            _dashboardRepository = dashboardRepository;
            _businessRepository = businessRepository;
            _log  = log;
        }
        private DateTime ParseDate(string dateStr, string label)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return default;

            if (!DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                throw new Exception($"{label} is not in the correct format dd-MM-yyyy");
            }

            return parsedDate;
        }

        public async Task<Response<FarmerSalesSummary>> GetFarmerDashboardSummaryAsync(

    ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode)
                           ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");
                DateTime? dateFrom = null;
                DateTime? dateTo = null;

                //if (!string.IsNullOrWhiteSpace(query.startDate) || !string.IsNullOrWhiteSpace(query.endDate))
                //{
                //    dateFrom = ParseDate(query.startDate, "Start date");
                //    dateTo = ParseDate(query.endDate, "End date");

                //    dateFrom = dateFrom?.Date;
                //    dateTo = dateTo?.Date.AddDays(1).AddSeconds(-1);
                //}


                var summary = await _dashboardRepository.GetFarmerDashboardSummaryAsync(query.UserId, query.startDate, query.endDate);
                if (summary == null)
                {
                    return new Response<FarmerSalesSummary>
                    {
                        Succeeded = true,
                        Data = null,
                        Message = "No Record Found"
                    };
                }
                
                return new Response<FarmerSalesSummary>
                {
                    Succeeded = true,
                    Data = summary,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving farmer dashboard: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }

        }
    }
}
