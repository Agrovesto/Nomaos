using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class LicenseValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ILicenseService licenseService)
        {
            var businessIdClaim = context.User.FindFirst("BusinessId");

            if (businessIdClaim != null)
            {
                var businessId = long.Parse(businessIdClaim.Value);

                var isValid = await licenseService
                    .IsBusinessLicenseValid(businessId);

                if (!isValid)
                {
                    context.Response.StatusCode = 403;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "Business license has expired."
                    });

                    return;
                }
            }

            await _next(context);
        }
    }
}
