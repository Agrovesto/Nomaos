using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using static Agrovesto.Backend.Core.Models.Common.Constants;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/admin")]
    [Authorize]
    //[ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : BaseController
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IWalletProviderManager _walletProvider;
        private readonly IInputFinancingManagement _inputFinancingManagement;
        private readonly IPaymentProviderManager _providerManager;
        private readonly IReportManager _reportManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IBusinessManager _businessManager;
        private readonly IAWSService _aWSService;
        private readonly IStorageCategoryRepository _storageCategoryRepository;
        private readonly ITemperatureControlType _temperatureControlType;
        private readonly IModuleRepository _moduleRepository;
        private readonly IOrganizationManager _organizationManager;
        private readonly IInputFinancingRepository _inputFinancingRepository;
        private readonly ISMSProviderRepository _sMSProviderRepository;
        public AdminController(IHttpContextAccessor httpContextAccessor,
            IWalletProviderManager walletProvider,
            IReportManager reportManager,
            IPaymentProviderManager providerManager,
            IAuthenticationRepository authenticationRepository,
            IInputFinancingManagement inputFinancingManagement,
            IBusinessManager businessManager,
            ISettingsManager settingsManager,
            IAWSService aWSService,
            IStorageCategoryRepository storageCategoryRepository,
            ITemperatureControlType temperatureControlType,
            IModuleRepository moduleRepository,
            IOrganizationManager organizationManager,
            IInputFinancingRepository inputFinancingRepository,
            ISMSProviderRepository sMSProviderRepository
            ) : base(httpContextAccessor)
        {
            _reportManager = reportManager;
            _providerManager = providerManager;
            _authenticationRepository = authenticationRepository;
            _walletProvider = walletProvider;
            _storageCategoryRepository = storageCategoryRepository;
            _inputFinancingManagement = inputFinancingManagement;
            _settingsManager = settingsManager;
            _businessManager = businessManager;
            _aWSService = aWSService;
            _temperatureControlType = temperatureControlType;
            _moduleRepository = moduleRepository;
            _organizationManager = organizationManager;
            _inputFinancingRepository = inputFinancingRepository;
            _sMSProviderRepository = sMSProviderRepository;
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
                    Succeeded =true,
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


        [HttpGet("inputfinancing-categories")]
        public async Task<IActionResult> GetInputFinancingCategory([FromQuery] ListQueryParams args)
        {
            try
            {

                var type = await _inputFinancingRepository.GetInputFinancingCategories(args);
                return Ok(new Response<List<ProductCategoryDto>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = type.Data
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

        [HttpPost("inputfinancing-category")]
        public async Task<IActionResult> CreateInputFinancingCategory([FromBody] ProductCategoryDto model,

        [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                model.BusinessCode = args.businessCode;
                await _inputFinancingRepository.CreateInputFinancingCategory(model);

                return Ok(new Response<ProductCategoryDto>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = model
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




        [HttpGet("organization-product-categories")]
        public async Task<IActionResult> OrganizationProductCategory([FromQuery] ListQueryParams args)
        {
            try
            {

                var type = await _organizationManager.GetCategories(args);
                return Ok(new Response<List<ProductCategoryDto>>()
                {
                    Code = (type != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = type.Data
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

        [HttpPost("organization-product-category")]
        public async Task<IActionResult> CreateProductCategoryAsync([FromBody] ProductCategoryDto model,

        [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                model.BusinessCode = args.businessCode;
                await _organizationManager.CreateProductCategoryAsync(model, args);

                return Ok(new Response<ProductCategoryDto>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = model
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

        [HttpPost("create-email-template")]
        public async Task<IActionResult> EmailTemplate()
        {

            try
            {

                await _aWSService.CreateTemplateAsync();

                return Ok(new Response<bool>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = true
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
        [HttpPost("create-storage-category")]
        public async Task<IActionResult> StorageCategory([FromBody] StorageCategory model, [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                model.CreatedByUserId = CurrentUserId;
                model.BusinessCode = args.businessCode;
                await _storageCategoryRepository.AddAsync(model);

                return Ok(new Response<StorageCategory>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = model
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

        [HttpPost("create-temperature-control-type")]
        public async Task<IActionResult> TemperaturControlType([FromBody] TemperatureControlType model, [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                model.CreatedByUserId = CurrentUserId;
                model.BusinessCode = args.businessCode;
                await _temperatureControlType.AddAsync(model);

                return Ok(new Response<TemperatureControlType>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = model
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

        [HttpPost("create-system-module")]
        public async Task<IActionResult> CreateSystemModules([FromBody] Module model, [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                model.CreatedByUserId = CurrentUserId;
                model.BusinessCode = args.businessCode;
                await _moduleRepository.AddAsync(model);

                return Ok(new Response<Module>()
                {
                    Code = Core.CoreUtils.Constants.ResponseCodes.Success,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
                    Data = model
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

        [HttpPost("create-location-type")]
        public async Task<IActionResult> LocationType([FromBody] LocationTypeModel model, [FromQuery] ListQueryParams args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
                    Message = ResponseMessages.Successful,
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

        [HttpPost("create-membership-type")]
        public async Task<IActionResult> MemberShipType([FromBody] MemberShipTypeModel model, [FromQuery] BusinessRegistrationArgs args)
        {
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                args.UserId = !string.IsNullOrEmpty(args.UserId) ? args.UserId : CurrentUserId;

                var type = await _settingsManager.CreateUpdateMemberShipType(model, args);

                return Ok(new Response<(bool status, MemberShipTypeModel data)>()
                {
                    Code = (type.data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed,


                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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
            
            // if (isAdmin == null || (!isAdmin.Equals("Business"))) throw new Exception("Forbidden");
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
                        Message = ResponseMessages.Successful,
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
        [Authorize(Roles = "SystemAdmin,Super Admin")]
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
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
        [Authorize(Roles = "SystemAdmin,Super Admin")]
        public async Task<IActionResult> CreateAgrovestoUser([FromBody] UserModel model, [FromQuery] string roleName)
        {
            
            

            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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

        public async Task<IActionResult> GetAllWallets([FromQuery] string businessCode)
        {
            var result = await _walletProvider.GetAll();
            return Ok(new Response<WalletProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("wallets/{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute][Range(1, long.MaxValue, ErrorMessage = "id must be a positive number.")] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

            var result = await _walletProvider.Get(id);
            return Ok(new Response<WalletProviderModel> { Succeeded = true, Data = result });
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
        public async Task<IActionResult> DisableWalletProvider([FromRoute][Range(1, long.MaxValue, ErrorMessage = "id must be a positive number.")] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
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
        public async Task<IActionResult> EnableWalletProvider([FromRoute][Range(1, long.MaxValue, ErrorMessage = "id must be a positive number.")] long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            bool result = await _walletProvider.EnableProvider(id);
            return Ok(new Response<object>()
            {
                Succeeded = result,
                Message = result ? "Provider has been activated successfully" : "An error occur while processing your request, Please try again"
            });
        }
        [HttpPut("enable-wallet-provider/{businessCode}")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> EnableWalletProvider([FromRoute][Required(ErrorMessage = "business code is required.")] string businessCode, [FromQuery]long id,
            [FromBody] WalletProviderParameterModel[]? model, [FromQuery] bool isDefault =false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            var result = await _walletProvider.EnableProviderForBusiness(businessCode, id, model, isDefault);
            return Ok(new Response<BusinessWalletProviderModel>()
            {
                Succeeded = true,
                Message = "Provider enabled successfully"
            });
        }
        [HttpGet("wallet/parameters/{id}/{Env}")]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<WalletProviderModel>), 500)]

        public async Task<IActionResult> GetWalletParameters([FromRoute][Range(1, long.MaxValue, ErrorMessage = "id must be a positive number.")] long id, [FromRoute][Required(ErrorMessage = "Environment is required.")] string Env)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<Dictionary<string, string>>
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }

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
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
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
        [HttpPost("wallet/business-parameter/edit")]
        [ProducesResponseType(typeof(Response<BusinessWalletProviderParameterModel>), 200)]
        [ProducesResponseType(typeof(Response<BusinessWalletProviderParameterModel>), 400)]
        [ProducesResponseType(typeof(Response<BusinessWalletProviderParameterModel>), 422)]
        [ProducesResponseType(typeof(Response<BusinessWalletProviderParameterModel>), 500)]
        public async Task<IActionResult> EditParameterForBusiness([FromBody] BusinessWalletProviderParameterModel model, [FromQuery]string businessCode)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                
                var result = await _walletProvider.EditBusinessParameterValue(model);


                return Ok(new Response<BusinessWalletProviderParameterModel>()
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
                    Message = ResponseMessages.Successful,
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
                    Message = ResponseMessages.Successful,
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
                

                var createMerchant = await _businessManager.CreateOrganization(model, args);

                return Ok(new Response<string>()
                {
                    Code = ResponseCodes.Success,
                    Succeeded = true,
                    Message = ResponseMessages.Successful,
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


        #region sms
        [HttpPost("sms/create-provider")]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 500)]
        public async Task<IActionResult> CreateSMSProvier([FromBody] SMSProviderModel model)
        {
            if (!ModelState.IsValid)
            {

                return BadRequest(new Response<Dictionary<string, string>>()
                {
                    Message = "Validation failed.",
                    Succeeded = false,
                    Data = ModelState
                        .Where(ms => ms.Value!.Errors.Any())
                        .ToDictionary(
                            ms => ms.Key,
                            ms => string.Join("; ", ms.Value!.Errors.Select(e => e.ErrorMessage))
                        )
                });
            }
            try
            {
                return Ok(new Response<SMSProviderModel>
                {
                    Succeeded = true,
                    Data = await _sMSProviderRepository.CreateProvider(model)
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

        [HttpGet("sms/get-providers")]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 500)]

        public async Task<IActionResult> GetAllSMSProviders([FromQuery] string businessCode)
        {
            var result = await _sMSProviderRepository.GetAll();
            return Ok(new Response<SMSProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("sms/get-providers-by-business/{businessCode}")]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 500)]

        public async Task<IActionResult> GetProvidersForBusiness(string businessCode)
        {
            var result = await _sMSProviderRepository.GetProvidersForBusiness(businessCode);
            return Ok(new Response<BusinessSMSProviderModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }

        [HttpGet("sms/provider-by-id/{id}")]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 500)]

        public async Task<IActionResult> GetSMSProviderById(long id)
        {
            var result = await _sMSProviderRepository.Get(id);
            return Ok(new Response<SMSProviderModel>()
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
        [HttpPut("sms/{businessCode}/disable/business")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> DisableProviderForBusiness(string businessCode, [FromQuery]long id)
        {
            bool result = await _sMSProviderRepository.DisableProviderForBusiness(businessCode,id);
            return Ok(new Response<object>()
            {
                Succeeded = result,
                Message = result ? "Provider has been disabled successfully" : "An error occur while processing your request, Please try again"
            });
        }

        [HttpPut("sms/{businessCode}/activate/business")]
        [ProducesResponseType(typeof(Response<object>), 200)]
        [ProducesResponseType(typeof(Response<object>), 400)]
        [ProducesResponseType(typeof(Response<object>), 422)]
        [ProducesResponseType(typeof(Response<object>), 500)]
        public async Task<IActionResult> EnableProviderForBusiness(string businessCode, [FromQuery]long id,
            [FromBody] SMSProviderParameterModel[]? overrideParameters, [FromQuery]bool isDefault=false)
        {
            var result = await _sMSProviderRepository.EnableProviderForBusiness(businessCode, id, overrideParameters, isDefault);
            return Ok(new Response<BusinessSMSProviderModel>()
            {
                Succeeded = true,
                Message = "Provider has been activated successfully" 
            });
        }
     
        [HttpGet("sms/parameters/{id}/{Env}")]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 500)]

        public async Task<IActionResult> GetParametersByProvider(long id, string Env)
        {
            var result = await _sMSProviderRepository.GetParametersByProvider(id, Env);
            return Ok(new Response<SMSProviderParameterModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("sms/parameters/{id}/{Env}/{businessCode}")]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 500)]

        public async Task<IActionResult> GetParametersByProvider(long id, string Env, string businessCode)
        {
            var result = await _sMSProviderRepository.GetParametersByProvider(id, businessCode,Env);
            return Ok(new Response<SMSProviderParameterModel[]>()
            {
                Succeeded = true,
                Data = result
            });

        }
        [HttpGet("sms/parameter/{id}/{businessCode}")]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderParameterModel>), 500)]

        public async Task<IActionResult> EditBusinessParameterValue(long id, string businessCode, [FromBody] BusinessSMSProviderParameterModel model)
        {model.ParameterId= id;
            
            
            var result = await _sMSProviderRepository.EditBusinessParameterValue(model);
            return Ok(new Response<BusinessSMSProviderParameterModel>()
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

        [HttpPost("sms/update-provider-parameters")]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 200)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 400)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 422)]
        [ProducesResponseType(typeof(Response<SMSProviderModel>), 500)]
        public async Task<IActionResult> UpdateProviderParameter([FromBody] SMSProviderModel model)
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
                var result = await _sMSProviderRepository.UpdateProviderParameter(model);


                return Ok(new Response<SMSProviderModel>()
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

    }
}
