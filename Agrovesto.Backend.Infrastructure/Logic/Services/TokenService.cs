using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class TokenService : ITokenService
    {
        private const int TokenExpiryMinutes = 3;
        private const int TokenLength = 9;

        private readonly DataContext _context;

        public TokenService(DataContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateToken(string ownerId, TokenUse use)
        {
            var value = Guid.NewGuid().ToString("N").Substring(0, TokenLength);

            _context.Tokens.Add(new Token
            {
                OwnerId = ownerId,
                Use = use,
                Value = value,
                GeneratedOn = DateTimeOffset.UtcNow,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            return value;
        }

        public async Task<(bool isValid, string message)> ValidateToken(string token)
        {
            var record = await _context.Tokens
                .FirstOrDefaultAsync(x => x.Value == token);

            if (record == null)
                return (false, "Invalid token");

            record.LastAttemptOn = DateTimeOffset.UtcNow;

            if (!record.IsActive)
            {
                await _context.SaveChangesAsync();
                return (false, "Token is no longer active");
            }

            if (DateTimeOffset.UtcNow - record.GeneratedOn >= TimeSpan.FromMinutes(TokenExpiryMinutes))
            {
                record.IsActive = false;
                await _context.SaveChangesAsync();
                return (false, "Token has expired");
            }

            await _context.SaveChangesAsync();
            return (true, "Token is valid");
        }
    }
}
