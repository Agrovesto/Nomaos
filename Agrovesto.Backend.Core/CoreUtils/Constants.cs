using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.CoreUtils
{
    public static class Constants
    {
        public static class Policies
        {
            public const string Cors = "CorsPolicy";
        }

        public static class ResponseCodes
        {
            public const string Success = "Agrovesto-000";
            public const string Unknown = "Agrovesto-001";
            public const string NotFound = "Agrovesto-002";
            public const string Failed = "Agrovesto-003";
            public const string Duplicate = "Agrovesto-004";
            public const string Concurrency = "Agrovesto-005";
            public const string FailedValidation = "Agrovesto-006";
            public const string BadRequest = "Agrovesto-007";
            public const string AlreadyExist = "Agrovesto-008";
            public const string AccessDenied = "Agrovesto-403";
            public const string BadRequestParameter = "Agrovesto-009";
            public const string ModelValidation = "Agrovesto-010";
            public const string NotSuccessful = "Agrovesto-011";
            public const string ParameterValidation = "Agrovesto-012";
        }

        public static class RedisKey
        {
            public const string InvalidateToken = "invalidate_token";
        }

        public static class Actors
        {
            public const string AgrovestoActor = "AgrovestoActor";
        }

        public static class Providers
        {
            public const string FCMBWallet = "FCMB";
            public const string SterlingWallet = "CafeOne";
            public const string MTNMOMO = "MOMO";
            public const string ETRANSACT = "Credo";
        }
    }


}
