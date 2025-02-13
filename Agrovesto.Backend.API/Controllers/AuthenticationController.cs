using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
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
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var createUser = await _authenticationRepository.Login(model);

                return Ok(new Response<TokenModel> ()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = createUser.Status,
                    Message = createUser.Message,
                    Data = createUser.response
                });


            }
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpGet("business")]
        public async Task<IActionResult> LoadBusiness([FromQuery]string businessCode, [FromQuery]string signage)
        {
            if(businessCode == null || signage == null) return StatusCode(403, new Response<string>
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
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }

        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserModel model, [FromQuery]BusinessRegistrationArgs args)
        {

            try
            {
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;
                var registerUser = await _businessManager.CreateBusinessUser(model, args);
                if (registerUser != null)
                {
                    await _businessManager.UpdateUserAccountProfile(registerUser, args, registerUser.UserId);
                }

                return Ok(new Response<SubAccountModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = registerUser
                });


            }
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }

        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            var request = await _authenticationRepository.ForgotPassword(model.Username);
            return Ok(request.Message);
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(PasswordResetModel model)
        {
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

    }
}
