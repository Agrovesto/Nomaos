using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BundleManagementController : ControllerBase
    {
        /**
         * 
         * POST /api/bundles → Create a bundle plan (with items)

GET /api/bundles/{orgId} → List all bundles for org

GET /api/bundles/{id} → Get details of one bundle

PUT /api/bundles/{id} → Update bundle plan

DELETE /api/bundles/{id} → Delete bundle plan
         **/
    }
}
