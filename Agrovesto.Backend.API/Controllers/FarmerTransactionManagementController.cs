using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmerTransactionManagementController : BaseController
    {
        private readonly IFarmerTransactionManager _farmerTransactionManager;
        public FarmerTransactionManagementController(IHttpContextAccessor httpContextAccessor,
            IFarmerTransactionManager farmerTransactionManager) : base(httpContextAccessor)
        {
            _farmerTransactionManager = farmerTransactionManager;
        }

        [HttpPost("record-sales")]
        public async Task<IActionResult> LogMarketSalesTransaction([FromBody] FarmerSalesTrackerRequestModel model, [FromQuery] TransactionQueryParams args)
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
                //if user to be created is a farmer, args.userId has to be cooperative user Id
                var userRole = CurrentUserRole;
                if (userRole == "Supervisor")
                {
                    args.supervisorId = CurrentUserId;
                    args.UserId = args.UserId;
                }
                else
                {
                    args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
              
                }
                var request = await _farmerTransactionManager.LogOfflineTransaction(model.request, args, args.FarmerId);

                return Ok(new Response<FarmerSalesTrackerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Logged Successfully",
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

        [HttpGet("farmer-transaction-byagent")]
        public async Task<IActionResult> GetFarmerTransactionByAgent([FromQuery] TransactionQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerTransactionManager.GetFarmersTransactionByAgent(query);
                return Ok(new PagedListResponse<List<FarmerSalesTrackerModel>>()
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

        [HttpGet("farmer-transaction")]
        public async Task<IActionResult> GetFarmerTransaction([FromQuery] TransactionQueryParams query)
        {
            
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                return await ExecuteAsync(
        () => _farmerTransactionManager.GetTransactionByFarmer(query.FarmerId, query),
        result => Ok(BuildPagedResponse(result, query.PageNumber))
    );
            

        }

         [HttpGet("farmer-transaction-by-userId")]
        public async Task<IActionResult> GetTransactionByFarmerByUserId([FromQuery] TransactionQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerTransactionManager.GetTransactionByFarmerByUserId(query);
                return Ok(new PagedListResponse<List<FarmerSalesTrackerModel>>()
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



        [HttpGet("farmer-transaction-bycooperative")]
        public async Task<IActionResult> GetFarmerTransactionByCooperative([FromQuery] TransactionQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerTransactionManager.GetFarmerSalesTransactionsByCooperativeAsync(query);
                return Ok(new PagedListResponse<List<FarmerSalesTrackerModel>>()
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

        [HttpGet("farmer-all-transaction")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] TransactionQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerTransactionManager.GetFarmerTransactions(query);
                return Ok(new PagedListResponse<List<FarmerSalesTrackerModel>>()
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

        [HttpGet("farmer-all-transaction-by-business")]
        public async Task<IActionResult> GetAllTransactionsByBusinessCode([FromQuery] TransactionQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerTransactionManager.GetTransactionsByBusinessCode(query);
                return Ok(new PagedListResponse<List<FarmerSalesTrackerModel>>()
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
    }
}
