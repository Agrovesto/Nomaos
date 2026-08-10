using Agrovesto.Backend.Core.Enums;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(string ownerId, TokenUse use);
        Task<(bool isValid, string message)> ValidateToken(string token);
    }
}
