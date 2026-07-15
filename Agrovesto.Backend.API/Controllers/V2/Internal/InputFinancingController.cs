using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
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
        //[HttpPost("package/generate-from-eop")]
        //public async Task<IActionResult> GenerateInputPackageFromEopAsync([FromBody] GenerateInputPackageRequest model, [FromQuery] ListQueryParams args)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new Response<string> { Succeeded = false, Message = "Validation failed" });

        //    try
        //    {
        //        args.organizationCode = args.organizationCode ?? OrganizationCode;
        //        if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");

        //        var result = await _inputFinancingManager.GenerateInputPackageFromEopAsync(model, args);
        //        return Ok(new Response<bool> { Succeeded = true, Data = result, Code = ResponseCodes.Success, Message = "Successful" });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return StatusCode(400, new Response<string> { Message = ex.ExceptionMessage, Succeeded = false });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(400, new Response<string> { Message = ex.Message, Succeeded = false });
        //    }
        //}
        [HttpGet("eop")]
        public async Task<IActionResult> GetEopByIdAsync([FromQuery] ListQueryParams args)
        {
            try
            {
                args.organizationCode = args.organizationCode ?? OrganizationCode;
                if (string.IsNullOrEmpty(args.organizationCode)) throw new BaseException(ExceptionType.UNAUTHORIZED, "Access Denied");
                if (args.Id <= 0) return BadRequest(new Response<string> { Message = "EOP Id is required", Succeeded = false });

                var result = await _inputFinancingManager.GetEopByIdAsync(args);
                return Ok(new Response<EopDto> { Succeeded = result != null, Data = result, Code = ResponseCodes.Success, Message = result != null ? "Successful" : "No record found" });
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

            if (string.IsNullOrWhiteSpace(args.organizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .CreateInputFinancingPackageConfig(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Created Successfully",
                HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(query.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
          
            var result = await _inputFinancingManager
                .UpdateInputPackageConfig(args.PackageId, model, query);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Created Successfully",
                HttpContext.TraceIdentifier
            ));
        }



        [HttpGet("package")]
        public async Task<IActionResult> GetPackageByIdAsync([FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode) ||
                string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            if (args.Id <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "Package Id is required",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.GetPackageByIdAsync(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<InputPackageDto>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }

        [HttpPatch("change-package-input")]
        public async Task<IActionResult> ReplacePackageItemAsync([FromBody] ReplacePackageItemRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
          //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode) )
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.ReplacePackageItemAsync(request,args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        [HttpPost("add-package-input")]
        public async Task<IActionResult> AddItemToPackageAsync([FromBody] AddPackageItemRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
          //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode) )
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.AddItemToPackageAsync(request,args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }

        [HttpDelete("remove-package-input")]
        public async Task<IActionResult> RemoveItemFromPackage([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.RemoveItemFromPackage(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
       
        //add-package-input

        [HttpDelete("deactivate-package")]
        public async Task<IActionResult> DeactivatePackage([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.DeactivateInputPackage(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        [HttpPatch("reactivate-package")]
        public async Task<IActionResult> ReactivatePackage( [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.ReactivatePackageAsync(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        [HttpGet("packages")]
        public async Task<IActionResult> GetPackagesAsync([FromQuery] InputPackageFilterRequest filter, [FromQuery] ListQueryParams args)
        {
            args.organizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.organizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
              .GetPackagesAsync(args);

            return Ok(ApiResponse<List<InputPackageListModel>>.SuccessPaged(
      result,
      "Successful",
      ResponseCodes.Success,
      HttpContext.TraceIdentifier
  ));
        }

        [HttpPost("package/add-additional-cost")]
        public async Task<IActionResult> AddAdditionalCostToPackageAsync([FromBody] AddAdditionalCostRequest request, [FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.AddAdditionalCostToPackageAsync(request, args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        [HttpPatch("package/update-additional-cost")]
        public async Task<IActionResult> UpdateAdditionalCostAsync([FromBody] UpdateAdditionalCostRequest request, [FromQuery] UpdateAdditionalCostArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.UpdateAdditionalCostAsync(request, args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }
        [HttpDelete("package/remove-additional-cost")]
        public async Task<IActionResult> RemoveAdditionalCostFromPackageAsync([FromQuery] ReplacePackageItemRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            //  args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }

            var result = await _inputFinancingManager.RemoveAdditionalCostFromPackageAsync(args);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    "No record found",
                    HttpContext.TraceIdentifier
                ));
            }

            return Ok(ApiResponse<bool>.Success(
                result,
                "Successful",
                HttpContext.TraceIdentifier
            ));
        }

        [HttpPost("package/link-wallet")]
        public async Task<IActionResult> UpdateOrganizationInputPackage(
         [FromQuery] WalletQueryParams filter,
         [FromBody] UserWalletModel request)
        {
            filter.organizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(filter.organizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.businessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            await _nomaOSWalletService
                .UpdateOrganizationInputPackage(filter, request.WalletProviderId, request);


            return Ok(ApiResponse<string>.Success(
                 "Request Processing",
                 "Successful",
                 ResponseCodes.Success,
                 HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .AddSubscriberAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Subscriber Added Successfully",
                HttpContext.TraceIdentifier
            ));
        }


          

        [HttpPost("approve-subscriber")]
        public async Task<IActionResult> ApproveSubscriptionAsync(
            [FromBody] ApproveSubscriptionRequest model,
            [FromQuery] InputSubscriptionRequestArgs args)
        {
            args.OrganizationCode ??= OrganizationCode;
            args.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .ApproveSubscriptionAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Subscriber Added Successfully",
                HttpContext.TraceIdentifier
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
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .GetSubscriptionsAsync(filter, filter.PageNumber, filter.PageSize);

          
            return Ok(ApiResponse<List<SubscriptionDetailDto>>.SuccessPaged(
    result,
    "Successful",
    ResponseCodes.Success,
    HttpContext.TraceIdentifier
));
        }
       

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetInputFinancingDashboardAsync(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;


            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .GetInputFinancingDashboardAsync(filter);

            return Ok(ApiResponse<InputFinancingDashboardDto>.Success(
                result,
                "Subscriber Added Successfully",
                HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .FetchSubscriptionForReview(filter);


            return Ok(ApiResponse<SubscriptionFullDetailDto>.Success(
                 result,
                 "Successful",
                 ResponseCodes.Success,
                 HttpContext.TraceIdentifier
                 ));
        }
       
        [HttpGet("subscription-detail")]
        [Authorize(Roles = "Organization,Agent")]
        public async Task<IActionResult> GetSubscriptionDetail(
            [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .GetSubscriptionDetail(filter);


            return Ok(ApiResponse<SubscriptionReviewResponseDto>.Success(
                 result,
                 "Successful",
                 ResponseCodes.Success,
                 HttpContext.TraceIdentifier
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
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .GetDocuments(filter.SubscriptionId);

            return Ok(ApiResponse<List<Core.Models.Businesses.Input.SubscriptionDocument>>.Success(
result,
"Successful",
ResponseCodes.Success,
HttpContext.TraceIdentifier
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
            if (string.IsNullOrWhiteSpace(query.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
             var result = await _inputFinancingManager
                .UploadCommissionLetterAsync(args.SubscriptionId, args.UserId, file);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Uploaded Successfully",
                HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.Env))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    $"Invalid request {nameof(filter.Env)}",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            await _inputFinancingManager
                .CommitmentPaymentAsync(request, filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processed",
                 "Successful",
                 ResponseCodes.Success,
                 HttpContext.TraceIdentifier
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
            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .RequestInputDisbursementApprovalAsync(args);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Request Sent Successfully",
                HttpContext.TraceIdentifier
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

            if (string.IsNullOrWhiteSpace(args.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(args.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .ApproveInputDisbursementRequestAsync(model, args);

            return Ok(ApiResponse<bool>.Success(
                true,
                "Request Approved Successfully",
                HttpContext.TraceIdentifier
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
            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            var result = await _inputFinancingManager
                .DisburseSubscriptionAsync(request, filter);


            return Ok(ApiResponse<DisburseSubscriptionResult>.Success(
    result,
    "Successful",
    ResponseCodes.Success,
    HttpContext.TraceIdentifier
));
        }


        //update create wallet with background service such that if a wallet is created for an organization, update input subscription with the walletid of the provider selected


        [HttpPost("subscription/commitment-reminder")]
        public async Task<IActionResult> SendCommitmentReminderAsync(
           [FromQuery] SubscriptionQueryFilter filter)
        {
            filter.OrganizationCode ??= OrganizationCode;
            filter.UserId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            if (string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    "Access Denied",
                    HttpContext.TraceIdentifier
                ));
            }
            await _inputFinancingManager
                .SendCommitmentReminderAsync(filter);


            return Ok(ApiResponse<string>.Success(
                 "Request Processing",
                 "Successful",
                 ResponseCodes.Success,
                 HttpContext.TraceIdentifier
                 ));
        }

        private string GenerateExportFileName(SubscriptionQueryFilter filter, ExportFormat format)
        {
            var status = filter.ViewType.HasValue
     ? EnumExtensions.GetDisplayName(filter.ViewType.Value).ToLower()
     : "all";

            var date = DateTime.UtcNow.ToString("ddMMyyyy");

            var extension = format == ExportFormat.Excel ? "xlsx" : "pdf";

            return $"{status}_subscriptions_{date}.{extension}";
        }
        //subscriber details for
        //pending commitment (1) endpoint

        //active commitment (2) endpoint
        //pending approval (3) endpoint // print commission letter
        //confirm input release (4) endpoint // approve disbursement
        //subscribe input detail to include subscriber information and package information
        //send commitment reminder to subscriber (notify agent or membership leader, notification note
        //escalete default reminder to subscriber (notify agent or membership leader, notification note
        #endregion
        /**  /api/financing-packages
/api/subscriptions
/api/commitments
/api/input-releases
/api/repayments
/api/risk
/api/reports
/api/eop
/api/warehouse

           **/
    }
}