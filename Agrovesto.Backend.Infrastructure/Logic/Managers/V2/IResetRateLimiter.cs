using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Google;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IResetRateLimiter
    {
        Task EnsureNotRateLimitedAsync(string userId, string purposeKey);
        Task RecordAttemptAsync(string userId, string purposeKey);
    }
    public class ResetRateLimiter : IResetRateLimiter
    {
        private readonly DataContext _context;
        public ResetRateLimiter(DataContext context) => _context = context;

        public async Task EnsureNotRateLimitedAsync(string userId, string purposeKey)
        {
            var recentCount = await _context.CredentialResetAttempts
                .Where(x => x.UserId == userId && x.PurposeKey == purposeKey
                         && x.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
                .CountAsync();

            if (recentCount >= 3)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Too many reset requests. Please try again later.");
        }

        public async Task RecordAttemptAsync(string userId, string purposeKey)
        {
            _context.CredentialResetAttempts.Add(new CredentialResetAttempt
            {
                UserId = userId,
                PurposeKey = purposeKey,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
