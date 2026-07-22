using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{

    [Route("api/v2/agent/wallet")]
    [ApiController]
    public class AgentWalletController : BaseController
    {
        private readonly INomaOSWalletService _walletManager;
        private readonly IWebhookRouter _router;

        public AgentWalletController( IHttpContextAccessor httpContextAccessor, IWebhookRouter router, INomaOSWalletService walletManager ) : base(httpContextAccessor)
        {
            _walletManager = walletManager;
            _router = router;
        }

        /// <summary>
        /// Create a wallet for a agent (Personal or Organization context)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            query.UserId = !string.IsNullOrWhiteSpace(query.UserId) ? query.UserId : CurrentUserId;

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
                    Succeeded = wallet.status,

                    Code = wallet.status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
