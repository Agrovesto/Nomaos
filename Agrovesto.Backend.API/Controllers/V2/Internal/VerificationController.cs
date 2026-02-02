using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificationController : ControllerBase
    {
        public VerificationController()
        {
            
        }


        ///Verify farmers phone number by sending otp to the farmer, if verification is successful, update farmers level to appropriate level
        ///
        ///
        ///

        ///verify farmers bvn by calling identity verification
        ///verify farmers nin by calling verification
        ///

        ///verify cooperative 
    }
}
