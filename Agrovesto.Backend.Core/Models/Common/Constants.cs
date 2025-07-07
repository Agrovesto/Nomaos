using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public static class Constants
    {
        public static class ValidationRegex
        {
            public const string General = "^.{0,500}$";
            public const string BusinessCode = "^.{0,255}$";
            public const string Reference = "^.{0,255}$";

        }
    }
}
