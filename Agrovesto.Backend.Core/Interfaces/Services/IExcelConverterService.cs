using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IExcelConverterService
    {
        byte[] ConvertFarmerDetailsToExcel(IEnumerable<FarmerModel> farmers);
        byte[] ConvertFarmerDetailsToPdf(IEnumerable<FarmerModel> farmers);
        byte[] ConvertFarmerTransactionsToExcel(IEnumerable<FarmerSalesTrackerModel> farmers);
        byte[] ConvertFarmerTransactionsToPdf(IEnumerable<FarmerSalesTrackerModel> farmers);
        byte[] ConvertSubscriptionToExcel(IEnumerable<BundleSubscriptionInterestModel> farmers);
        byte[] ConvertSubscriptionToPDF(IEnumerable<BundleSubscriptionInterestModel> farmers);
        Task<byte[]> GenerateFarmerExcel(List<FarmerDetailDto> farmers);
    }
}
