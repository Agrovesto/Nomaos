using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataMigrationController : BaseController
    {
        private readonly IMigrationService _migrationService;
        private readonly IUserRoleReconciliationService reconciliationService;
        public DataMigrationController(IHttpContextAccessor httpContextAccessor, IMigrationService migrationService,
            IUserRoleReconciliationService userRoleReconciliationService) : base(httpContextAccessor)
        {
            reconciliationService= userRoleReconciliationService;
            _migrationService = migrationService;
        }

        [HttpPost("run-user")]
        public async Task<IActionResult> RunUserTableMigrationAsync()
        {            
            await _migrationService.RunUserTableMigrationAsync();
            return Ok("result");
        }
        [HttpPost("reconcile-user-data")]
        public async Task<IActionResult> RunUserReconciliation()
        {
            // Preview only — nothing written to DB
            var preview = await reconciliationService.ReconcileAsync(dryRun: true);
            Console.WriteLine($"Would create {preview.FarmersCreated} farmers, {preview.AgentsCreated} agents, " +
                               $"{preview.MembershipsCreated} memberships, {preview.OrganizationsCreated} organizations.");

            // Actually run it
            var actual = await reconciliationService.ReconcileAsync(dryRun: false);

            return Ok(new
            {
                Preview = preview,
                Actual = actual
            });
        }
        [HttpPost("run-organization")]
        public async Task<IActionResult> RunOrganizationTableMigrationAsync()
        {   
             await _migrationService.RunOrganizationTableMigrationAsync();
             return Ok("result");
        }

        [Authorize]
        [HttpPost("run-new-business-migration")]
        public async Task<IActionResult> RunOrganizationProductCategoryTableMigrationAsync([FromQuery]string sourceBusinessCode, 
            [FromQuery]string newBusinessCode)
        {
            string userId = CurrentUserId;
             await _migrationService.RunOrganizationProductCategoryTableMigrationAsync(sourceBusinessCode, newBusinessCode);
             await _migrationService.RunInputFinancingCategoryTableMigrationAsync(sourceBusinessCode, newBusinessCode);
             await _migrationService.RunTemperatureControlTypeTableMigrationAsync(sourceBusinessCode, newBusinessCode, userId);
             await _migrationService.RunStorageCategoryTableMigrationAsync(sourceBusinessCode, newBusinessCode, userId);
             await _migrationService.RunLocationTypeTableMigrationAsync(sourceBusinessCode, newBusinessCode);
             await _migrationService.RunMemberShipTypeTableMigrationAsync(sourceBusinessCode, newBusinessCode, userId);
             await _migrationService.RunModuleTableMigrationAsync(sourceBusinessCode, newBusinessCode, userId);
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
