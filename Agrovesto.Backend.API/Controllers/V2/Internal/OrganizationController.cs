using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using ValidationException = Agrovesto.Backend.Core.Exceptions.ValidationException;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/organization")]
    [ApiController]
    public class OrganizationController : BaseController
    {

        private readonly IBusinessManager _businessManager;
        private readonly IOrganizationManager _organizationManager;
        private readonly IOnBoardingManager _onboardingManager;
        private readonly Agrovesto.Backend.Infrastructure.DB.DataContext _context; 
        private readonly IMembershipManager _membershipManager;
        private readonly IInputFinancingManager _inputFinancingManager;
        private readonly INomaOSWalletService _walletService;
        private readonly IFarmMappingManager _farmMappingManager;
        private readonly IBulkMembershipExcelParser _parser;
        private readonly IBulkMembershipValidator _validator;
        private readonly IBulkMembershipUploadService _memberShipUploadService;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private readonly IBulkFarmerUploadService _bulkFarmerUploadService;

        public OrganizationController(IHttpContextAccessor httpContextAccessor,
            IBusinessManager businessManager,
             INomaOSWalletService walletService,
             IOrganizationManager organizationManager,
             IMembershipManager membershipManager,
             IInputFinancingManager inputFinancingManager,
             IOnBoardingManager onboardingManager,
             IFarmMappingManager farmMappingManager,

                IBulkFarmerUploadService bulkFarmerUploadService,
        IBulkMembershipExcelParser parser,
        IBulkMembershipValidator validator,
        IBulkMembershipUploadService memberShipUploadService) : base(httpContextAccessor)
        {
                _walletService = walletService;
            _farmMappingManager = farmMappingManager;
            _organizationManager = organizationManager;
            _businessManager = businessManager;
            _onboardingManager = onboardingManager;
            _membershipManager = membershipManager;
            _inputFinancingManager = inputFinancingManager;
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _parser = parser;
            _validator = validator;
            _memberShipUploadService = memberShipUploadService;
            _membershipManager = membershipManager;
            _context = (Agrovesto.Backend.Infrastructure.DB.DataContext)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(Agrovesto.Backend.Infrastructure.DB.DataContext));
        }
        #region settings

     
        [HttpPost("create-location")]
        public async Task<IActionResult> Location(
      [FromBody] CreateLocationDto model,
      [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Any() == true)
                    .ToDictionary(
                        x => x.Key,
                        x => string.Join("; ", x.Value!.Errors.Select(e => e.ErrorMessage))
                    );

                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Succeeded = false,
                    Message = "Validation failed.",
                    Data = errors
                });
            }

            try
            {
                args.organizationCode ??= OrganizationCode;

                if (string.IsNullOrWhiteSpace(args.organizationCode))
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.UNAUTHORIZED,
                        "Access Denied");
                }

                var location = await _organizationManager.CreateLocation(model, args);

                return Ok(new Response<LocationCreateResponse>
                {
                    Code = location != null
                        ? Core.CoreUtils.Constants.ResponseCodes.Success
                        : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = location != null,
                    Message = location != null
                        ? "Successful"
                        : "Failed",

                    Data = location
                });
            }
            catch (BaseException ex)
            {
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = ex.ExceptionMessage
                });
            }
            catch (DbUpdateException ex) when
       (ex.InnerException is SqlException sqlEx && sqlEx.Number == 547)
            {
                return BadRequest(new Response<string>
                {
                    Succeeded = false,
                    Message = "Unable to save the record because some related data is invalid or missing"
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unhandled exception");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response<string>
                    {
                        Succeeded = false,
                        Message = "An unexpected error occurred. Please contact your administrator."
                    });
            }
        }

        [HttpPatch("edit-location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] EditLocationDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                model.Id = args.Id;
                var type = await _organizationManager.UpdateLocationAsync(model, args);

                return Ok(new Response<OrganizationLocationModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var type = await _organizationManager.GetLocationAsync(args);
                return Ok(new PagedListResponse<List<LocationListResponseDto>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type.Data,
                    Count = type.Count,
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize,
                    
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

        [HttpGet("location-overview")]
        public async Task<IActionResult> LocationOverview([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var type = await _organizationManager.GetFullDashboardAsync(args);
                return Ok(new Response<LocationDashboardFullDto>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("sync-stock")]
        public async Task<IActionResult> SyncLocationStockLevels([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                await _organizationManager.SyncLocationStockLevels(args);
                return Ok(new Response<string>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,


                    Succeeded = true,
                    Message = "Successful",
                    Data = "Stock levels synchronized successfully"
              
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


        [HttpGet("location")]
        public async Task<IActionResult> GetLocation([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var type = await _organizationManager.GetLocationById(query);
                return Ok(new Response<LocationResponseDto>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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
        [HttpDelete("delete-location")]
        public async Task<IActionResult> DeleteLocation([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.DeleteLocation(model, args);

                return Ok(new Response<bool>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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



        [HttpPatch("edit-sublocation")]
        public async Task<IActionResult> EditSubLocationAsync([FromBody] EditSubLocationDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.EditSubLocationAsync(model, args);

                return Ok(new Response<OrganizationLocationModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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


        [HttpPost("create-sublocation")]
        public async Task<IActionResult> CreateSubLocationAsync(
    [FromBody] CreateSubLocationDto model,
    [FromQuery] ListQueryParams args)
        {
            // enrich request context
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode))
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));

            var result = await _organizationManager.CreateSubLocationAsync(model, args);

            return Ok(ApiResponse<OrganizationLocationModel>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
      
        [HttpGet("sublocations/by-parent")]
        public async Task<IActionResult> GetSubLocationsByParentAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetSubLocationsByParentAsync(args.Id, args);
                return Ok(new PagedListResponse<List<SubLocationListDto>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type.Data,
                    Count = type.Count,
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize,
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
        [HttpPut("reassign-primary-agent")]
        public async Task<IActionResult> ReassignPrimaryAgentAsync([FromQuery] ListQueryParams args, [FromBody] ReassignPrimaryAgentDto model)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                model.LocationId = args.Id;
                model.PerformedByUserId = CurrentUserId;
                var type = await _organizationManager.ReassignPrimaryAgentAsync(model);
                return Ok(new Response<bool>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type,
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

        [HttpPut("remove-agent-from-location")]
        public async Task<IActionResult> RemoveAgentFromLocationAsync([FromQuery] ListQueryParams args, [FromBody] RemoveAgentFromLocationDto model)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                model.LocationId = args.Id;
                model.RemovedByUserId = CurrentUserId;
                var type = await _organizationManager.RemoveAgentFromLocationAsync(model);
                return Ok(new Response<bool>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type,
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
        [HttpGet("location-agents")]
        public async Task<IActionResult> GetLocationAgents([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetLocationAgentsAsync(args);
                return Ok(new Response<LocationAgentsResponseDto>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type,
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
        [HttpGet("locations-by-agent")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetLocationsByAgentAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                args.UserId = CurrentUserId;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetLocationsByAgentAsync(args);
                return Ok(new Response<PagedListResponse<List<AgentLocationListResponseDto>>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type,
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

        [HttpGet("sublocations")]
        public async Task<IActionResult> GetSubLocationsAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetSubLocationsAsync(args);
                return Ok(new PagedListResponse<List<LocationResponseDto>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type.Data,
                    Count = type.Count,
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize,
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

        [HttpGet("sublocation")]
        public async Task<IActionResult> GetSubLocationByIdAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var type = await _organizationManager.GetSubLocationByIdAsync(query);
                return Ok(new Response<LocationResponseDto>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpDelete("delete-sublocation")]
        public async Task<IActionResult> DeleteSubLocationAsync([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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

                args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.DeleteSubLocationAsync(model, args);

                return Ok(new Response<bool>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpPost("add-inventory")]
        public async Task<IActionResult> AddInventoryAsync([FromBody] CreateInventoryDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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
                args.UserId = CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.AddInventoryAsync(model, args);

                return Ok(new Response<bool>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = true
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
        [HttpDelete("delete-inventory")]
        public async Task<IActionResult> DeleteInventory([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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
                args.UserId = CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                await _organizationManager.DeleteInventory(model, args);

                return Ok(new Response<bool>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = true
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
        [HttpPatch("update-inventory")]
        public async Task<IActionResult> EditInventoryAsync([FromBody] EditInventoryDto model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
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
                args.UserId = CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.EditInventoryAsync(model, args);

                return Ok(new Response<LocationInventory>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful"
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

        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetInventoryByLocationAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _organizationManager.GetInventoryByLocationAsync(args);
                return Ok(new PagedListResponse<List<InventoryListDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpGet("location-activity")]
        public async Task<IActionResult> GetLocationActivityLogsAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _organizationManager.GetLocationActivityLogsAsync(args);
                return Ok(new PagedListResponse<List<LocationActivityLogResponse>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        //[HttpGet("agent-by-location")]
        //public async Task<IActionResult> GetAgentsByLocationAsync([FromQuery] ListQueryParams args)
        //{
        //    try
        //    {
        //        args.organizationCode = args.organizationCode ?? OrganizationCode;
        //        if (string.IsNullOrEmpty(args.organizationCode))
        //        {
        //            throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
        //        }

        //        var response = await _organizationManager.GetAgentsByLocationAsync(args);
        //        return Ok(new PagedListResponse<List<LocationAgentDetailResponse>>()
        //        {
        //            Code = ResponseCodes.Success,
        //            Succeeded = true,
        //            Message = response != null ? "Successful" : "Failed",
        //            Data = response.Data,
        //            Count = response.Count,
        //            PageSize = args.PageSize,
        //            PageNumber = args.PageNumber
        //        });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.ExceptionMessage,
        //            Succeeded = false,

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.Message,
        //            Succeeded = false,
        //        });
        //    }

        //}

        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] OrganizationRoleModel model, [FromQuery] ListQueryParams args)
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

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.CreateRole(model, args);

                return Ok(new Response<OrganizationRoleModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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
        [HttpGet("roles")]

        public async Task<IActionResult> GetRolesAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                var type = await _organizationManager.GetRolesAsync(args);
                return Ok(new Response<List<OrganizationRoleModel>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [HttpGet("role")]

        public async Task<IActionResult> GetRoleById([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var type = await _organizationManager.GetRoleById(query);
                return Ok(new Response<OrganizationRoleModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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



        #endregion settings

        #region OrganizationWallet
        [HttpPost("create-wallet")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] WalletQueryParams query)
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
            (bool flowControl, IActionResult value) = ParameterValidator(request, query);
            if (!flowControl)
            {
                return value;
            }
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }

                var wallet = await _walletService.CreateWallet(request, query);

                return Ok(new Response<(bool status, string message)>()
                {
                    Data = wallet,


                    Message = wallet.message,
                    Succeeded = wallet.status,

                    Code = wallet.status ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("wallets")]
        public async Task<IActionResult> GetWallets([FromQuery] UserWalletParams query)
        {
            try
            {

                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _walletService.GetUserWallletsByUserId(query);
                return Ok(new PagedListResponse<List<UserWalletResponseModel>>()
                {
                    Data = result.Data,
                    Message = result.Count > 0 ? "Successful" : "Failed",
                    Succeeded = true,
                    Count = result.Count,
                    //PageNumber = query.PageNumber,
                    //PageSize = 10,
                    Code = (result.Count > 0) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpPost("wallet-topup")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var response = await _walletService.TopUpWallet(request, args);
                return Ok(new Response<ProviderResponse>()
                {
                    Data = response,
                    Message = "Successful",
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

        [HttpPost("wallet-withdraw")]
        public async Task<IActionResult> WithdrawWallet([FromBody] WithdrawWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var response = await _walletService.WithdrawWallet(request, args);
                return Ok(new Response<ProviderResponse>()
                {
                    Data = response,
                    Message = "Successful",
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

        private (bool flowControl, IActionResult value) ParameterValidator(CreateWalletRequest request, WalletQueryParams query)
        {
            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(query.env))
                return (flowControl: false, value: BadRequest("Environment is required"));

            if (string.IsNullOrWhiteSpace(request.WalletProviderCode))
                return (flowControl: false, value: BadRequest("WalletProviderCode is required"));

            if (string.IsNullOrWhiteSpace(query.businessCode))
                return (flowControl: false, value: BadRequest("BusinessCode is required"));
            if (string.IsNullOrEmpty(query.UserId))
            {
                return (flowControl: false, value: BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = new Dictionary<string, string>
                    {
                            { "UserId", "UserId is required" }
                    }

                }));
            }

            return (flowControl: true, value: null);
        }

        #endregion OrganizationWallet

      #region OrganizationProduct
        [HttpPost("create-product")]
        public async Task<IActionResult> CreateOrganizationProduct([FromBody] OrganizationProductDto model, [FromQuery] ListQueryParams args)
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
                args.UserId = CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");

                var result = await _organizationManager.CreateOrganizationProduct(model, args);

                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data=true
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

        [HttpGet("products")]
        public async Task<IActionResult> GetOrganizationProductList([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");

                var response = await _organizationManager.GetOrganizationProductList(args);

                return Ok(new PagedListResponse<List<OrganizationProductDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpGet("product")]
        public async Task<IActionResult> GetorganizationProductById([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");

                if (args.Id <= 0)
                    return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

                var response = await _organizationManager.GetorganizationProductById(args);

                return Ok(new Response<OrganizationProductDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = response != null,
                    Message = response != null ? "Successful" : "No record found",
                    Data = response
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

        [HttpPatch("edit-product")]
        public async Task<IActionResult> EditProductAsync([FromBody] EditOrganizationProductDto model, [FromQuery] ListQueryParams args)
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
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");

                if (args.Id <= 0)
                    return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

                var result = await _organizationManager.EditProductAsync(args, model);

                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = result,
                    Message = result ? "Successful" : "Failed",
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        [HttpPut("deactivate-product")]
        public async Task<IActionResult> DeactivateProductAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");

                if (args.Id <= 0)
                    return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

                var result = await _organizationManager.DeactivateProductAsync(args);

                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = result,
                    Message = result ? "Successful" : "Failed",
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }
        #endregion OrganizationProduct

        [HttpGet("bulk-jobs")]
        public async Task<IActionResult> ListBulkJobs([FromQuery] string organizationCode = null, [FromQuery] string status = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.BulkUploadJobs.AsQueryable();

                if (!string.IsNullOrWhiteSpace(organizationCode))
                    query = query.Where(j => j.OrganizationCode == organizationCode);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(j => j.Status == status);

                var total = await query.CountAsync();

                var items = await query
                    .OrderByDescending(j => j.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(j => new
                    {
                        j.Id,
                        j.OrganizationCode,
                        j.Status,
                        j.ErrorMessage,
                        j.CreatedAt,
                        j.CompletedAt,
                        j.TotalRows,
                        j.Successful,
                        j.Failed
                    })
                    .ToListAsync();

                return Ok(new PagedListResponse<List<object>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = items.Cast<object>().ToList(),
                    Count = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }

        [HttpGet("bulk-jobs/{id}/file")]
        public async Task<IActionResult> DownloadBulkJobFile(Guid id)
        {
            try
            {
                var job = await _context.BulkUploadJobs.FindAsync(id);
                if (job == null)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Job not found" });

                if (string.IsNullOrWhiteSpace(job.FilePath) || !System.IO.File.Exists(job.FilePath))
                    return NotFound(new Response<string> { Succeeded = false, Message = "Uploaded file not found" });

                var fs = System.IO.File.OpenRead(job.FilePath);
                var fileName = Path.GetFileName(job.FilePath);
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(fs, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }
        [HttpGet("bulk-jobs/{id}")]
        public async Task<IActionResult> GetBulkJob(Guid id)
        {
            try
            {
                var job = await _context.BulkUploadJobs.FindAsync(id);
                if (job == null)
                    return NotFound(new Response<string> { Succeeded = false, Message = "Job not found" });

                return Ok(new Response<object>
                {
                    Succeeded = true,
                    Data = new
                    {
                        job.Id,
                        job.FilePath,
                        job.OrganizationCode,
                        job.Status,
                        job.ErrorMessage,
                        job.CreatedAt,
                        job.CompletedAt,
                        job.TotalRows,
                        job.Successful,
                        job.Failed
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string> { Succeeded = false, Message = ex.Message });
            }
        }

        #region AgentMgt

        [HttpGet("agent-mgt-overview")]

        public async Task<IActionResult> GetAgentManagementOverview([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationDashboardAsync(query);
                return Ok(new Response<OrganizationDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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
        [HttpGet("agents")]
        public async Task<IActionResult> GetOrganizationAgents([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _organizationManager.GetOrganizationAgents(query);
                return Ok(new PagedListResponse<List<OrganizationAgentResponse>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
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

        //create-agent
        [HttpPost("create-agent")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;


                var request = await _organizationManager.CreateAgentUser(model, args);


                return Ok(new Response<CreateAgentResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
                    Data = request
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
        [HttpPost("edit-agent")]
        [AllowAnonymous]
        public async Task<IActionResult> EditAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = args.UserId;
                
                args.organizationCode = args.organizationCode ?? OrganizationCode;


                var request = await _organizationManager.EditAgentUser(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request
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


        [HttpGet("agent-detail")]
        public async Task<IActionResult> GetAgentDetail([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentDetail(args);
            return Ok(response);
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
        [HttpGet("agent-locations")]
        public async Task<IActionResult> GetAgentLocationAssignment([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentAssignedLocations(args);
                return Ok(new PagedListResponse<List<OrganizationAgentLocationResponseModel>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ? "Successful" : "Failed",
                    Data = response,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpPost("change-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeAgentLocation([FromBody] ProfileRequestModel model,
            [FromQuery] OrganizationAgentArgs args)
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
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }


                var request = await _organizationManager.ReassignAgentToLocation(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request != null ? "Successful" : "Failed",
                    Data = request
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


        [HttpPost("assign-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignAgentLocation([FromBody] LocationAgentAssignment  model,
           [FromQuery] ListQueryParams args)
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
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                await _organizationManager.AssignAgentToLocation(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        [HttpPost("assign-agent-role")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignAgentRole([FromBody] ProfileRequestModel model,
           [FromQuery] OrganizationAgentArgs args)
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
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                await _organizationManager.AssignAgentToTole(model, args);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        [HttpGet("agent-activity-log")]
        public async Task<IActionResult> GetAgentActivityLogs([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentActivityLogs(args);
               
                if (response == null || response.Data == null || !response.Data.Any())
                {
                    return Ok(new PagedListResponse<List<OrganizationAgentActivityLog>>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "No record found",
                        Data = new List<OrganizationAgentActivityLog>(),
                        Count = 0,
                        PageSize = args.PageSize,
                        PageNumber = args.PageNumber
                    });
                }

                return Ok(new PagedListResponse<List<OrganizationAgentActivityLog>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpGet("agent-transaction-history")]
        public async Task<IActionResult> GetAgentTransactionHistory([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentTransactionHistory(args);
              
                if (response == null || response.Data == null || !response.Data.Any())
                {
                    return Ok(new PagedListResponse<List<OrganizationAgentTransactionHistory>>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "No record found",
                        Data = new List<OrganizationAgentTransactionHistory>(),
                        Count = 0,
                        PageSize = args.PageSize,
                        PageNumber = args.PageNumber
                    });
                }

                return Ok(new PagedListResponse<List<OrganizationAgentTransactionHistory>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpGet("agent-performance-summary")]
        public async Task<IActionResult> GetOrganizationAgentPerformanceSummary([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
                {
                    { "UserId", "UserId is required" }
                }
                    });
                }

                var response = await _organizationManager.GetOrganizationAgentPerformanceSummary(args);

               
                if (response == null)
                {
                    return Ok(new Response<OrganizationAgentPerformanceSummary>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "No record found",
                        Data = null
                    });
                }

                return Ok(new Response<OrganizationAgentPerformanceSummary>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false
                });
            }
        }
        [HttpGet("agent-subscribers")]
        public async Task<IActionResult> GetAgentSubscribers([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentSubscribers(args);
              
                if (response == null || response.Data == null || !response.Data.Any())
                {
                    return Ok(new PagedListResponse<List<OrganizationAgentSubscriberModel>>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "No record found",
                        Data = new List<OrganizationAgentSubscriberModel>(),
                        Count = 0,
                        PageSize = args.PageSize,
                        PageNumber = args.PageNumber
                    });
                }

                return Ok(new PagedListResponse<List<OrganizationAgentSubscriberModel>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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


        [HttpGet("agent-input-allocation")]
        public async Task<IActionResult> GetAgentInputAllocations([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "UserId is required" }
}

                    });
                }
                var response = await _organizationManager.GetAgentInputAllocations(args);

                if (response == null || response.Data == null || !response.Data.Any())
                {
                    return Ok(new PagedListResponse<List<OrganizationAgentInputAllocationModel>>
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "No record found",
                        Data = new List<OrganizationAgentInputAllocationModel>(),
                        Count = 0,
                        PageSize = args.PageSize,
                        PageNumber = args.PageNumber
                    });
                }

                return Ok(new PagedListResponse<List<OrganizationAgentInputAllocationModel>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpPost("assign-agent")]
        public async Task<IActionResult> AssignAgentToOrganization([FromBody] OrganizationAgentAssignmArgs model,
          [FromQuery] OrganizationAgentAssignmArgs args)
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

                args.OrganizationCode = args.OrganizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(model.OrganizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                model.LocationId = args.Id;
                model.OnboardedById = CurrentUserId;
                model.BusinessCode = args.BusinessCode;
                await _organizationManager.AssignAgent(model);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful"
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


        #endregion AgentMgt

        #region Membership
        [HttpPost("upload-bulk-membership")]
        [RequestSizeLimit(MaxFileSizeBytes)] // 10 MB cap
        public async Task<IActionResult> BulkMembershipUpload(
       IFormFile file,
       [FromQuery] OnBoardingArgs args)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var onboardedById = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;
            
            using var stream = file.OpenReadStream();
            List<BulkMembershipRowDto> rows;
            try
            {
                rows = _parser.Parse(
                    stream, args.businessCode, onboardedById,
                    args.organizationCode, args.Id);
            }
            catch (Exception ex)
            {
                return BadRequest($"Could not parse file: {ex.Message}");
            }

            if (!rows.Any())
                return BadRequest("The file contains no data rows.");

            // Pre-validate ALL rows — zero DB writes until this passes
            var validationErrors = await _validator.ValidateAllAsync(rows, args);
            if (validationErrors.Any())
            {
                return UnprocessableEntity(new
                {
                    Message = "Validation failed. No records were saved.",
                    Errors = validationErrors
                });
            }

            // Process (chunked transactions)
            var result = await _memberShipUploadService.ProcessBulkMembershipUploadAsync(rows, args);

            return result.FailureCount == 0
                ? Ok(result)
                : StatusCode(207, result); 
        }

        [HttpGet("membership-mgt-overview")]
        public async Task<IActionResult> GetOrganizationMembershipDashboard([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationMembershipDashboard(query);
                return Ok(new Response<OrganizationMembershipDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpPost("create-membership")]

        public async Task<IActionResult> CreateMembershipUser([FromBody] MemberShipRequestModel model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.organizationCode = args.organizationCode ?? OrganizationCode;

                var request = await _organizationManager.CreateMembershipUser(model, args);


                return Ok(new Response<CreateMembershipResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
                    Data = request
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

        [HttpGet("memberships")]
        public async Task<IActionResult> GetCooperativesByOrganization([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var result = await _organizationManager.GetMembershipsByOrganization(query);
                return Ok(new PagedListResponse<List<OrganizationMemberShipResponseModel>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
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

        [HttpGet("membership/farmers")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;

                var currentRole = CurrentUserRole;
                if (string.IsNullOrEmpty(query.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "userId is required");
                }
                var result = await _membershipManager.GetMembershipFarmers(query);
                return Ok(new PagedListResponse<List<FarmerMembershipDto>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
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
        [HttpPost("membership/create-farmer")]
        public async Task<IActionResult> CreateMembershipFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
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
                if (string.IsNullOrEmpty(args.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Membership userId is required");
                }
                args.OnboardedByUserId = CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;


                await EnrichCooperativeAsync(args);


                var request = await _membershipManager.CreateFarmerAsync(model, args);


                return Ok(new Response<CreateMembershipResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
                    Data = request
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

        [HttpGet("membership-detail")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.UserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Membership userId is required");
                }
                var result = await _membershipManager.GetMembershipDashboardAsync(query);
                return Ok(new Response<MembershipDashboardDto>()
                {
                    Data = result,


                    Message = result != null ? "Successful" : "Could not retrieve data",
                    Succeeded = result != null ? true : false,

                    Code = result != null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        #endregion Membership

        #region FarmerMgt

        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkUpload(
            IFormFile file,
            [FromQuery] OnBoardingArgs args)
        {
            if (file is null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only CSV files are accepted.");

            if (string.IsNullOrWhiteSpace(args.businessCode))
                return BadRequest("businessCode is required.");

            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.UserId))
                return Unauthorized("Access Denied");
            args.organizationCode = OrganizationCode;
          

            List<BulkFarmerRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = BulkFarmerCsvParser.Parse(stream);
            }
            catch (Exception ex)
            {
                return BadRequest($"Could not parse CSV: {ex.Message}");
            }

            if (rows.Count == 0)
                return BadRequest("The uploaded file contains no data rows.");

            var uploadedBy = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            var result = await _bulkFarmerUploadService.ProcessAsync(rows, args);

            int statusCode = result.Failed > 0 ? StatusCodes.Status207MultiStatus
                                               : StatusCodes.Status200OK;

            return StatusCode(statusCode, result);
        }


        //[HttpPost("bulk-onboard")]
        //public async Task<IActionResult> BulkOnboard(IFormFile file,[FromQuery] OnBoardingArgs args)
        //{
        //    if (!ModelState.IsValid)
        //    {

        //        return BadRequest(new Response<Dictionary<string, string>>()
        //        {
        //            Message = "Validation failed.",
        //            Succeeded = false,
        //            Data = ModelState
        //                .Where(ms => ms.Value.Errors.Any())
        //                .ToDictionary(
        //                    ms => ms.Key,
        //                    ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))
        //                )
        //        });
        //    }
        //    try
        //    {

        //        args.OnboardedByUserId = CurrentUserId;
        //        args.UserId = CurrentUserId;

        //        args.organizationCode = args.organizationCode ?? OrganizationCode;

        //        args.userType = "Farmer";

        //        if (file == null || file.Length == 0)
        //            return BadRequest("Excel file is required.");


        //        using var stream = file.OpenReadStream();
        //        var response = await _organizationManager.BulkOnboardFarmersBatchAsync(stream, args);

        //        return Ok(new Response<BulkOnboardingQueueResponse>()
        //        {
        //            Code = ResponseCodes.Success,
        //            Succeeded = true,
        //            Message = "Successful",
        //            Data = response
        //        });


        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.ExceptionMessage,
        //            Succeeded = false,

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new Response<string>
        //        {
        //            Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
        //            Succeeded = false,
        //        });
        //    }

        //}
         
       
        
        [HttpGet("farmer/sales-overview")]
        public async Task<IActionResult> GetOrganizationFarmerSalesSummary([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.FarmerUserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Farmer user Id is required");
                }
                var overview = await _organizationManager.GetOrganizationFarmerSalesSummary(query);
                return Ok(new Response<OrganizationFarmerSalesSummary>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpGet("farmer/sales-history")]

        public async Task<IActionResult> FarmerSalesHistory([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.FarmerUserId))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Farmer user Id is required");
                }
                var response = await _organizationManager.FarmerSalesHistory(query, cancellationToken);
                return Ok(new PagedListResponse<List<OrganizationFarmerSalesHistor>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
                    Data = response.Data,
                    Count = response.Count,
                    PageSize = query.PageSize,
                    PageNumber = query.PageNumber
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

        [HttpGet("farmer/input-payment-overview")]
        public async Task<IActionResult> GetFinancingRepaymentSummaryAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _inputFinancingManager.GetFinancingRepaymentSummaryAsync(args);
                return Ok(new Response<FinancingRepaymentSummaryDto>()
                {
                    Code = (response != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = response != null ? "Record Retrieved" : "No Records Found",
                    Data = response
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

        [HttpGet("farmer/input-payment-history")]
        public async Task<IActionResult> GetRepaymentBreakdownAsync([FromQuery] ListQueryParams args, CancellationToken cancellationToken)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var response = await _inputFinancingManager.GetRepaymentBreakdownAsync(args.Id, args);
                return Ok(new PagedListResponse<List<RepaymentBreakdownDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
                    Data = response.Items,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
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

        [HttpGet("farmer/input-financing-detail")]
        public async Task<IActionResult> GetLoanDetailAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
      
                var response = await _inputFinancingManager.GetLoanDetailAsync(args);
                return Ok(new Response<LoanDetailDto>()
                {
                    Code = (response != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = response != null ? "Record Retrieved" : "No Records Found",
                    Data = response
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

        [HttpGet("farmer/input-financing-overview")]

        public async Task<IActionResult> GetOrganizationFarmerSummary([FromQuery] ListQueryParams query)
        {
            try
            {
                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var overview = await _inputFinancingManager.GetInputLoanSummaryAsync(query);
                return Ok(new Response<InputLoanSummaryDto>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = overview != null ? "Record Retrieved" : "No Records Found",
                    Data = overview
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

        [HttpGet("farmer/input-financing-history")]

        public async Task<IActionResult> GetFinancingBreakdownAsync([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;

                var response = await _inputFinancingManager.GetFinancingBreakdownAsync(query);
                return Ok(new PagedListResponse<List<FinancingBreakdownDto>>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response.Count > 0 ? "Record Retrieved" : "No Records Found",
                    Data = response.Items,
                    Count = response.Count,
                    PageSize = query.PageSize,
                    PageNumber = query.PageNumber
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


        //deactivate farmer
        [HttpPut("deactivate-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> DeactivateFarmerFromOrganization([FromQuery] AssignmentArgs args, 
            [FromBody] TaskDescription model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                args.OnboardedByUserId = CurrentUserId;
               
                await EnrichFarmerAsync(args);
                var request = await _organizationManager.DeactivateFarmerFromOrganization(args, model.Message);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request
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
      
        //link cooperative
        [HttpPut("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {
                if (!model.MembershipIds.Any())
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "No Memberhsip selected.");
                }
                args.OnboardedByUserId = CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;

                var request = await _organizationManager.AssignFarmerToCooperatives(args);


                return Ok(new Response<FarmerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request
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
        [HttpPut("remove-farmer-from-cooperative")]
        public async Task<IActionResult> RemoveFarmerFromCooperative([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
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
                if (!model.MembershipIds.Any())
                {
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "No Memberhsip selected.");
                }
                args.OnboardedByUserId = CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;
                var CooperativeIds = args.CooperativeIds
                                    .Where(x => x.HasValue)
                                    .Select(x => x.Value)
                                    .ToList();
                var updateDetail = await _organizationManager.RemoveFarmerFromCooperatives(args.farmerUserId, CooperativeIds);


                return Ok(new Response<bool>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = updateDetail
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

        [HttpPost("create-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
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
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                args.organizationCode = args.organizationCode ?? OrganizationCode;


                var request = await _organizationManager.CreateFarmerAsync(model, args);


                return Ok(new Response<CreateFarmerResponseModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = request.Message,
                    Data = request
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

        [HttpGet("farmer-mgt-overview")]

        public async Task<IActionResult> GetOrganizationFarmerDashboardAsync([FromQuery] ListQueryParams query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }

                var overview = await _organizationManager.GetOrganizationFarmerDashboardAsync(query);
                return Ok(new Response<OrganizationFarmerDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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

        [HttpGet("farmers")]
        public async Task<IActionResult> GetOrganizationFarmersAsync([FromQuery] ListQueryParams query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



                var result = await _organizationManager.GetOrganizationFarmersAsync(query);
                return Ok(new PagedListResponse<List<OrganizationFarmerResponse>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Data.Count > 0,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
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

        [HttpGet("farmer-detail")]
        public async Task<IActionResult> GetFarmerDetailsAsync([FromQuery] ListQueryParams args)
        {
            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode))
                {
                    throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
                }
                if (string.IsNullOrEmpty(args.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
                        {
                            { "UserId", "UserId is required" }
                        }

                    });
                }
                var response = await _organizationManager.GetFarmerDetailsAsync(args);
                return Ok(response);
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

        [HttpPost("farmer-face-capture")]
        public async Task<IActionResult> SaveFarmerImage([FromForm] ProfileImageModel model, [FromQuery] OnBoardingArgs args)
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
               if(string.IsNullOrEmpty(args.UserId))
                {
                    throw new ValidationException("UserId is required");
                }


                args.organizationCode = args.organizationCode ?? OrganizationCode;
                var saveFarmerFace = await _onboardingManager.SaveFarmerImage(model, args);


                return Ok(new Response<object>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = saveFarmerFace
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

        //bulk farmer upload

        //assign agent
        [HttpPut("change-farmer-agents")]
        public async Task<IActionResult> ChangeFarmerAgents(
       [FromQuery] AssignmentArgs args,
        [FromBody] AssignmentArgs model)
        {
            try {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
            args.AgentIds = model.AgentIds;

            if (model == null || model.AgentIds == null || !model.AgentIds.Any())
                return BadRequest("AgentIds are required");

            var result = await _onboardingManager.ChangeFarmerAgents(args, args.Id);

            if (result == null)
                return NotFound("Farmer not found");

            
                return Ok(new Response<FarmerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Farmer agents updated successfully",
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
        [HttpPut("assign-agent-to-farmer")]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.AgentIds = model.AgentIds;
                var request = await _onboardingManager.AssignFarmerToAgents(args);


                return Ok(new Response<FarmerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request
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

        [HttpPut("remove-farmer-from-agent")]
        public async Task<IActionResult> RemoveFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {

            try
            {

                args.organizationCode = args.organizationCode ?? OrganizationCode;
                await EnrichFarmerAsync(args);
                args.UserId = args.farmerUserId;
                args.AgentIds = model.AgentIds;
                var request = await _onboardingManager.RemoveFarmerToAgents(args);


                return Ok(new Response<FarmerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = request
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

        //list farmer, including the agent and cooperative,
        [HttpGet("farmer/farms")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(query.farmerUserId)) throw new BaseException(
                            Core.Enums.ExceptionType.BAD_REQUEST,
                            "Farmer Id is required.");
                var farmer = await _businessManager.GetFarmerByUserId(query.farmerUserId);

                if (farmer == null)
                    throw new BaseException(
                        ExceptionType.NOTFOUND,
                        "Farmer not found for the specified user.");

                query.farmerId = farmer.Id;

                var result = await _farmMappingManager.FetchFarmsByFarmer(query);
                return Ok(new Response<List<FarmListRespopnseObject>>()
                {
                    Data = result,
                    Message = result != null ? "Successful" : "No Record Found",
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("farmer/farm-details")]
        public async Task<IActionResult> FetchFarmDetails([FromQuery] FarmMappingArgs query)
        {
            try
            {

                query.organizationCode = query.organizationCode ?? OrganizationCode;
                var currentRole = CurrentUserRole;
                if (query.farmId <= 0)
                {

                    return BadRequest(new Response<string>
                    {
                        Message = "Farm Id is required.",
                        Succeeded = false
                    });
                }



                var result = await _farmMappingManager.FetchFarmDetails(query);
                return Ok(new Response<FarmDetailsResponse>()
                {
                    Data = result,
                    Message = result != null ? "Successful" : "Farm Details not found",
                    Succeeded = result != null,
                    Code = (result != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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


        #endregion FarmerMgt

        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
            if (string.IsNullOrEmpty(args.organizationCode))
            {
                throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Access Denied");
            }
        }
              
        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.CooperativeIds ??= new List<long?>();

            if (!args.CooperativeIds.Contains(coop.Id))
                args.CooperativeIds.Add(coop.Id);


            args.MembershipIds ??= new List<long?>();

            if (!args.MembershipIds.Contains(coop.Id))
                args.MembershipIds.Add(coop.Id);
        }
        private async Task EnrichAgentAsync(OnBoardingArgs args)
        {
            //  if(CurrentUserRole != args.userType) return;
            if (args.Id != 0) return;

            var agent = await _businessManager.GetAgentDetailByUserId(args.AgentUserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }
        private async Task EnrichFarmerAsync(AssignmentArgs args)
        {
            if (args.Id != 0) return;
            if(string.IsNullOrEmpty(args.farmerUserId)) throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Farmer Id is required.");
            var farmer = await _businessManager.GetFarmerByUserId(args.farmerUserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Farmer not found for the specified user.");

            args.Id = farmer.Id;

        }

    }
}
