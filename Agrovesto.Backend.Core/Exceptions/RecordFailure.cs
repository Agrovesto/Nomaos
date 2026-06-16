using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.MandEService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Exceptions
{
    public static class Failure
    {
        public static void RecordFailure(
            BulkFarmerUploadResultDto result, BulkFarmerRowDto row, string reason)
        {
            result.Failed++;
            result.Errors.Add(new FarmerRowErrorDto
            {
                RowIndex = row.RowIndex,
                PhoneNumber = row.PhoneNumber ?? "(missing)",
                Reason = reason,
            });
        }
        
    }
}
