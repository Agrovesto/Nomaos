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

        /// <summary>
        /// A short UUID: the 128 bits of a v4 GUID rendered as URL-safe base64 rather than hex,
        /// so it is 22 characters instead of 36 while keeping the full entropy of the GUID.
        /// </summary>
        public static string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
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

        /// <summary>
        /// Issues a single-use token for <paramref name="purpose"/>. Unlike an OTP the token is
        /// never delivered to the user - it is returned to the caller that proved the user's
        /// identity and replayed on the follow-up request.
        /// </summary>
        Task<TokenResult> GenerateTokenAsync(string userId, OtpPurpose purpose, int expiryMinutes = 3);

        Task<(bool isValid, string message)> ValidateTokenAsync(string userId, string token, OtpPurpose purpose);

        Task InvalidateTokenAsync(string userId, string token, OtpPurpose purpose);
    }

    public class OtpService : IOtpService
    {
        private const int MaxAttempts = 5;

        // OneTimePassword.Destination is required, but a token has nowhere to be sent.
        private const string TokenDestination = "internal";

        // Tokens and OTPs share the table, so the purpose is what tells them apart:
        // these purposes are issued as tokens and are never delivered to the user.
        private static readonly OtpPurpose[] TokenPurposes = { OtpPurpose.WalletPinUpdate };

        private readonly DataContext _context;
        public OtpService(DataContext context)
        {
            _context = context;
        }

        public async Task<OtpResult> GenerateOtpAsync(GenerateOtpRequest request)
        {
            // ---- Rate limit: 3 OTPs in 10 minutes ---
            // Tokens are not user requested, so they don't count towards the limit.
            var recentCount = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == request.UserId &&
                    !TokenPurposes.Contains(x.Purpose) &&
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
                    !TokenPurposes.Contains(x.Purpose) &&
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
                if (otp.AttemptCount >= MaxAttempts)
                    otp.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return null;
            }

            otp.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<TokenResult> GenerateTokenAsync(string userId, OtpPurpose purpose, int expiryMinutes = 3)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "UserId is required to generate a token.");

            // A user only ever holds one live token per purpose.
            await InvalidateActiveTokens(userId, purpose);

            var token = OtpGenerator.GenerateToken();

            var record = new OneTimePassword
            {
                UserId = userId,
                Purpose = purpose,
                // A token has no delivery channel, so Target is left at its default and
                // Destination carries a marker - the column is required by the table.
                Destination = TokenDestination,
                CodeHash = OtpGenerator.Hash(token),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                AttemptCount = 0
            };

            _context.OneTimePasswords.Add(record);
            await _context.SaveChangesAsync();

            return new TokenResult
            {
                Token = token,
                ExpiresAt = new DateTimeOffset(record.ExpiresAt, TimeSpan.Zero)
            };
        }

        public async Task<(bool isValid, string message)> ValidateTokenAsync(string userId, string token, OtpPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Invalid token");

            var record = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
                return (false, "Invalid token");

            record.AttemptCount++;

            if (record.UsedAt != null)
            {
                await _context.SaveChangesAsync();
                return (false, "Token is no longer active");
            }

            if (record.ExpiresAt <= DateTime.UtcNow)
            {
                record.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (false, "Token has expired");
            }

            if (record.CodeHash != OtpGenerator.Hash(token))
            {
                if (record.AttemptCount >= MaxAttempts)
                    record.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return (false, "Invalid token");
            }

            await _context.SaveChangesAsync();
            return (true, "Token is valid");
        }

        public async Task InvalidateTokenAsync(string userId, string token, OtpPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var hash = OtpGenerator.Hash(token);

            var record = await _context.OneTimePasswords
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose &&
                    x.CodeHash == hash &&
                    x.UsedAt == null);

            if (record == null)
                return;

            record.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task InvalidateActiveTokens(string userId, OtpPurpose purpose)
        {
            var activeTokens = await _context.OneTimePasswords
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose &&
                    x.UsedAt == null)
                .ToListAsync();

            if (activeTokens.Count == 0)
                return;

            foreach (var record in activeTokens)
                record.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
