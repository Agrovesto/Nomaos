using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IBusinessManager _businessManager;
        public AuthenticationController(IAuthenticationRepository authenticationRepository, 
            IBusinessManager businessManager)
        {
            _authenticationRepository = authenticationRepository;
            _businessManager = businessManager;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var createUser = await _authenticationRepository.Login(model);

                return Ok(new Response<(bool Status, string Message)> ()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.Message,
                    Data = createUser
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
        public async Task<IActionResult> Register([FromBody] UserModel model, [FromQuery]BusinessRegistrationArgs args)
        {

            try
            {
             
                var registerUser = await _businessManager.CreateBusinessUser(model, args);

                return Ok(new Response<UserModel>()
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
    }
}
