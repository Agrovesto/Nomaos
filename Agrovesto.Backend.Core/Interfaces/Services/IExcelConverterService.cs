using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
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
        Task<(List<UserModel> ValidItems, List<(int Row, string ErrorMessage)> Errors)> ExtractFarmersFromExcel(Stream content, BusinessRegistrationArgs args);
        Task<byte[]> GenerateFarmerExcel(List<FarmerDetailDto> farmers);
        List<BulkOnboardingExcelRow> ParseExcel(Stream fileStream);
    }
}
