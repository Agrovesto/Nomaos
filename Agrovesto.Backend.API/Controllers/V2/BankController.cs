using Agrovesto.Backend.Core.Models.Common;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agrovesto.Backend.API.Controllers.V2
{

    [ApiController]
    [Route("api/[controller]")]
    public class BankController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BankController> _logger;

        public BankController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, ILogger<BankController> logger) : base(httpContextAccessor)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetBanks()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "bank.json");

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Could not find internal file holding bank data");
                return NotFound("Error retrieving list of banks");
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);

            var banks = JsonSerializer.Deserialize<List<BankModel>>(json);

            if (banks == null)
            {
                _logger.LogError("Could not read bank list from file");
                return BadRequest("Unable to read bank data.");
            }

            var response = banks.Select(x => new
            {
                BankName = x.BankName,
                Code = x.BankCode
            });

            return Ok(response);
        }
    }
}
