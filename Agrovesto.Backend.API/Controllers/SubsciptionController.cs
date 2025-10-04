using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubsciptionController : ControllerBase
    {
        /**
         * POST /api/subscriptions → Create subscription request

Request includes: FarmerId/CoopId, BundlePlanId (optional), CropType, Farmland size, Equity details

GET /api/subscriptions/{farmerId} → Farmer views own subscriptions

GET /api/subscriptions/by-coop/{coopId} → Cooperative views all their farmers’ subscriptions

GET /api/subscriptions/{id} → Get subscription details (for Agent/Admin review)

PUT /api/subscriptions/{id}/approve → Approve subscription (Admin/Org/Agent)

PUT /api/subscriptions/{id}/reject → Reject subscription
         */
    }
}
