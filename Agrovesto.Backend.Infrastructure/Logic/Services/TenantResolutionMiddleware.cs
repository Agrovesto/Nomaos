using Agrovesto.Backend.Core.Models;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantContextAccessor tenantAccessor,
            DataContext db)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var businessIdClaim = context.User.FindFirst("BusinessId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            long? businessId = null;

            // STEP 1: Resolve Business
            if (!string.IsNullOrEmpty(businessIdClaim))
            {
                businessId = long.Parse(businessIdClaim);
            }
            else
            {
                // fallback: pick default business
                businessId = await db.UserBusinesses
                    .Where(x => x.UserId == userId && x.IsDefault)
                    .Select(x => (long?)x.BusinessId)
                    .FirstOrDefaultAsync();
            }

            if (businessId == null)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("No active business found");
                return;
            }

            // STEP 2: Load membership
            var membership = await db.UserBusinesses
                .Include(x => x.Business)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.BusinessId == businessId);

            if (membership == null || !membership.IsActive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid business access");
                return;
            }

            // STEP 3: License validation
            var license = await db.BusinessLicenses
                .FirstOrDefaultAsync(x =>
                    x.BusinessId == businessId &&
                    x.IsActive &&
                    x.StartDate <= DateTimeOffset.UtcNow &&
                    x.EndDate >= DateTimeOffset.UtcNow);

            if (license == null)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("License expired");
                return;
            }

            // STEP 4: Load modules
            var modules = await db.BusinessLicenseModules
                .Where(x =>
                    x.BusinessLicenseId == license.Id &&
                    x.IsEnabled)
                .Select(x => x.SystemModule.Code)
                .ToListAsync();

            // STEP 5: Build Tenant Context
            tenantAccessor.Current = new TenantContext
            {
                UserId = userId,
                BusinessId = membership.BusinessId,
                BusinessCode = membership.Business.BusinessCode,
                Role = membership.Role,
                IsLicenseValid = true,
                LicenseExpiry = license.EndDate,
                Modules = modules,
                ThemeColor = membership.Business.ThemeColor,
                LogoUrl = membership.Business.BusinessLogo
            };

            await _next(context);
        }
    }
}
