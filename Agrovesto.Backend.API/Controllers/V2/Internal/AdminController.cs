using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

        public readonly IAdminUserManagementManager _adminUserManagementManager;
        public static string AdminUserType { get; private set; } = UserType.Agrovesto.ToString();
        private const int MaxCustomRangeDays = 366 * 5;

        public AdminController(IHttpContextAccessor httpContextAccessor,
          //  IAuthenticationRepository authenticationRepository,
            IOnBoardingManager onBoardingManager,
            IAdminDashboardManager adminDashboardManager,
            IAdminUserManagementManager adminUserManagementManager) : base(httpContextAccessor)
        {
           // _authenticationRepository = authenticationRepository;
            _onBoardingManager = onBoardingManager;
            _adminDashboardManager = adminDashboardManager;
            _adminUserManagementManager = adminUserManagementManager;
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

        ///// <summary>
        ///// Allow a SystemAdmin user to create admin users (other admins).
        ///// </summary>
        //[HttpPost("auth/create-admin")]
        //[Authorize]
        //public async Task<IActionResult> CreateAdmin([FromBody] UserModel model, [FromQuery] string? roleName, [FromQuery] string businessCode)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new Response<Dictionary<string, string>>()
        //        {
        //            Message = "Validation failed.",
        //            Succeeded = false,
        //            Data = ModelState
        //                .Where(ms => ms.Value!.Errors.Any())
        //                .ToDictionary(
        //                    ms => ms.Key,
        //                    ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
        //                )
        //        });
        //    }

        //    // Only allow SystemAdmin to perform this action
        //    if (!CurrentUserRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase) || !CurrentUserRoles.Contains("Super Admin",StringComparer.OrdinalIgnoreCase))
        //        return Unauthorized(new Response<string> { Succeeded = false, Message = ResponseMessages.AccessDenied, Code = ResponseCodes.AccessDenied });

        //    try
        //    {
        //        var role =roleName;
        //        model.UserType = AdminUserType;
        //        model.BusinessCode = businessCode;
        //        var createUser = await _onBoardingManager.RegisterAdmin(model, role);

        //        return Ok(new Response<(UserModel model, bool status, string message)>()
        //        {
        //            Code = ResponseCodes.Success,
        //            Succeeded = createUser.status,
        //            Message = createUser.message,
        //            Data = createUser
        //        });
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

        ///// <summary>
        ///// Allow a SystemAdmin user to create other users using the onboarding flow.
        ///// </summary>
        ////[HttpPost("auth/create-user")]
        ////[Authorize]
        ////public async Task<IActionResult> CreateUserBySuperAdmin([FromBody] Agrovesto.Backend.Core.V2.Onboarding.OnboardingRequestModel model,
        ////    [FromQuery] Agrovesto.Backend.Core.V2.Onboarding.OnBoardingArgs args)
        ////{
        ////    if (!ModelState.IsValid)
        ////        return ValidationProblemResponse();

        ////    if (!CurrentUserRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        ////        return Unauthorized(new Response<string> { Succeeded = false, Message = ResponseMessages.AccessDenied, Code = ResponseCodes.AccessDenied });

        ////    try
        ////    {
        ////        args.OnboardedByUserId = CurrentUserId;
        ////        // Preserve organization resolution behavior from other onboarding endpoints
        ////        var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
        ////        if (orgError != null) return orgError;
        ////        args.organizationCode = resolvedOrgCode;
        ////        args.userType = AdminUserType;
        ////        var result = await _onBoardingManager.CreateBusinessUser(model, args);
        ////        return Ok(Success(result, result.Message, ResponseMessages.Successful));
        ////    }
        ////    catch (BaseException ex)
        ////    {
        ////        return StatusCode((int)ex.HttpStatusCode, new Response<string>
        ////        {
        ////            Message = ex.ExceptionMessage,
        ////            Succeeded = false,
        ////        });
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        return StatusCode(500, new Response<string>
        ////        {
        ////            Message = ex.Message,
        ////            Succeeded = false,
        ////        });
        ////    }
        ////}

       
        
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
            var userAgent = Request.Headers["User-Agent"].ToString();

            return await ExecuteAsync(
                () => _onBoardingManager.AdminLogin(model, businessCode, ip, userAgent),
                result =>
                {
                    var response = new Response<AdminLoginResponseModel>
                    {
                        Code = result.Status == AdminLoginStatus.Success ? AdminResponseCodes.Success : AdminResponseCodes.Failed,
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
     
            return Ok(new Response<DashboardDto>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful,
                Data = result
            });
        }


        // GET api/admin/users/summary
        [HttpGet("user/summary")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> Summary(CancellationToken ct)
        {
            var result = await _adminUserManagementManager.GetSummaryAsync(ct);

            return Ok(new Response<UserManagementSummaryDto>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful,
                Data = result
            });
        }

        // GET api/admin/users?search=&status=&role=&department=&page=1&pageSize=20
        [HttpGet("user")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> List([FromQuery] AdminUserListQuery q, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.GetUsersAsync(q, ct);
            return Ok(new Response<PagedResult<AdminUserListItemDto>>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful,
                Data = result
            });
        }

        // GET api/admin/users/export?format=csv|xlsx (accepts the same filters as the list)
        [HttpGet("user/export")]
        [HasPermission(Permissions.Users.Export)]
        public async Task<IActionResult> Export([FromQuery] AdminUserListQuery q, [FromQuery] string format = "csv", CancellationToken ct = default)
        {
            try
            {
                var (content, type, name) = await _adminUserManagementManager.ExportAsync(q, format, ct);
                return File(content, type, name);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // GET api/admin/roles
        [HttpGet("roles")]
        [HasPermission(Permissions.Users.Invite)]
        public async Task<IActionResult> Roles(CancellationToken ct)
        {
            var result = await _adminUserManagementManager.GetAssignableRolesAsync(ct);
            return Ok(new Response<IReadOnlyList<AdminRoleOptionDto>>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful,
                Data = result
            }); 
        }

        // POST api/admin/users/invite
        [HttpPost("user/invite")]
        [HasPermission(Permissions.Users.Invite)]
        public async Task<IActionResult> Invite([FromBody] InviteAdminUserRequest req, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.InviteAsync(req, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

            if (result.Data is null)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<InviteAdminUserResponse>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                // User is created either way. Tell the UI when only the email failed.
                Message = result.Data.EmailSent
                    ? ResponseMessages.Successful
                    : "User created, but the invitation email could not be sent. Use Resend invitation.",
                Data = result.Data
            });
        }

        // POST api/admin/user/{adminId}/resend-invite
        [HttpPost("user/{adminId}/resend-invite")]
        [HasPermission(Permissions.Users.Invite)]
        public async Task<IActionResult> Resend(string adminId, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.ResendAsync(adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

            if (result.Data is null)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<InviteAdminUserResponse>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = result.Data.EmailSent
                    ? ResponseMessages.Successful
                    : "The invitation email could not be sent. Please try again.",
                Data = result.Data
            });
        }

        // POST api/admin/invitations/accept (public, used by the invitee)
        [AllowAnonymous]
        [HttpPost("invitations/accept")]
        public async Task<IActionResult> Accept([FromBody] AcceptInviteRequest req, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.AcceptAsync(req, ct);

            if (!result.Success)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<object>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful
            });
        }

        // Shared failure response, same Response<T> shape with Succeeded = false
        private IActionResult Failure(int statusCode, string? error) =>
            StatusCode(statusCode, new Response<object>()
            {
                Code = AdminResponseCodes.Failed,
                Succeeded = false,
                Message = error
            });


        // POST api/admin/users/{adminId}/deactivate
        [HttpPost("user/{adminId}/deactivate")]
        [HasPermission(Permissions.Users.Manage)]
        public async Task<IActionResult> Deactivate(string adminId, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.DeactivateUserAsync(
                adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

            if (result.Data is null)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<AdminUserStatusDto>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = "User deactivated successfully.",
                Data = result.Data
            });
        }


        // GET api/admin/users/{adminId}
        [HttpGet("user/{adminId}")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUser(string adminId, CancellationToken ct)
            => Respond(await _adminUserManagementManager.GetUserDetailAsync(adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct));

        // GET api/admin/users/{adminId}/access
        [HttpGet("user/{adminId}/access")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUserAccess(string adminId, CancellationToken ct)
            => Respond(await _adminUserManagementManager.GetUserAccessAsync(adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!,ct));

        // GET api/admin/users/{adminId}/activity?page=1&pageSize=20
        [HttpGet("user/{adminId}/activity")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUserActivity(string adminId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
            => Respond(await _adminUserManagementManager.GetUserActivityAsync(adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, page, pageSize, ct));

        // GET api/admin/users/{adminId}/logins?page=1&pageSize=20
        [HttpGet("user/{adminId}/login-history")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUserLogins(string adminId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
            => Respond(await _adminUserManagementManager.GetUserLoginHistoryAsync(adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, page, pageSize, ct));

        private IActionResult Respond<T>(AccessResult<T> r) =>
            r.Data is null
                ? Failure(r.StatusCode, r.Error)
                : Ok(new Response<T>()
                {
                    Code = AdminResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = r.Data
                });

        // PUT api/admin/users/{userId}
        [HttpPut("user/{adminId}")]
        [HasPermission(Permissions.Users.Manage)]
        public async Task<IActionResult> UpdateUser(string adminId, [FromBody] UpdateAdminUserRequest req, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.UpdateUserAsync(
                adminId, req, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

            if (result.Data is null)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<AdminUserDetailDto>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = "User updated successfully.",
                Data = result.Data
            });
        }
    }
}
