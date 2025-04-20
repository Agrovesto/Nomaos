using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class ReportManager: IReportManager
    {
        private readonly IFarmerRepository _farmerRepository;
        private readonly IExcelConverterService _excelConverterService;
        private readonly ILogger<ReportManager> _log;
        public ReportManager(IFarmerRepository farmerRepository,
            ILogger<ReportManager> log, IExcelConverterService excelConverterService)
        {
            _farmerRepository = farmerRepository;
            _excelConverterService = excelConverterService;
            _log = log;
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
        public async Task<FileDownloadModel> FilterFarmerDetailsForDownload(string? businessCode,
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



                var result = await _farmerRepository.FilterFarmerForDownload(businessCode,
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
                        var entry = archive.CreateEntry($"Schedule_{timestamp}.xlsx");
                        using var entryStream = entry.Open();
                        entryStream.Write(excelBytes, 0, excelBytes.Length);
                    }

                    model.FileBytes = ms.ToArray();
                    model.MimeType = "application/zip";
                    model.FileName = $"Schedule_{timestamp}.zip";
                }
                else
                {

                    switch (exportFormat?.ToLower())
                    {
                        case "pdf":
                            model.FileBytes = _excelConverterService.ConvertFarmerDetailsToPdf(result);
                            model.MimeType = "application/pdf";
                            model.FileName = $"Schedule_{timestamp}.pdf";
                            break;

                        case "csv":
                        default:
                            model.FileBytes = _excelConverterService.ConvertFarmerDetailsToExcel(result);
                            model.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            model.FileName = $"Schedule_{timestamp}.xlsx";
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
