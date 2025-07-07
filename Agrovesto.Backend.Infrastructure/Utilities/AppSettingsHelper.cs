using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class AppSettingsHelper
    {
        private static IConfiguration _config;
        public static void AppSettingConfigure(IConfiguration configuration)
        {
            _config = configuration;
        }

        public static string Setting(string key)
        {
            return _config.GetSection(key).Value;
        }
    }
}
