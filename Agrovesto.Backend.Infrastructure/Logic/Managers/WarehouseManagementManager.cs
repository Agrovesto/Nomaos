using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class WarehouseManagementManager : IWarehouseManagementManager
    {
        private readonly IWarehouseManagementRepository _repository;
        private readonly IAgentRepository _agentRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly ILogger<WarehouseManagementManager> _log;
        public WarehouseManagementManager(IWarehouseManagementRepository repository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, ILogger<WarehouseManagementManager> log, IAgentRepository agentRepository)
        {
            _repository = repository;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _log = log;
            _agentRepository = agentRepository;
        }

        #region warehousemgt
        public async Task<Response<List<WarehouseInputTransitModel>>> GetWarehouseInputTransit(string userId, ListQueryParams query)
        {
            {
                try
                {
                    var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                    if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                    {
                        business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                    }
                    if (business == null)
                    {
                        throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                    }

                    var warehouse = await _repository.GetWarehouseInputTransit(business.BusinessCode);
                    if (warehouse == null) return new Response<List<WarehouseInputTransitModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Message = "No Record Found"
                    };
                    return new Response<List<WarehouseInputTransitModel>>
                    {
                        Succeeded = true,
                        Data = warehouse,
                        Message = "Retrieved Successfully"
                    };
                }
                catch (BaseException ex)
                {
                    _log.LogError($"Error retrieving Inventory: {ex.Message}");

                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
                }
                catch (Exception ex)
                {
                    throw new NotFoundException($"No Record Found");
                }
            }
        }
        public async Task<Response<List<WarehouseSummaryMetrics>>> GetWarehouseSummaryByBusiness(string userId, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

                var warehouse = await _repository.GetWarehouseSummaryByBusiness(business.BusinessCode);
                if (warehouse == null) return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<List<WarehouseSummaryMetrics>>> StockSummaryByWarehouseByAgent(string userId, string businessCode)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehouse = await _repository.StockSummaryByWarehouseByAgent(business.BusinessCode, userId);
                if (warehouse == null) return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<List<WarehouseSummaryMetrics>>> StockSummaryByWarehouseId(string userId, long warehouseId, string businessCode)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(businessCode);


                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehouse = await _repository.StockSummaryByWarehouse(business.BusinessCode, warehouseId);
                if (warehouse == null) return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<WarehouseSummaryMetrics>>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            (bool status, WarehouseModel data) createWarehouse = new(false, new WarehouseModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    model.BelongToBusinessCode = args.businessCode;
                    model.CreatedOn = DateTimeOffset.Now;

                    var warehouse = await _repository.GetWarehousesByBusiness(model.BelongToBusinessCode);
                    if (warehouse.Any(x => x.Name == model.Name && x.Lga == model.Lga && x.State == model.State)) throw new Exception($"A Warehouse with same name exist in {model.Location} {model.Lga} {model.State}");

                    createWarehouse = await _repository.CreateWarehouse(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouse;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }


        public async Task<(bool status, WarehouseModel data)> UpdateWarehouse(WarehouseModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            if (model.Id == 0) throw new Exception("Warehouse to be updated needs to be selected");
            (bool status, WarehouseModel data) createWarehouse = new(false, new WarehouseModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    model.BelongToBusinessCode = args.businessCode;

                    createWarehouse = await _repository.UpdateWarehouse(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouse;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }


        public async Task<PagedListResponse<List<WarehouseModel>>> GetWarehousesByAgent(string userId,
           ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);
                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found for Agent");
                var warehouse = await _repository.GetWarehousesByAgent(business.BusinessCode, userId);
                if (warehouse == null) return new PagedListResponse<List<WarehouseModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<WarehouseModel>>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Count = warehouse.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<WarehouseModel>>> GetWarehousesByBusiness(string userId,
              ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }
                var warehouse = await _repository.GetWarehousesByBusiness(business.BusinessCode);
                if (warehouse == null) return new PagedListResponse<List<WarehouseModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<WarehouseModel>>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Count = warehouse.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<Response<WarehouseModel>> GetWarehouseByBusiness(string userId, long warehouseId)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehouse = await _repository.GetWarehouseByBusiness(business.BusinessCode, warehouseId);
                if (warehouse == null) return new Response<WarehouseModel>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<WarehouseModel>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<(bool status, WarehouseUserModel data)> CreateWarehouseManager(WarehouseUserModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            (bool status, WarehouseUserModel data) createWarehouseManager = new(false, new WarehouseUserModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var warehouse = await _repository.GetWarehouseByBusiness(business.BusinessCode, args.WarehouseId);
                    if (warehouse == null) throw new Exception("Could not retrieve warehouse");
                    model.BelongToBusinessCode = args.businessCode;
                    model.WarehouseId = warehouse.Id;
                    model.UserId = args.UserId;
                    //model.c = DateTimeOffset.Now;
                    var agent = await _agentRepository.GetAgentDetailByUserId(args.UserId);
                    if (agent == null) throw new Exception("Could not retrieve agent");

                    var manager = await _repository.GetWarehousesManagerByBusiness(model.BelongToBusinessCode);
                    if (manager.Any(x => x.Name == agent.Name && x.Lga == agent.Lga && x.State == agent.State && x.PhoneNumber == agent.PhoneNumber)) throw new Exception($"A Warehouse manager with same name exist in {model.Location} {model.Lga} {model.State}");


                    createWarehouseManager = await _repository.CreateWarehouseManager(model);

                    warehouse.HasbeenAssignedManager = true;
                    warehouse.ManagedByUserId = args.UserId;
                    await _repository.UpdateWarehouse(warehouse);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouseManager;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouseManager;
        }
        public async Task<(bool status, WarehouseUserModel data)> UpdateWarehouseManager(WarehouseUserModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            if (model.Id == 0) throw new Exception("Warehouse to be updated needs to be selected");
            (bool status, WarehouseUserModel data) updateWarehouseManager = new(false, new WarehouseUserModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    model.BelongToBusinessCode = args.businessCode;

                    updateWarehouseManager = await _repository.UpdateWarehouseManager(model);



                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return updateWarehouseManager;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return updateWarehouseManager;
        }


        public async Task<PagedListResponse<List<WarehouseUserModel>>> GetWarehousesManagerByBusiness(string userId,
           ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehousemanager = await _repository.GetWarehousesManagerByBusiness(business.BusinessCode);
                if (warehousemanager == null) return new PagedListResponse<List<WarehouseUserModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<WarehouseUserModel>>
                {
                    Succeeded = true,
                    Data = warehousemanager,
                    Count = warehousemanager.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<PagedListResponse<List<WarehouseUserModel>>> GetWarehousesManagersByBusiness(string userId, long warehouseId,
          ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehousemanager = await _repository.GetWarehousesManagersByBusiness(business.BusinessCode, warehouseId);
                if (warehousemanager == null) return new PagedListResponse<List<WarehouseUserModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<WarehouseUserModel>>
                {
                    Succeeded = true,
                    Data = warehousemanager,
                    Count = warehousemanager.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<WarehouseUserModel>> GetWarehouseManagerByBusiness(string userId, long warehouseId)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var warehouse = await _repository.GetWarehouseManagerByBusiness(business.BusinessCode, warehouseId);
                if (warehouse == null) return new Response<WarehouseUserModel>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<WarehouseUserModel>
                {
                    Succeeded = true,
                    Data = warehouse,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        #endregion warehouseMgt


        #region inputmgt
        private string GenerateReference(string businessCode)
        {
            return $"{businessCode}{GenerateRandomDigits(4)}{1}";
        }
        public string GenerateRandomDigits(int length)
        {
            string mct = Regex.Replace(Guid.NewGuid().ToString(), "[^1-9]", "");

            while (mct.Length < length)
            {
                mct += Regex.Replace(Guid.NewGuid().ToString(), "[^1-9]", "");
            }

            return mct.Substring(0, length);
        }
        public async Task<WarehouseInputInventoryModel> StockUpInput(WarehouseInputInventoryModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            WarehouseInputInventoryModel createWarehouse = new WarehouseInputInventoryModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var warehouse = await _repository.GetWarehouseManagerByBusiness(business.BusinessCode, args.WarehouseId);
                    if (warehouse == null) { throw new Exception("Warehouse could not be found"); }
                    model.BelongToBusinessCode = args.businessCode;
                    model.WarehouseId = args.WarehouseId;
                    model.ManagedByUserId = warehouse.UserId;
                    var input = await _repository.GetInputByWarehouse(model.BelongToBusinessCode, args.WarehouseId);

                    if (input != null && (input.Exists(x => x.InputName == model.InputName && x.Brand == model.Brand))) { throw new Exception($"The warehouse already have same stock of input"); }

                    model.InputComingFrom = model.InputComingFrom;
                    model.DeliveredTo = warehouse.Location;
                    model.Status = InputTransitStatus.Packed;
                    model.Reference = GenerateReference(business.BusinessCode);
                    createWarehouse = await _repository.CreateAsync(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouse;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }
        public async Task<InputDeliveryTrackerModel> ConfirmInputShipment(InputDeliveryTrackerModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            InputDeliveryTrackerModel createWarehouse = new InputDeliveryTrackerModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    //WarehouseInputInventoryModel
                    var warehouse = await _repository.GetWarehouseManagerByBusiness(business.BusinessCode, args.WarehouseId) ?? throw new Exception("Warehouse could not be found");
                    var user = await _authenticationRepository.GetUserById(warehouse.UserId);
                    var inputName = await _repository.GetInputBYIdAsync(business.BusinessCode, model.WarehouseInputInventoryId) ?? throw new Exception("Could not retrive input");
                    model.BusinessCode = business.BusinessCode;
                    model.InputName = inputName.InputName;
                    model.Reference = inputName.Reference;
                    model.WarehouseId = warehouse.Id;
                    model.WarehouseInputInventoryId = model.WarehouseInputInventoryId;
                    var logDelivery = await _repository.LogDeliveryTracker(model);

                    var status = Enum.GetName(typeof(InputTransitStatus), model.Status);

                    var ShipmentDate = DateTimeOffset.Now;
                    var update = await _repository.UpdateInputReceiptStatus(business.BusinessCode, model.WarehouseInputInventoryId, status, "", ShipmentDate, 0);

                    if (!update) throw new Exception("No record found for input");

                    model.Email = user.model.Email;

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    //send mail and sms
                    SendStockDisbursementMail(model, args, business);
                    return model;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }

        private static void SendStockDisbursementMail(InputDeliveryTrackerModel bWorker, BusinessRegistrationArgs args, BusinessModel business)
        {
            var propagationUrl = AppSettingsHelper.Setting("propagation:url");
            var mailRequest = new StockMailBody
            {
                To = bWorker.Email,
                ToName = $"{bWorker.Email}",
                Subject = $"Warehouse Stock delivery {business.BusinessName}",
                From = business.BusinessEmail,
                FromName = business.BusinessName,
                Reference = bWorker.Reference,
                DeliveredBy = bWorker.DeliveredBy,
                PhoneNumber = bWorker.DeliveryContact,
                Quantity = bWorker.QuantityLoaded.ToString(),
                input = bWorker.InputName,
                BusinessName = business.BusinessName
            };
            var mail = SendGridService.DynamicStockInputDelivery(mailRequest);
        }

        public async Task<InputDeliveryTrackerModel> ConfirmReceiptOfInput(InputDeliveryTrackerModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            InputDeliveryTrackerModel createWarehouse = new InputDeliveryTrackerModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var warehouse = await _repository.GetWarehouseManagerByBusiness(business.BusinessCode, args.WarehouseId) ?? throw new Exception("Warehouse could not be found");

                    var tracker = await _repository.GetInputTracker(model.Reference) ?? throw new Exception("record not found");

                    if ((model.QuantityDelivered != tracker.QuantityLoaded) || (model.QuantityReceived != tracker.QuantityLoaded))
                    {
                        //send a mail and end the transaction

                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Kindly contact sender to reconcile disparity in Quantity");
                    }
                    var status = Enum.GetName(typeof(InputTransitStatus), model.Status);
                    model.Reference = tracker.Reference;
                    model.BusinessCode = business.BusinessCode;
                    var logDelivery = await _repository.LogDeliveryTracker(model);
                    var update = await _repository.UpdateInputReceiptStatus(business.BusinessCode, tracker.WarehouseInputInventoryId, status,
                        model.DeliveredTo, DateTimeOffset.Now, model.QuantityReceived);
                    if (!update) throw new Exception("No record found for input");



                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return model;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }
        public async Task<bool> AddMoreInputToStock(InputUpdateModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var createWarehouse = false;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {


                    createWarehouse = await _repository.AddMoreAsync(model.InputId, model.UnitsToAdd);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouse;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }


        public async Task<bool> DepleteFromInput(InputUpdateModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var createWarehouse = false;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {


                    createWarehouse = await _repository.DepleteAsync(model.InputId, model.UnitsToRemove);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createWarehouse;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createWarehouse;
        }


        public async Task<PagedListResponse<List<WarehouseInputInventoryModel>>> GetInputByWarehouse(string userId,
        ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var inputs = await _repository.GetInputByWarehouse(business.BusinessCode, query.warehouseId);
                if (inputs == null) return new PagedListResponse<List<WarehouseInputInventoryModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<WarehouseInputInventoryModel>>
                {
                    Succeeded = true,
                    Data = inputs,
                    Count = inputs.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<WarehouseInputInventoryModel>> GetInput(string userId, long warehouseId)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var input = await _repository.GetInputBYIdAsync(business.BusinessCode, warehouseId);
                if (input == null) return new Response<WarehouseInputInventoryModel>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<WarehouseInputInventoryModel>
                {
                    Succeeded = true,
                    Data = input,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        #endregion inputmgt

        #region bundlemgt
        public async Task<BundleModel> UpdateBundleEquity(BundleModel model, BusinessRegistrationArgs args)
        {

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var updateBundle = new BundleModel(); ;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate that each item has a valid WarehouseInputInventoryId
                    model.BelongToBusinessCode = args.businessCode;
                    updateBundle = await _repository.UpdateBundleEquitPrice(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return updateBundle;

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return updateBundle;
        }



        public async Task<BundleModel> CreateBundleAsync(BundleModel model, BusinessRegistrationArgs args)
        {
            if (model.BundlePackage == null || model.BundlePackage.Count == 0)
                throw new ArgumentException("Bundle must contain at least one item.");

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var createBundle = new BundleModel();
            var strategy = _context.Database.CreateExecutionStrategy();
            var totalOtherCost = 0m;
            var bundlecost = 0m;
            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate that each item has a valid WarehouseInputInventoryId
                    foreach (var item in model.BundlePackage)
                    {
                        if (item.WarehouseInputInventoryId <= 0)
                            throw new ArgumentException($"Invalid inventory ID for item: {item.Name}");
                        var getInputInventory = await _repository.GetInputBYIdAsync(business.BusinessCode, item.WarehouseInputInventoryId);
                        if (getInputInventory == null || getInputInventory.IsAvailable == false)
                            throw new ArgumentException($"This item does not exist: {item.Name}");

                        item.Name = getInputInventory.InputName;
                      //TODO: ONLY DEPLETE after disbursement  var depleteUnit = await _repository.DepleteAsync(item.WarehouseInputInventoryId, item.Unit);
                        var cost = (item.Unit * getInputInventory.Price);
                        bundlecost += cost;
                    }
                    if (model.OtherBundlePaymentCosts.Count > 0)
                    {
                        foreach (var item in model.OtherBundlePaymentCosts)
                        {
                            totalOtherCost += item.Amount;

                        }

                    }
                    totalOtherCost += bundlecost;
                    if (model.BundlePrice < totalOtherCost)
                    { throw new Exception("Total Bundle Cost calculation mismatched"); }
                    model.CreatedBy = args.UserId;
                    model.BelongToBusinessCode = business.BusinessCode;

                    var bundles = await _repository.GetBundleByBusiness(business.BusinessCode);
                    if (bundles != null && bundles.Any(x => x.Name == model.Name)) throw new Exception($"There is an existing bundle package with same name");

                    if (model.IsPercentageEquity)
                    {
                        model.BundleEquityPrice = model.BundlePrice * (decimal)(model.EquityPercentage / 100);
                    }
                    if (model.ShouldAddCommission)
                    {
                        model.BundleEquityPlusCommission = model.CommissionPrice + model.BundleEquityPrice;
                        model.BundleEquityPrice += model.CommissionPrice;
                    }
                    createBundle = await _repository.CreateBundle(model);


                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return createBundle;

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return createBundle;
        }
        
        public async Task<PagedListResponse<List<BundleModel>>> GetBundleByWarehouseManager(string userId,
  ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = !string.IsNullOrEmpty(query.businessCode)
       ? await _businessRepository.GetBusinessByCodeAsync(query.businessCode)
       : await _businessRepository.GetBusinessByUserIdAsync(userId);


                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var bundles = await _repository.GetBundleByWarehouseManager(business.BusinessCode, userId);
                if (bundles == null) return new PagedListResponse<List<BundleModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<BundleModel>>
                {
                    Succeeded = true,
                    Data = bundles,
                    Count = bundles.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<BundleModel>>> GetBundlesByBusiness(string userId,
     ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = !string.IsNullOrEmpty(query.businessCode)
       ? await _businessRepository.GetBusinessByCodeAsync(query.businessCode)
       : await _businessRepository.GetBusinessByUserIdAsync(userId);


                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var bundles = await _repository.GetBundleByBusiness(business.BusinessCode);
                if (bundles == null) return new PagedListResponse<List<BundleModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<BundleModel>>
                {
                    Succeeded = true,
                    Data = bundles,
                    Count = bundles.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<BundleModel>> GetBundle(string userId, long bundleId)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var bundle = await _repository.GetBundleByIdAsync(business.BusinessCode, bundleId);
                if (bundle == null) return new Response<BundleModel>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<BundleModel>
                {
                    Succeeded = true,
                    Data = bundle,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        #endregion bundlemgt


    }
}
