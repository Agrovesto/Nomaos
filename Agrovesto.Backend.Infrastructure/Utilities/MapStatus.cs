using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class ProviderMapStatus
    {
        public static string MapStatus(string status)
        {
            return status?.ToLower() switch
            {
                "success" => "SUCCESS",
                "successful" => "SUCCESS",
                "completed" => "SUCCESS",

                "failed" => "FAILED",
                "error" => "FAILED",

                "pending" => "PENDING",

                _ => "FAILED"
            };
        }
    }
}
