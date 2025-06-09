using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/Business")]
    //[ApiController]
    public class BusinessController : BaseController
    {
        private readonly IBusinessManager _businessManager;

        private readonly IRoleManager _roleManager;
        private readonly IWalletManager _walletManager;
        private readonly IWarehouseManagementManager _warehouseManagementManager;
        private readonly IInputFinancingManagement _inputFinancingManagement;
        public BusinessController(IHttpContextAccessor httpContextAccessor, IBusinessManager businessManager,
            IWarehouseManagementManager warehouseManagementManager, IInputFinancingManagement inputFinancingManagement,
            IRoleManager roleManager, IWalletManager walletManager) : base(httpContextAccessor)
        {
            _businessManager = businessManager;
            _roleManager = roleManager;
            _walletManager = walletManager;
            _inputFinancingManagement = inputFinancingManagement;
            _warehouseManagementManager = warehouseManagementManager;
        }

        //assign supervisor to agent
        [HttpPost("assign-supervisor")]
        public async Task<IActionResult> AssignSupervisorToAgent([FromQuery] string agentId, [FromQuery] string supervisorId)
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
                var result = await _businessManager.AssignSupervisorToAgent(agentId, supervisorId);
                return Ok(new Response<AgentModel>()
                {
                    Data = result,
                    Message = "Retrieved",
                    Succeeded = true,

                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        //retrieve agent by supervisor

        [HttpGet("user-profile")]
        public async Task<IActionResult> GetUserProfile([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _walletManager.GetSubAccount(query.UserId);
                return Ok(new Response<SubAccountModel>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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
        
        [HttpGet("agents-bysupervisor")]
        public async Task<IActionResult> AgentsBySupervisor([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.AgentsBySupervisor(query.UserId, query);
                return Ok(new PagedListResponse<List<AgentModel>>()
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

        #region dashboard


        #region agent
        [HttpGet("agent/users/summary")]
        public async Task<IActionResult> GetBusinessEntitiesMetricsAsyncByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetBusinessEntitiesMetricsAsyncByAgent(query);
                return Ok(new Response<BusinessEntitiesMetrics>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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

        #endregion agent
        //
        [HttpGet("warehouse/input-disbursement/summary")]
        public async Task<IActionResult> GetWarehouseInputTransit([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _warehouseManagementManager.GetWarehouseInputTransit(query.UserId, query);
                return Ok(new Response<List<WarehouseInputTransitModel>> ()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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

        //user summary metrics
        [HttpGet("users/summary")]
        public async Task<IActionResult> GetBusinessEntitiesMetricsAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetBusinessEntitiesMetricsAsync(query);
                return Ok(new Response<BusinessEntitiesMetrics>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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
        //bundle subscription summary metrics
        [HttpGet("bundle-subscription/summary")]
        public async Task<IActionResult> FarmerBundleSubscriptions([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.FarmerBundleSubscriptions(query);
                return Ok(new Response<List<SubscriptionData>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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
        [HttpGet("bundle-subscription-interest/analysis")]
        public async Task<IActionResult> GetBundleSubscriptionAnalysisByBusiness([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _inputFinancingManagement.GetBundleSubscriptionAnalysisByBusiness(query);
                return Ok(new Response<List<BundleSubscriptionSummary>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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

        [HttpGet("warehouse/stocks/summary")]
        public async Task<IActionResult> GetWarehouseSummaryByBusiness([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _warehouseManagementManager.GetWarehouseSummaryByBusiness(query.UserId, query);
                return Ok(new Response<List<WarehouseSummaryMetrics>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,                    
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

        [HttpGet("agent/warehouse/stocks/summary")]
        public async Task<IActionResult> StockSummaryByWarehouseByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _warehouseManagementManager.StockSummaryByWarehouseByAgent(query.UserId, query.businessCode);
                return Ok(new Response<List<WarehouseSummaryMetrics>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,                    
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
        [HttpGet("warehouse/stocks/summary/{id}")]
        public async Task<IActionResult> StockSummaryByWarehouseByAgent([FromQuery] ListQueryParams query, long id)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _warehouseManagementManager.StockSummaryByWarehouseId(query.UserId, id, query.businessCode);
                return Ok(new Response<List<WarehouseSummaryMetrics>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,                    
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

        #endregion dashboard
        //

        [HttpPost("assign-agent-role")]
   
        public async Task<IActionResult> MakeUserAgent([FromQuery] BusinessRegistrationArgs args)
        {
            try
            {
                
                var registerUser = await _businessManager.MakeUserAgent(args);
                
                //if (args.RoleName.Equals("Farmer"))
                //{
                //    var saveFarmerFace = await _businessManager.SaveFarmerImage(new ImageModel
                //    {
                //        FarmerUserId=registerUser.UserId,
                //        File=model.FarmerCapturedImage
                //    }, args);


                //}

                return Ok(new Response<UserModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = registerUser
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


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserModel model, [FromQuery] BusinessRegistrationArgs args)
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
                args.UserId = !string.IsNullOrEmpty(CurrentUserId) ? CurrentUserId : args.UserId;
                var registerUser = await _businessManager.CreateBusinessUser(model, args);
                if (registerUser != null && registerUser.SubAccountType != SubAccountType.Agent  )
                {
                  var wallet=  await _businessManager.UpdateUserAccountProfile(registerUser, args, args.UserId);
                }
                //if (args.RoleName.Equals("Farmer"))
                //{
                //    var saveFarmerFace = await _businessManager.SaveFarmerImage(new ImageModel
                //    {
                //        FarmerUserId=registerUser.UserId,
                //        File=model.FarmerCapturedImage
                //    }, args);


                //}

                return Ok(new Response<SubAccountModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = registerUser
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




        [HttpPost("update-user-profile")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateUserProfile([FromBody] SubAccountModel model, [FromQuery] BusinessRegistrationArgs args)
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
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;
                model.UserId = args.UserId;
                var update=  await _businessManager.UpdateUserAccountProfile(model, args, args.UserId);
                if (update.WalletCode != null)
                {
                    return Ok(new Response<SubAccountModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = true,
                        Message = "Successful",
                        Data = model
                    });

                }
                else
                {
                    return BadRequest(new Response<SubAccountModel>()
                    {
                        Code = ResponseCodes.Success,
                        Succeeded = false,
                        Message = "Could not update profile",
                        Data = model
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

        [HttpPost("upload-image")]
        [AllowAnonymous]
        public async Task<IActionResult> CaptureFarmersImage([FromForm] ImageModel model, [FromQuery] BusinessRegistrationArgs args)
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
               if(CurrentUserRole != "Farmer" && string.IsNullOrEmpty(model.FarmerUserId))
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
                if(CurrentUserRole == "Farmer")
                {
                    model.FarmerUserId = CurrentUserId;
                }
                
                var saveFarmerFace = await _businessManager.SaveFarmerImage(model, args);


                return Ok(new Response<object>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = saveFarmerFace
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

        [HttpPost("onboard-business")]
        public async Task<IActionResult> CreateMerchant([FromBody] BusinessModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            //  if (isAdmin == null || !isAdmin.Equals("Admin")) throw new Exception("Forbidden");


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
                args.UserId = CurrentUserId;
                var createMerchant = await _businessManager.CreateBusiness(model, args);

                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = $"{createMerchant.BusinessName} Successfully Onboarded"
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
        [HttpGet("wallet-details")] //accessed by agrovesto
        public async Task<IActionResult> RetrieveCustomerWallet([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.RetrieveCustomerWallet(query);
                return Ok(new Response<CafeOneCustomerWalletResponse>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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
       [HttpGet("staffs")] //accessed by agrovesto
        public async Task<IActionResult> GetWorkersByBusiness([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetStaffByBusinessCode(query.businessCode);
                return Ok(new PagedListResponse<List<BusinessWorkerModel>>()
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
     
        [HttpPost("create-staff")]
        public async Task<IActionResult> CreateBusinessWorker([FromBody] BusinessWorkerModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");


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
                args.UserId = CurrentUserId;
                var createWorker = await _businessManager.CreateBusinessWorkerAsync(model, args);

                return Ok(new Response<BusinessWorkerModel>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = createWorker
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
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] RoleModel roleModel)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");

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
                var data = await _roleManager.BusinesCreateRoleAsync(CurrentUserId, roleModel);
                return Ok(new AgrovestoResponse<RoleModel>()
                {
                    Message = "",
                    Result = data.Result,

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

        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] SubAccountModel model, [FromQuery] ParameterArgs args)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            args.UserId = CurrentUserId;
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
                
                var data = await _walletManager.UpdateBusinessProfile(model, args);
                return Ok(new AgrovestoResponse<SubAccountModel>()
                {
                    Message = "",
                    Result = data,

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
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || (!isAdmin.Equals("Business"))) return StatusCode(403, new Response<string>
            {
                Message = "Forbidden",//"An unexpected error occurred. Please contact your administrator.",
                Succeeded = false,
            });
            try
            {
                var data = await _roleManager.GetRolesByBusinessAsync(CurrentUserId);
                return Ok(new PagedListResponse<List<RoleModel>>()
                {
                    Message = "",
                    Data = data.Data,
                    Count = data.Count

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
        [HttpGet("business-staff")] //acessed by business
        public async Task<IActionResult> GetStaffsByBusinessCode([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetStaffbyBusiness(query.UserId, query);
                return Ok(new PagedListResponse<List<BusinessWorkerModel>>()
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
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgentsByBusinessCode([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetAgentsByBusinessCode(query.UserId, query);
                return Ok(new PagedListResponse<List<AgentModel>>()
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
      
        [HttpGet("businesses")]
        public async Task<IActionResult> GetBusinessesAsync([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetBusinessesAsync(query);
                return Ok(new PagedListResponse<List<BusinessModel>>()
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

        [HttpGet("cooperatives")]
        public async Task<IActionResult> GetCooperativesByBusinessCode([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetCooperativesByBusinessCode(query.UserId, query);
                return Ok(new PagedListResponse<List<CooperativeModel>>()
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
         
        [HttpGet("cooperatives-byagent")]
        public async Task<IActionResult> GetCooperativesByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetCooperativesByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<CooperativeModel>>()
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

        [HttpGet("farmer-image/{imageKey}")]
        public async Task<IActionResult> GetFarmerImage([FromQuery] ListQueryParams query, string imageKey)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetFarmerDetailWithImage(query, imageKey);
                return Ok(new Response<FarmerModel>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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

        [HttpGet("farmers")]
        public async Task<IActionResult> GetFarmersByBusinessCode([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetFarmersByBusinessCode(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerModel>>()
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

        [HttpGet("view-cooperative-assigned-agent")]
        public async Task<IActionResult> ViewAgentAssignedToCooperative([FromQuery] long cooperativeId)
        {
            try
            {
                var result = await _businessManager.ViewAgentAssignedToCooperative(cooperativeId);
                return Ok(new Response<AgentModel>()
                {
                    Data = result,
                    Message = "Retrieved",
                    Succeeded = true,

                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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
        public async Task<IActionResult> AssignAgentToCooperative([FromQuery] string agentId, [FromQuery] long cooperativeId)
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
                var result = await _businessManager.AssignCooperativeToAgent(agentId, cooperativeId);
                return Ok(new Response<CooperativeModel>()
                {
                    Data = result,
                    Message = "Retrieved",
                    Succeeded = true,

                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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
       
        [HttpGet("farmers-byagent")]
        public async Task<IActionResult> GetFarmersByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetFarmersByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerModel>>()
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

        [HttpGet("farmers-bycooperative")]
        public async Task<IActionResult> GetFarmersByCooperativeId([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetFarmersByCooperativeId(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerModel>>()
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

        [HttpPost("assign-farmer-to-cooperative")]
        public async Task<IActionResult> AssignFarmerToCooperative([FromQuery] long farmerId, [FromQuery] long cooperativeId)
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
                var result = await _businessManager.AssignFarmerToCooperative(farmerId, cooperativeId);
                return Ok(new Response<FarmerModel>()
                {
                    Data = result,
                    Message = "Assigned Successful",
                    Succeeded = true,

                    Code = Core.CoreUtils.Constants.ResponseCodes.Success
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

        [HttpGet("bank-account-details")]
        public async Task<IActionResult> GetUserBankAccountDetails([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetUserBankAccountDetails(query);
                return Ok(new Response<UserAccountDetailsModel>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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
     [HttpGet("bank-account-balance")]
        public async Task<IActionResult> GetUserBankAccountBalance([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetUserBankAccountBalance(query);
                return Ok(new Response<UserWalletBalanceModel>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
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
