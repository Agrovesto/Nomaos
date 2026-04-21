using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/wallet")]
    [ApiController]
    public class UserWalletController : BaseController
    {
        private readonly INomaOSWalletService _walletManager;
        private readonly IWebhookRouter _router;

        public UserWalletController(IHttpContextAccessor httpContextAccessor, IWebhookRouter router, INomaOSWalletService walletManager) : base(httpContextAccessor)
        {
            _router = router;
            _walletManager = walletManager;
        }
        [HttpGet("user-wallet")]
        public async Task<IActionResult> GetUserWallet([FromQuery] UserWalletParams query)
        {
            try
            {
               
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _walletManager.GetUserWallletsByUserId(query);
                return Ok(new PagedListResponse<List<UserWalletResponseModel>>()
                {
                    Data = result.Data,
                    Message = result.Count> 0? "Successful": "Failed",
                    Succeeded = true,
                    Count = result.Count,

                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = (result.Count > 0) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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


        //[HttpGet("wallet-detail")]
        //public async Task<IActionResult> GetUserWalletDetail([FromQuery] UserWalletParams query)
        //{
        //    try
        //    {

        //        query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
        //        var result = await _walletManager.GetUserWallletsByUserId(query);
        //        return Ok(new PagedListResponse<List<UserWalletResponseModel>>()
        //        {
        //            Data = result.Data,
        //            Message = result.Count > 0 ? "Successful" : "Failed",
        //            Succeeded = true,
        //            Count = result.Count,

        //            //PageNumber = query.PageNumber,
        //            //PageSize = 10,
        //            Code = (result.Count > 0) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
        //        });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.ExceptionMessage,
        //            Succeeded = false,

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.Message,
        //            Succeeded = false,
        //        });
        //    }


        //}


        /// <summary>
        /// Create a wallet for a user (Personal or Organization context)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request,[FromQuery] WalletQueryParams query)
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
            if (string.IsNullOrWhiteSpace(query.env))
                return BadRequest("Environment is required");

            if (string.IsNullOrWhiteSpace(request.WalletProviderCode))
                return BadRequest("WalletProviderCode is required");

            if (string.IsNullOrWhiteSpace(query.businessCode))
                return BadRequest("BusinessCode is required");
            if (string.IsNullOrEmpty(query.UserId))
            {
                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                });
            }
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
               
                var wallet = await _walletManager.CreateWallet(request, query);

                return Ok(new Response<(bool status, string message)>()
                {
                    Data = wallet,


                    Message = wallet.message,
                    Succeeded = wallet.status ,

                    Code = wallet.status  ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
        [HttpPost("{provider}/top-up")]
        public async Task<IActionResult> HandleWebhook(string provider)
        {
            try
            {
                await _router.RouteAsync(provider, Request);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                // log error
                return Ok(); // still return 200 to avoid retries storm
            }
        }
        ///// <summary>
        ///// Get all wallets for a user
        ///// </summary>
        //[HttpGet("user/{userId}")]
        //public async Task<IActionResult> GetUserWallets(string userId, [FromQuery] string? businessCode = null)
        //{
        //    if (string.IsNullOrWhiteSpace(userId))
        //        return BadRequest("UserId is required");

        //    try
        //    {
        //        var wallets = await _walletManager.GetUserWallletsByUserId(new UserWalletParams
        //        {
        //            UserId = userId,
        //            businessCode = businessCode
        //        });

        //        return Ok(new { success = true, data = wallets });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        //    }
        //}
       
    }
}
