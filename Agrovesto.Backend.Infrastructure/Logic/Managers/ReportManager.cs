using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class ReportManager: IReportManager
    {
        private readonly IFarmerRepository _farmerRepository;
        private readonly IFarmerTransactionRepository _farmerTransactionRepository;
        private readonly IInputFinancingManagementRepository _inputFinancingManagementRepository;
        private readonly IExcelConverterService _excelConverterService;
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<ReportManager> _log;
        public ReportManager(IFarmerRepository farmerRepository,
            IFarmerTransactionRepository transactionRepository,
            IInputFinancingManagementRepository inputFinancingManagementRepository,
            ILogger<ReportManager> log, IExcelConverterService excelConverterService, IBusinessRepository businessRepository)
        {
            _inputFinancingManagementRepository = inputFinancingManagementRepository;
            _farmerRepository = farmerRepository;
            _farmerTransactionRepository = transactionRepository;
            _excelConverterService = excelConverterService;
            _log = log;
            _businessRepository = businessRepository;
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

        #region admin
        public async Task<Response<AdminBusinessEntitiesMetrics>> GetCoreBusinessMetrics()
        {
            var bizEntities = new AdminBusinessEntitiesMetrics();
            try
            {
                //get farmer summary
                //get trader summary
                //get business summary
                //get procurement summary
                var business = await _businessRepository.GetBusinessesAsync();
                var farmers = await _farmerRepository.GetFarmers();
                var traders = 126;
                var procurement = 6;

                bizEntities.TotalBusiness = business.LongCount();
                bizEntities.TotalFarmers = farmers.LongCount();
                bizEntities.TotalTraders = traders;
                bizEntities.TotalProcurement = procurement;
                return new Response<AdminBusinessEntitiesMetrics>
                {
                    Succeeded = true,
                    Data = bizEntities,
                    Message = "Retrieved Successfully"
                };

            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        #endregion admin
        public async Task<FileDownloadModel> FilterSubscription(string? businessCode,
string? cooperativeName, string? state,
string? startDate, string? endDate, string? exportFormat)
        {
            try
            {


                DateTime? dateFrom = null;
                DateTime? dateTo = null;

                if (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
                {
                    dateFrom = ParseDate(startDate, "Start date");
                    dateTo = ParseDate(endDate, "End date");

                    dateFrom = dateFrom?.Date;
                    dateTo = dateTo?.Date.AddDays(1).AddSeconds(-1);
                }



                var result = await _inputFinancingManagementRepository.FilterSubscriptionForDownload(businessCode,
                    cooperativeName, state, dateFrom, dateTo);

                if (result.Count == 0)
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");


                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var model = new FileDownloadModel();

                // if schedule reference is not provided
                bool onlyZipCriteria =
                    (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate)
                    && string.IsNullOrWhiteSpace(businessCode));

                if (onlyZipCriteria)
                {

                    var excelBytes = _excelConverterService.ConvertSubscriptionToExcel(result);
                    using var ms = new MemoryStream();
                    using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry($"input_subscription_{timestamp}.xlsx");
                        using var entryStream = entry.Open();
                        entryStream.Write(excelBytes, 0, excelBytes.Length);
                    }

                    model.FileBytes = ms.ToArray();
                    model.MimeType = "application/zip";
                    model.FileName = $"farmer_{timestamp}.zip";
                }
                else
                {

                    switch (exportFormat?.ToLower())
                    {
                        case "pdf":
                            model.FileBytes = _excelConverterService.ConvertSubscriptionToPDF(result);
                            model.MimeType = "application/pdf";
                            model.FileName = $"input_subscription_{timestamp}.pdf";
                            break;

                        case "csv":
                        default:
                            model.FileBytes = _excelConverterService.ConvertSubscriptionToExcel(result);
                            model.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            model.FileName = $"input_subscription_{timestamp}.xlsx";
                            break;
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error retrieving Input Subscription  detail: {ex.Message}");
                throw new NotFoundException($"No Record Found: {ex.Message}");
            }
        }


        public async Task<FileDownloadModel> FilterFarmerDetailsForDownload(string? businessCode,
            string? organizationCode,
 string? cooperativeName, string? state,
string? startDate, string? endDate, string? exportFormat)
        {
            try
            {


                DateTime? dateFrom = null;
                DateTime? dateTo = null;

                if (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
                {
                    dateFrom = ParseDate(startDate, "Start date");
                    dateTo = ParseDate(endDate, "End date");

                    dateFrom = dateFrom?.Date;
                    dateTo = dateTo?.Date.AddDays(1).AddSeconds(-1);
                }



                var result = await _farmerRepository.FilterFarmerForDownload(businessCode, organizationCode,
                    cooperativeName, state,  dateFrom, dateTo);

                if (result.Count == 0)
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");

              
                  var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var model = new FileDownloadModel();

                // if schedule reference is not provided
                bool onlyZipCriteria =
                    (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate) 
                    && string.IsNullOrWhiteSpace(businessCode));

                if (onlyZipCriteria)
                {

                    var excelBytes = _excelConverterService.ConvertFarmerDetailsToExcel(result);
                    using var ms = new MemoryStream();
                    using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry($"farmer_{timestamp}.xlsx");
                        using var entryStream = entry.Open();
                        entryStream.Write(excelBytes, 0, excelBytes.Length);
                    }

                    model.FileBytes = ms.ToArray();
                    model.MimeType = "application/zip";
                    model.FileName = $"farmer_{timestamp}.zip";
                }
                else
                {

                    switch (exportFormat?.ToLower())
                    {
                        case "pdf":
                            model.FileBytes = _excelConverterService.ConvertFarmerDetailsToPdf(result);
                            model.MimeType = "application/pdf";
                            model.FileName = $"farmer_{timestamp}.pdf";
                            break;

                        case "csv":
                        default:
                            model.FileBytes = _excelConverterService.ConvertFarmerDetailsToExcel(result);
                            model.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            model.FileName = $"farmer_{timestamp}.xlsx";
                            break;
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error retrieving farmer detail: {ex.Message}");
                throw new NotFoundException($"No Record Found: {ex.Message}");
            }
        }




        public async Task<FileDownloadModel> FilterFarmerTransactionForDownload(
string? businessCode,
string? OrganizationCode,
string? FarmerUserId,
string? state,
string? marketDay,
string? marketName,
string? cropName,
string? startDate,
string? endDate, string? exportFormat)
        {
            try
            {


                DateTime? dateFrom = null;
                DateTime? dateTo = null;

                if (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
                {
                    dateFrom = ParseDate(startDate, "Start date");
                    dateTo = ParseDate(endDate, "End date");

                    dateFrom = dateFrom?.Date;
                    dateTo = dateTo?.Date.AddDays(1).AddSeconds(-1);
                }
                string farmerId = string.Empty;
                if (!string.IsNullOrEmpty(FarmerUserId))
                {
                    var farmer = await _farmerRepository.GetFarmerByUserId(FarmerUserId, OrganizationCode) ?? throw new Exception("Farmer does not exist");
                    farmerId = farmer.UserId;
                }
              


                var result = await _farmerTransactionRepository.FilterFarmerTransactionForDownload(businessCode,farmerId,state,marketDay,marketName, cropName,dateFrom, dateTo);

                if (result.Count == 0)
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");


                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var model = new FileDownloadModel();

                // if schedule reference is not provided
                bool onlyZipCriteria =
                    (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate)
                    && string.IsNullOrWhiteSpace(businessCode));

                if (onlyZipCriteria)
                {

                    var excelBytes = _excelConverterService.ConvertFarmerTransactionsToExcel(result);
                    using var ms = new MemoryStream();
                    using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry($"transaction{timestamp}.xlsx");
                        using var entryStream = entry.Open();
                        entryStream.Write(excelBytes, 0, excelBytes.Length);
                    }

                    model.FileBytes = ms.ToArray();
                    model.MimeType = "application/zip";
                    model.FileName = $"transaction{timestamp}.zip";
                }
                else
                {

                    switch (exportFormat?.ToLower())
                    {
                        case "pdf":
                            model.FileBytes = _excelConverterService.ConvertFarmerTransactionsToPdf(result);
                            model.MimeType = "application/pdf";
                            model.FileName = $"transaction{timestamp}.pdf";
                            break;

                        case "csv":
                        default:
                            model.FileBytes = _excelConverterService.ConvertFarmerTransactionsToExcel(result);
                            model.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            model.FileName = $"transaction{timestamp}.xlsx";
                            break;
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                _log.LogError($"Error retrieving farmer detail: {ex.Message}");
                throw new NotFoundException($"No Record Found: {ex.Message}");
            }
        }

    }
}
