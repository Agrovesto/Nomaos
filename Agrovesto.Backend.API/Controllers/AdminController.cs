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
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Infrastructure.Logic.Managers;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/admin")]
    //[ApiController]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : BaseController
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IWalletProviderManager _walletProvider;
        public AdminController(IHttpContextAccessor httpContextAccessor,
            IWalletProviderManager walletProvider,
            IAuthenticationRepository authenticationRepository) : base(httpContextAccessor)
        {
            _authenticationRepository = authenticationRepository;
            _walletProvider = walletProvider;
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdminUser([FromBody] UserModel model)
        {
            //var isAdmin = User.Claims.ToArray().IsAdmin();
            //if (!isAdmin) throw new BadRequestException("Not Authorized");

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
                var createUser = await _authenticationRepository.RegisterAdmin(model, model.UserType);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
                    Data = createUser
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

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateAgrovestoUser([FromBody] UserModel model, [FromQuery] string roleName)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || !isAdmin.Equals("Super Admin")) throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Forbidden"); 


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
                var createUser = await _authenticationRepository.RegisterAdmin(model, roleName);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
                    Data = createUser
                });


            }
            catch (BaseException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpGet("users")]

        public async Task<IActionResult> GetUsers([FromQuery] ListQueryParams query)
        {
            try
            {

                var result = await _authenticationRepository.GetUsersAsync();
                return Ok(new PagedListResponse<List<UserViewModel>>()
                {
                    Data = result.model,
                    Message = result.message,
                    Succeeded = result.status,
                    Count = result.model.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.status && result.model != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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



        #region wallet
        [HttpPost("wallet")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]
        public async Task<IActionResult> CreateWalletProvider([FromBody] WalletProviderModel model)
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
            try
            {
                return Ok(new Response<WalletProviderModel>
                {
                    Succeeded = true,
                    Data = await _walletProvider.CreateProvider(model)
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
        //[Authorize(Roles = "Admin")]
        [HttpGet("wallet/parameters/{id}/{Env}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetWalletParameters(long id, string Env)
        {
            var result = await _walletProvider.GetParametersByProvider(id, Env);
            return Ok(new Response<WalletProviderParameterModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Updates parameter
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //  [Authorize(Roles = "Admin")]

        [HttpPost("wallet/parameters")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 422)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]
        public async Task<IActionResult> UpdateWalletParameters([FromBody] WalletProviderModel model)
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
            try
            {
                var result = await _walletProvider.UpdateProviderParameter(model);


            return Ok(new Response<WalletProviderModel>()
            {
                Succeeded = true,
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }


        #endregion


    }
}
