using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/Business")]
    //[ApiController]
    public class BusinessController : BaseController
    {
        private readonly IBusinessManager _businessManager;

        private readonly IRoleManager _roleManager;
        public BusinessController(IHttpContextAccessor httpContextAccessor,IBusinessManager businessManager, IRoleManager roleManager) : base (httpContextAccessor)
        {
            _businessManager = businessManager;
            _roleManager = roleManager;
        }

        [HttpPost("onboard-business")]
        public async Task<IActionResult> CreateMerchant([FromBody] BusinessModel model, [FromQuery]BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || !isAdmin.Equals("Admin")) throw new Exception("Forbidden");


            try
            {
                args.UserId = CurrentUserId;
                var createMerchant = await _businessManager.CreateBusiness(model, args);

                return Ok(new Response<BusinessModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = createMerchant
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
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        [HttpPost("create-worker")]
        public async Task<IActionResult> CreateBusinessWorker([FromBody] BusinessWorkerModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || ( !isAdmin.Equals("MerchantAdmin"))) throw new Exception("Forbidden");


            try
            {
                args.UserId = CurrentUserId;
                var createWorker = await _businessManager.CreateBusinessWorkerAsync(model, args);

                return Ok(new Response<BusinessWorkerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = createWorker
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
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] RoleModel roleModel)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("MerchantAdmin"))) throw new Exception("Forbidden");

            var data = await _roleManager.BusinesCreateRoleAsync(CurrentUserId, roleModel);
            return Ok(new AgrovestoResponse<RoleModel>()
            {
                Message = "",
                Result = data.Result,

            });
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("MerchantAdmin"))) throw new Exception("Forbidden");

            var data = await _roleManager.GetRolesByBusinessAsync(CurrentUserId);
            return Ok(new PagedListResponse<List<RoleModel>>()
            {
                Message = "",
                Data = data.Data,
                Count = data.Count

            });
        }



    }
}
