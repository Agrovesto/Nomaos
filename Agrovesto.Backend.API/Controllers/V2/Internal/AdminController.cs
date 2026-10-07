using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/admin")]
    //[Authorize(Policy = "AdminPanel")]
    [ApiController]
    public class AdminController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IAdminDashboardManager _adminDashboardManager;
        public static string AdminUserType { get; private set; } = UserType.Agrovesto.ToString();
        private const int MaxCustomRangeDays = 366 * 5;

        public AdminController(IHttpContextAccessor httpContextAccessor,
          //  IAuthenticationRepository authenticationRepository,
            IOnBoardingManager onBoardingManager,
            IAdminDashboardManager adminDashboardManager) : base(httpContextAccessor)
        {
           // _authenticationRepository = authenticationRepository;
            _onBoardingManager = onBoardingManager;
            _adminDashboardManager = adminDashboardManager;
        }

        /// <summary>
        /// Seed a system/super admin user. This is intended for initial seeding and should be protected in production.
        /// </summary>
        [HttpPost("auth/seed-super-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedSuperAdmin([FromQuery]string businessCode)
        {
            try
            {
                // If caller did not supply a model use a default seed (matches existing DB initializer defaults)
                var seed = new UserModel
                {
                    Email = "reloarhub@maildrop.cc",
                    Password = "Reloarhub144828@",
                    PhoneNumber = "01126201565",
                    UserType = AdminUserType,
                    FirstName = "Reloarhub Solutions",
                    BusinessCode=businessCode
                };

                var result = await _onBoardingManager.RegisterAdmin(seed, "SystemAdmin");

                return result.status
                    ? Ok(new Response<(UserModel model, bool status, string message)>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = true,
                        Message = result.message,
                        Data = result
                    })
                    : BadRequest(new Response<string> { Succeeded = false, Message = result.message, Code = ResponseCodes.Failed });
            }
            catch (BaseException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        /// <summary>
        /// Allow a SystemAdmin user to create admin users (other admins).
        /// </summary>
        [HttpPost("auth/create-admin")]
        [Authorize]
        public async Task<IActionResult> CreateAdmin([FromBody] UserModel model, [FromQuery] string? roleName, [FromQuery] string businessCode)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            // Only allow SystemAdmin to perform this action
            if (!CurrentUserRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
                return Unauthorized(new Response<string> { Succeeded = false, Message = ResponseMessages.AccessDenied, Code = ResponseCodes.AccessDenied });

            try
            {
                var role =roleName;
                model.UserType = AdminUserType;
                model.BusinessCode = businessCode;
                var createUser = await _onBoardingManager.RegisterAdmin(model, role);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = createUser.status,
                    Message = createUser.message,
                    Data = createUser
                });
            }
            catch (BaseException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        /// <summary>
        /// Allow a SystemAdmin user to create other users using the onboarding flow.
        /// </summary>
        //[HttpPost("auth/create-user")]
        //[Authorize]
        //public async Task<IActionResult> CreateUserBySuperAdmin([FromBody] Agrovesto.Backend.Core.V2.Onboarding.OnboardingRequestModel model,
        //    [FromQuery] Agrovesto.Backend.Core.V2.Onboarding.OnBoardingArgs args)
        //{
        //    if (!ModelState.IsValid)
        //        return ValidationProblemResponse();

        //    if (!CurrentUserRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        //        return Unauthorized(new Response<string> { Succeeded = false, Message = ResponseMessages.AccessDenied, Code = ResponseCodes.AccessDenied });

        //    try
        //    {
        //        args.OnboardedByUserId = CurrentUserId;
        //        // Preserve organization resolution behavior from other onboarding endpoints
        //        var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
        //        if (orgError != null) return orgError;
        //        args.organizationCode = resolvedOrgCode;
        //        args.userType = AdminUserType;
        //        var result = await _onBoardingManager.CreateBusinessUser(model, args);
        //        return Ok(Success(result, result.Message, ResponseMessages.Successful));
        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode((int)ex.HttpStatusCode, new Response<string>
        //        {
        //            Message = ex.ExceptionMessage,
        //            Succeeded = false,
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new Response<string>
        //        {
        //            Message = ex.Message,
        //            Succeeded = false,
        //        });
        //    }
        //}

       
        
        [HttpPost("auth/login")]
        [AllowAnonymous]
        [AllowPendingCredentialChange]
        [EnableRateLimiting("admin-login")]
        public async Task<IActionResult> AdminLogin(
        [FromBody] AdminLoginModel model, [FromQuery] string? businessCode)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            return await ExecuteAsync(
                () => _onBoardingManager.AdminLogin(model, businessCode, ip),
                result =>
                {
                    var response = new Response<AdminLoginResponseModel>
                    {
                        Code = result.Status == AdminLoginStatus.Success ? ResponseCodes.Success : ResponseCodes.Failed,
                        Succeeded = result.Status == AdminLoginStatus.Success,
                        Message = result.Message,
                        Data = result.Data
                    };

                    return result.Status switch
                    {
                        AdminLoginStatus.Success => Ok(response),
                        AdminLoginStatus.InvalidCredentials => Unauthorized(response),   // 401
                        _ => StatusCode(StatusCodes.Status403Forbidden, response)        // inactive / no admin access
                    };
                });
        }

        /// <summary>
        /// GET api/admin/dashboard?period=month
        /// GET api/admin/dashboard?period=custom&amp;from=2026-09-01&amp;to=2026-09-30
        /// period: today | week | month | quarter | year | custom
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get([FromQuery] DashboardQuery query,
            
            CancellationToken ct)
        {
            if (query.Period == DashboardPeriod.Custom)
            {
                if (query.From is null || query.To is null)
                    return BadRequest(new { error = "Custom period requires 'from' and 'to' (yyyy-MM-dd)." });

                if (query.From > query.To)
                    return BadRequest(new { error = "'from' must be on or before 'to'." });

                if (query.To.Value.DayNumber - query.From.Value.DayNumber > MaxCustomRangeDays)
                    return BadRequest(new { error = "Custom range is too large." });
            }

            var result = await _adminDashboardManager.GetAsync(query.Period, query.From, query.To, query.Chart, ct);
            return Ok(result);
        }

      

    }
}
