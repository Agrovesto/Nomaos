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
    }
}
