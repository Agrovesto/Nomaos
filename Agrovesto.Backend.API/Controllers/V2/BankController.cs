using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2
{

    [ApiController]
    [Route("api/[controller]")]
    public class BankController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ICafeOneWalletService _cafeOneWalletService;


        private readonly ILogger<BankController> _logger;

        public BankController(IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment,
            ILogger<BankController> logger,
            ICafeOneWalletService cafeOneWalletService) : base(httpContextAccessor)
        {
            _environment = environment;
            _logger = logger;
            _cafeOneWalletService = cafeOneWalletService;
        }

        [HttpGet()]
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
                Message = request == null ? ResponseCodes.Failed : ResponseCodes.Success,
                Data = request != null ? request : null
            });
        }

        [HttpGet("name-inquiry/{accountNumber}/{bankCode}")]
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
                Message = request == null ? ResponseCodes.Failed : ResponseCodes.Success,
                Data = request != null ? request : null
            });
        }
    }
}
