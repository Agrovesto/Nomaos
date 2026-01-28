using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/account")]
    [ApiController]
    public class AuthenticationController : BaseController
    {
        private readonly IOnBoardingManager _onBoardingManager;
        private readonly IBusinessManager _businessManager;
        public AuthenticationController(IHttpContextAccessor httpContextAccessor, IOnBoardingManager onBoardingManager,
            IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _onBoardingManager = onBoardingManager;
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
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;

                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                //if (args.Id == 0 && currentRole == "Cooperative")
                //{
                //    var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);
                //    args.Id = coop.Id;
                //}
                if (file == null || file.Length == 0)
                return BadRequest("Excel file is required.");


            using var stream = file.OpenReadStream();
            var response = await _onBoardingManager.BulkOnboardBusinessUsers(stream, args);

            //return Ok(response);
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

        [HttpPost("create-account")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] OnboardingRequestModel model, [FromQuery] OnBoardingArgs args)
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
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var currentRole = CurrentUserRole;

                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

                if (args.Id == 0 && currentRole == "Cooperative")
                {
                    var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);
                    args.Id = coop.Id;
                }
                args.OnboardedByUserId = args.UserId;
                var request = await _onBoardingManager.CreateBusinessUser(model, args);


                return Ok(new Response<OnboardingResponseModel>()
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
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] ConfirmOtpRequest model)
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

                var request = await _onBoardingManager.ConfirmOtp(model);


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
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest model)
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

                await _onBoardingManager.ResendOnboardingOtp(model);


                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = "OTP sent"
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

        [HttpPost("create-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProfile(
    [FromBody] ProfileRequestModel model,
    [FromQuery] OnBoardingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            try
            {
                args.OnboardedByUserId = CurrentUserId;
                NormalizeOrganizationCode(args);
                ResolveUserContext(args);
                if (CurrentUserId !=null &&  CurrentUserRole != args.userType)
                {
                    await EnrichArgsByRoleAsync(args);
                }

                var result = await _onBoardingManager.CreateUserProfile(model, args);

                return Ok(new Response<(bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
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

        /** public async Task<IActionResult> CreateProfile([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
                 if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                 {
                     args.organizationCode = OrganizationCode;
                 }
                 var currentRole = CurrentUserRole;

                 // FARMER: allow implicit self-onboarding
                 if (currentRole == "Farmer")
                 {


                     args.UserId = !string.IsNullOrWhiteSpace(CurrentUserId)
                         ? CurrentUserId
                         : args.UserId;
                 }
                 else
                 {
                     // NON-FARMER: must explicitly provide UserId
                     if (string.IsNullOrWhiteSpace(args.UserId))
                     {
                         throw new BaseException(
                             ExceptionType.BAD_REQUEST,
                             "UserId must be provided to create a profile for the farmer."
                         );
                     }

                 }


                 model.OnboardedByUserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : null;
                 // args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;


                 if (args.Id == 0 && currentRole == "Cooperative")
                 {
                     var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);

                     if (coop == null)
                     {
                         throw new BaseException(
                             ExceptionType.NOTFOUND,
                             "Cooperative not found for the specified user."
                         );
                     }

                     // Set cooperative Id
                     args.Id = coop.Id;

                     // Ensure CooperativeIds list exists
                     args.CooperativeIds ??= new List<long?>();

                     // Add cooperative Id if not already present
                     if (!args.CooperativeIds.Any(x => x == coop.Id))
                     {
                         args.CooperativeIds.Add(coop.Id);
                     }
                 }// ================= COOPERATIVE ROLE =================
                 if (args.Id == 0 && currentRole == "Cooperative")
                 {
                     var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);

                     if (coop == null)
                     {
                         throw new BaseException(
                             ExceptionType.NOTFOUND,
                             "Cooperative not found for the specified user."
                         );
                     }

                     args.Id = coop.Id;

                     args.CooperativeIds ??= new List<long?>();

                     if (!args.CooperativeIds.Any(x => x == coop.Id))
                     {
                         args.CooperativeIds.Add(coop.Id);
                     }
                 }

                 // ================= AGENT ROLE =================
                 if (args.Id == 0 && currentRole == "Agent")
                 {
                     var agent = await _businessManager.GetAgentDetailByUserId(args.UserId);

                     if (agent == null)
                     {
                         throw new BaseException(
                             ExceptionType.NOTFOUND,
                             "Agent not found for the specified user."
                         );
                     }

                     args.Id = agent.Id;

                     args.AgentIds ??= new List<long?>();

                     if (!args.AgentIds.Any(x => x == agent.Id))
                     {
                         args.AgentIds.Add(agent.Id);
                     }
                 }

                 var request = await _onBoardingManager.CreateUserProfile(model, args);


                 return Ok(new Response<(bool status, string message)>()
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
         **/

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model, [FromQuery] string? businessCode)
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
            var isAgrovesto = "IsBusiness";//DomainHeader;
            try
            {
                var createUser = await _onBoardingManager.Login(model, businessCode);
                if (createUser.Status)
                {
                    return Ok(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.Response
                    });
                }
                else
                {
                    return BadRequest(new Response<TokenModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = createUser.Status,
                        Message = createUser.Message,
                        Data = createUser.Response
                    });
                }


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
        [HttpPost("assign-agents-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToAgents([FromQuery] AssignmentArgs args)
        {

            try
            {
                args.UserId = CurrentUserId;

                var request = await _onBoardingManager.AssignFarmerToAgents(args);


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

        [HttpPost("assign-cooperatives-to-farmer")]
        [AllowAnonymous]
        public async Task<IActionResult> AssignFarmerToCooperatives([FromQuery] AssignmentArgs args)
        {

            try
            {
                args.UserId ??= CurrentUserId;
                args.OnboardedByUserId = CurrentUserId;

                var request = await _onBoardingManager.AssignFarmerToCooperatives(args);


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


        //[HttpPost("Verify-NIN")]
        //[AllowAnonymous]
        //public async Task<IActionResult> VerifyNIN([FromBody] ProfileRequestModel model, [FromQuery] OnBoardingArgs args)
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
        //        if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
        //        {
        //            args.organizationCode = OrganizationCode;
        //        }
        //        var currentRole = CurrentUserRole;

        //        model.OnboardedByUserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : null;
        //        args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;

        //        if (args.Id == 0 && currentRole == "Cooperative")
        //        {
        //            var coop = await _businessManager.GetCooperativeDetailByUserId(args.UserId);
        //            args.Id = coop.Id;
        //        }
        //        var request = await _onBoardingManager.CreateUserProfile(model, args);


        //        return Ok(new Response<(bool status, string message)>()
        //        {
        //            Code = ResponseCodes.Success,
        //            Succeeded = true,
        //            Message = "Successful",
        //            Data = request
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


        private IActionResult ValidationProblemResponse()
        {
            return BadRequest(new Response<Dictionary<string, string>>
            {
                Message = "Validation failed.",
                Succeeded = false,
                Data = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .ToDictionary(
                        ms => ms.Key,
                        ms => string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage)))
            });
        }
        private void NormalizeOrganizationCode(OnBoardingArgs args)
        {
            if (!string.IsNullOrEmpty(OrganizationCode) &&
                string.IsNullOrEmpty(args.organizationCode))
            {
                args.organizationCode = OrganizationCode;
            }
        }
        private void ResolveUserContext(OnBoardingArgs args)
        {
            var currentRole = CurrentUserRole;

            if (currentRole == "Farmer")
            {
                args.UserId ??= CurrentUserId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(args.UserId))
                {
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "UserId must be provided to create a profile for the farmer."
                    );
                }
            }
        }
        private async Task EnrichArgsByRoleAsync(OnBoardingArgs args)
        {
            var currentRole = CurrentUserRole;

            switch (currentRole)
            {
                case "Cooperative":
                    await EnrichCooperativeAsync(args);
                    break;

                case "Agent":
                    await EnrichAgentAsync(args);
                    break;
            }
        }
        private async Task EnrichCooperativeAsync(OnBoardingArgs args)
        {
          //  if (CurrentUserRole != args.userType) return;
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
        private async Task EnrichAgentAsync(OnBoardingArgs args)
        {
          //  if(CurrentUserRole != args.userType) return;
            if (args.Id != 0) return;
            
            var agent = await _businessManager.GetAgentDetailByUserId(CurrentUserId);

            if (agent == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Agent not found for the specified user.");

            args.Id = agent.Id;

            args.AgentIds ??= new List<long?>();

            if (!args.AgentIds.Contains(agent.Id))
                args.AgentIds.Add(agent.Id);
        }

    }
}
