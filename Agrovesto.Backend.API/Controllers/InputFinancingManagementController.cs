using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InputFinancingManagementController : BaseController
    {
        private readonly IInputFinancingManagement _inputFinancingManagement;
        private readonly IFarmerCreditScoremanager _farmerCreditScoremanager;
        public InputFinancingManagementController(IHttpContextAccessor httpContextAccessor,
            IInputFinancingManagement inputFinancingManagement,
            IFarmerCreditScoremanager farmerCreditScoremanager) : base(httpContextAccessor)
        {
            _inputFinancingManagement = inputFinancingManagement;
            _farmerCreditScoremanager = farmerCreditScoremanager;
        }
        [HttpGet("credit-score")]
        public async Task<IActionResult> GetBusinessesAsync([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _farmerCreditScoremanager.GetFarmerScoreAsync(query);
                return Ok(new PagedListResponse<List<FarmerCreditScoreModel>>()
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

        [HttpGet("subscription-interests-bylocation")]
        public async Task<IActionResult> GetInterestRequestByLocation([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var result = await _inputFinancingManagement.GetInterestRequestByLocation(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
        [HttpGet("subscription-interests-byagent")]
        public async Task<IActionResult> GetInterestRequestByAgent([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetInterestRequestByAgent(query);
                
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result.Data,
                    Message = result.Count != 0 ? "Successful" : "No Record",
                    Succeeded = result.Count !=0 ? true : false,
                    Count = result.Count,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    
                    Code = result.Count != 0 ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpGet("subscription-interests-bycooperative")]
        public async Task<IActionResult> GetInterestRequestByCooperative([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetInterestRequestByCooperative(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
        [HttpGet("subscription-interests")]
        public async Task<IActionResult> GetSubscriptionIntesrest([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.GetInterestRequestByBusiness(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result.Data,
                    Message = result.Data.Any() ?  "Successful" : "No Record",
                    Succeeded = result.Data.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Data.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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
       
        
        [HttpPost("subscription-receipt")]
        public async Task<IActionResult> GetSubscriptionIntesrest([FromBody]BundleSubscriptionInterestModel model, [FromQuery]BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.RetrieveFarmerInputDisbursementReceipt(model, query);
                return Ok(new Response<BundleInterestReceiptModel>()
                {
                    Data = result,
                    Succeeded = true,
                    Code = Constants.ResponseCodes.Success
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

     [HttpPost("retrieve-bundle-payment-slip")]
        public async Task<IActionResult> RetrievePaymentSlip([FromBody] BundleSubscriptionInterestModel model, [FromQuery]BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.RetrievePaymentSlip(model, query);
                return Ok(new Response<BundleSubscriptionInterestResponseModel>()
                {
                    Data = result,
                    Succeeded = true,
                    Code = Constants.ResponseCodes.Success
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

        [HttpPost("indicate-interest")]
        public async Task<IActionResult> CreateInterest([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
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

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                var bundle = await _inputFinancingManagement.CreateInputFinancingInterest(model, args);
                if (bundle.status)
                {
                    return Ok(new Response<BundleSubscriptionInterestResponseModel>()
                    {
                        Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                        Succeeded = true,
                        Message = "Please try again later",
                        Data = bundle.data
                    });

                }
                else
                {
                    return Ok(new Response<BundleSubscriptionInterestResponseModel>()
                    {
                        Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                        Succeeded = false,
                        Message = ResponseMessages.Successful,
                        Data = bundle.data
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


        [HttpPost("confirm-equity-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
           // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED,"Forbidden");
         
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
               // args.UserId = CurrentUserId;
                var bundle = await _inputFinancingManagement.ConfirmEquityCommitmentPayment(model, args);
                return Ok(new Response<BundleSubscriptionInterestModel>()
                {
                    Code = (bundle.status) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = bundle.data
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


        [HttpGet("input-disbursements")]
        public async Task<IActionResult> GetAllInputDisbursement([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.GetAllInputDisbursement(query);
                return Ok(new PagedListResponse<List<BundleDisbursementModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        [HttpPost("confirm-disbursement")]
        public async Task<IActionResult> SubmitDisbursement([FromForm] BundleDisbursementModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
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
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var input = await _inputFinancingManagement.DisburseInput(model, args);
                return Ok(new Response<BundleDisbursementModel>()
                {
                    Code = (input != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = input
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
           [HttpPost("approve-disbursement")]
        public async Task<IActionResult> ApproveEquityForDisbursement([FromBody] BundleSubscriptionInterestModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
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
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                var input = await _inputFinancingManagement.ApproveEquityForDisbursement(model, args);
                return Ok(new Response<BundleSubscriptionInterestModel>()
                {
                    Code = (input != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = input
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
        [HttpGet("business-disbursement")]
        public async Task<IActionResult> GetDisbursementByBusiness([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                var result = await _inputFinancingManagement.GetDisbursementByBusiness(query);
                return Ok(new PagedListResponse<List<BundleDisbursementModel>>()
                {
                    Data = result !=null ? result.Data : null,
                    Message = result.Count !=0 ? "Successful" : "No Record",
                    Succeeded = result.Count != 0 ? true : false,
                    Count = result.Count,
                   PageNumber = result.PageNumber,
                    PageSize = 10,
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

        [HttpGet("agent-disbursement")]
        public async Task<IActionResult> GetDisbursementByBusinessAgent([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetDisbursementByAgent(query);
                return Ok(new PagedListResponse<List<BundleDisbursementModel>>()
                {
                    Data = result.Data,
                    Message = result.Count != 0 ? "Successful" : "No Record",
                    Succeeded = result.Count != 0 ? true : false,
                    Count = result.Count,
                    PageNumber = result.PageNumber,
                    PageSize = 10,
                    Code = result != null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                }); ;
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

        [HttpGet("paidsubscription-interests-byagent")]
        public async Task<IActionResult> GetPaidSubscriptionInterestByBusinessAgent([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;

                var result = await _inputFinancingManagement.GetPaidSubscriptionInterestByBusinessAgent(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result != null ? ResponseMessages.Successful : ResponseMessages.NotFound,
                    Succeeded = result !=null ? true : false,
                    Count = result !=null ? result.Count: 0,
                    PageSize = 10,
                    Code = result!=null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                }); ;
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
        [HttpGet("paidsubscription-interests")]
        public async Task<IActionResult> GetSubscriptionGetPaidSubscriptionInterestByBusinessIntesrest([FromQuery] BusinessRegistrationArgs query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.GetPaidSubscriptionInterestByBusiness(query);
                return Ok(new PagedListResponse<List<BundleSubscriptionInterestModel>>()
                {
                    Data = result,
                    Message = result.Any() ? "Successful" : "No Record",
                    Succeeded = result.Any() ? true : false,
                    Count = result.Count,
                    //PageNumber = ,
                    PageSize = 10,
                    Code = result.Any() ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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

        //SaveBundleSubscriptionRepayment

        [HttpPost("save-repayment")]
        public async Task<IActionResult> SaveBundleSubscriptionRepayment([FromBody] RepaymentBreakdownModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
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


                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(args.organizationCode))
                {
                    args.organizationCode = OrganizationCode;
                }
                var input = await _inputFinancingManagement.SaveBundleSubscriptionRepayment( args, model);
                return Ok(new Response<(bool status, string message)>()
                {
                    Code = (input.status) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = input.status,
                    Message = ResponseMessages.Successful,
                    Data = input
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

    }
}
