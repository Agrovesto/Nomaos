using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class FarmerExtensions
    {
        public static FarmerModel ApplySubAccount(this FarmerModel farmer, SubAccount sub)
        {
            if (sub == null) return farmer;
            farmer.NIN = EncryptionUtil.MaskSensitiveNumber(sub.NIN);
            farmer.BVN = EncryptionUtil.MaskSensitiveNumber(sub.Bvn);
            return farmer;
        }
    }

}
