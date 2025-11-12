using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditScoreController : ControllerBase
    {
        /**
         * GET /api/credit-score/{farmerId} → Get farmer credit score

PUT /api/credit-score/{farmerId}/recompute → Force recompute score (system/admin job)
         */
    }

}
