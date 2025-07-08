using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IIdentityVerificationManager
    {
        Task<BVNVerificationResponse<BVN_data, Verification>> VerifyBvn(BVNDataModel model);
        Task<NINDataVerificationResponse<NINData, Verification>> VerifyNIN(NINDataModel model);
    }
}
