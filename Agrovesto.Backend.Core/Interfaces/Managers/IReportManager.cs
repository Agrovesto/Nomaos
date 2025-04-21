using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IReportManager
    {
        Task<FileDownloadModel> FilterFarmerDetailsForDownload(string? businessCode, string? cooperativeName, string? state, string? startDate, string? endDate, string? exportFormat);
        Task<FileDownloadModel> FilterFarmerTransactionForDownload(string? businessCode, string? FarmerUserId, string? state, string? marketDay, string? marketName, string? cropName, string? startDate, string? endDate, string? exportFormat);
    }
}
