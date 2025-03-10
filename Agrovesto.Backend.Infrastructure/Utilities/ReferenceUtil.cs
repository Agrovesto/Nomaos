using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{

    public static class ReferenceUtil
    {
        public static string GenerateWalletReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateWalletReferenceNumber();
            }
            return $"w{r}ag";
        }

        public static string GenerateBuyReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateBuyReferenceNumber();
            }
            return $"b{r}ag";
        }

        public static string GenerateSellReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateSellReferenceNumber();
            }
            return $"s{r}ag";
        }

        public static string GenerateWithdrawReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateWithdrawReferenceNumber();
            }
            return $"P{r}aout";
        }

        public static string GenerateTopUpReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateTopUpReferenceNumber();
            }
            return $"Top{r}";
        }
        public static string GeneratePayoutReferenceNumber()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GeneratePayoutReferenceNumber();
            }
            return $"payout{r}";
        }

        public static string GenerateTransferReference()
        {
            Random gen = new Random();
            string r = gen.Next(0, 1000000).ToString("D7");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateTransferReference();
            }
            return r;
        }
    }

}
