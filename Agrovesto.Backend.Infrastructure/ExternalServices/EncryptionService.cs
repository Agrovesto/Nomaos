using Agrovesto.Backend.Core.Models.Common;
using Amazon.Extensions.NETCore.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Agrovesto.Backend.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class EncryptionService : IEncryptionService
    {
        private readonly AesOptions _aesOptions;
        public EncryptionService(IOptions<AesOptions> aesOptions)
        {
            _aesOptions = aesOptions.Value;
        }

        public string DecryptAes(string encrypttextbase64)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[32];
                byte[] iv = new byte[16];
                Array.Copy(Encoding.UTF8.GetBytes(_aesOptions.Key), keyBytes, 32);
                Array.Copy(Encoding.UTF8.GetBytes(_aesOptions.Iv), iv, 16);

                // Decrypt the text
                byte[] ecryptByte = Convert.FromBase64String(encrypttextbase64); //Encoding.UTF8.GetBytes(encrypttext.Substring(0, encrypttext.Length));
                var decryptor = aes.CreateDecryptor(keyBytes, iv);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(ecryptByte, 0, ecryptByte.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        /// <summary>
        /// return base 64 of the encrypted value
        /// </summary>
        /// <param name="plaintext"></param>
        /// <returns></returns>
        public string EncryptAes(string plaintext)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[32];
                byte[] iv = new byte[16];
                Array.Copy(Encoding.UTF8.GetBytes(_aesOptions.Key), keyBytes, 32);
                Array.Copy(Encoding.UTF8.GetBytes(_aesOptions.Iv), iv, 16);
                aes.Key = keyBytes;
                aes.IV = iv;

                byte[] textBytes = Encoding.UTF8.GetBytes(plaintext);
                var cryptor = aes.CreateEncryptor();
                byte[] encryptText = cryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);

                return Convert.ToBase64String(encryptText);
            }
        }
    }

}
