using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : BaseController
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IBusinessManager _businessManager;
        public AuthenticationController(IHttpContextAccessor httpContextAccessor, IAuthenticationRepository authenticationRepository,
            IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _authenticationRepository = authenticationRepository;
            _businessManager = businessManager;
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model, [FromQuery]string? businessCode)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            var isAgrovesto = DomainHeader;
            try
            {
                var createUser = await _authenticationRepository.Login(model, businessCode, isAgrovesto);
                if (createUser.Status)
                {
                    return Ok(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.response
                    });
                }
                else
                {
                    return BadRequest(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.response
                    });
                }


            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpGet("business")]
        public async Task<IActionResult> LoadBusiness([FromQuery] string businessCode, [FromQuery] string signage)
        {
            if (businessCode == null || signage == null) return StatusCode(403, new Response<string>
            {
                Message = "Could not load page",//"An unexpected error occurred. Please contact your administrator.",
                Succeeded = false,
            });
            try
            {
                // signage= HttpUtility.UrlDecode(signage);
                signage = signage.Replace(" ", "+");
                var business = await _businessManager.GetBusinessBySinageSaltAsync(businessCode, signage);

                return Ok(new Response<BusinessLoadModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = business
                });


            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }

        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {

                var request = await _authenticationRepository.ForgotPassword(model.Username);
                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request.Message
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(PasswordResetModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try { 
            var resetPasswordRequest = await _authenticationRepository.ResetPassword(model);
            if (resetPasswordRequest.Status)
            {
                return Ok(new Response<(bool Status, string Message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Success",
                    Data = resetPasswordRequest
                });
            }
            else
            {
                return BadRequest(new Response<(bool Status, string Message)>()
                {
                    Code = ResponseCodes.NotSuccessful,
                    Succeeded = false,
                    Message = "Failed",
                    Data = resetPasswordRequest
                });
            }
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

    }
}
