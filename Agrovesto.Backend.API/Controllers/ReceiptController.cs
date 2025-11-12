using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiptController : ControllerBase
    {
        /**
         * GET /api/receipts/{repaymentId} → Get receipt for a repayment

GET /api/receipts/by-farmer/{farmerId} → Farmer views all receipts

GET /api/receipts/by-coop/{coopId} → Coop views receipts for their network
         */
    }
}
