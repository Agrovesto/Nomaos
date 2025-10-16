using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Amazon.Util.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    }

}
