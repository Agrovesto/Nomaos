using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class PhoneNumberHelper
    {
        // +2348034567891 / 2348034567891 / 8034567891 -> 08034567891 ; null if not a valid Nigerian mobile
        public static string? NormalizeNigerian(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("234")) digits = digits[3..];
            if (digits.Length == 10) digits = "0" + digits;

            return Regex.IsMatch(digits, @"^0[789][01]\d{8}$") ? digits : null;
        }

        // 08034567891 -> +234 803 456 7891
        public static string? FormatDisplay(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var d = new string(phone.Where(char.IsDigit).ToArray());
            if (d.StartsWith("234")) d = d[3..];
            if (d.StartsWith("0")) d = d[1..];
            return d.Length == 10 ? $"+234 {d[..3]} {d[3..6]} {d[6..]}" : phone;
        }
    }
}
