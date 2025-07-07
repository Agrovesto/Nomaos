using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IEncryptionService
    {
        string EncryptAes(string plaintext);
        string DecryptAes(string encrypttextbase64);
    }
}
