using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/farmer")]
    [ApiController]
    public class FarmerController : BaseController
    {
        private readonly IMembershipManager _membershipManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly INomaOSWalletService _walletService;
        public FarmerController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,
            INomaOSWalletService walletService,
            IMembershipManager membershipManager, IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
            _membershipManager = membershipManager;
            _businessManager = businessManager;
            _walletService = walletService;
        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                if (CurrentUserRole != "Farmer" && string.IsNullOrEmpty(query.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "Farmer UserId is required" }
}

                    });
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetFarmerDashboard(query);
                return Ok(new Response<FarmerDashboardDto>()
                {
                    Data = result,


                    Message = result != null ? "Successful" : "Could not retrieve data",
                    Succeeded = result != null ? true : false,

                    Code = result != null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("memberships")]
        public async Task<IActionResult> GetFarmerMemberships([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _businessManager.GetMembershipsByFarmer(query);
                return Ok(new PagedListResponse<List<FarmerMembershipSummaryDto>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
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
        [HttpGet("membership-detail")]
        public async Task<IActionResult> GetMembershipDetail([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                if (string.IsNullOrEmpty(query.UserId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "Membership UserId is required.",
                        Succeeded = false
                    });

                }


                var result = await _businessManager.GetFarmerMembershipDetails(query);
                return Ok(new Response<FarmerMembershipOverviewDto>()
                {
                    Data = result,
                    Message = "Successful",
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


        [HttpGet("wallets")]
        public async Task<IActionResult> GetWallets([FromQuery] UserWalletParams query)
        {
            try
            {

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _walletService.GetUserWallletsByUserId(query);
                return Ok(new PagedListResponse<List<UserWalletResponseModel>>()
                {
                    Data = result,
                    Message = result.Count > 0 ? "Successful" : "Failed",
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


        [HttpPost("create-wallet")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] WalletQueryParams query)
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
            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

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

                var wallet = await _walletService.CreateWallet(request, query);

                return Ok(new Response<(bool status, string message)>()
                {
                    Data = wallet,


                    Message = wallet.status ? "Successful" : "Could not create wallet",
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


        [HttpGet("wallet-summary")]
        public async Task<IActionResult> GetUserFinancialSummary([FromQuery] UserWalletParams query)
        {
            try
            {

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _walletService.GetUserFinancialSummary(query);
                return Ok(new PagedListResponse<FarmerFinancialSummaryDto>()
                {
                    Data = result,
                    Message =  "Successful",
                    Succeeded = true,
                    Code =Core.CoreUtils.Constants.ResponseCodes.Success 
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
