using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
   
        public static class NumberToWords
        {
            private static readonly string[] Ones =
            {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        };

            private static readonly string[] Tens =
            {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

            /// <summary>Converts a Naira amount (with optional kobo) into words. Returns just the words, e.g. "Two Hundred and Fifty Thousand Naira Only".</summary>
            public static string Convert(decimal amount)
            {
                if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

                long naira = (long)Math.Truncate(amount);
                int kobo = (int)Math.Round((amount - naira) * 100, MidpointRounding.AwayFromZero);

                var sb = new StringBuilder();
                sb.Append(naira == 0 ? "Zero" : ConvertWholeNumber(naira));
                sb.Append(" Naira");

                if (kobo > 0)
                {
                    sb.Append(", ");
                    sb.Append(ConvertWholeNumber(kobo));
                    sb.Append(" Kobo");
                }

                sb.Append(" Only");
                return sb.ToString();
            }

            private static string ConvertWholeNumber(long number)
            {
                if (number == 0) return "";

                var sb = new StringBuilder();

                if (number >= 1_000_000_000)
                {
                    sb.Append(ConvertWholeNumber(number / 1_000_000_000)).Append(" Billion ");
                    number %= 1_000_000_000;
                }
                if (number >= 1_000_000)
                {
                    sb.Append(ConvertWholeNumber(number / 1_000_000)).Append(" Million ");
                    number %= 1_000_000;
                }
                if (number >= 1_000)
                {
                    sb.Append(ConvertWholeNumber(number / 1_000)).Append(" Thousand ");
                    number %= 1_000;
                }
                if (number >= 100)
                {
                    sb.Append(ConvertWholeNumber(number / 100)).Append(" Hundred ");
                    number %= 100;
                    if (number > 0) sb.Append("and ");
                }
                if (number >= 20)
                {
                    sb.Append(Tens[number / 10]).Append(' ');
                    number %= 10;
                    if (number > 0) sb.Append(Ones[number]).Append(' ');
                }
                else if (number > 0)
                {
                    sb.Append(Ones[number]).Append(' ');
                }

                return sb.ToString().Trim();
            }
       
    }
}
