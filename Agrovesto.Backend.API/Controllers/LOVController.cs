using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure;
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
        private readonly INigeriaStateImporterService _nigeriaStateImporterService;
        private readonly IFlutterwaveService _flutterwaveService;

        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ISMSService _smsService;
        public LOVController(ICafeOneWalletService cafeOneWalletService,
            INigeriaStateImporterService nigeriaStateImporterService,
             IFlutterwaveService flutterwaveService, IWebHostEnvironment hostingEnvironment, ISMSService smsService)
        {
            _cafeOneWalletService = cafeOneWalletService;
            _hostingEnvironment = hostingEnvironment;
            _nigeriaStateImporterService = nigeriaStateImporterService;
            _flutterwaveService = flutterwaveService;
            _smsService = smsService;
        }


        [AllowAnonymous]
        [HttpGet("states")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> States()
        {

            var request = await _nigeriaStateImporterService.GetStatesAsync();
            return Ok(new Response<List<StateModel>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
            });
        }
        [AllowAnonymous]
        [HttpGet("lga/{stateId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Cities(long stateId)
        {

            var request = await _nigeriaStateImporterService.GetLgasByState(stateId);

            return Ok(new Response<List<LGAModel>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
            });
        }


        [AllowAnonymous]
        [HttpGet("wards/{lgaId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Wards(long lgaId)
        {

            var request = await _nigeriaStateImporterService.GetWardsByLga(lgaId);

            return Ok(new Response<List<WardModel>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
            });
        }


        [AllowAnonymous]
        [HttpPost("send-sms")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendSMS()
        {
            var messageBody = $"A new business  has been added to your merchant list for Farmer Management Platform." +
                        $"You can always reach out to the business using  ";


            var sms2 = await _smsService.HandleTermiiSMS(new TermiiSmsRequest
            {
                to = $"2349061562512",
                sms = messageBody
            });
        
            return Ok(new Response<(TermiiResponse response, bool status)>()
            {
                Code = ResponseCodes.Success,
                Succeeded = true,
                Message = "success",
                Data =sms2
            });
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

            var request = await _cafeOneWalletService.InterBankNameEnquiry(accountNumber, bankCode, env);
           
         
            return Ok(new Response<NameEnquiry>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request != null ? request : null
            });
        }
        //[AllowAnonymous]
        //[HttpGet("banks")]
        //[ProducesResponseType(typeof(Response<object>), 200)]
        //[ProducesResponseType(typeof(Response<object>), 400)]
        //[ProducesResponseType(typeof(Response<object>), 422)]
        //[ProducesResponseType(typeof(Response<object>), 500)]
        //public async Task<IActionResult> Banks([FromQuery] string env, [FromQuery] string? countryCode = "NG")
        //{
        //    var getbanks = await _flutterwaveService.GetBanks(env, countryCode);

        //    return Ok(getbanks);
        //    //return Ok(new Response<BankResponseModel>()
        //    //{
        //    //    Code = ResponseCodes.Success,
        //    //    Succeeded = true,
        //    //    Message = "Success",
        //    //    Data = await _flutterwaveService.GetBanks(countryCode)
        //    //});

        //}

/**
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

        **/
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
