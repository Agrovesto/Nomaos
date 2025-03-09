using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InputFinancingManagementController : BaseController
    {
        private readonly IInputFinancingManagement _inputFinancingManagement;
        public InputFinancingManagementController(IHttpContextAccessor httpContextAccessor,
            IInputFinancingManagement inputFinancingManagement) : base(httpContextAccessor)
        {
            _inputFinancingManagement = inputFinancingManagement;
        }
        [HttpGet("subscription-interests-bylocation")]
        public async Task<IActionResult> GetInterestRequestByLocation([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetInterestRequestByLocation(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("subscription-interests-bycooperative")]
        public async Task<IActionResult> GetInterestRequestByCooperative([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetInterestRequestByCooperative(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
        [HttpGet("subscription-interests")]
        public async Task<IActionResult> GetSubscriptionIntesrest([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.GetInterestRequestByBusiness(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ?  "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpPost("indicate-interest")]
        public async Task<IActionResult> CreateInterest([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
           // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                var bundle = await _inputFinancingManagement.CreateInputFinancingInterest(model, args);
                return Ok(new Response<BundleSubscriptionInterestResponseModel>()
                {
                    Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = bundle.data
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


        [HttpPatch("confirm-equity-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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
               // args.UserId = CurrentUserId;
                var bundle = await _inputFinancingManagement.ConfirmEquityCommitmentPayment(model, args);
                return Ok(new Response<(bool status, string message)>()
                {
                    Code = (bundle.status) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = bundle
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

    }
}
