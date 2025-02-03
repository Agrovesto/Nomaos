using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.Models.Common.Constants;
using System.ComponentModel.DataAnnotations;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.CoreUtils;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/Admin")]
    //[ApiController]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : BaseController
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        public AdminController(IHttpContextAccessor httpContextAccessor,IAuthenticationRepository authenticationRepository) :base (httpContextAccessor)
        {
            _authenticationRepository = authenticationRepository;
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdminUser([FromBody] UserModel model)
        {
            //var isAdmin = User.Claims.ToArray().IsAdmin();
            //if (!isAdmin) throw new BadRequestException("Not Authorized");
           

            try
            {
                var createUser = await _authenticationRepository.RegisterAdmin(model, "Admin");

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
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

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateAgrovestoUser([FromBody] UserModel model, [FromQuery]string roleName)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || !isAdmin.Equals("Admin")) throw new Exception("Forbidden");


            try
            {
                var createUser = await _authenticationRepository.RegisterAdmin(model, roleName);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
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
