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
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/admin")]
    [Authorize]
    [ApiController]
    public class AdminController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IAdminDashboardManager _adminDashboardManager;
        private readonly IAdminFarmerManager _farmerManager;
        private readonly IAdminMembershipManager _membershipManager;
        private readonly IAdminOrganizationManager _organizationManager;
        private readonly IAdminAgentManager _agentManager;
        public readonly IAdminUserManagementManager _adminUserManagementManager;
        private const int MaxCustomRangeDays = 366 * 5;
        private readonly IAdminSettingsManager _adminSettingsManager;

        public AdminController(IHttpContextAccessor httpContextAccessor,
          //  IAuthenticationRepository authenticationRepository,
            IOnBoardingManager onBoardingManager,
            IAdminDashboardManager adminDashboardManager,
            IAdminFarmerManager farmerManager,
            IAdminMembershipManager membershipManager,
            IAdminOrganizationManager organizationManager,
            IAdminAgentManager agentManager,
            IAdminUserManagementManager adminUserManagementManager,
            IAdminSettingsManager adminSettingsManager) : base(httpContextAccessor)
        {
            _farmerManager = farmerManager;
            _membershipManager = membershipManager;
            _organizationManager = organizationManager;
            _agentManager = agentManager;
            _adminSettingsManager = adminSettingsManager;
            // _authenticationRepository = authenticationRepository;
            _onBoardingManager = onBoardingManager;
            _adminDashboardManager = adminDashboardManager;
            _adminUserManagementManager = adminUserManagementManager;
        }

        #region settings

        // GET api/admin/account/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken ct)
            => Respond(await _adminSettingsManager.GetProfileAsync(CurrentUserId, ct), ResponseMessages.Successful);

        // POST api/admin/account/profile-image   (multipart/form-data, field name: image)
        [HttpPost("profile/image")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChangeProfileImage(IFormFile image, CancellationToken ct)
        {
            if (image is null || image.Length == 0)
                return StatusCode(400, new Response<object>
                {
                    Code = AdminResponseCodes.Failed,
                    Succeeded = false,
                    Message = "Please select an image."
                });

            await using var stream = image.OpenReadStream();
            return Respond(await _adminSettingsManager.UpdateProfileImageAsync(CurrentUserId, stream, image.Length, ct),
                           "Profile image updated successfully.");
        }

        [HttpPost("settings/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            var result = await _adminSettingsManager.ChangePasswordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!, req, ct);

            if (!result.Succeeded)
                return StatusCode(result.StatusCode, new Response<object>()
                {
                    Code = AdminResponseCodes.Failed,       // your failure constant
                    Succeeded = false,
                    Message = result.Error
                });

            return Ok(new Response<object>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = "Changes saved successfully — The updated information has been recorded."
            });
        }


        // GET api/admin/settings/personal-info
        [HttpGet("settings/personal-info")]
        public async Task<IActionResult> GetPersonalInfo(CancellationToken ct)
            => Respond(await _adminSettingsManager.GetPersonalInfoAsync(CurrentUserId, ct), ResponseMessages.Successful);

        // PUT api/admin/settings/personal-info
        [HttpPut("settings/personal-info")]
        public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoRequest req, CancellationToken ct)
            => Respond(await _adminSettingsManager.UpdatePersonalInfoAsync(CurrentUserId, req, ct),
                       "Changes saved successfully — The updated information has been recorded.");

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private IActionResult Respond<T>(AccessResult<T> r, string successMessage) =>
            r.Succeeded
                ? Ok(new Response<T>()
                {
                    Code = AdminResponseCodes.Success,
                    Succeeded = true,
                    Message = successMessage,
                    Data = r.Data
                })
                : StatusCode(r.StatusCode, new Response<object>()
                {
                    Code = AdminResponseCodes.Failed,
                    Succeeded = false,
                    Message = r.Error
                });
        #endregion settings


        [HttpPost("auth/logout")]
        [Authorize]
        [AllowPendingCredentialChange]
        public async Task<IActionResult> AdminLogout([FromBody] AdminLogoutModel? model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // use whatever BuildClaims sets for user id
            var sessionId = User.FindFirstValue("SessionId");

            if (string.IsNullOrWhiteSpace(userId) || User.FindFirstValue("portal") != "admin")
                return Unauthorized();

            return await ExecuteAsync(
                () => _onBoardingManager.AdminLogout(userId, sessionId, model?.AllDevices ?? false),
                _ => Ok(new Response<object>
                {
                    Code = AdminResponseCodes.Success,
                    Succeeded = true,
                    Message = "Logged out successfully"
                }));
        }
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
                        AdminLoginStatus.TooManyAttempts => StatusCode(StatusCodes.Status429TooManyRequests, response),
                        _ => StatusCode(StatusCodes.Status403Forbidden, response)        // inactive / no admin access
                    };
                });
        }


        [HttpPost("auth/refresh")]
        [AllowAnonymous]                  // the access token is expired by definition
        [AllowPendingCredentialChange]    // weak-credential users must still be able to refresh
        [EnableRateLimiting("admin-login")]
        public async Task<IActionResult> AdminRefreshToken([FromBody] AdminRefreshTokenModel model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            return await ExecuteAsync(
                () => _onBoardingManager.RefreshToken(model.RefreshToken, ip),
                result =>
                {
                    if (!result.Status)
                    {
                        return Unauthorized(new Response<AdminLoginResponseModel>
                        {
                            Code = AdminResponseCodes.Failed,
                            Succeeded = false,
                            Message = result.Message
                        });
                    }

                    var t = result.Response;
                    return Ok(new Response<AdminLoginResponseModel>
                    {
                        Code = AdminResponseCodes.Success,
                        Succeeded = true,
                        Message = result.Message,
                        Data = new AdminLoginResponseModel
                        {
                            Token = t.Token,
                            TokenType = t.TokenType,
                            ExpiresOn = t.ExpiresOn,
                            RefreshToken = t.RefreshToken ?? string.Empty,
                            RefreshTokenExpiresOn = t.RefreshTokenExpiresOn,
                            TwoFactorEnabled = t.TwoFactorEnabled,
                            RequiresCredentialChange = t.RequiresCredentialChange,
                            SecurityNotice = t.SecurityNotice
                        }
                    });
                });
        }
        /// <summary>
        /// GET api/admin/dashboard?period=month
        /// GET api/admin/dashboard?period=custom&amp;from=2026-09-01&amp;to=2026-09-30
        /// period: today | week | month | quarter | year | custom
        /// </summary>
        [HttpGet("dashboard")]
        [HasPermission(Permissions.Dashboard.View)]
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
            var unmet = PasswordPolicy.Validate(req.Password);
            if (unmet.Count > 0)
                return Failure(400, string.Join(" ", unmet));


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
        // POST api/admin/users/{adminId}/reactivate
        [HttpPost("user/{adminId}/reactivate")]
        [HasPermission(Permissions.Users.Manage)]
        public async Task<IActionResult> Reactivate(string adminId, CancellationToken ct)
        {
            var result = await _adminUserManagementManager.ReactivateUserAsync(
                adminId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);

            if (result.Data is null)
                return Failure(result.StatusCode, result.Error);

            return Ok(new Response<AdminUserStatusDto>()
            {
                Code = AdminResponseCodes.Success,
                Succeeded = true,
                Message = "User reactivated successfully.",
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


        [HttpGet("farmer/summary")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> Summary()
        {
            var businessId = RequireBusinessId();

            var data = await _farmerManager.GetSummaryAsync(businessId);
            return Ok(new Response<FarmerSummaryDto> { Succeeded = true, Data = data });
        }

        [HttpGet("farmer/list")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> List([FromQuery] FarmerListQuery query)
        {
            query.BusinessId = RequireBusinessId();
            var data = await _farmerManager.GetFarmersAsync(query);
            return Ok(new Response<PagedResult<FarmerListItemDto>> { Succeeded = true, Data = data });
        }

        [HttpGet("farmer/export")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> Export([FromQuery] FarmerListQuery query, [FromQuery] string format = "csv")
        {
            query.BusinessId = RequireBusinessId();
            var rows = await _farmerManager.GetAllForExportAsync(query);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");

            return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
                ? File(FarmerExport.ToXlsx(rows),
                       "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                       $"farmers-{stamp}.xlsx")
                : File(FarmerExport.ToCsv(rows), "text/csv", $"farmers-{stamp}.csv");
        }

        [HttpGet("farmer/{userId}")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> Details(string userId)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetFarmerDetailsAsync(businessId, userId);
            if (data == null)
                return NotFound(new Response<FarmerDetailsDto> { Succeeded = false, Message = "Farmer not found." });

            return Ok(new Response<FarmerDetailsDto> { Succeeded = true, Data = data });
        }

        [HttpPost("farmer/{userId}/suspend")]
        [HasPermission(Permissions.Farmers.Manage)]
        public async Task<IActionResult> Suspend(string userId
            //, 
           // [FromBody] SuspendFarmerRequest request
            )
        {
            var BusinessId = RequireBusinessId();
            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ok = await _farmerManager.SuspendFarmerAsync(BusinessId, userId, actingUserId, null);

            return ok
                ? Ok(new Response<bool> { Succeeded = true, Data = true, Message = "Farmer account suspended." })
                : BadRequest(new Response<bool> { Succeeded = false, Message = "Farmer not found or already suspended." });
        }
        [HttpPost("farmer/{userId}/reactivate")]
        [HasPermission(Permissions.Farmers.Manage)]
        public async Task<IActionResult> Reactivate(string userId
            //, 
           // [FromBody] SuspendFarmerRequest request
            )
        {
            var BusinessId = RequireBusinessId();
            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ok = await _farmerManager.ReactivateFarmerAsync(BusinessId, userId, actingUserId, null);

            return ok
                ? Ok(new Response<bool> { Succeeded = true, Data = true, Message = "Farmer account reactivated." })
                : BadRequest(new Response<bool> { Succeeded = false, Message = "Farmer not found or already active." });
        }

        [HttpGet("farmer/{userId}/farms/summary")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> FarmSummary(string userId)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetFarmSummaryAsync(businessId, userId);
            if (data == null)
                return NotFound(new Response<FarmSummaryDto> { Succeeded = false, Message = "Farmer not found." });

            return Ok(new Response<FarmSummaryDto> { Succeeded = true, Data = data });
        }

        [HttpGet("farmer/{userId}/farms")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> Farms(string userId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetFarmsAsync(businessId, userId, page, pageSize);
            if (data == null)
                return NotFound(new Response<PagedResult<FarmListItemDto>> { Succeeded = false, Message = "Farmer not found." });

            return Ok(new Response<PagedResult<FarmListItemDto>> { Succeeded = true, Data = data });
        }
        [HttpGet("farmer/{userId}/organization-membership")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> OrganizationMembership(string userId)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetOrganizationMembershipAsync(businessId, userId);
            if (data == null)
                return NotFound(new Response<FarmerOrgMembershipDto> { Succeeded = false, Message = "Farmer not found." });

            return Ok(new Response<FarmerOrgMembershipDto> { Succeeded = true, Data = data });
        }

        [HttpPost("farmer/create")]
        [HasPermission(Permissions.Farmers.Manage)]
        public async Task<IActionResult> CreateFarmerAsync([FromBody] AdminCreateFarmerRequest model, [FromQuery] AdminOnBoardingArgs args,
            CancellationToken ct)
        {
            args.businessCode = RequireBusinessCode();
            args.OnboardedByUserId= User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var result = await _farmerManager.CreateFarmerAsync(model, args, ct);
                return Ok(new Response<(bool, string, string[])>
                {
                    Succeeded = true,
                    Data = result,
                    Message=result.msg,
                    Code= result.status ? AdminResponseCodes.Success : AdminResponseCodes.Failed
                });
            }
            catch (BaseException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, new
                {
                    status = false,
                    message = ex.ExceptionMessage,
                    errors = ex.Errors
                });
            }
        }

        [HttpGet("farmer/{userId}/inputfinance/summary")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> GetInputFinanceSummaryAsync(string userId)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetInputFinanceSummaryAsync(businessId, userId);
            if (data == null)
                return NotFound(new Response<InputFinanceSummaryDto> { Succeeded = false, Message = "Not found." });

            return Ok(new Response<InputFinanceSummaryDto> { Succeeded = true, Data = data });
        }

        [HttpGet("farmer/{userId}/inputfinance")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> GetInputFinancePackagesAsync(string userId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var businessId = RequireBusinessId();
            var data = await _farmerManager.GetInputFinancePackagesAsync(businessId, userId, page, pageSize);
            if (data == null)
                return NotFound(new Response<PagedResult<InputFinancePackageDto>> { Succeeded = false, Message = "Not found." });

            return Ok(new Response<PagedResult<InputFinancePackageDto>> { Succeeded = true, Data = data });
        }


        [HttpGet("farmer/wallets/{userId}")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> WalletList(string userId, [FromQuery] FarmerListQuery query)
        {
            query.BusinessCode = RequireBusinessCode();
            query.UserId = userId;
            var data = await _farmerManager.GetFarmerWallletsByUserId(query);
            return Ok(new Response<PagedResult<FarmerWalletModule>> { Succeeded = true, Data = data });
        }

        [HttpGet("farmer/wallets/{userId}/summary")]
        [HasPermission(Permissions.Farmers.View)]
        public async Task<IActionResult> WalletSummary(string userId)
        {
            var businessCode = RequireBusinessCode();
            var data = await _farmerManager.GetFarmerWalletSummaryAsync(userId, businessCode);
            if (data == null)
                return NotFound(new Response<FarmerWalletSummaryDto> { Succeeded = false, Message = "Wallet not found." });

            return Ok(new Response<FarmerWalletSummaryDto> { Succeeded = true, Data = data });
        }


        #region agent
        [HttpGet("agent/list")]
        [HasPermission(Permissions.Agents.View)]
        public async Task<IActionResult> ListAgents([FromQuery] AgentListQuery query)
        {
            query.BusinessId = RequireBusinessId();
            var data = await _agentManager.GetAgentsAsync(query);
            return Ok(new Response<PagedResult<AgentListItemDto>> { Succeeded = true, Data = data });
        }
        #endregion

        #region membership
        [HttpGet("membership/list")]
        [HasPermission(Permissions.Memberships.View)]
        public async Task<IActionResult> ListMemberships([FromQuery] MemberShipListQuery query)
        {
            query.BusinessId = RequireBusinessId();
            var data = await _membershipManager.GetMembershipsAsync(query);
            return Ok(new Response<PagedResult<MemberShipListItemDto>> { Succeeded = true, Data = data });
        }
        #endregion membership

        #region organization
        [HttpGet("organization/list")]
        [HasPermission(Permissions.Organizations.View)]
        public async Task<IActionResult> ListOrganizations([FromQuery] OrganizationListQuery query)
        {
            query.BusinessId = RequireBusinessId();
            var data = await _organizationManager.GetOrganizationsAsync(query);
            return Ok(new Response<PagedResult<OrganizationListItemDto>> { Succeeded = true, Data = data });
        }
        #endregion 

    }
}
