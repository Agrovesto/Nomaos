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
            IAuthenticationRepository authenticationRepository) :base (httpContextAccessor)
        {
            _authenticationRepository = authenticationRepository;
            _walletProvider = walletProvider;
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdminUser([FromBody] UserModel model)
        {
            //var isAdmin = User.Claims.ToArray().IsAdmin();
            //if (!isAdmin) throw new BadRequestException("Not Authorized");
           

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
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateAgrovestoUser([FromBody] UserModel model, [FromQuery]string roleName)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || !isAdmin.Equals("Admin")) throw new Exception("Forbidden");


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
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
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
            return Ok(new Response<WalletProviderModel>
            {
                Succeeded = true,
                Data = await _walletProvider.CreateProvider(model)
            });
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

            var result = await _walletProvider.UpdateProviderParameter(model);


            return Ok(new Response<WalletProviderModel>()
            {
                Succeeded = true,
                Data = result
            });

        }


        #endregion


    }
}
