using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Agrovesto.Backend.Core.Models.Common.Constants;
using System.ComponentModel.DataAnnotations;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.CoreUtils;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Core.Models.Payment;
using Microsoft.AspNetCore.Authorization;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/admin")]
    //[ApiController]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : BaseController
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IWalletProviderManager _walletProvider;
        private readonly IInputFinancingManagement _inputFinancingManagement;
        private readonly IPaymentProviderManager _providerManager;
        private readonly IReportManager _reportManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IBusinessManager _businessManager;
        public AdminController(IHttpContextAccessor httpContextAccessor,
            IWalletProviderManager walletProvider,
            IReportManager reportManager,
            IPaymentProviderManager providerManager,
            IAuthenticationRepository authenticationRepository,
            IInputFinancingManagement inputFinancingManagement,
            IBusinessManager businessManager,
            ISettingsManager settingsManager) : base(httpContextAccessor)
        {
            _reportManager = reportManager;
            _providerManager = providerManager;
            _authenticationRepository = authenticationRepository;
            _walletProvider = walletProvider;
            _inputFinancingManagement = inputFinancingManagement;
            _settingsManager = settingsManager;
            _businessManager = businessManager;
        }

        [AllowAnonymous]
        [HttpPost("create-location-type")]
        public async Task<IActionResult> LocationType([FromBody] LocationTypeModel model, [FromQuery] ListQueryParams args)
        {
            var isAdmin = CurrentUserRole;
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

                var type = await _businessManager.CreateLocationTypeAsync(model, args);

                return Ok(new Response<LocationTypeModel>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = "Successful",
                    Data = type
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

        [AllowAnonymous]
        [HttpPost("create-membership-type")]
        public async Task<IActionResult> MemberShipType([FromBody]MemberShipTypeModel model,[FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
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

                var type = await _settingsManager.CreateUpdateMemberShipType(model,args);

                    return Ok(new Response<(bool status, MemberShipTypeModel data)>()
                    {
                        Code = (type.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                        Succeeded = true,
                        Message = "Successful",
                        Data = type
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


        #region FarmerActivities
        [HttpGet("overview")]
        public async Task<IActionResult> GetCoreBusinessMetrics()
        {
            try
            {
                var isAdmin = CurrentUserRole;
                 if (isAdmin == null  || !isAdmin.Equals("Business")) throw new Exception("Forbidden");

                var result = await _reportManager.GetCoreBusinessMetrics();
                return Ok(new Response<AdminBusinessEntitiesMetrics>()
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

        
        #endregion FarmerActivities

        [HttpPost("input-subscription-settlement")]
        public async Task<IActionResult> SettleBusiness([FromQuery] BusinessRegistrationArgs args)
        {
            var isAdmin = CurrentUserRole;
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

                var bundle = await _inputFinancingManagement.SettlePendingFarmerBundleSubscriptionEquityToBusiness(args);
                if (bundle.status)
                {
                    return Ok(new Response<(bool status, BundleSubscriptionInterestModel data)>()
                    {
                        Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                        Succeeded = true,
                        Message = "Successful",
                        Data = bundle
                    });

                }
                else
                {
                    return Ok(new Response<(bool status, BundleSubscriptionInterestModel data)>()
                    {
                        Code = (bundle.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                        Succeeded = false,
                        Message = "Please try again later",
                        Data = bundle
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

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdminUser([FromBody] UserModel model)
        {
            //var isAdmin = User.Claims.ToArray().IsAdmin();
            //if (!isAdmin) throw new BadRequestException("Not Authorized");

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
                var createUser = await _authenticationRepository.RegisterAdmin(model, model.UserType);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
                    Data = createUser
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

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateAgrovestoUser([FromBody] UserModel model, [FromQuery] string roleName)
        {
            var isAdmin = CurrentUserRole;
            if (isAdmin == null || !isAdmin.Equals("Super Admin")) throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Forbidden"); 


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
                var createUser = await _authenticationRepository.RegisterAdmin(model, roleName);

                return Ok(new Response<(UserModel model, bool status, string message)>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = createUser.message,
                    Data = createUser
                });


            }
            catch (BaseException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<string>
                {
                    Message = ex.Message,//"An unexpected error occurred. Please contact your administrator.",
                    Succeeded = false,
                });
            }
        }
        [HttpGet("users")]

        public async Task<IActionResult> GetUsers([FromQuery] ListQueryParams query)
        {
            try
            {

                var result = await _authenticationRepository.GetUsersAsync();
                return Ok(new PagedListResponse<List<UserViewModel>>()
                {
                    Data = result.model,
                    Message = result.message,
                    Succeeded = result.status,
                    Count = result.model.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.status && result.model != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
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



        #region wallet
        [HttpPost("wallet")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]
        public async Task<IActionResult> CreateWalletProvider([FromBody] WalletProviderModel model)
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
                return Ok(new Response<WalletProviderModel>
                {
                    Succeeded = true,
                    Data = await _walletProvider.CreateProvider(model)
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
        //[Authorize(Roles = "Admin")]

        [HttpGet("wallets")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetAllWallets()
        {
            var result = await _walletProvider.GetAll();
            return Ok(new Response<WalletProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }

        [HttpGet("wallets/{id}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetById(long id)
        {
            var result = await _walletProvider.Get(id);
            return Ok(new Response<WalletProviderModel>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Disables provider
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
      //  [Authorize(Roles = "Admin")]
        [HttpPut("wallet/{id}/deactivate")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> DisableWalletProvider(long id)
        {
            bool result = await _walletProvider.DisableProvider(id);
            return Ok(new Response<object>()
            {
                Succeeded = result,
                Message = result ? "Provider has been disabled successfully" : "An error occur while processing your request, Please try again"
            });
        }

        [HttpPut("wallet/{id}/activate")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> EnableWalletProvider(long id)
        {
            bool result = await _walletProvider.EnableProvider(id);
            return Ok(new Response<object>()
            {
                Succeeded = result,
                Message = result ? "Provider has been disabled successfully" : "An error occur while processing your request, Please try again"
            });
        }
        [HttpGet("wallet/parameters/{id}/{Env}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetWalletParameters(long id, string Env)
        {
            var result = await _walletProvider.GetParametersByProvider(id, Env);
            return Ok(new Response<WalletProviderParameterModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("wallet/parameter/{id}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetWalletParameter(long id)
        {
            var result = await _walletProvider.GetParametersById(id);
            return Ok(new Response<WalletProviderParameterModel>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Updates parameter
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //  [Authorize(Roles = "Admin")]

        [HttpPost("wallet/parameters")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 422)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]
        public async Task<IActionResult> UpdateWalletParameters([FromBody] WalletProviderModel model)
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
                var result = await _walletProvider.UpdateProviderParameter(model);


            return Ok(new Response<WalletProviderModel>()
            {
                Succeeded = true,
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        [HttpPost("wallet/parameter/edit")]
        [ProducesResponseType(typeof(Response<WalletProviderParameterModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderParameterModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderParameterModel>), 422)]
        [ProducesResponseType(typeof(Response<WalletProviderParameterModel>), 500)]
        public async Task<IActionResult> EditParameterValue([FromBody] WalletProviderParameterModel model)
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
                var result = await _walletProvider.EditParameterValue(model);


                return Ok(new Response<WalletProviderParameterModel>()
                {
                    Succeeded = true,
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
                    Message = ex.Message,
                    Succeeded = false,
                });
            }
        }

        #endregion

        #region payment
        /// <summary>
        /// Gets all provider
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        // [Authorize(Policy = "MANAGE_PROVIDER")]
        [HttpGet("payment/all")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllProviders([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {

            var result = await _providerManager.Get();
            return Ok(new Response<PaymentProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Creates provider
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // [Authorize(Policy = "MANAGE_PROVIDER")]
        [HttpPost("payment")]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 500)]
        public async Task<IActionResult> CreateProvider([FromBody] PaymentProviderModel model)
        {
            return Ok(new Response<PaymentProviderModel>
            {
                Succeeded = true,
                Data = await _providerManager.CreateProvider(model)
            });
        }
        /// <summary>
        /// Updates provider
        /// </summary>
        /// <param name="providerModel"></param>
        /// <returns></returns>
       // [Authorize(Policy = "MANAGE_PROVIDER")]
        [HttpPut("payment/{id}")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]

        public async Task<IActionResult> UpdateProvider([FromBody] PaymentProviderModel providerModel)
        {
            PaymentProviderModel result = await _providerManager.UpdateProvider(providerModel);
            return Ok(new Response<object>()
            {
                Succeeded = true,
                Message = "Provider has been updated successfully",
                Data = result
            });
        }

        /// <summary>
        /// Gets parameters 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[Authorize(Roles = "Admin")]
        [HttpGet("payment/parameters/{id}/{Env}")]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 500)]

        public async Task<IActionResult> GetParameters(long id, string Env)
        {
            var result = await _providerManager.GetParametersByProvider(id, Env);
            return Ok(new Response<ProviderParameterModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Updates parameter
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //  [Authorize(Roles = "Admin")]

        [HttpPost("payment/parameters")]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 422)]
        [ProducesResponseType(typeof(Response<PaymentProviderModel>), 500)]
        public async Task<IActionResult> UpdateParameters([FromBody] PaymentProviderModel model)
        {

            var result = await _providerManager.UpdateProviderParameter(model);


            return Ok(new Response<PaymentProviderModel>()
            {
                Succeeded = true,
                Data = result
            });

        }

        /// <summary>
        /// Disables provider
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
      //  [Authorize(Roles = "Admin")]
        [HttpPut("payment/{id}/disable")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> DisableProvider(long id)
        {
            bool result = await _providerManager.DisableProvider(id);
            return Ok(new Response<object>()
            {
                Succeeded = result,
                Message = result ? "Provider has been disabled successfully" : "An error occur while processing your request, Please try again"
            });
        }

        #endregion payment

        #region OrganizationMgt
        [HttpGet("organization-mgt-overview")]

        public async Task<IActionResult> GetOrganizationManagementOverview([FromQuery] ListQueryParams query)
        {
            try
            {
                var overview = await _businessManager.GetBusinessDashboardAsync(query);
                return Ok(new Response<BusinessOrganizationDashboardResponse>()
                {
                    Code = (overview != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,

                    Succeeded = true,
                    Message = "Successful",
                    Data = overview
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


        [HttpGet("registered-organizations")] //accessed by agrovesto
        public async Task<IActionResult> GetBusinessOrganization([FromQuery] ListQueryParams query)
        {
            try
            {
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _businessManager.GetBusinessOrganizationAsync(query.businessCode);
                return Ok(new PagedListResponse<List<OrganizationModel>>()
                {
                    Data = result,
                    Message = "Successful",
                    Succeeded = true,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
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

        [AllowAnonymous]
        [HttpPost("register-organization")]
        public async Task<IActionResult> CreateOrganization([FromBody] OrganizationModel model, [FromQuery] BusinessRegistrationArgs args)
        {

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
                var isAdmin = CurrentUserRole;

                var createMerchant = await _businessManager.CreateOrganization(model, args);

                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = "Successful",
                    Data = $"{createMerchant.OrganizationName} Successfully Onboarded"
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

        #endregion OrganizationMgt
    }
}
