using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DisbursementController : ControllerBase
    {
        /**
         * POST /api/disbursements/{invoiceId} → Disburse input or cash based on org config

Mode: “Input” or “Cash”

Receiver: Farmer or Coop

Amount or input details

GET /api/disbursements/{farmerId} → Farmer views what they collected

GET /api/disbursements/by-coop/{coopId} → Coop views disbursements under them

GET /api/disbursements/{id} → Get details
         */
    }
}
