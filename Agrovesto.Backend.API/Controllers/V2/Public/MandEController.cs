using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.MandE;
using Agrovesto.Backend.Core.Models.Common.MandEService;
using Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Utilities;
using CsvHelper.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Public
{
    [Route("api/v2/m-and-e")]
    [ApiController]
    public class MandEController : BaseController
    {
        private readonly IMandEActivityManager _datasetManager;
        public MandEController(IHttpContextAccessor httpContextAccessor, IMandEActivityManager svc) : base(httpContextAccessor)

        {
            _datasetManager = svc;
        }
        // CREATE
        [HttpPost("create-dataset")]
        public async Task<IActionResult> CreateDataset([FromBody] CreateMandEDatasetDto dto, [FromQuery] ListQueryParams query)
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
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : query.UserId;


                var result = await _datasetManager.CreateDatasetAsync(dto, query);

                return Ok(new Response<long>()
                {
                    Code = result > 0 ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = result > 0 ? true : false,
                    Message = result > 0 ? "Logged Successfully" : "Failed to log activity type",
                    Data = result
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
        // UPDATE
        // [HttpPut("{datasetId:long}")]
        //public async Task<IActionResult> UpdateDataset(long datasetId, [FromBody] UpdateMandEDatasetDto dto);

        // DELETE (soft delete recommended)
        //[HttpDelete("{datasetId:long}")]
        // public async Task<IActionResult> DeleteDataset(long datasetId);

        // GET single
        // [HttpGet("{datasetId:long}")]
        //public async Task<IActionResult> GetDataset(long datasetId);
        [HttpGet("dataset-overview")]
        public async Task<IActionResult> GetOverview([FromQuery] ListQueryParams query)
        {
           
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var datasets = await _datasetManager.GetDatasetOverviewAsync(query);
                return Ok(new Response<DatasetOverviewDto>()
                {
                    Data = datasets,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
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
        // LIST (by business / org)
        [HttpGet("datasets")]
        public async Task<IActionResult> GetDatasets([FromQuery] ListQueryParams query)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var datasets = await _datasetManager.GetDatasetsAsync(query);
                return Ok(new PagedListResponse<IEnumerable<MandEDatasetListDto>>()
                {
                    Data = datasets,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = datasets !=null ? datasets.Count() : 0,
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

        // CREATE FIELD
        [HttpPost("create-field")]
        public async Task<IActionResult> CreateField(
           // long datasetId,
            [FromBody] CreateMandEFieldDto dto, [FromQuery] ListQueryParams query)
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
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : query.UserId;

                var field = await _datasetManager.CreateDataSetFieldAsync(
               //datasetId, 
               dto, query);
                return Ok(new Response<long>()
                {
                    Code = field > 0 ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = field > 0 ? true : false,
                    Message = field > 0 ? "Logged Successfully" : "Failed to submit field",
                    Data = field
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

        // UPDATE FIELD (including formula)
        //[HttpPut("{fieldId:long}")]
        //public async Task<IActionResult> UpdateField(
          //  long datasetId,
            //long fieldId,
            //[FromBody] UpdateMandEFieldDto dto);

        // DELETE FIELD
        //[HttpDelete("{fieldId:long}")]
       // public async Task<IActionResult> DeleteField(long datasetId, long fieldId);

        // LIST FIELDS
        [HttpGet("getfields/{datasetId}")]
        public async Task<IActionResult> GetFields(long datasetId)
        {
             


            try
            {
                var fields = await _datasetManager.GetDataSetFieldsAsync(datasetId);
                return Ok(new PagedListResponse<IEnumerable<MandEFieldDto>>()
                {
                    Data = fields,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Count = fields != null ? fields.Count() : 0,
                   // PageNumber = query.PageNumber,
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

        // VALIDATE FORMULA (used by frontend before save)
        //[HttpPost("validate-formula")]
        //public async Task<IActionResult> ValidateFormula(
        //    long datasetId,
        //    [FromBody] ValidateFormulaDto dto);

        // CREATE SINGLE ROW
        [HttpPost("create-record")]
        public async Task<IActionResult> CreateRecord(
            //long datasetId,
            [FromBody] CreateMandERecordDto dto, [FromQuery] ListQueryParams query)
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
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : query.UserId;

                var record = await _datasetManager.CreateDataSetRecordAsync(query.Id, dto, query);
                return Ok(new Response<long>()
                {
                    Code = record > 0 ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = record > 0 ? true : false,
                    Message = record > 0 ? "Logged Successfully" : "Failed to log records",
                    Data = record
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


        [HttpPut("update-record/{recordId}")]
        public async Task<IActionResult> UpdateRecord(
            long recordId,
            [FromBody] UpdateMandERecordDto dto, [FromQuery] ListQueryParams query)
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
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : query.UserId;

                 await _datasetManager.UpdateDataSetRecordAsync(recordId, dto, query);
                return Ok(new Response<UpdateMandERecordDto>()
                {
                    Code =  ResponseCodes.Success ,
                    Succeeded = true,
                    Message =  "Logged Successfully" ,
                    Data = dto
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

        // UPDATE ROW
        //[HttpPut("{recordId:long}")]
        //public async Task<IActionResult> UpdateRecord(
        //    long datasetId,
        //    long recordId,
        //    [FromBody] UpdateMandERecordDto dto);

        // DELETE ROW
        //[HttpDelete("{recordId:long}")]
        //public async Task<IActionResult> DeleteRecord(long datasetId, long recordId);

        // GET SINGLE ROW
        //[HttpGet("{recordId:long}")]
        //public async Task<IActionResult> GetRecord(long datasetId, long recordId);

        // LIST ROWS (paging + filters)
        [HttpGet("getrecords")]
        public async Task<IActionResult> GetRecords(
           // long datasetId,
            [FromQuery] ListQueryParams query)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;
                query.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : query.UserId;

                var records = await _datasetManager.GetDataSetRecordsAsync(query.Id, query);
                return Ok(new Response<IEnumerable<MandERecordDto>>()
                {
                    Code = records !=null ? ResponseCodes.Success : ResponseCodes.Failed,
                    Succeeded = records != null ? true : false,
                    Message = records != null ? "Logged Successfully" : "Failed to log activity type",
                    Data = records
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

        // BULK UPLOAD (CSV / Excel)
        //[HttpPost("bulk-upload")]
        //public async Task<IActionResult> BulkUpload(
        //    long datasetId,
        //    IFormFile file);

        //// DOWNLOAD TEMPLATE (Excel with headers + formulas)
        //[HttpGet("template")]
        //public async Task<IActionResult> DownloadTemplate(long datasetId);
        // CREATE CHART
        [HttpPost("create-chart")]
        public async Task<IActionResult> CreateChart(
            long datasetId,
            [FromBody] CreateMandEChartDto dto)
        {
            var chart = await _datasetManager.CreateChartAsync(datasetId, dto);
            return Ok(chart);
        }

        // UPDATE CHART
        //[HttpPut("{chartId:long}")]
        //public async Task<IActionResult> UpdateChart(
        //    long datasetId,
        //    long chartId,
        //    [FromBody] UpdateMandEChartDto dto);

        // DELETE CHART
        //[HttpDelete("{chartId:long}")]
        //public async Task<IActionResult> DeleteChart(long datasetId, long chartId);

        // LIST CHARTS
        [HttpGet("get-charts")]
        public async Task<IActionResult> GetCharts(long datasetId)
        {
            var charts = await _datasetManager.GetChartsAsync(datasetId);
            return Ok(charts);
        }

        // PREVIEW / RENDER CHART DATA
        //[HttpPost("{chartId:long}/data")]
        //public async Task<IActionResult> GetChartData(
        //    long datasetId,
        //    long chartId,
        //    [FromBody] ChartQueryDto query);

        // GENERIC AGGREGATION
        //[HttpPost("aggregate")]
        //public async Task<IActionResult> Aggregate(
        //    long datasetId,
        //    [FromBody] AggregationRequestDto dto);

        //// QUICK KPI
        //[HttpGet("kpis")]
        //public async Task<IActionResult> GetKpis(
        //    long datasetId,
        //    [FromQuery] KpiQueryDto query);

        //// GRANT ACCESS
        //[HttpPost]
        //public async Task<IActionResult> GrantPermission(
        //    long datasetId,
        //    [FromBody] GrantPermissionDto dto);

        //// UPDATE ROLE
        //[HttpPut("{permissionId:long}")]
        //public async Task<IActionResult> UpdatePermission(
        //    long datasetId,
        //    long permissionId,
        //    [FromBody] UpdatePermissionDto dto);

        //// REMOVE ACCESS
        //[HttpDelete("{permissionId:long}")]
        //public async Task<IActionResult> RevokePermission(
        //    long datasetId,
        //    long permissionId);

        //// LIST USERS & ROLES
        //[HttpGet]
        //public async Task<IActionResult> GetPermissions(long datasetId);

        // Supported field types
        //[HttpGet("field-types")]
        //public IActionResult GetFieldTypes();

        //// Supported chart types
        //[HttpGet("chart-types")]
        //public IActionResult GetChartTypes();

        //// Formula functions (SUM, AVG, IF, etc.)
        //[HttpGet("formula-functions")]
        //public IActionResult GetFormulaFunctions();
    }
}
