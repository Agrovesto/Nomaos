using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.MandE;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/m-and-e-activity-types")]
    [ApiController]

    public class MandEController : BaseController
    {
        private readonly IMandEActivityManager _svc;
        public MandEController(IHttpContextAccessor httpContextAccessor, IMandEActivityManager svc) : base(httpContextAccessor)

        {
            _svc = svc;
        }
        [HttpPost("activity-type")]
        public async Task<IActionResult> Submit([FromBody] CreateMandEActivityTypeModel dto, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;


                var log = await _svc.CreateActivityTypes(dto, args);

                return Ok(new Response<long>()
                {
                    Code = log > 0 ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = log > 0 ? true : false,
                    Message = log > 0 ? "Logged Successfully" : "Failed to log activity type",
                    Data = log
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        [HttpPost("activity-type-subactivity")]
        public async Task<IActionResult> Submit([FromBody] CreateMandESubActivityModel dto, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;


                var log = await _svc.CreateActivityTypeSubActivities(dto, args);

                return Ok(new Response<long>()
                {
                    Code = log > 0 ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = log > 0 ? true : false,
                    Message = log > 0 ? "Logged Successfully" : "Failed to log sub activity type",
                    Data = log
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        /// <summary>
        /// Create a single field definition
        /// </summary>
        [HttpPost("single-field")]
        public async Task<IActionResult> CreateSingleField([FromBody] MandEFieldDefinition model, [FromQuery] ListQueryParams args)
        {   if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;

                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;
                var created = await _svc.CreateFieldAsync(model, args);
                return Ok(new { success = true, data = created });
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }

        /// <summary>
        /// Create multiple field definitions (bulk)
        /// </summary>
        [HttpPost("bulk-field")]
        public async Task<IActionResult> CreateFieldsBulk([FromBody] IEnumerable<CreateMandEFieldDefinitionModel> models, [FromQuery]ListQueryParams args)
        {
            if (models == null || !models.Any())
                return BadRequest("No field definitions provided.");

            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                
           
                var created = await _svc.CreateFieldsBulkAsync(models, args);
                return Ok(new { success = true, data = created });
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetTypes([FromQuery] ListQueryParams query)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _svc.GetActivityTypesAsync(query.UserId, query);
                return Ok(new PagedListResponse<IEnumerable<MandEActivityTypeModel>>()
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = result.Count(),
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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


        [HttpGet("{activityTypeId:long}/sub-activities")]
        public async Task<IActionResult> GetSubs([FromRoute] long activityTypeId)
        {


          //  Ok(await _svc.GetSubActivitiesAsync(activityTypeId));
            try
            {
                //if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                //{
                //    query.organizationCode = OrganizationCode;
                //}
                //query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _svc.GetSubActivitiesAsync(activityTypeId);
                return Ok(new PagedListResponse<IEnumerable<SubMandEActivityModel>>()
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = result.Count(),
                 //   PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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



        [HttpGet("{subActivityId:long}/fields")]
        public async Task<IActionResult> GetFields([FromRoute] long subActivityId)
        {

            try
            {
                //if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                //{
                //    query.organizationCode = OrganizationCode;
                //}
                //query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _svc.GetFieldsAsync(subActivityId);
                return Ok(new PagedListResponse<IEnumerable<TrackableMandEFieldModel>>()
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = result.Count(),
                    //   PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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
        [HttpPost("log")]
        public async Task<IActionResult> Submit([FromBody] SubmitFarmerActivityEntryModel dto, [FromQuery] BusinessRegistrationArgs args)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;


var log = await _svc.SubmitEntryAsync(dto, args);

                return Ok(new Response<long>()
                {
                    Code =log>0 ? ResponseCodes.Success: ResponseCodes.Failed,
                    Succeeded = log > 0 ? true : false,
                    Message = log > 0 ? "Logged Successfully" : "Failed to log activity",
                    Data = log
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
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }


        [HttpGet("logs")]
        public async Task<IActionResult> GetActivitiesLog([FromQuery] ListQueryParams query)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _svc.GetMandELogsAsync(query);
                return Ok(new PagedListResponse<IEnumerable<MandEActivityLogModel>>()
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = result.Count(),
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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


        [HttpGet("{subActivityId:long}/logs")]
        public async Task<IActionResult> GetActivitiesLog(long subActivityId, [FromQuery] ListQueryParams query)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _svc.GetMandELogsAsync(subActivityId,query);
                return Ok(new PagedListResponse<IEnumerable<MandEActivityLogModel>>()
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = result.Count(),
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

    }


}
