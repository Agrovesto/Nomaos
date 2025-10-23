using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Amazon.Util.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [ApiController]
    [Route("api/input-scheme")]
    public class InputFinancingSchemeController : BaseController
    {
        private readonly IInputFinancingSchemeManager _manager;

        public InputFinancingSchemeController(IHttpContextAccessor httpContextAccessor, IInputFinancingSchemeManager manager) : base(httpContextAccessor)
        {
            _manager = manager;
        }

        [HttpPost("create-scheme")]
        public async Task<IActionResult> CreateScheme([FromQuery] BusinessRegistrationArgs args, [FromBody] InputFinancingSchemeDto model)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var result = await _manager.CreateSchemeAsync(model, args);
                return Ok(new Response<(bool Success, string Message)>()
                {
                    Data = result,
                    Succeeded = true,
                    Code = Constants.ResponseCodes.Success
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _manager.GetAllAsync(query);


                return Ok(new Response<List<InputFinancingSchemeDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = result.Data
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

        [HttpGet("single")]
        public async Task<IActionResult> GetById([FromQuery] BusinessRegistrationArgs args)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;



                var result = await _manager.GetByIdAsync(args.Id);


                return Ok(new Response<InputFinancingSchemeDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = result
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

        [HttpPut("update-scheme")]
        public async Task<IActionResult> Update([FromQuery] BusinessRegistrationArgs args, [FromBody] CreateInputFinancingSchemeDto dto)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;



                await _manager.UpdateAsync(args.Id, dto);


                return Ok(new Response<InputFinancingSchemeDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",

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

        [HttpDelete("delete-scheme")]
        public async Task<IActionResult> Delete([FromQuery] BusinessRegistrationArgs args)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;



                await _manager.DeleteAsync(args.Id);


                return Ok(new Response<InputFinancingSchemeDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",

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

        [HttpPost("log-subscription")]
        public async Task<IActionResult> LogInputSubscription([FromQuery] BusinessRegistrationArgs args, [FromBody] InputSubscriptionRequestDto model)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var result = await _manager.CreateSubscription(model, args);
                return Ok(new Response<ResponseDto>()
                {
                    Data = result,
                    Succeeded = true,
                    //Code = result.Success
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }



    }

}
