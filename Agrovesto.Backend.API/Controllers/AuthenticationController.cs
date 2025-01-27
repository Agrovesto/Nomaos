using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        public AuthenticationController(IAuthenticationRepository authenticationRepository)
        {
            _authenticationRepository = authenticationRepository;
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
    }
}
