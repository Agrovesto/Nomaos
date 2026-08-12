using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.ExternalServices.BGService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

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
                    Message = result.Count > 0 ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = true,
                    Count = result.Count,
                    // Carried through from the repository, which has already normalised them.
                    // Left unset they default to 0, and TotalPages then divides by zero.
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
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

        [HttpGet("user-wallet-transaction")]
        public async Task<IActionResult> GetUserWallet([FromQuery] ListQueryParams query)
        {
            try
            {

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _walletManager.GetUserWalletTransaction(query);
                return Ok(new PagedListResponse<List<FarmerTransactionDto>>()
                {
                    Data = result.Data,
                    Message = result.Count > 0 ? ResponseMessages.Successful : ResponseMessages.Failed,
                    Succeeded = true,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
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

        [HttpPost("wallet-topup")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var response = await _walletManager.TopUpWallet(request, args);
                return Ok(new Response<ProviderResponse>()
                {
                    Data = response,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpPost("wallet-withdraw")]
        public async Task<IActionResult> WithdrawWallet([FromBody] WithdrawWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var response = await _walletManager.WithdrawWallet(request, args);
                return Ok(new Response<ProviderResponse>()
                {
                    Data = response,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("complete-payment")]
        public async Task<IActionResult> CompletePayment([FromQuery] string reference,
            [FromQuery] string walletProviderCode, [FromQuery] string env, [FromQuery] string businessCode)
        {
            try
            {



                var request = await _walletManager.GetRequestToPayStatus(walletProviderCode, reference, env, businessCode);
                return Ok(ApiResponse<bool>.Success(
                    request,
                ResponseCodes.Success,
                HttpContext.TraceIdentifier
                ));
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


        /// <summary>
        /// Create a wallet for a user (Personal or Organization context)
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
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;

                var wallet = await _walletManager.CreateWallet(request, query);

                return Ok(new Response<CreateWalletResult>()
                {
                    Data = wallet.data,
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

        [HttpPost("validate-pin")]
        [Authorize]
        public async Task<IActionResult> ValidateWalletPin([FromBody] ValidateWalletPinRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            query.UserId = !string.IsNullOrWhiteSpace(query.UserId) ? query.UserId : CurrentUserId;
            if (string.IsNullOrEmpty(query.UserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "Your session has expired. Please sign in again and retry.",
                    Succeeded = false
                });
            }

            try
            {
                var (status, message, token) = await _walletManager.ValidateWalletPin(request, query);

                return Ok(new Response<ValidateWalletPinResult>()
                {
                    Data = new ValidateWalletPinResult
                    {
                        IsValid = status,
                        Token = token?.Token,
                        ExpiresAt = token?.ExpiresAt
                    },
                    Message = message,
                    Succeeded = status,
                    Code = status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        /// <summary>
        /// Emails a one-time code to the signed-in user so they can reset a transaction PIN they have forgotten.
        /// </summary>
        [HttpPost("send-pin-reset-otp")]
        [Authorize]
        public async Task<IActionResult> SendWalletPinResetOtp([FromQuery] WalletQueryParams query)
        {
            query.UserId = !string.IsNullOrWhiteSpace(query.UserId) ? query.UserId : CurrentUserId;
            if (string.IsNullOrEmpty(query.UserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "Your session has expired. Please sign in again and retry.",
                    Succeeded = false
                });
            }

            try
            {
                var (status, message, data) = await _walletManager.SendWalletPinResetOtp(query);

                return Ok(new Response<WalletPinResetOtpResult>()
                {
                    Data = data,
                    Message = message,
                    Succeeded = status,
                    Code = status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        /// <summary>
        /// Verifies a PIN reset code emailed by send-pin-reset-otp and, on success, returns the
        /// same short-lived token that validate-pin issues, for spending at update-pin.
        /// </summary>
        [HttpPost("verify-pin-reset-otp")]
        [Authorize]
        public async Task<IActionResult> VerifyWalletPinResetOtp([FromBody] VerifyPinResetOtpRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            query.UserId = !string.IsNullOrWhiteSpace(query.UserId) ? query.UserId : CurrentUserId;
            if (string.IsNullOrEmpty(query.UserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "Your session has expired. Please sign in again and retry.",
                    Succeeded = false
                });
            }

            try
            {
                var (status, message, token) = await _walletManager.VerifyWalletPinResetOtp(request, query);

                return Ok(new Response<ValidateWalletPinResult>()
                {
                    Data = new ValidateWalletPinResult
                    {
                        IsValid = status,
                        Token = token?.Token,
                        ExpiresAt = token?.ExpiresAt
                    },
                    Message = message,
                    Succeeded = status,
                    Code = status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpPost("update-pin")]
        [Authorize]
        public async Task<IActionResult> UpdateWalletPin([FromBody] UpdateWalletPinRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            query.UserId = !string.IsNullOrWhiteSpace(query.UserId) ? query.UserId : CurrentUserId;
            if (string.IsNullOrEmpty(query.UserId))
            {
                return Unauthorized(new Response<string>
                {
                    Message = "Your session has expired. Please sign in again and retry.",
                    Succeeded = false
                });
            }

            try
            {
                var (status, message) = await _walletManager.UpdateWalletPin(request, query);

                return Ok(new Response<bool>()
                {
                    Data = status,
                    Message = message,
                    Succeeded = status,
                    Code = status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("wallet-summary")]
        public async Task<IActionResult> GetUserFinancialSummary([FromQuery] UserWalletParams query)
        {
            try
            {

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var userFinancialSummary = await _walletManager.GetUserFinancialSummary(query);

                WalletDashboardResponseModel dashboardResponse = new WalletDashboardResponseModel()
                {
                    OutStandingInputDebt = userFinancialSummary.OutstandingInputDebt,
                    NextDuePaymentAmount = userFinancialSummary.NextRepaymentAmount,
                    NextDuePaymentDate = userFinancialSummary.NextRepaymentDate,
                    TotalWalletBalance = userFinancialSummary.TotalWalletBalance
                };
                return Ok(dashboardResponse);
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

    }
}
