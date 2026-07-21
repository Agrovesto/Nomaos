using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/input-financing")]
    [ApiController]
    public class InputFinancingController : BaseController
    {
        private readonly IInputFinancingManager _inputFinancingManager;
        private readonly INomaOSWalletService _nomaOSWalletService;
        public InputFinancingController(IHttpContextAccessor httpContextAccessor,
            INomaOSWalletService nomaOSWalletService,
             IInputFinancingManager inputFinancingManager) : base(httpContextAccessor)
        {

            _inputFinancingManager = inputFinancingManager;
            _nomaOSWalletService= nomaOSWalletService;
        }

        #region InputFinancing

        [HttpPost("eop")]
        public async Task<IActionResult> CreateEopAsync([FromBody] CreateEopRequest model, [FromQuery] ListQueryParams args)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Response<string> { Succeeded = false, Message = "Validation failed" });

            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");

                var result = await _inputFinancingManager.CreateEopAsync(model, args);
                return Ok(new Response<EopResponseDto> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }
     
        [HttpGet("eop")]
        public async Task<IActionResult> GetEopByIdAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");
                if (args.Id <= 0) return BadRequest(new Response<string> { Message = "EOP Id is required", Succeeded = false });

                var result = await _inputFinancingManager.GetEopByIdAsync(args);
                return Ok(new Response<EopDto> { 
                    Succeeded = result != null, 
                    Data = result, Code = ResponseCodes.Success,

                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

        [HttpGet("eops")]
        public async Task<IActionResult> GetEopsAsync([FromQuery] EopFilterRequest filter, [FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                var result = await _inputFinancingManager.GetEopsAsync(filter, args);
                return Ok(new Response<PagedResult<EopDto>> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
            }
        }

       
        [HttpPost("create-package")]
        public async Task<IActionResult> CreateManualInputPackageAsync(
            [FromBody] CreateInputPackageRequest model,
            [FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode) || string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .CreateInputFinancingPackageConfig(model, args);

            return Ok(ApiResponse<string>.Success(
                 ResponseMessages.Successful, ResponseCodes.Success
            ));
        }
        [HttpPatch("update-package")]
        public async Task<IActionResult> UpdateInputPackageConfig(
            [FromBody] UpdateInputPackageConfigRequest model,
            [FromQuery] ReplacePackageItemRequestArgs args,
            [FromQuery] InputPackageConfigArgs query)
        {
            query.OrganizationCode ??= OrganizationCode;
            query.createdByUserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(query.OrganizationCode) || string.IsNullOrWhiteSpace(query.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager
                .UpdateInputPackageConfig(args.PackageId, model, query);

            return Ok(ApiResponse<string>.Success(
                ResponseMessages.Successful, ResponseCodes.Success
            ));
        }
        [HttpGet("packages")]
        public async Task<IActionResult> GetPackagesAsync([FromQuery] InputPackageFilterRequest filter, [FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode) || string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
              .GetPackagesAsync(args);

            return Ok(ApiResponse<List<InputPackageListModel>>.SuccessPaged(
                    result,
                    ResponseMessages.Successful,
                    ResponseCodes.Success
            ));
        }

        [HttpGet("package")]
        public async Task<IActionResult> GetPackageByIdAsync([FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode) || string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            if (args.Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Package Id is required",
                    ResponseCodes.BadRequest,
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.GetPackageByIdAsync(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    ResponseMessages.NotFound,
                    ResponseCodes.NotFound,
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<InputPackageDto>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }

        [HttpPatch("change-package-input")]
        public async Task<IActionResult> ReplacePackageItemAsync([FromBody] ReplacePackageItemRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager.ReplacePackageItemAsync(request,args);


            return Ok(ApiResponse<bool>.Success(
                result,
               ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
        [HttpPost("add-package-input")]
        public async Task<IActionResult> AddItemToPackageAsync([FromBody] AddPackageItemRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager.AddItemToPackageAsync(request,args);

            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }

        [HttpDelete("remove-package-input")]
        public async Task<IActionResult> RemoveItemFromPackage([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager.RemoveItemFromPackage(args);


            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
       
       
        [HttpDelete("deactivate-package")]
        public async Task<IActionResult> DeactivatePackage([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager.DeactivateInputPackage(args);

            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
        [HttpPatch("reactivate-package")]
        public async Task<IActionResult> ReactivatePackage( [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager.ReactivatePackageAsync(args);
            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
     
        [HttpPost("package/add-additional-cost")]
        public async Task<IActionResult> AddAdditionalCostToPackageAsync([FromBody] AddAdditionalCostRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager.AddAdditionalCostToPackageAsync(request, args);
            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
        [HttpPatch("package/update-additional-cost")]
        public async Task<IActionResult> UpdateAdditionalCostAsync([FromBody] UpdateAdditionalCostRequest request, [FromQuery] UpdateAdditionalCostArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager.UpdateAdditionalCostAsync(request, args);

            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
        [HttpDelete("package/remove-additional-cost")]
        public async Task<IActionResult> RemoveAdditionalCostFromPackageAsync([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            var result = await _inputFinancingManager.RemoveAdditionalCostFromPackageAsync(args);

            return Ok(ApiResponse<bool>.Success(
                result,
                ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }

        [HttpPost("package/link-wallet")]
        public async Task<IActionResult> UpdateOrganizationInputPackage(
         [FromQuery] WalletQueryParams filter,
         [FromBody] UserWalletModel request)
        {
            filter.organizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.organizationCode) || string.IsNullOrWhiteSpace(filter.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            await _nomaOSWalletService
                .UpdateOrganizationInputPackage(filter, request.WalletProviderId, request);


            return Ok(ApiResponse<string>.Success(
                 ResponseMessages.Successful,                
                 ResponseCodes.Success
                 ));
        }


        #endregion InputFinancing


        #region subscriptions
        [HttpPost("add-subscriber")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> AddSubscriberAsync(
        [FromBody] AddSubscriberRequest model,
        [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .AddSubscriberAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }         

        [HttpPost("approve-subscriber")]
        public async Task<IActionResult> ApproveSubscriptionAsync(
            [FromBody] ApproveSubscriptionRequest model,
            [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .ApproveSubscriptionAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseMessages.Successful,
                 ResponseCodes.Success
            ));
        }
          
        
        [HttpGet("get-subscribers")]

        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetSubscriptionsAsync(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                    !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                filter.CurrentLoggedInUserRole = UserType.Agent.ToString();
            }
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .GetSubscriptionsAsync(filter, filter.PageNumber, filter.PageSize);

          
            return Ok(ApiResponse<List<SubscriptionDetailDto>>.SuccessPaged(
    result,
    ResponseCodes.Success,
    ResponseMessages.Successful,
    HttpContext.TraceIdentifier
));
        }
       
    

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetInputFinancingDashboardAsync(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .GetInputFinancingDashboardAsync(filter);

            return Ok(ApiResponse<InputFinancingDashboardDto>.Success(
                result,
               ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }

        [HttpGet("agent-dashboard")]
        public async Task<IActionResult> GetAgentSubscriptionOverview(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .GetAgentSubscriptionOverview(filter);

            return Ok(ApiResponse<AgentSubscriptionOverviewDto>.Success(
                result,
               ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }
        
        [HttpGet("export-subscription")]
        public async Task<IActionResult> ExportSubscriptions(
            [FromQuery] SubscriptionQueryFilter filter, [FromQuery] ExportFormat format)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                    !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                filter.CurrentLoggedInUserRole = UserType.Agent.ToString();
            }
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }


            var fileBytes = await _inputFinancingManager.ExportSubscriptionsAsync(filter, format);

            var fileName = GenerateExportFileName(filter, format);

            var contentType = format switch
            {
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.Pdf => "application/pdf",
                _ => "application/octet-stream"
            };

            Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

            return File(fileBytes, contentType);
        }
        
        [HttpGet("review-subscription")]
        public async Task<IActionResult> ReviewSubscription(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .FetchSubscriptionForReview(filter);


            return Ok(ApiResponse<SubscriptionFullDetailDto>.Success(
                 result,
                 ResponseCodes.Success,
                 ResponseMessages.Successful
                 ));
        }
       
        [HttpGet("subscription-detail")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetSubscriptionDetail(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .GetSubscriptionDetail(filter);


            return Ok(ApiResponse<SubscriptionReviewResponseDto>.Success(
                 result,
                 ResponseCodes.Success,
                 ResponseMessages.Successful
                 ));
        }

        [HttpGet("subscription/documents")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetSubscriptionDocuments(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                    !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                filter.CurrentLoggedInUserRole = UserType.Agent.ToString();
            }
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .GetDocuments(filter.SubscriptionId);

            return Ok(ApiResponse<List<Core.Models.Businesses.Input.SubscriptionDocument>>.Success(
                    result,
                    ResponseCodes.Success,
                    ResponseMessages.Successful
                    ));    
        }


        [HttpGet("subscription/commission-letter")]
        public async Task<IActionResult> DownloadCommissionLetter([FromQuery]long subscriptionId)
        {
            
            var result =
                await _inputFinancingManager.DownloadCommissionLetterAsync(subscriptionId);

            return File(result.FileBytes, "application/pdf", result.FileName);
        }
       
        [HttpGet("subscription/commitment/payment-receipt")]
        public async Task<IActionResult> DownloadPaymentReceipt([FromQuery]long subscriptionId)
        {
           
            string role="";
            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                   !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                role = UserType.Agent.ToString();
            }

            var result =
                await _inputFinancingManager.GetPaymentReceiptAsync(subscriptionId, role);

            return File(result.FileContent, "application/pdf", result.FileName);
        }
       
        [HttpGet("subscription/commitment-form")]
        public async Task<IActionResult> DownloadCommitmentLetter([FromQuery] SubscriptionQueryFilter query)
        {
            if (string.IsNullOrWhiteSpace(query.OrganizationCode) || string.IsNullOrWhiteSpace(query.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result =
                await _inputFinancingManager.DownloadCommitmentLetter(query);

            return File(result.FileContent, "application/pdf", result.FileName);
        }
        //upload commission letter
        [HttpPost("subscription/upload-commitment-form")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadSignedCommitmentForm(
            IFormFile file,
            [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .UploadCommissionLetterAsync(args.SubscriptionId, args.UserId, file);

            return Ok(ApiResponse<bool>.Success(
                true,
                ResponseCodes.Success,
                ResponseMessages.Successful

            ));
        }


        //escalate default
        //repay financing
        //pay commitment fee
        [HttpPost("subscription/commitment/initiate-payment")]
        public async Task<IActionResult> SendCommitmentReminderAsync(
           [FromQuery] InputSubscriptionRequestArgs filter,
           [FromBody] RecordCommitmentPaymentRequest request)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.Env))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    $"Invalid request {nameof(filter.Env)}",
                    HttpContext.TraceIdentifier
                ));
            }
            await _inputFinancingManager
                .CommitmentPaymentAsync(request, filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processing",
                 ResponseCodes.Success,
                 ResponseMessages.Successful
                 ));
        }

        //request input release
        [HttpPut("request-release-input")]
        public async Task<IActionResult> RequestInputDisbursementApprovalAsync(
            [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;
            if (string.IsNullOrEmpty(args.SubscriberUserId))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Subscriber user Id is required",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .RequestInputDisbursementApprovalAsync(args);

            return Ok(ApiResponse<bool>.Success(
                true,
               ResponseCodes.Success,
               ResponseMessages.Successful
            ));
        }

        //release input
        [HttpPost("release-input")]
        public async Task<IActionResult> ApproveInputDisbursementRequestAsync(
            [FromQuery] ApproveDisbursementRequestDto model,
            [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;
            if (string.IsNullOrEmpty(args.SubscriberUserId))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Subscriber user Id is required",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.OrganizationCode) || string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .ApproveInputDisbursementRequestAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,ResponseCodes.Success, ResponseMessages.Successful
            ));
        }



        //disburse input
        [HttpPost("disburse-input")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> DisburseSubscriptionAsync(
            [FromForm] DisburseSubscriptionRequest request,
            [FromQuery] InputSubscriptionRequestArgs filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                    !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                filter.CurrentLoggedInUserRole = UserType.Agent.ToString();
            }
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            var result = await _inputFinancingManager
                .DisburseSubscriptionAsync(request, filter);


            return Ok(ApiResponse<DisburseSubscriptionResult>.Success(
                    result,ResponseCodes.Success, ResponseMessages.Successful
                    ));
        }

        //update create wallet with background service such that if a wallet is created for an organization, update input subscription with the walletid of the provider selected


        [HttpPost("subscription/commitment-reminder")]
        public async Task<IActionResult> SendCommitmentReminderAsync(
           [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            await _inputFinancingManager
                .SendCommitmentReminderAsync(filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processing",
             
                 ResponseCodes.Success,ResponseMessages.Successful
                 ));
        }
        [HttpPost("subscription/record-repayment")]
        public async Task<IActionResult> RecordRepaymentAsync(
   [FromQuery] InputSubscriptionRequestArgs filter,
   [FromBody] RecordRepaymentRequest request)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode) || string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                      ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }
            await _inputFinancingManager
                .RecordRepaymentAsync(request, filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processed",
                 ResponseCodes.Success,ResponseMessages.Successful
                 ));
        }

        [HttpPost("subscription/repayment-reminder")]
        public async Task<IActionResult> SendRepaymentReminder(
           [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            (bool flowControl, IActionResult value) = ValidateAuthorization(filter);
            if (!flowControl)
            {
                return value;
            }
            await _inputFinancingManager
                .SendRepaymentReminderAsync(filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processing",
                 ResponseCodes.Success, ResponseMessages.Successful
                 ));
        }

        [HttpPost("subscription/repayment-schedule")]
        public async Task<IActionResult> GetRepaymentScheduleAsync(
           [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            (bool flowControl, IActionResult value) = ValidateAuthorization(filter);
            if (!flowControl)
            {
                return value;
            }
            var result = await _inputFinancingManager
                .GetRepaymentScheduleAsync(filter);


            return Ok(ApiResponse<List<RepaymentScheduleItemDto>>.Success(
                result,
                 ResponseCodes.Success,
                 ResponseMessages.Successful
                 ));
        }
        [HttpGet("subscription/repayment-ledger")]

        public async Task<IActionResult> GetRepaymentLedgerAsync(
        [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;
            (bool flowControl, IActionResult value) = ValidateAuthorization(filter);
            if (!flowControl)
            {
                return value;
            }
            var result = await _inputFinancingManager
                .GetRepaymentLedgerAsync(filter);


            return Ok(ApiResponse<List<RepaymentLedgerEntryDto>>.SuccessPaged(
                result,
                ResponseCodes.Success,ResponseMessages.Successful
             ));
        }

        private (bool flowControl, IActionResult value) ValidateAuthorization(SubscriptionQueryFilter filter)
        {
            if (CurrentUserRoles.Contains("Agent", StringComparer.OrdinalIgnoreCase) &&
                                !CurrentUserRoles.Contains("Organization", StringComparer.OrdinalIgnoreCase))
            {
                filter.CurrentLoggedInUserRole = UserType.Agent.ToString();
            }
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return (flowControl: false, value: Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                )));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return (flowControl: false, value: Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                )));
            }

            return (flowControl: true, value: null);
        }

        private string GenerateExportFileName(SubscriptionQueryFilter filter, ExportFormat format)
        {
            var status = filter.ViewType.HasValue
     ? Infrastructure.Utilities.EnumExtensions.GetDisplayName(filter.ViewType.Value).ToLower()
     : "all";

            var date = DateTime.UtcNow.ToString("ddMMyyyy");

            var extension = format == ExportFormat.Excel ? "xlsx" : "pdf";

            return $"{status}_subscriptions_{date}.{extension}";
        }
        #endregion
      
    }
}