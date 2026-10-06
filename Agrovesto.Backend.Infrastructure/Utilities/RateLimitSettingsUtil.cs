using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public class RateLimitSettingsUtil
    {
        public RateLimitFeature OTP { get; set; }
        public RateLimitFeature LOGIN { get; set; }
        public RateLimitFeature RESET_PASSWORD { get; set; }
        public RateLimitFeature FORGOT_CREDENTIAL { get; set; }
        public RateLimitFeature FORGOT_CREDENTIAL_IP { get; set; }

        public class RateLimitFeature
        {
            public int MaxAttempts { get; set; }
            public int WindowMinutes { get; set; }
        }
    }
}
