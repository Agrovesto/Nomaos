using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class HashingHelper
    {
        public static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);

            var builder = new StringBuilder();

            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2")); // lowercase hex
            }

            return builder.ToString();
        }
    }
}
