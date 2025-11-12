using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        /**
         * POST /api/invoices/{subscriptionId} → Generate invoice (system after approval)

GET /api/invoices/{farmerId} → Farmer views all invoices

GET /api/invoices/{id} → Get invoice by Id

GET /api/invoices/by-ref/{refCode} → Get invoice by Reference Code (for collection)

PUT /api/invoices/{id}/approve → Approve invoice for disbursement

PUT /api/invoices/{id}/mark-collected → Mark as collected
         */
    }
}
