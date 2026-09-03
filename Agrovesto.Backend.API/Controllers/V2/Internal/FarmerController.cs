using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Agrovesto.Backend.Infrastructure.V2.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/farmer")]
    [ApiController]
    public class FarmerController : BaseController
    {
        private readonly IBusinessManager _businessManager;
        private readonly INomaOSWalletService _walletService;
        private readonly IFarmMappingManager _farmMappingManager;
        private readonly IFarmerTransactionManager _farmerTransactionManager;
        public FarmerController(IHttpContextAccessor httpContextAccessor,
            INomaOSWalletService walletService,
            IFarmMappingManager farmMappingManager,
            IFarmerTransactionManager farmerTransactionManager,
            IBusinessManager businessManager) : base(httpContextAccessor)
        {
            _businessManager = businessManager;
            _farmMappingManager = farmMappingManager;
            _walletService = walletService;
            _farmerTransactionManager = farmerTransactionManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetFarmerDashboardData([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            return await ExecuteAsync(
              () =>
              {
                  query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                  return _businessManager.GetFarmerDashboard(query);
              },
              result => Ok(Wrap(result, ResponseMessages.Successful, ResponseMessages.NotFound)));

        }

        [HttpGet("memberships")]
        public async Task<IActionResult> GetFarmerMemberships([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(
                        () =>
                {
                            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                            return _businessManager.GetMembershipsByFarmer(query);
                },
                result => Ok(new PagedListResponse<List<FarmerMembershipSummaryDto>>
                        {
                            Data = result,
                            Message = ResponseMessages.Successful,
                            Succeeded = true,
                            Count = result.Count,
                            PageNumber = query.PageNumber,
                            PageSize = 10,
                            Code = ResponseCodes.Success
                        }));
        }
      
        [HttpGet("membership-detail")]
        public async Task<IActionResult> GetMembershipDetail([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            if (RequireField(query.UserId, "Membership UserId") is { } error) return error;

            return await ExecuteAsync(
                () => _businessManager.GetFarmerMembershipDetails(query),
                result => Ok(new Response<FarmerMembershipOverviewDto>
                {
                    Data = result,
                    Message = ResponseMessages.Successful,
                    Succeeded = true,
                    Code = ResponseCodes.Success
                }));
        }

        [HttpGet("wallets")]
        public async Task<IActionResult> GetWallets([FromQuery] UserWalletParams query)
        {
            return await ExecuteAsync(
             () =>
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

        [HttpPost("create-wallet")]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] WalletQueryParams query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

            if (string.IsNullOrWhiteSpace(query.env))
                return BadRequest("Environment is required");

            if (string.IsNullOrWhiteSpace(request.WalletProviderCode))
                return BadRequest("WalletProviderCode is required");

            if (string.IsNullOrWhiteSpace(query.businessCode))
                return BadRequest("BusinessCode is required");

            if (RequireField(query.UserId, "UserId") is { } error) return error;
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(
                () =>
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


        [HttpGet("wallet-summary")]
        public async Task<IActionResult> GetUserFinancialSummary([FromQuery] UserWalletParams query)
        {
            return await ExecuteAsync(
             () =>
             {
                 query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                 return _walletService.GetUserFinancialSummary(query);
             },
             result => Ok(new PagedListResponse<FarmerFinancialSummaryDto>
             {
                 Data = result,
                 Message = ResponseMessages.Successful,
                 Succeeded = true,
                 Code = ResponseCodes.Success
             }));
        }

        [HttpPost("wallet-topup")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            return await ExecuteAsync(
                        () =>
                        {
                            args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                            return _walletService.TopUpWallet(request, args);
                        },
                        response => Ok(Success(response, ResponseMessages.Successful)));
        }

        [HttpPost("wallet-withdraw")]
        public async Task<IActionResult> WithdrawWallet([FromBody] WithdrawWalletRequest request, [FromQuery] WalletQueryParams args)
        {
            return await ExecuteAsync(
             () =>
             {
                 args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                 return _walletService.WithdrawWallet(request, args);
             },
             response => Ok(Success(response, ResponseMessages.Successful)));
        }
      
        [HttpGet("farms")]
        public async Task<IActionResult> FetchFarmsByFarmer([FromQuery] FarmMappingArgs query)
        {

            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;
            query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;

            if (RequireField(query.userId, "User Id") is { } error) return error;

            return await ExecuteAsync(async () =>
            {
                await EnrichFarmerAsync(query);
                return await _farmMappingManager.FetchFarmsByFarmer(query);
            },
            result => Ok(Wrap(result)));
        }
        [HttpGet("farm-details")]
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

        [HttpPost("add-farm")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFarm([FromForm] SubmitFarmRequest model, [FromQuery] FarmMappingArgs query)
        {
            if (!ModelState.IsValid)
                return ValidationProblemResponse();

            query.userId = !string.IsNullOrEmpty(query.userId) ? query.userId : CurrentUserId;
            if (RequireField(query.userId, "User Id") is { } error) return error;
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(async () =>
            {
                query.performedByUserId = CurrentUserId;
                     await EnrichFarmerAsync(query);
                return await _farmMappingManager.SubmitFarmAsync(model, query);
            },
            request => Ok(new Response<long>
            {
                Code = ResponseCodes.Success,
                Succeeded = true,
                Message = request < 1 ? ResponseMessages.Successful : ResponseMessages.Failed,
                Data = request
            }));
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgentsByFarmer([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(
                        () =>
                        {
                               query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                            return _businessManager.GetAgentsByFarmer(query);
                        },
                        result => Ok(BuildPagedResponse(result, query.PageNumber, 10)));
        }
      
        [HttpGet("user-wallet-transaction")]
        public async Task<IActionResult> GetUserWallet([FromQuery] ListQueryParams query)
        {
            return await ExecuteAsync(
                       () =>
                       {
                           query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                           return _walletService.GetUserWalletTransaction(query);
                       },
                       result => Ok(new PagedListResponse<List<FarmerTransactionDto>>
                       {
                           Data = result.Data,
                           Message = result.Count > 0 ? ResponseMessages.Successful : ResponseMessages.NotFound,
                           Succeeded = true,
                           Count = result.Count,
                           // Carried through from the repository, which has already normalised them.
                           // Hardcoding PageSize here made totalPages disagree with the rows returned.
                           PageNumber = result.PageNumber,
                           PageSize = result.PageSize,
                           Code = result.Count > 0 ? ResponseCodes.Success : ResponseCodes.Failed
                       }));
        }

        [HttpGet("input-obligations")]
        public async Task<IActionResult> GetFarmerInputObligations([FromQuery] ListQueryParams query)
        {
            var orgError = TryResolveOrganizationCode(query.organizationCode, out var resolvedOrgCode);
            if (orgError != null) return orgError;
            query.organizationCode = resolvedOrgCode;

            return await ExecuteAsync(
                       () =>
                       {
                             query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                           return _farmerTransactionManager.GetFarmerInputObligations(query);
                       },
                       result => Ok(new PagedListResponse<List<InputObligationDto>>
                       {
                           Data = result.Data,
                           Message = result.Count > 0 ? ResponseMessages.Successful : ResponseMessages.NotFound,
                           Succeeded = true,
                           Count = result.Count,
                           PageNumber = query.PageNumber,
                           PageSize = 10,
                           Code = result.Count > 0 ? ResponseCodes.Success : ResponseCodes.Failed
                       }));
        }

     

        private async Task EnrichFarmerAsync(FarmMappingArgs args)
        {
            if (args.farmerId != 0) return;

            var farmer = await _businessManager.GetFarmerByUserId(CurrentUserId);

            if (farmer == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "Farmer not found for the specified user.");

            args.farmerId = farmer.Id;
        }

    }

}
