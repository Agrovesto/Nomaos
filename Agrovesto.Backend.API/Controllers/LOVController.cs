using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.LocationService;
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

        private readonly IFlutterwaveService _flutterwaveService;

        private readonly IWebHostEnvironment _hostingEnvironment;
        public LOVController(ICafeOneWalletService cafeOneWalletService,
             IFlutterwaveService flutterwaveService, IWebHostEnvironment hostingEnvironment)
        {
            _cafeOneWalletService = cafeOneWalletService;
            _hostingEnvironment = hostingEnvironment;

            _flutterwaveService = flutterwaveService;
        }

        [AllowAnonymous]
        [HttpGet("cafeonebanks")]
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
        [HttpGet("banks")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> Banks([FromQuery] string env, [FromQuery] string? countryCode = "NG")
        {
            var getbanks = await _flutterwaveService.GetBanks(env, countryCode);

            return Ok(getbanks);
            //return Ok(new Response<BankResponseModel>()
            //{
            //    Code = ResponseCodes.Success,
            //    Succeeded = true,
            //    Message = "Success",
            //    Data = await _flutterwaveService.GetBanks(countryCode)
            //});

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

        [AllowAnonymous]
        [HttpGet("states")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> States()
        {

            var request = State_CityService.GetStates().GetAwaiter().GetResult();
            return Ok(new Response<List<States>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
            });
        }
        [AllowAnonymous]
        [HttpGet("cities/{code}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Cities(string code)
        {

            var request = State_CityService.GetCities(code).GetAwaiter().GetResult();
            return Ok(new Response<List<Cities>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
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
