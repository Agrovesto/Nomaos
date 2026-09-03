using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/farms")]
    [ApiController]
    public class FarmMappingController : BaseController
    {
        private readonly IFarmMappingManager _farmMappingManager;
        private readonly IBusinessManager _businessManager;
        private readonly IOnBoardingManager _onBoardingManager;

        public FarmMappingController(IHttpContextAccessor httpContextAccessor,
            IFarmMappingManager farmMappingManager,
            IBusinessManager businessManager, IOnBoardingManager onBoardingManager) : base(httpContextAccessor)
        {
            _farmMappingManager = farmMappingManager;
            _businessManager = businessManager;
            _onBoardingManager = onBoardingManager;
        }

        [HttpGet("by-membership")]
        public async Task<IActionResult> FetchAllFarmsByFarmersBelongingToAMembership([FromQuery] FarmMappingArgs query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;

                if (string.IsNullOrEmpty(query.userId))
                    throw new BaseException(ExceptionType.ACCESS_DENIED, "Access Denied");

                await EnsureRoleAsync(query.userId, "MemberShip");

                return await _farmMappingManager.FetchAllFarmsByFarmersBelongingToAMembership(query);
            },
            result => Ok(Wrap(result)));
        }

        [HttpGet("by-agent")]
        public async Task<IActionResult> FetchAllFarmsByFarmersBelongingToAnAgent([FromQuery] FarmMappingArgs query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                
                query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;

                await EnsureRoleAsync(query.userId!, "Agent");

                return await _farmMappingManager.FetchAllFarmsByFarmersBelongingToAnAgent(query);
            },
            result => Ok(Wrap(result)));
        }

        [HttpGet("by-farmer")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(async () =>
            {
                await ResolveOrValidateFarmerIdAsync(query);
                return await _farmMappingManager.FetchFarmsByFarmer(query);
            },
            result => Ok(Wrap(result)));
        }

        [HttpGet("details")]
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

        [HttpPost("submit-farm")]
        public async Task<IActionResult> CreateFarm([FromForm] SubmitFarmRequest model, [FromQuery] FarmMappingArgs args)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();
            var orgError = TryResolveOrganizationCode(args.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            args.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                args.performedByUserId = CurrentUserId;
              
                await ResolveOrValidateFarmerIdAsync(args);

                return await _farmMappingManager.SubmitFarmAsync(model, args);
            },
            request => Ok(new Response<long>
            {
                Code = ResponseCodes.Success,
                Succeeded = true,
                Message = ResponseMessages.Successful,
                Data = request
            }));
        }


        private async Task EnrichFarmerAsync(FarmMappingArgs args)
        {
            if (args.farmerId != 0) return;

            var farmer = await _businessManager.GetFarmerByUserId(CurrentUserId);

            if (farmer == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Farmer not found for the specified user.");

            args.farmerId = farmer.Id;
        }

        private async Task EnsureRoleAsync(string userId, string role)
        {
            if (!await _onBoardingManager.IsUserInRoleAsync(userId, role))
                throw new BaseException(ExceptionType.BAD_REQUEST, $"The supplied user is not a {role}.");
        }

        private async Task ResolveOrValidateFarmerIdAsync(FarmMappingArgs args)
        {
            var isFarmer = CurrentUserRoles.Contains("Farmer", StringComparer.OrdinalIgnoreCase);

            if (isFarmer)
            {
                await EnrichFarmerAsync(args);

                if (args.farmerId <= 0)
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Unable to resolve farmer profile for current user.");
            }
            else
            {
                if (args.farmerId <= 0)
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Farmer Id is required.");

                await _onBoardingManager.EnsureFarmerIdIsValidAsync(args.farmerId);
            }
        }
    }
}
