using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public interface IRateLimitService
    {
        Task IsRateLimitedAsync(string key, RateLimitType rateLimitType);
    }
    public class RateLimitServiceUtil : IRateLimitService
    {
        private readonly IDistributedCache _cache;
        private readonly RateLimitSettingsUtil _settings;


        public RateLimitServiceUtil(IDistributedCache cache, IOptions<RateLimitSettingsUtil> options)
        {
            _cache = cache;
            _settings = options.Value;
        }

        public async Task IsRateLimitedAsync(string key, RateLimitType rateLimitType)
        {
            int maxAttempts;
            TimeSpan window;

            switch (rateLimitType)
            {
                case RateLimitType.OTP:
                    maxAttempts = _settings.OTP.MaxAttempts;
                    window = TimeSpan.FromMinutes(_settings.OTP.WindowMinutes);
                    break;
                case RateLimitType.LOGIN:
                    maxAttempts = _settings.LOGIN.MaxAttempts;
                    window = TimeSpan.FromMinutes(_settings.LOGIN.WindowMinutes);
                    break;
                case RateLimitType.RESET_PASSWORD:
                    maxAttempts = _settings.RESET_PASSWORD.MaxAttempts;
                    window = TimeSpan.FromMinutes(_settings.RESET_PASSWORD.WindowMinutes);
                    break;
                case RateLimitType.FORGOT_CREDENTIAL:
                    maxAttempts = _settings.FORGOT_CREDENTIAL.MaxAttempts;
                    window = TimeSpan.FromMinutes(_settings.FORGOT_CREDENTIAL.WindowMinutes);
                    break;
                case RateLimitType.FORGOT_CREDENTIAL_IP:
                    maxAttempts = _settings.FORGOT_CREDENTIAL_IP.MaxAttempts;
                    window = TimeSpan.FromMinutes(_settings.FORGOT_CREDENTIAL_IP.WindowMinutes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rateLimitType), "Unknown rate limit type");
            }

            var cacheKey = $"{rateLimitType.ToString().ToLower()}:{key}";

            var cachedValue = await _cache.GetStringAsync(cacheKey);
            int requestCount = 0;
            if (!string.IsNullOrEmpty(cachedValue))
                int.TryParse(cachedValue, out requestCount);

            if (requestCount >= maxAttempts)
                throw new BaseException(ExceptionType.TOO_MANY_REQUESTS, "Too many requests. Please wait before trying again.");

            requestCount++;

            await _cache.SetStringAsync(cacheKey, requestCount.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(window)
                });
        }
    }

}
