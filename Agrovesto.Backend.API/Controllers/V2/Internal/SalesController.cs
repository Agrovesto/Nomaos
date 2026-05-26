using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly ITenantContextAccessor _tenant;

        public SalesController(ITenantContextAccessor tenant)
        {
            _tenant = tenant;
        }

        [HttpPost("create")]
        public void CreateSale()
        {
            var businessId = _tenant.Current.BusinessId;

            var hasAccess = _tenant.Current.Modules
                .Contains("SALES");


            if (!hasAccess)
                throw new Exception("Module not enabled");


        }

    }
}
