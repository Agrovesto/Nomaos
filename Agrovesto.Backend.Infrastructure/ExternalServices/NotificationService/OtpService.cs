using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{

    public static class OtpGenerator
    {
        public static string Generate(int length = 6)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            return string.Concat(bytes.Select(b => (b % 10).ToString()));
        }

        public static string Hash(string otp)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(otp)));
        }
    }

    public interface IOtpService
    {
        Task<OtpResult> GenerateOtpAsync(GenerateOtpRequest request);

        Task InvalidateActiveOtps(string userId, OtpTarget target);

        Task<OneTimePassword> VerifyOtpAsync(
            string userId,
            string code,
            OtpPurpose purpose);
    }

    public class OtpService : IOtpService
    {
        private readonly DataContext _context;
        public OtpService(DataContext context)
        {
            _context = context;
        }

        public async Task<OtpResult> GenerateOtpAsync(GenerateOtpRequest request)
        {
            // ---- Rate limit: 3 OTPs in 10 minutes ----
            var recentCount = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == request.UserId &&
                    x.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
                .CountAsync();

            if (recentCount >= 3)
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Too many OTP requests. Please try again later.");

            // Invalidate existing
            await InvalidateActiveOtps(request.UserId, request.Target);

            var code = OtpGenerator.Generate();
            var hash = OtpGenerator.Hash(code);

            var otp = new OneTimePassword
            {
                UserId = request.UserId,
                Target = request.Target,
                Destination = request.Destination,
                Purpose = request.Purpose,
                CodeHash = hash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpiryMinutes),
                AttemptCount = 0
            };

            _context.OneTimePasswords.Add(otp);
            await _context.SaveChangesAsync();

            return new OtpResult
            {
                Code = code,
                Destination=otp.Destination,
                ExpiresAt = otp.ExpiresAt
            };
        }

        public async Task InvalidateActiveOtps(string userId, OtpTarget target)
        {
            var activeOtps = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == userId &&
                    x.Target == target &&
                    x.UsedAt == null
                    //&&
                    //x.ExpiresAt > DateTime.UtcNow
                    )
                .ToListAsync();

            foreach (var otp in activeOtps)
                otp.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

        }

        public async Task<OneTimePassword> VerifyOtpAsync(string userId, string code, OtpPurpose purpose)
        {
            var hash = OtpGenerator.Hash(code);

            var otp = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose &&
                    x.UsedAt == null &&
                    x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return null;

            otp.AttemptCount++;

            if (otp.CodeHash != hash)
            {
                if (otp.AttemptCount >= 5)
                    otp.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return null;
            }

            otp.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return otp;
        }
    }
}
