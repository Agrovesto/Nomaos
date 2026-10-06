using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/internal/admin")]
    [ApiController]
    public class AdminController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;

        public static string AdminUserType { get; private set; } = UserType.Agrovesto.ToString();

        public AdminController(IHttpContextAccessor httpContextAccessor,
          //  IAuthenticationRepository authenticationRepository,
            IOnBoardingManager onBoardingManager) : base(httpContextAccessor)
        {
           // _authenticationRepository = authenticationRepository;
            _onBoardingManager = onBoardingManager;
        }

        /// <summary>
        /// Seed a system/super admin user. This is intended for initial seeding and should be protected in production.
        /// </summary>
        [HttpPost("seed-super-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedSuperAdmin([FromBody] UserModel? model)
        {
            try
            {
                // If caller did not supply a model use a default seed (matches existing DB initializer defaults)
                var seed = model ?? new UserModel
                {
                    Email = "reloarhub@gmail.com",
                    Password = "Reloarhub144828@",
                    PhoneNumber = "08126201565",
                    UserType = AdminUserType,
                    FirstName = "Reloarhub Solutions",
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
        [HttpPost("create-admin")]
        [Authorize]
        public async Task<IActionResult> CreateAdmin([FromBody] UserModel model, [FromQuery] string? roleName)
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
        [HttpPost("create-user")]
        [Authorize]
        public async Task<IActionResult> CreateUserBySuperAdmin([FromBody] Agrovesto.Backend.Core.V2.Onboarding.OnboardingRequestModel model, [FromQuery] Agrovesto.Backend.Core.V2.Onboarding.OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            if (!CurrentUserRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
                return Unauthorized(new Response<string> { Succeeded = false, Message = ResponseMessages.AccessDenied, Code = ResponseCodes.AccessDenied });

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                // Preserve organization resolution behavior from other onboarding endpoints
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                args.userType = AdminUserType;
                var result = await _onBoardingManager.CreateBusinessUser(model, args);
                return Ok(Success(result, result.Message, ResponseMessages.Successful));
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
    }
}
