using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/input-financing")]
    [ApiController]
    public class InputFinancingController : BaseController
    {
        private readonly IInputFinancingManager _inputFinancingManager;
        public InputFinancingController(IHttpContextAccessor httpContextAccessor,
             IInputFinancingManager inputFinancingManager) : base(httpContextAccessor)
        {

            _inputFinancingManager = inputFinancingManager;
        }

        #region InputFinancing

        [HttpPost("eop")]
        public async Task<IActionResult> CreateEopAsync([FromBody] CreateEopRequest model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response<string> { Succeeded = false, Message = "Validation failed" });

            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");

                var result = await _inputFinancingManager.CreateEopAsync(model, args);
                return Ok(new Response<EopResponseDto> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }
        [HttpPost("package/generate-from-eop")]
        public async Task<IActionResult> GenerateInputPackageFromEopAsync([FromBody] GenerateInputPackageRequest model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response<string> { Succeeded = false, Message = "Validation failed" });

            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");

                var result = await _inputFinancingManager.GenerateInputPackageFromEopAsync(model, args);
                return Ok(new Response<bool> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }
        [HttpGet("eop")]
        public async Task<IActionResult> GetEopByIdAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");
                if (args.Id <= 0) return BadRequest(new Response<string> { Message = "EOP Id is required", Succeeded = false });

                var result = await _inputFinancingManager.GetEopByIdAsync(args);
                return Ok(new Response<EopDto> { Succeeded = result != null, Data = result, Code = ResponseCodes.Success, Message = result != null ? "Successful" : "No record found" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

        [HttpGet("eops")]
        public async Task<IActionResult> GetEopsAsync([FromQuery] EopFilterRequest filter, [FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                var result = await _inputFinancingManager.GetEopsAsync(filter, args);
                return Ok(new Response<PagedResult<EopDto>> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

       
        [HttpPost("create-package")]
        public async Task<IActionResult> CreateManualInputPackageAsync(
    [FromBody] CreateInputPackageRequest model,
    [FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager
                .CreateInputFinancingPackageConfig(model, args);

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        //public async Task<IActionResult> CreateManualInputPackageAsync([FromBody] CreateInputPackageRequest model, [FromQuery] ListQueryParams args)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new Response<string> { Succeeded = false, Message = "Validation failed" });

        //    try
        //    {
        //        args.organizationCode = args.organizationCode ?? OrganizationCode;
        //        if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");

        //        args.UserId = string.IsNullOrEmpty(args.UserId) ? CurrentUserId : args.UserId;
        //        var result = await _inputFinancingManager.CreateInputFinancingPackageConfig(model, args);
        //        return Ok(new Response<bool> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
        //    }
        //}



        [HttpGet("package")]
        public async Task<IActionResult> GetPackageByIdAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");
                if (args.Id <= 0) return BadRequest(new Response<string> { Message = "Package Id is required", Succeeded = false });

                var result = await _inputFinancingManager.GetPackageByIdAsync(args);
                return Ok(new Response<InputPackageDto> { Succeeded = result != null, Data = result, Code = ResponseCodes.Success, Message = result != null ? "Successful" : "No record found" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackagesAsync([FromQuery] InputPackageFilterRequest filter, [FromQuery] ListQueryParams args)
        {
            try
            {
                var result = await _inputFinancingManager.GetPackagesAsync(filter, args);
                return Ok(new Response<PagedResult<InputPackageDto>> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

        #endregion InputFinancing

        /**  /api/financing-packages
/api/subscriptions
/api/commitments
/api/input-releases
/api/repayments
/api/risk
/api/reports
/api/eop
/api/warehouse

           **/
    }
}