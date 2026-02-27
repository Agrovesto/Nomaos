using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/memberships")]
    [ApiController]
    public class MembershipController : BaseController
    {

        private readonly IMembershipManager _membershipManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;
        public MembershipController(IHttpContextAccessor httpContextAccessor,
            IOnBoardingManager onBoardingManager,
            IMembershipManager membershipManager, IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _onBoardingManager=onBoardingManager;
            _membershipManager = membershipManager;
            _businessManager = businessManager;
        }
        [HttpPost("bulk-onboard")]
        public async Task<IActionResult> BulkOnboard(
IFormFile file,
[FromQuery] OnBoardingArgs args)
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
                args.UserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                args.userType = "Farmer";
                await EnrichCooperativeAsync(args);

                if (file == null || file.Length == 0)
                    return BadRequest("Excel file is required.");


                using var stream = file.OpenReadStream();
                var response = await _onBoardingManager.BulkOnboardFarmers(stream, args);

                return Ok(new Response<BulkOnboardingResult>()
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

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetMembershipDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                
                if (CurrentUserRole != "MemberShip" && string.IsNullOrEmpty(query.UserId))
                {
                    return BadRequest(new Response<Dictionary<string, string>>()
                    {
                        Message = "Validation failed.",
                        Succeeded = false,
                        Data = new Dictionary<string, string>
{
    { "UserId", "Membership UserId is required" }
}

                    });
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
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
                args.UserId = CurrentUserId;
                NormalizeOrganizationCode(args);
               
                    await EnrichCooperativeAsync(args);
             

                var request = await _membershipManager.CreateFarmerAsync(model, args);


                return Ok(new Response<CreateMembershipResponseModel>()
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

        [HttpGet("farmers")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;



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

        [HttpGet("farmer-details")]
        public async Task<IActionResult> GetFarmerDetails([FromQuery] string? userId)
        {
            try
            { 

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new Response<string>
                    {
                        Message = "User Id is required.",
                        Succeeded = false
                    });
                }

                var profile = await _onBoardingManager.GetFarmerProfile(userId);

                return Ok(new Response<FarmerProfileDto>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = profile
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


        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }
        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
            if (args.Id != 0) return;

            var coop = await _businessManager.GetCooperativeDetailByUserId(CurrentUserId);

            if (coop == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Cooperative not found for the specified user.");

            args.Id = coop.Id;

            args.CooperativeIds ??= new List<long?>();

            if (!args.CooperativeIds.Contains(coop.Id))
                args.CooperativeIds.Add(coop.Id);
        }

    }
}
