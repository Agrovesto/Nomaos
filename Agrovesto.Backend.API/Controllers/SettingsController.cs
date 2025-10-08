using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : BaseController
    {
        private readonly IWalletSetUpManager _walletSetUpManager;
        private readonly ISettingsManager _settingsManager;
        public SettingsController(IHttpContextAccessor httpContextAccessor,
            IWalletSetUpManager walletSetUpManager, ISettingsManager settingsManager ) : base(httpContextAccessor)
        {
            _walletSetUpManager = walletSetUpManager;
            _settingsManager = settingsManager;
        }

        [HttpPost("orgConfig")]
        public async Task<IActionResult> SetUpOrganizationConfiguration([FromBody] OrganizationConfigurationModel model, [FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                await _settingsManager.CreateOrUpdateOrgConfig(model, query);
                return Ok(new Response<bool>()
                {
                    Data = true,
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

        [HttpGet("orgConfig")]
        public async Task<IActionResult> RetrieveOrgConfiguration([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _settingsManager.GetByCodeAsync(query);
                return Ok(new Response<OrganizationConfigurationModel>()
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
       
        //update config put
        //delete config delete
        [HttpPost("wallet-setup")]
        public async Task<IActionResult> CreateUserWallet([FromQuery] ListQueryParams query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var wallet=await _walletSetUpManager.CreateWalletSingleAsync(query);
                return Ok(new Response<(bool status, string message)>()
                {
                    Data = wallet,
                    Succeeded = wallet.status,
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

        [HttpPost("wallet-setup-multiple")]
        public async Task<IActionResult> CreateMultipleUserWallet([FromQuery] ListQueryParams query)
        {
            try
            {
                var hasOrgCode = OrganizationCode;
                if (!string.IsNullOrEmpty(hasOrgCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var wallet = await _walletSetUpManager.CreateWalletMultipleAsync(query);
                return Ok(new Response<(bool status, string message)>()
                {
                    Data = wallet,
                    Succeeded = wallet.status,
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
