using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/farm/activities")]
    [ApiController]
    public class FarmManagementController : BaseController
    {
        private readonly IFarmPlotDetailsManager _farmPlotDetailsManager;

        public FarmManagementController(IHttpContextAccessor httpContextAccessor,
            IFarmPlotDetailsManager farmPlotDetailsManager) : base(httpContextAccessor)
        {
            
            _farmPlotDetailsManager = farmPlotDetailsManager;
        }
        [HttpPost("update-plot-details")]
    
        public async Task<IActionResult> UpdateFarmPlotDetails([FromBody] FarmPlotDetailsModel model, [FromQuery] BusinessRegistrationArgs args)
        {

            try
            {
                //if user to be created is a farmer, args.userId has to be cooperative user Id
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                var updateDetail = await _farmPlotDetailsManager.UpdateFarmPlotDetails(model, args);
                

                return Ok(new Response<FarmPlotDetailsModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = updateDetail
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


        [HttpPost("plot-details")]

        public async Task<IActionResult> GetPlotDetails([FromQuery] long farmerId)
        {

            try
            {
     
                var updateDetail = await _farmPlotDetailsManager.GetFarmDetailByFarmer(farmerId);


                return Ok(new Response<FarmPlotDetailsModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = updateDetail
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
