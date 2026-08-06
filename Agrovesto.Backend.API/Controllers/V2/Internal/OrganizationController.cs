using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/organization")]
    [ApiController]
    public class OrganizationController : BaseController
    {
       private readonly IBusinessManager _businessManager;
        private readonly IOrganizationManager _organizationManager;
        private readonly IOnBoardingManager _onboardingManager;
        private readonly IMembershipManager _membershipManager;
        private readonly IInputFinancingManager _inputFinancingManager;
        private readonly INomaOSWalletService _walletService;
        private readonly IFarmMappingManager _farmMappingManager;
        private readonly IBulkMembershipExcelParser _parser;
        private readonly IBulkMembershipValidator _validator;
        private readonly IBulkMembershipUploadService _memberShipUploadService;
        private readonly ILogger<OrganizationController> _logger;

         private readonly IBulkFarmerUploadService _bulkFarmerUploadService;
        private readonly DataContext _context;
        public OrganizationController(IHttpContextAccessor httpContextAccessor,
            ILogger<OrganizationController> logger,
            IBusinessManager businessManager,
             INomaOSWalletService walletService,
             IOrganizationManager organizationManager,
             IMembershipManager membershipManager,
             IInputFinancingManager inputFinancingManager,
             IOnBoardingManager onboardingManager,
             IFarmMappingManager farmMappingManager,
             DataContext context,
                IBulkFarmerUploadService bulkFarmerUploadService,
        IBulkMembershipExcelParser parser,
        IBulkMembershipValidator validator,
        IBulkMembershipUploadService memberShipUploadService) : base(httpContextAccessor)
        {
            _context = context;
                _walletService = walletService;
            _farmMappingManager = farmMappingManager;
            _organizationManager = organizationManager;
            _businessManager = businessManager;
            _onboardingManager = onboardingManager;
            _membershipManager = membershipManager;
            _inputFinancingManager = inputFinancingManager;
            _bulkFarmerUploadService = bulkFarmerUploadService;
            _parser = parser;
            _logger = logger;
            _validator = validator;
            _memberShipUploadService = memberShipUploadService;
            _membershipManager = membershipManager;
         }
        #region settings

     
        [HttpPost("create-location")]
        public async Task<IActionResult> Location(
        [FromBody] CreateLocationDto model,
        [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);


                var location = await _organizationManager.CreateLocation(model, args);
                return Ok(Wrap(location, ResponseMessages.Successful));
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
              
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response<string>
                    {
                        Succeeded = false,
                        Message = ex.Message ?? "An unexpected error occurred. Please contact your administrator."
                    });
            }
        }

        [HttpPatch("edit-location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] EditLocationDto model, [FromQuery] ListQueryParams args)
        {

            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                model.Id = args.Id;
                return await _organizationManager.UpdateLocationAsync(model, args);
            },
          type => Ok(Wrap(type)));
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetLocationAsync(args);
            },
        type => Ok(WrapPaged(type, args.PageNumber, args.PageSize)));
        }

        [HttpGet("location-overview")]
        public async Task<IActionResult> LocationOverview([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetFullDashboardAsync(args);
            },
         type => Ok(Wrap(type)));
        }

        [HttpGet("sync-stock")]
        public async Task<IActionResult> SyncLocationStockLevels([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                await _organizationManager.SyncLocationStockLevels(args);
                return "Stock levels synchronized successfully";
            },
        message => Ok(Wrap(message)));
        }

        [HttpGet("location")]
        public async Task<IActionResult> GetLocation([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
            () =>
            {
              
                return _organizationManager.GetLocationById(query);
            },
            type => Ok(Wrap(type)));
        }
        [HttpDelete("delete-location")]
        public async Task<IActionResult> DeleteLocation([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.DeleteLocation(model, args);
            },
            type => Ok(Wrap(type)));
        }

        [HttpPatch("edit-sublocation")]
        public async Task<IActionResult> EditSubLocationAsync([FromBody] EditSubLocationDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.EditSubLocationAsync(model, args);
            },
            type => Ok(Wrap(type)));
        }

        [HttpPost("create-sublocation")]
        public async Task<IActionResult> CreateSubLocationAsync(
        [FromBody] CreateSubLocationDto model,
        [FromQuery] ListQueryParams args)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode))
                return Unauthorized(ApiResponse<object>.Fail(ResponseMessages.AccessDenied, HttpContext.TraceIdentifier));

            var result = await _organizationManager.CreateSubLocationAsync(model, args);
            return Ok(ApiResponse<OrganizationLocationModel>.Success(result, ResponseMessages.Successful, HttpContext.TraceIdentifier));

        }

        [HttpGet("sublocations/by-parent")]
        public async Task<IActionResult> GetSubLocationsByParentAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetSubLocationsByParentAsync(args.Id, args);
            },
              type => Ok(WrapPaged(type, args.PageNumber, args.PageSize)));
        }
        [HttpPut("reassign-primary-agent")]
        public async Task<IActionResult> ReassignPrimaryAgentAsync([FromQuery] ListQueryParams args, [FromBody] ReassignPrimaryAgentDto model)
        {
            return await ExecuteAsync(() =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                model.LocationId = args.Id;
                model.PerformedByUserId = CurrentUserId;
                return _organizationManager.ReassignPrimaryAgentAsync(model);
            },
        type => Ok(Wrap(type)));
        }

        [HttpPut("remove-agent-from-location")]
        public async Task<IActionResult> RemoveAgentFromLocationAsync([FromQuery] ListQueryParams args, [FromBody] RemoveAgentFromLocationDto model)
        {
            return await ExecuteAsync(() =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                model.LocationId = args.Id;
                model.RemovedByUserId = CurrentUserId;
                return _organizationManager.RemoveAgentFromLocationAsync(model);
            },
        type => Ok(Wrap(type)));
        }
        [HttpGet("location-agents")]
        public async Task<IActionResult> GetLocationAgents([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetLocationAgentsAsync(args);
            },
        type => Ok(Wrap(type)));
        }
        [HttpGet("locations-by-agent")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetLocationsByAgentAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                args.UserId = CurrentUserId;
                return await _organizationManager.GetLocationsByAgentAsync(args);
            },
       type => Ok(Wrap(type)));
        }

        [HttpGet("sublocations")]
        public async Task<IActionResult> GetSubLocationsAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetSubLocationsAsync(args);
            },
        type => Ok(WrapPaged(type, args.PageNumber, args.PageSize)));
        }

        [HttpGet("sublocation")]
        public async Task<IActionResult> GetSubLocationByIdAsync([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
             () =>
             {
                 
                 return _organizationManager.GetSubLocationByIdAsync(query);
             },
             type => Ok(Wrap(type)));
        }

        [HttpDelete("delete-sublocation")]
        public async Task<IActionResult> DeleteSubLocationAsync([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.DeleteSubLocationAsync(model, args);
            },
            type => Ok(Wrap(type)));
        }

        [HttpPost("add-inventory")]
        public async Task<IActionResult> AddInventoryAsync([FromBody] CreateInventoryDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.UserId = CurrentUserId;
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.AddInventoryAsync(model, args);
            },
            _ => Ok(Wrap(true)));
        }
        [HttpDelete("delete-inventory")]
        public async Task<IActionResult> DeleteInventory([FromBody] DeleteLocationDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.UserId = CurrentUserId;
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                await _organizationManager.DeleteInventory(model, args);
                return true;
            },
            _ => Ok(Wrap(true)));
        }
        [HttpPatch("update-inventory")]
        public async Task<IActionResult> EditInventoryAsync([FromBody] EditInventoryDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.UserId = CurrentUserId;
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.EditInventoryAsync(model, args);
            },
            type => Ok(new Response<LocationInventory> { Code = type != null ? ResponseCodes.Success : ResponseCodes.Failed, Succeeded = true, Message = "Successful" }));
        }

        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetInventoryByLocationAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetInventoryByLocationAsync(args);
            },
       response => Ok(WrapPaged(response, args.PageNumber, args.PageSize, response != null ? "Successful" : "Failed")));

        }

        [HttpGet("location-activity")]
        public async Task<IActionResult> GetLocationActivityLogsAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetLocationActivityLogsAsync(args);
            },
       response => Ok(WrapPaged(response, args.PageNumber, args.PageSize, response != null ? "Successful" : "Failed")));

        }

        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] OrganizationRoleModel model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.CreateRole(model, args);
            },
            type => Ok(Wrap(type)));
        }
        [HttpGet("roles")]
        public async Task<IActionResult> GetRolesAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetRolesAsync(args);
            },
        type => Ok(Wrap(type)));
        }

        [HttpGet("role")]
        public async Task<IActionResult> GetRoleById([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
            () =>
            {
                return _organizationManager.GetRoleById(query);
            },
            type => Ok(Wrap(type)));
        }

        #endregion settings

        #region OrganizationWallet
        [HttpPost("create-wallet")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            (bool flowControl, IActionResult value) = ParameterValidator(request, query);
            if (!flowControl) return value;
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(() =>
            {
              
                return _walletService.CreateWallet(request, query);
            },
            wallet => Ok(new Response<CreateWalletResult>
            {
                Data = wallet.data,
                Message = wallet.message,
                Succeeded = wallet.status,
                Code = wallet.status ? ResponseCodes.Success : ResponseCodes.Failed
            }));
        }

        [HttpGet("wallets")]
        public async Task<IActionResult> GetWallets([FromQuery] UserWalletParams query)
        {
            return await ExecuteAsync(() =>
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                return _walletService.GetUserWallletsByUserId(query);
            },
                    result => Ok(new PagedListResponse<List<UserWalletResponseModel>>
                    {
                        Data = result.Data,
                        Message = result.Count > 0 ? ResponseMessages.Successful : ResponseMessages.Failed,
                        Succeeded = true,
                        Count = result.Count,
                        Code = result.Count > 0 ? ResponseCodes.Success : ResponseCodes.Failed
                    }));
        }

        [HttpPost("wallet-topup")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            return await ExecuteAsync(() =>
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                return _walletService.TopUpWallet(request, args);
            },
        response => Ok(Wrap(response)));
        }

        [HttpPost("wallet-withdraw")]
        public async Task<IActionResult> WithdrawWallet([FromBody] WithdrawWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            return await ExecuteAsync(() =>
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                return _walletService.WithdrawWallet(request, args);
            },
            response => Ok(Wrap(response)));
        }

        private (bool flowControl, IActionResult value) ParameterValidator(CreateWalletRequest request, WalletQueryParams query)
        {
            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(query.env))
                return (false, BadRequest("Environment is required"));

            if (string.IsNullOrWhiteSpace(request.WalletProviderCode))
                return (false, BadRequest("WalletProviderCode is required"));

            if (string.IsNullOrWhiteSpace(query.businessCode))
                return (false, BadRequest("BusinessCode is required"));

            if (RequireField(query.UserId, "UserId") is { } error)
                return (false, error);

            return (true, null);
        }
        #endregion OrganizationWallet

        #region OrganizationProduct
        [HttpPost("create-product")]
        public async Task<IActionResult> CreateOrganizationProduct([FromBody] OrganizationProductDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.UserId = CurrentUserId;
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.CreateOrganizationProduct(model, args);
            },
            _ => Ok(Wrap(true)));
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetOrganizationProductList([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(async () =>
            {
                args.organizationCode = RequireOrganizationCode(args.organizationCode);
                return await _organizationManager.GetOrganizationProductList(args);
            },
          response => Ok(WrapPaged(response, args.PageNumber, args.PageSize, response != null ? "Successful" : "Failed")));
        }

        [HttpGet("product")]
        public async Task<IActionResult> GetorganizationProductById([FromQuery] ListQueryParams args)
        {
            args.organizationCode = RequireOrganizationCode(args.organizationCode);

            if (args.Id <= 0)
                return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

            return await ExecuteAsync(
                () => _organizationManager.GetorganizationProductById(args),
                response => Ok(new Response<OrganizationProductDto>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = response != null,
                    Message = response != null ? "Successful" : "No record found",
                    Data = response
                }));
        }

        [HttpPatch("edit-product")]
        public async Task<IActionResult> EditProductAsync([FromBody] EditOrganizationProductDto model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            args.organizationCode = RequireOrganizationCode(args.organizationCode);

            if (args.Id <= 0)
                return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

            return await ExecuteAsync(
                () => _organizationManager.EditProductAsync(args, model),
                result => Ok(new Response<bool> { Code = ResponseCodes.Success, Succeeded = result, Message = result ? "Successful" : "Failed", Data = result }));
        }

        [HttpPut("deactivate-product")]
        public async Task<IActionResult> DeactivateProductAsync([FromQuery] ListQueryParams args)
        {
            args.organizationCode = RequireOrganizationCode(args.organizationCode);

            if (args.Id <= 0)
                return BadRequest(new Response<string> { Message = "Product Id is required", Succeeded = false });

            return await ExecuteAsync(
                () => _organizationManager.DeactivateProductAsync(args),
                result => Ok(new Response<bool> { Code = ResponseCodes.Success, Succeeded = result, Message = result ? "Successful" : "Failed", Data = result }));
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
                    Message = ResponseMessages.Successful,
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
            return await ExecuteAsync(async () =>
            {
                query.organizationCode = RequireOrganizationCode(query.organizationCode);
                return await _organizationManager.GetOrganizationDashboardAsync(query);
            },
        overview => Ok(Wrap(overview)));
        }
        [HttpGet("agents")]
        public async Task<IActionResult> GetOrganizationAgents([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(() =>
            {   query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                return _organizationManager.GetOrganizationAgents(query);
            },
            result => Ok(BuildPagedResponse(result, query.PageNumber, query.PageSize)));

        }

        //create-agent
        [HttpPost("create-agent")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {

            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                return await _organizationManager.CreateAgentUser(model, args);
            },
            request => Ok(Success(request, request.Message)));
        }
        [HttpPost("edit-agent")]
        [AllowAnonymous]
        public async Task<IActionResult> EditAgentUser([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
                () =>
                {
                    args.OnboardedByUserId = CurrentUserId;
                    return _organizationManager.EditAgentUser(model, args);
                },
                request => Ok(Success(request, ResponseMessages.Successful)));
        }


        [HttpGet("agent-detail")]
        public async Task<IActionResult> GetAgentDetail([FromQuery] ListQueryParams args)
        {
            args.organizationCode = RequireOrganizationCode(args.organizationCode);
            if (RequireField(args.UserId, "UserId") is { } error) return error;

            return await ExecuteAsync(
                () => _organizationManager.GetAgentDetail(args),
                response => Ok(response)); 
            
        }
        [HttpGet("agent-locations")]
        public async Task<IActionResult> GetAgentLocationAssignment([FromQuery] ListQueryParams args)
        {
            args.organizationCode = RequireOrganizationCode(args.organizationCode);
            if (RequireField(args.UserId, "UserId") is { } error) return error;

            return await ExecuteAsync(
                () => _organizationManager.GetAgentAssignedLocations(args),
                response => Ok(new PagedListResponse<List<OrganizationAgentLocationResponseModel>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = response != null ?ResponseMessages.Successful : ResponseMessages.Failed,
                    Data = response,
                    Count = response.Count,
                    PageSize = args.PageSize,
                    PageNumber = args.PageNumber
                }));
        }


        [HttpPost("change-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeAgentLocation([FromBody] ProfileRequestModel model,
            [FromQuery] OrganizationAgentArgs args)
        {

            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            return await ExecuteAsync(async () =>
            {
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                args.OrganizationCode = RequireOrganizationCode(args.OrganizationCode);
                return await _organizationManager.ReassignAgentToLocation(model, args);
            },
            request => Ok(new Response<bool> { Code = ResponseCodes.Success, Succeeded = true, Message = request ? ResponseMessages.Successful : ResponseMessages.Failed, Data = request }));

        }


        [HttpPost("assign-agent-location")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignAgentLocation([FromBody] LocationAgentAssignment  model,
           [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
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
                return ValidationProblemResponse();

            try
            {
                args.OnboardedById = CurrentUserId;
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                var orgError = TryResolveOrganizationCode(args.OrganizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.OrganizationCode = resolvedOrgCode; if (string.IsNullOrEmpty(args.OrganizationCode))
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
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
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
               

                return Ok(new PagedListResponse<List<OrganizationAgentActivityLog>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
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
              

                return Ok(new PagedListResponse<List<OrganizationAgentTransactionHistory>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;

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


                return Ok(new Response<OrganizationAgentPerformanceSummary>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
                
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
              

                return Ok(new PagedListResponse<List<OrganizationAgentSubscriberModel>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode; 
                
                
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


                return Ok(new PagedListResponse<List<OrganizationAgentInputAllocationModel>>
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
                return ValidationProblemResponse();

            try
            {

                var orgError = TryResolveOrganizationCode(args.OrganizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.OrganizationCode = resolvedOrgCode;
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
            return await HandleBulkMembershipUploadAsync(file, args, _parser, _validator, _memberShipUploadService, _logger);

        }

        [HttpGet("membership-mgt-overview")]
        public async Task<IActionResult> GetOrganizationMembershipDashboard([FromQuery] ListQueryParams query)
        {
            return await ExecuteAsync(async () =>
            {
                query.organizationCode = RequireOrganizationCode(query.organizationCode);
                return await _organizationManager.GetOrganizationMembershipDashboard(query);
            },
                    overview => Ok(Wrap(overview)));
        }

        [HttpPost("create-membership")]

        public async Task<IActionResult> CreateMembershipUser([FromBody] MemberShipRequestModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
                () =>
                {
                    args.OnboardedByUserId = CurrentUserId;
                    return _organizationManager.CreateMembershipUser(model, args);
                },
                request => Ok(Success(request, request.Message)));
        }

        [HttpGet("memberships")]
        public async Task<IActionResult> GetCooperativesByOrganization([FromQuery] ListQueryParams query)
        {
            return await ExecuteAsync(async () =>
            {
                query.organizationCode = RequireOrganizationCode(query.organizationCode);
                return await _organizationManager.GetMembershipsByOrganization(query);
            },
                 result => Ok(BuildPagedResponse(result, query.PageNumber, 10)));
        }

        [HttpGet("membership/farmers")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            if (RequireField(query.UserId, "UserId") is { } error) return error;

            return await ExecuteAsync(
                () => _membershipManager.GetMembershipFarmers(query),
                result => Ok(BuildPagedResponse(result, query.PageNumber, 10)));

        }
        [HttpPost("membership/create-farmer")]
        public async Task<IActionResult> CreateMembershipFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            if (RequireField(args.UserId, "UserId") is { } error) return error;
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
                await EnrichCooperativeAsync(args);
                return await _membershipManager.CreateFarmerAsync(model, args);
            },
            request => Ok(Success(request, request.Message)));
        }

        [HttpGet("membership-detail")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            if (RequireField(query.UserId, "UserId") is { } error) return error;

            return await ExecuteAsync(
                () =>
                {
                    return _membershipManager.GetMembershipDetailAsync(query);
                },
                result => Ok(Wrap(result, ResponseMessages.Successful, ResponseMessages.NotFound)));

        }

        #endregion Membership

        #region FarmerMgt

        [HttpPost("bulk-onboard")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> BulkUpload(
        IFormFile file,
        [FromQuery] OnBoardingArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.businessCode))
                return Error("BUSINESS_CODE_REQUIRED", "A business code is required to upload farmers.");

            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.UserId))
                return Error("UNAUTHORIZED", "Your session has expired. Please sign in again and retry.", StatusCodes.Status401Unauthorized);

            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await HandleBulkFarmerUploadAsync(file, args, _bulkFarmerUploadService, _logger);

        }
        
        [HttpGet("farmer/sales-overview")]
        public async Task<IActionResult> GetOrganizationFarmerSalesSummary([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            if (RequireField(query.FarmerUserId, "FarmerUserId") is { } error) return error;

            return await ExecuteAsync(
                () => _organizationManager.GetOrganizationFarmerSalesSummary(query),
                overview => Ok(Wrap(overview)));
        }

        [HttpGet("farmer/sales-history")]

        public async Task<IActionResult> FarmerSalesHistory([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            if (RequireField(query.FarmerUserId, "FarmerUserId") is { } error) return error;

            return await ExecuteAsync(
                () => _organizationManager.FarmerSalesHistory(query, cancellationToken),
                response => Ok(WrapPaged(response, query.PageNumber, query.PageSize, response.Count > 0 ? "Record Retrieved" : "No Records Found")));

        }

        [HttpGet("farmer/input-payment-overview")]
        public async Task<IActionResult> GetFinancingRepaymentSummaryAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(
           () =>
           {
               args.organizationCode = RequireOrganizationCode(args.organizationCode);
               return _inputFinancingManager.GetFinancingRepaymentSummaryAsync(args);
           },
           response => Ok(Wrap(response, "Record Retrieved", "No Records Found")));

        }

        [HttpGet("farmer/input-payment-history")]
        public async Task<IActionResult> GetRepaymentBreakdownAsync([FromQuery] ListQueryParams args, CancellationToken cancellationToken)
        {
            try
            {
                var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                args.organizationCode = resolvedOrgCode;
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

        [HttpGet("farmer/input-financing-detail")]
        public async Task<IActionResult> GetLoanDetailAsync([FromQuery] ListQueryParams args)
        {
            return await ExecuteAsync(
           () =>
           {
               args.organizationCode = RequireOrganizationCode(args.organizationCode);
               return _inputFinancingManager.GetLoanDetailAsync(args);
           },
           response => Ok(Wrap(response, "Record Retrieved", "No Records Found")));

        }

        [HttpGet("farmer/input-financing-overview")]

        public async Task<IActionResult> GetOrganizationFarmerSummary([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
            () =>
            {
                return _inputFinancingManager.GetInputLoanSummaryAsync(query);
            },
            overview => Ok(Wrap(overview, "Record Retrieved", "No Records Found")));

        }

        [HttpGet("farmer/input-financing-history")]

        public async Task<IActionResult> GetFinancingBreakdownAsync([FromQuery] ListQueryParams query, CancellationToken cancellationToken)
        {
            try
            {

                var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
                if (orgError != null) return orgError;
                query.organizationCode = resolvedOrgCode;


                var response = await _inputFinancingManager.GetFinancingBreakdownAsync(query);
                return Ok(new PagedListResponse<List<FinancingBreakdownDto>>()
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


        //deactivate farmer
        [HttpPut("deactivate-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> DeactivateFarmerFromOrganization([FromQuery] AssignmentArgs args, 
            [FromBody] TaskDescription model)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                args.OnboardedByUserId = CurrentUserId;
                await EnrichFarmerAsync(args);
                return await _organizationManager.DeactivateFarmerFromOrganization(args, model.Message);
            },
            request => Ok(Success(request, ResponseMessages.Successful)));

        }

        //link cooperative
        [HttpPut("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                if (!model.MembershipIds.Any())
                    throw new BaseException(ExceptionType.BAD_REQUEST, "No Memberhsip selected.");

                args.OnboardedByUserId = CurrentUserId;
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;
                return await _organizationManager.AssignFarmerToCooperatives(args);
            },
                    request => Ok(Success(request, ResponseMessages.Successful)));
        }
        [HttpPut("remove-farmer-from-cooperative")]
        public async Task<IActionResult> RemoveFarmerFromCooperative([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                if (!model.MembershipIds.Any())
                    throw new BaseException(ExceptionType.BAD_REQUEST, "No Memberhsip selected.");

                args.OnboardedByUserId = CurrentUserId;
                await EnrichFarmerAsync(args);
                args.CooperativeIds = model.MembershipIds;

                var cooperativeIds = args.CooperativeIds.Where(x => x.HasValue).Select(x => x.Value).ToList();
                return await _organizationManager.RemoveFarmerFromCooperatives(args.farmerUserId, cooperativeIds, args.organizationCode);
            },
            updateDetail => Ok(Success(updateDetail, ResponseMessages.Successful)));

        }

        [HttpPost("create-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFarmer([FromBody] CreateFarmerRequest model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
                () =>
                {
                    args.OnboardedByUserId = CurrentUserId;
                    args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
         
                    return _organizationManager.CreateFarmerAsync(model, args);
                },
                request => Ok(Success(request, request.Message)));
        }

        [HttpGet("farmer-mgt-overview")]

        public async Task<IActionResult> GetOrganizationFarmerDashboardAsync([FromQuery] ListQueryParams query)
        {
            return await ExecuteAsync(async () =>
            {
                query.organizationCode = RequireOrganizationCode(query.organizationCode);
                return await _organizationManager.GetOrganizationFarmerDashboardAsync(query);
            },
        overview => Ok(Wrap(overview)));
        }

        [HttpGet("farmers")]
        public async Task<IActionResult> GetOrganizationFarmersAsync([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(() =>
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                return _organizationManager.GetOrganizationFarmersAsync(query);
            },
        result => Ok(new PagedListResponse<List<OrganizationFarmerResponse>>
        {
            Data = result.Data,
            Message = result.Message,
            Succeeded = true,
            Count = result.Count,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Code = (result.Succeeded && result.Data != null) ? ResponseCodes.Success : ResponseCodes.Failed
        }));

        }

        [HttpGet("farmer-detail")]
        public async Task<IActionResult> GetFarmerDetailsAsync([FromQuery] ListQueryParams args)
        {
            args.organizationCode = RequireOrganizationCode(args.organizationCode);
            if (RequireField(args.UserId, "UserId") is { } error) return error;

            return await ExecuteAsync(
                () => _organizationManager.GetFarmerDetailsAsync(args),
                response => Ok(response)); 
            
        }

        [HttpPost("farmer-face-capture")]
        public async Task<IActionResult> SaveFarmerImage([FromForm] ProfileImageModel model, [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            if (string.IsNullOrEmpty(args.UserId))
                return BadRequest(new Response<string> { Message = "UserId is required", Succeeded = false });
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
                () =>
                {
                    return _onboardingManager.SaveFarmerImage(model, args);
                },
                result => Ok(Success(result, ResponseMessages.Successful)));

        }

        //bulk farmer upload

        //assign agent
        [HttpPut("change-farmer-agents")]
        public async Task<IActionResult> ChangeFarmerAgents(
        [FromQuery] AssignmentArgs args,
        [FromBody] AssignmentArgs model)
        {
            if (model?.AgentIds == null || !model.AgentIds.Any())
                return BadRequest("AgentIds are required");
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                await EnrichFarmerAsync(args);
                args.AgentIds = model.AgentIds;
                return await _onboardingManager.ChangeFarmerAgents(args, args.Id);
            },
            result => result == null
                ? NotFound("Farmer not found")
                : Ok(new Response<FarmerModel> { Code = ResponseCodes.Success, Succeeded = true, Message = "Farmer agents updated successfully", Data = result }));

        }
        [HttpPut("assign-agent-to-farmer")]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                await EnrichFarmerAsync(args);
                args.AgentIds = model.AgentIds;
                return await _onboardingManager.AssignFarmerToAgents(args);
            },
        request => Ok(Success(request, ResponseMessages.Successful)));

        }

        [HttpPut("remove-farmer-from-agent")]
        public async Task<IActionResult> RemoveFarmerToAgents([FromQuery] AssignmentArgs args, [FromBody] AssignmentArgs model)
        {
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                await EnrichFarmerAsync(args);
                args.UserId = args.farmerUserId;
                args.AgentIds = model.AgentIds;
                return await _onboardingManager.RemoveFarmerToAgents(args);
            },
                    request => Ok(Success(request, ResponseMessages.Successful)));
        }

        //list farmer, including the agent and cooperative,
        [HttpGet("farmer/farms")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                
                if (string.IsNullOrEmpty(query.farmerUserId))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Farmer Id is required.");

                var farmer = await _businessManager.GetFarmerByUserId(query.farmerUserId);
                if (farmer == null)
                    throw new BaseException(ExceptionType.NOTFOUND, "Farmer not found for the specified user.");

                query.farmerId = farmer.Id;
                return await _farmMappingManager.FetchFarmsByFarmer(query);
            },
                    result => Ok(Wrap(result)));
        }

        [HttpGet("farmer/farm-details")]
        public async Task<IActionResult> FetchFarmDetails([FromQuery] FarmMappingArgs query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            if (query.farmId <= 0)
                return BadRequest(new Response<string> { Message = "Farm Id is required.", Succeeded = false });

            return await ExecuteAsync(
                () => _farmMappingManager.FetchFarmDetails(query),
                result => Ok(Wrap(result)));
        }


        #endregion FarmerMgt
   
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
