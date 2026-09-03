using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class EncryptionUtil
    {
        public static string MaskSensitiveNumber(string value, int visibleStart = 4, int visibleEnd = 2)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            if (value.Length <= visibleStart + visibleEnd)
                return new string('*', value.Length);

            var maskedLength = value.Length - visibleStart - visibleEnd;

            return value.Substring(0, visibleStart)
                   + new string('*', maskedLength)
                   + value.Substring(value.Length - visibleEnd);
        }
        public static byte[] GetSalt()
        {
            byte[] salt = new byte[256];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        public static string HashSecret(this string secret, byte[] salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
               password: secret,
               salt: salt,
               prf: KeyDerivationPrf.HMACSHA1,
               iterationCount: 10000,
               numBytesRequested: 256 / 8));
        }

        public static string EncryptTransactionPin(this string transactionPin, byte[] salt)
        {
            return HashSecret(transactionPin, salt);
        }

    }
}

