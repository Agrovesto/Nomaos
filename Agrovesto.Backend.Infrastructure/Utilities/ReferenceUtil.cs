using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{

    public static class ReferenceUtil
    {
        public static string GenerateInvoiceReference(Organization org, string seasonCode, DateTime date, Guid beneficiaryId, int serial)
        {
            // ORG-<SEASON>-YY-<beneficiarySuffix>-<serial>
            var year = date.ToString("yy");
            var bene = beneficiaryId.ToString().Substring(0, 4).ToUpper();
            return $"{org.OrganizationCode}-{seasonCode}-{year}-{bene}-{serial:0000}";
        }
        public static string GenerateReceiptReference(Organization org, string seasonCode, DateTime date, Guid beneficiaryId, int serial)
        {
            // ORG-<SEASON>-YY-<beneficiarySuffix>-<serial>
            var year = date.ToString("yy");
            var bene = beneficiaryId.ToString().Substring(0, 4).ToUpper();
            return $"{org.OrganizationCode}-{seasonCode}-{year}-{bene}-{serial:0000}";
        }
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
            string r = gen.Next(0, 1000000).ToString("D3");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateTransferReference();
            }
            return r;
        }
    }

}
