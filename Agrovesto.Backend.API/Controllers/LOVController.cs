using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/lov")]
    [ApiController]
    public class LOVController : ControllerBase
    {
        private readonly ICafeOneWalletService _cafeOneWalletService;

        private readonly IWebHostEnvironment _hostingEnvironment;
        public LOVController(ICafeOneWalletService cafeOneWalletService, IWebHostEnvironment hostingEnvironment)
        {
            _cafeOneWalletService = cafeOneWalletService;
            _hostingEnvironment = hostingEnvironment;
        }

        [AllowAnonymous]
        [HttpGet("banks")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetBanks([FromQuery] string env)
        {

            var request = await _cafeOneWalletService.GetBanks(env);
            return Ok(new Response<List<CafeOneBankData>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request != null ? request : null
            });
        }

        [AllowAnonymous]
        [HttpGet("resolve-user-account/{accountNumber}/{bankCode}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResolveAccountNumber(string accountNumber, string bankCode, [FromQuery] string env)
        {

            var request = await _cafeOneWalletService.ResolveUserAccount(accountNumber, bankCode, env);
            return Ok(new Response<AccountResolutionResolutionResponseData>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request != null ? request : null
            });
        }


        //[AllowAnonymous]
        //[HttpGet("userTypes")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //[ProducesResponseType(422)]
        //[ProducesResponseType(500)]
        //public async Task<IActionResult> GetUserTypes([FromQuery] string env)
        //{

        //    var sendRequest= 

        //    var request = await _cafeOneWalletService.GetBanks(env);
        //    return Ok(new Response<List<CafeOneBankData>>()
        //    {
        //        Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
        //        Succeeded = request != null,
        //        Message = request == null ? "Failed" : "Success",
        //        Data = request != null ? request : null
        //    });
        //}

    }
}
