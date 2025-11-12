using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepaymentController : ControllerBase
    {
        /**
         * POST /api/repayments → Make repayment (cash, produce, or both)

Request includes SubscriptionId, Mode, Amount, ProduceValue

GET /api/repayments/{farmerId} → Farmer views repayments made

GET /api/repayments/by-coop/{coopId} → Coop views repayments for their network

GET /api/repayments/{id} → Get repayment details

PUT /api/repayments/{id}/mark-complete → Admin/Agent marks repayment as fully settled
         */
    }
}
