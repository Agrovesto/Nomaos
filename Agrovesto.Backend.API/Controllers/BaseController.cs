using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    //[ApiController]
    //[Authorize]
    // [ValidateModelFilter]
    public class BaseController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        protected static readonly IReadOnlySet<string> AllowedExtensions =
     new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xls", ".csv" };
        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected string RequireOrganizationCode(string provided)
        {
            var code = !string.IsNullOrEmpty(provided) ? provided : OrganizationCode;
            if (string.IsNullOrEmpty(code))
                throw new BaseException(ExceptionType.UNAUTHORIZED, ResponseMessages.AccessDenied);
            return code;
        }

        protected IActionResult? RequireField(string value, string fieldName)
        {
            if (!string.IsNullOrEmpty(value)) return null;
            return BadRequest(new Response<Dictionary<string, string>>
            {
                Message = "Validation failed.",
                Succeeded = false,
                Data = new Dictionary<string, string> { { fieldName, $"{fieldName} is required" } }
            });
        }

        // For endpoints that always report Succeeded = true and derive Code from data presence
        protected Response<T> Wrap<T>(T data, string? successMessage = null, string? notFoundMessage = null)
        {
            bool hasValue = !EqualityComparer<T>.Default.Equals(data, default);

            return new Response<T>
            {
                Code = hasValue ? ResponseCodes.Success : ResponseCodes.Failed,
                Succeeded = true,
                Message = hasValue ? (successMessage ?? ResponseMessages.Successful) : (notFoundMessage ?? "No record found"),
                Data = data
            };
        }

        protected PagedListResponse<List<T>> WrapPaged<T>(PagedListResponse<List<T>> result, int pageNumber, int pageSize, string? message = null)
        {
            return new PagedListResponse<List<T>>
            {
                Code = ResponseCodes.Success,
                Succeeded = true,
                Message = message ?? (result.Count > 0 ? "Record Retrieved" : "No Records Found"),
                Data = result.Data,
                Count = result.Count,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }

        protected async Task<IActionResult> HandleBulkMembershipUploadAsync(
            IFormFile file, OnBoardingArgs args,
            IBulkMembershipExcelParser parser, IBulkMembershipValidator validator,
            IBulkMembershipUploadService uploadService, ILogger logger)
        {
            if (ValidateUploadFile(file) is { } fileError) return fileError;

            if (string.IsNullOrWhiteSpace(args.businessCode))
                return Error("BUSINESS_CODE_REQUIRED", "A business code is required to upload memberships.");

            var onboardedById = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
            if (string.IsNullOrWhiteSpace(onboardedById))
                return Error("UNAUTHORIZED", "Your session has expired. Please sign in again and retry.", StatusCodes.Status401Unauthorized);

            args.organizationCode = !string.IsNullOrEmpty(args.organizationCode) ? args.organizationCode : OrganizationCode;

            List<BulkMembershipRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = parser.Parse(stream, file.FileName, args.businessCode, onboardedById, args.organizationCode, args.Id);
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported membership file type uploaded: {FileName}", file.FileName);
                return Error("FILE_TYPE_INVALID", "We couldn't read that file format. Please upload a CSV or Excel file.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse membership upload file: {FileName}", file.FileName);
                return Error("FILE_PARSE_ERROR", "We couldn't read this file. Please check that it's a valid CSV/Excel export and try again.");
            }

            if (rows.Count == 0)
                return Error("FILE_EMPTY", "This file doesn't contain any membership records to upload.");

            var validationErrors = await validator.ValidateAllAsync(rows, args);
            if (validationErrors.Count > 0)
            {
                return UnprocessableEntity(new ApiErrorResponse
                {
                    Code = "VALIDATION_FAILED",
                    Message = "We found problems with some rows. No records were saved — please fix and re-upload.",
                    Details = validationErrors.GroupBy(e => e.RowNumber)
                        .ToDictionary(g => g.Key.ToString(), g => string.Join("; ", g.SelectMany(e => e.Messages)))
                });
            }

            try
            {
                var result = await uploadService.ProcessBulkMembershipUploadAsync(rows, args);
                return result.FailureCount == 0 ? Ok(result) : StatusCode(StatusCodes.Status207MultiStatus, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Membership upload processing failed for business {BusinessCode}", args.businessCode);
                return Error("UPLOAD_PROCESSING_FAILED", "Something went wrong while processing your upload. Please try again, and contact support if the problem continues.", StatusCodes.Status500InternalServerError);
            }
        }
        protected async Task<IActionResult> HandleBulkFarmerUploadAsync(
    IFormFile file,
    OnBoardingArgs args,
    IBulkFarmerUploadService uploadService,
    ILogger logger)
        {
            if (ValidateUploadFile(file) is { } fileError) return fileError;

            List<BulkFarmerRowDto> rows;
            try
            {
                await using var stream = file.OpenReadStream();
                rows = BulkFarmerCsvParser.Parse(stream, file.FileName);
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning(ex, "Unsupported file type uploaded: {FileName}", file.FileName);
                return Error("FILE_TYPE_INVALID", "We couldn't read that file format. Please upload a CSV or Excel file.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse bulk upload file: {FileName}", file.FileName);
                return Error("FILE_PARSE_ERROR",
                    "We couldn't read this file. Please check that it's a valid CSV/Excel export and try again.");
            }

            if (rows.Count == 0)
                return Error("FILE_EMPTY", "This file doesn't contain any farmer records to upload.");

            try
            {
                var result = await uploadService.ProcessAsync(rows, args);

                int statusCode = result.Failed > 0
                    ? StatusCodes.Status207MultiStatus
                    : StatusCodes.Status200OK;

                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bulk farmer upload processing failed for business {BusinessCode}", args.businessCode);
                return Error("UPLOAD_PROCESSING_FAILED",
                    "Something went wrong while processing your upload. Please try again, and contact support if the problem continues.",
                    StatusCodes.Status500InternalServerError);
            }
        }
        protected IActionResult? ValidateUploadFile(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return Error("FILE_MISSING", "Please choose a file to upload before submitting.");

            if (file.Length > MaxFileSizeBytes)
                return Error("FILE_TOO_LARGE",
                    $"That file is too large. Please upload a file smaller than {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Error("FILE_TYPE_INVALID",
                    "We couldn't read that file. Please upload a CSV or Excel file (.csv, .xlsx, .xls).");

            return null;
        }
        protected async Task<IActionResult> ExecuteAsync<T>(
            Func<Task<T>> action,
            Func<T, IActionResult> onSuccess)
        {
            try
            {
                var result = await action();
                return onSuccess(result);
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

        protected Response<T> Success<T>(T data, string message = null, string code = ResponseCodes.Success)
        {
            return new Response<T>
            {
                Code = code,
                Succeeded = true,
                Message = message,
                Data = data
            };
        }

        protected PagedListResponse<List<T>> BuildPagedResponse<T>(
            PagedListResponse<List<T>> result, int pageNumber, int pageSize = 10)
        {
            return new PagedListResponse<List<T>>
            {
                Data = result.Data,
                Message = result.Message,
                Succeeded = result.Succeeded,
                Count = result.Count,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Code = (result.Succeeded && result.Data != null)
                    ? Core.CoreUtils.Constants.ResponseCodes.Success
                    : Core.CoreUtils.Constants.ResponseCodes.Failed
            };
        }
        protected IActionResult ValidationProblemResponse()
        {
            return BadRequest(new Response<Dictionary<string, string>>()
            {
                Message = "Validation failed.",
                Succeeded = false,
                Data = ModelState
                    .Where(ms => ms.Value?.Errors.Any() == true)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                    )
            });


        }
        protected ObjectResult Error(string code, string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            return StatusCode(statusCode, new ApiErrorResponse
            {
                Code = code,
                Message = message
            });
        }
        public string? DomainHeader
        {
            get
            {
                var headers = _httpContextAccessor.HttpContext?.Request.Headers;
                var header = _httpContextAccessor.HttpContext?.Request.Headers["Domain"];
                if (string.IsNullOrEmpty(header)) return string.Empty;
                return header;
            }
        }

        protected JsonWebToken? TryReadJwt()
        {
            var token = Token;

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JsonWebTokenHandler();

            if (!handler.CanReadToken(token))
                return null;

            try
            {
                return handler.ReadJsonWebToken(token);
            }
            catch
            {
                return null;
            }
        }
        public string? CurrentUserId =>
    TryReadJwt()?
        .Claims.FirstOrDefault(x => x.Type.Equals("sub", StringComparison.OrdinalIgnoreCase))
        ?.Value;
        public string? CurrentUserEmail =>
            TryReadJwt()?
                .Claims.FirstOrDefault(x => x.Type.Equals("email", StringComparison.OrdinalIgnoreCase))
                ?.Value;
     
        
        
        public string? CurrentUserRole
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null) return null;

                var roleClaim = jwt.Claims.FirstOrDefault(c =>
                    c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                                  StringComparison.OrdinalIgnoreCase));

                if (roleClaim != null) return roleClaim.Value;

                roleClaim = jwt.Claims.FirstOrDefault(c => c.Type.Equals("Roles", StringComparison.OrdinalIgnoreCase));
                return roleClaim?.Value;
            }
        }
        public IEnumerable<string> CurrentUserRoles
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null) return Enumerable.Empty<string>();

                var roles = jwt.Claims
                    .Where(c =>
                        c.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                                      StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals("Roles", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList();

                return roles;
            }
        }
        public string? OrganizationCode
        {
            get
            {
                var jwt = TryReadJwt();
                if (jwt == null)
                    return null;

                var claimValue = jwt.Claims
                    .FirstOrDefault(x => x.Type.Equals("Organizations", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(claimValue))
                    return null;

                try
                {
                    var organizations = JsonSerializer.Deserialize<List<OrganizationClaim>>(claimValue);

                    return organizations?.Any() == true
                        ? string.Join(",", organizations.Select(o => o.OrganizationCode))
                        : null;
                }
                catch
                {
                    return null;
                }
            }
        }


        public string Token
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HeaderNames.Authorization, out var tokenValues) == true)
                {
                    var token = tokenValues.ToString().Trim(); 

                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return token.Substring("Bearer ".Length).Trim(); 
                    }
                }
                return string.Empty;
            }
        }
        protected List<OrganizationClaim> CurrentUserOrganizations
        {
            get
            {
                var raw = User.FindFirst("Organizations")?.Value;
                if (string.IsNullOrWhiteSpace(raw)) return new List<OrganizationClaim>();
                return JsonSerializer.Deserialize<List<OrganizationClaim>>(raw) ?? new List<OrganizationClaim>();
            }
        }
        
        protected IActionResult? TryResolveOrganizationCode(string? suppliedCode, out string? resolvedCode)
        {
            var userOrgs = CurrentUserOrganizations;

            if (string.IsNullOrWhiteSpace(suppliedCode))
            {
                if (userOrgs.Count == 1)
                {
                    resolvedCode = userOrgs[0].OrganizationCode;
                    return null;
                }

                if (userOrgs.Count > 1)
                {
                    resolvedCode = null;
                    return Unauthorized(ApiResponse<object>.Fail(
                        "Multiple organizations found for this user. Please specify an organizationCode.",
                        ResponseCodes.AccessDenied
                    ));
                }

                resolvedCode = null;
                return null;
            }

            if (userOrgs.All(o => !o.OrganizationCode.Equals(suppliedCode, StringComparison.OrdinalIgnoreCase)))
            {
                resolvedCode = null;
                return Unauthorized(ApiResponse<object>.Fail(
                    ResponseMessages.AccessDenied,
                    ResponseCodes.AccessDenied
                ));
            }

            resolvedCode = suppliedCode;
            return null;
        }
        protected void NormalizeOrganizationCode(OnBoardingArgs args)
        {

            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }

        public class OrganizationClaim
        {
            public string? OrganizationCode { get; set; }
            public string? OrganizationName { get; set; }
        }

    }

}
