using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.LocationService;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
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
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IIdentityVerificationManager _verificationManager;
        private readonly IWalletProviderManager _walletProvider;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly INotificationService _smsService;
        private readonly IBusinessManager _businessManager;
        private readonly ISettingsManager _settingsManager;
        public LOVController(ICafeOneWalletService cafeOneWalletService,
            IGoogleDriveService googleDriveService,
            IIdentityVerificationManager verificationManager,
            INigeriaStateImporterService nigeriaStateImporterService,
             IFlutterwaveService flutterwaveService,
             IWebHostEnvironment hostingEnvironment, INotificationService smsService,
             ISettingsManager settingsManager, IWalletProviderManager walletProvider, IBusinessManager businessManager)
        {
            _cafeOneWalletService = cafeOneWalletService;
            _hostingEnvironment = hostingEnvironment;
            _nigeriaStateImporterService = nigeriaStateImporterService;
            _flutterwaveService = flutterwaveService;
            _smsService = smsService;
            _googleDriveService = googleDriveService;
            _verificationManager = verificationManager;
            _settingsManager = settingsManager;
            _walletProvider = walletProvider;
            _businessManager = businessManager;
        }

        [HttpGet("location-types")]

        public async Task<IActionResult> GetLocationTypes([FromQuery] ListQueryParams args)
        {
            try
            {

                var type = await _businessManager.GetLocationAsync(args);
                return Ok(new Response<List<LocationTypeModel>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("location-type")]

        public async Task<IActionResult> GetLocationType([FromQuery]ListQueryParams query)
        {
            try
            {

                var type = await _businessManager.GetLocationById(query);
                return Ok(new Response<LocationTypeModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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


        [HttpGet("membership-types")]

        public async Task<IActionResult> GetUsers([FromQuery] BusinessRegistrationArgs args)
        {
            try
            {

                var type = await _settingsManager.GetMembershipTypes(args);
                return Ok(new Response<List<MemberShipTypeModel>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("membership-type/{id}")]

        public async Task<IActionResult> GetUsers(long id)
        {
            try
            {

                var type = await _settingsManager.GetMemberShipTypeById(id);
                return Ok(new Response<MemberShipTypeModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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



        [HttpPost("supporting-image")]
        public async Task<IActionResult> UploadImage([FromForm] SupportingDOcument model)
        {
            var imageUrl = await _googleDriveService.UploadFileAsync(model.File);
            return Ok(new { imageUrl });
        }

        [HttpGet("nin/{nin}")]
        [ProducesResponseType(typeof(NINDataVerificationResponse<NINData, Verification>), 200)]
        [ProducesResponseType(typeof(NINDataVerificationResponse<NINData, Verification>), 400)]
        [ProducesResponseType(typeof(NINDataVerificationResponse<NINData, Verification>), 500)]
        public async Task<IActionResult> VerifyNIN(string nin)
        {
            if (!ModelState.IsValid)
            {
                var message = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new Response<NINData>()
                {
                    Succeeded = false,
                    Message = message
                });
            }
            var sendRequest = await _verificationManager.VerifyNIN(new NINDataModel
            {
                number= nin,
            });
            return Ok(new Response<NINData>()
            {
                Code = sendRequest.response_code,
                Data = sendRequest.nin_data,
                Succeeded = sendRequest.status,
               // Message = sendRequest.verification,
                Message = sendRequest.detail
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
            try { 
            var request = await _nigeriaStateImporterService.GetStatesAsync();
            return Ok(new Response<List<StateModel>>()
            {
                Code = request == null ? ResponseCodes.NotSuccessful : ResponseCodes.Success,
                Succeeded = request != null,
                Message = request == null ? "Failed" : "Success",
                Data = request.Count > 0 ? request : null
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


        [HttpGet("wallet/provider")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetAllWallets()
        {
            var result = await _walletProvider.GetAll();
            return Ok(new Response<WalletProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("wallet/provider/{id}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetById(long id)
        {
            var result = await _walletProvider.Get(id);
            return Ok(new Response<WalletProviderModel>()
            {
                Succeeded = true,
                Data = result
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
