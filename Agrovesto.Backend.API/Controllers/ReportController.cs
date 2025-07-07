using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.Dashboard;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Constants = Agrovesto.Backend.Core.Models.Common.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : BaseController
    {
        private readonly IReportManager _reportManager;
        private readonly IDashboardManager _dashboardManager;
        public ReportController(IHttpContextAccessor httpContextAccessor,
            IReportManager reportManager,
            IDashboardManager dashboardManager) : base(httpContextAccessor)
        {
            _reportManager = reportManager;
            _dashboardManager = dashboardManager;
        }
        [HttpGet("farmer-dashboard")]
        public async Task<IActionResult> GetFarmerDashboardSummaryAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _dashboardManager.GetFarmerDashboardSummaryAsync(query);
                return Ok(new Response<FarmerSalesSummary>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Code = (result.Succeeded && result.Data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }

        [HttpGet("download-input-subscription")]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 200)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 400)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 422)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 500)]
        public async Task<IActionResult> DownloadInputSubscription(
   [FromQuery] string? businessCode,
   [FromQuery] string? organizationCode,
  [FromQuery] string? cooperativeName,
  [FromQuery] string? state,
   [MaxLength(100)][RegularExpression(Constants.ValidationRegex.General)][FromQuery] string startDate = null,
   [MaxLength(100)][RegularExpression(Core.Models.Common.Constants.ValidationRegex.General)][FromQuery] string endDate = null,
[FromQuery] string? exportFormat = null)
        {

            try
            {


                var result = await _reportManager.FilterFarmerDetailsForDownload(businessCode, organizationCode,cooperativeName, state, startDate, endDate, exportFormat);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
                return File(result.FileBytes, result.MimeType);

                //return Ok(new Response<FileDownloadModel>()
                //{
                //    Succeeded = true,
                //    Data = result
                //});
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }


        [HttpGet("download-farmer")]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 200)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 400)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 422)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 500)]
        public async Task<IActionResult> FilterScheduleForDownload(
        [FromQuery] string? businessCode,
        [FromQuery] string? organizationCode,
       [FromQuery] string? cooperativeName,
       [FromQuery] string? state,
        [MaxLength(100)][RegularExpression(Constants.ValidationRegex.General)][FromQuery] string startDate = null,
        [MaxLength(100)][RegularExpression(Core.Models.Common.Constants.ValidationRegex.General)][FromQuery] string endDate = null,
     [FromQuery] string? exportFormat = null)
        {

            try
            {


                var result = await _reportManager.FilterFarmerDetailsForDownload(businessCode, organizationCode, cooperativeName, state, startDate, endDate, exportFormat);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
                return File(result.FileBytes, result.MimeType);

                //return Ok(new Response<FileDownloadModel>()
                //{
                //    Succeeded = true,
                //    Data = result
                //});
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

              [HttpGet("download-sales-report")]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 200)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 400)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 422)]
        [ProducesResponseType(typeof(Response<FarmerModel[]>), 500)]
        public async Task<IActionResult> FilterForDownload(
        [FromQuery] string? businessCode,
       [FromQuery] string? FarmerUserId,
       [FromQuery] string? state,
       [FromQuery] string? marketDay,
       [FromQuery] string? marketName,
       [FromQuery] string? cropName,
        [MaxLength(100)][RegularExpression(Constants.ValidationRegex.General)][FromQuery] string startDate = null,
        [MaxLength(100)][RegularExpression(Core.Models.Common.Constants.ValidationRegex.General)][FromQuery] string endDate = null,
     [FromQuery] string? exportFormat = null)
        {

            try
            {


                var result = await _reportManager.FilterFarmerTransactionForDownload(businessCode, FarmerUserId, state, marketDay, marketName,cropName,startDate, endDate, exportFormat);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
                return File(result.FileBytes, result.MimeType);

                //return Ok(new Response<FileDownloadModel>()
                //{
                //    Succeeded = true,
                //    Data = result
                //});
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }


    }
}
