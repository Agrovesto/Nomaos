using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataMigrationController : ControllerBase
    {
        private readonly IMigrationService _migrationService;
        public DataMigrationController(IMigrationService migrationService)
        {
            _migrationService = migrationService;
        }
        [HttpPost("run-user")]
        public async Task<IActionResult> RunUserTableMigrationAsync()
        {
            
             await _migrationService.RunUserTableMigrationAsync();
            return Ok("result");

        }
        [HttpPost("run-organization")]
        public async Task<IActionResult> RunOrganizationTableMigrationAsync()
        {
            
             await _migrationService.RunOrganizationTableMigrationAsync();
            return Ok("result");

        }
        //[HttpPost("run-commitmentPlan")]
        //public async Task<IActionResult> RunCommitmentPlanMigrationAsync()
        //{
            
        //     await _migrationService.RunCommitmentPlanMigrationAsync();
        //    return Ok("result");

        //}
        //[HttpPost("run-repaymentPlan")]
        //public async Task<IActionResult> RunRepaymentPlanMigrationAsync()
        //{
            
        //     await _migrationService.RunRepaymentPlanMigrationAsync();
        //    return Ok("result");

        //}
    }
}
