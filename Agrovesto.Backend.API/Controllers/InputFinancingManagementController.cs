using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InputFinancingManagementController : BaseController
    {
        private readonly IInputFinancingManagement _inputFinancingManagement;
        public InputFinancingManagementController(IHttpContextAccessor httpContextAccessor,
            IInputFinancingManagement inputFinancingManagement) : base(httpContextAccessor)
        {
            _inputFinancingManagement = inputFinancingManagement;
        }
        [HttpPost("indicate-interest")]
        public async Task<IActionResult> CreateInterest([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("MerchantAdmin"))) throw new Exception("Forbidden");


            try
            {
                args.UserId = CurrentUserId;
                var bundle = await _inputFinancingManagement.CreateInputFinancingInterest(model, args);
                return Ok(new Response<BundleSubscriptionInterestModel>()
                {
                    Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = bundle.data
                });


            }
            //catch (BaseException ex)
            //{
            //    return StatusCode((int)ex.HttpStatusCode, new ValidationResponse<string>
            //    {
            //        Message = ex.ExceptionMessage,
            //        Succeeded = false,

            //    });
            //}
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

    }
}
