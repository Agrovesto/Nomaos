using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmerTransactionManagementController : BaseController
    {
        public FarmerTransactionManagementController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }
    }
}
