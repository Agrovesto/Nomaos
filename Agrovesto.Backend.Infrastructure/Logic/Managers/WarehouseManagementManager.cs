using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class WarehouseManagementManager : IWarehouseManagementManager
    {
        private readonly IWarehouseManagementRepository _repository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly ILogger<WarehouseManagementManager> _log;
        public WarehouseManagementManager(IWarehouseManagementRepository repository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, ILogger<WarehouseManagementManager> log)
        {
            _repository = repository;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _log = log;
        }

        #region warehousemgt
        public async Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.BelongToBusinessCode = args.businessCode;

                var createWarehouse = await _repository.CreateWarehouse(model);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createWarehouse;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }


        public async Task<(bool status, WarehouseModel data)> UpdateWarehouse(WarehouseModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            if (model.Id == 0) throw new Exception("Warehouse to be updated needs to be selected");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.BelongToBusinessCode = args.businessCode;

                var createWarehouse = await _repository.UpdateWarehouse(model);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createWarehouse;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }


        public async Task<PagedListResponse<List<WarehouseModel>>> GetWarehousesByBusiness(string userId,
           ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

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
                    Message = "Retrieved Successfulluy"
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
                    Message = "Retrieved Successfulluy"
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

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var warehouse = await _repository.GetWarehouseByBusiness(business.BusinessCode, args.WarehouseId);
                if (warehouse == null) throw new Exception("Could not retrieve warehouse");
                model.BelongToBusinessCode = args.businessCode;
                model.WarehouseId = warehouse.Id;
                var createWarehouseManager = await _repository.CreateWarehouseManager(model);

                warehouse.HasbeenAssignedManager = true;
                warehouse.ManagedByUserId = createWarehouseManager.data.Id;
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
        }
        public async Task<(bool status, WarehouseUserModel data)> UpdateWarehouseManager(WarehouseUserModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            if (model.Id == 0) throw new Exception("Warehouse to be updated needs to be selected");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.BelongToBusinessCode = args.businessCode;

                var updateWarehouseManager = await _repository.UpdateWarehouseManager(model);

                

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return updateWarehouseManager;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
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
                    Message = "Retrieved Successfulluy"
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
                    Message = "Retrieved Successfulluy"
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
                    Message = "Retrieved Successfulluy"
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
        public async Task<WarehouseInputInventoryModel> StockUpInput(WarehouseInputInventoryModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.BelongToBusinessCode = args.businessCode;
                model.WarehouseId = args.WarehouseId;

                var createWarehouse = await _repository.CreateAsync(model);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createWarehouse;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }
        public async Task<bool> AddMoreInputToStock(InputUpdateModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {


                var createWarehouse = await _repository.AddMoreAsync(model.InputId, model.UnitsToAdd);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createWarehouse;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }


        public async Task<bool> DepleteFromInput(InputUpdateModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {


                var createWarehouse = await _repository.DepleteAsync(model.InputId, model.UnitsToRemove);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createWarehouse;
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }


        public async Task<PagedListResponse<List<WarehouseInputInventoryModel>>> GetInputByWarehouse(string userId,
        ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

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
                    Message = "Retrieved Successfulluy"
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

                var input = await _repository.GetByIdAsync(business.BusinessCode, warehouseId);
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
                    Message = "Retrieved Successfulluy"
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
        public async Task<BundleModel> CreateBundleAsync(BundleModel model, BusinessRegistrationArgs args)
        {
            if (model.BundlePackage == null || model.BundlePackage.Count == 0)
                throw new ArgumentException("Bundle must contain at least one item.");

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate that each item has a valid WarehouseInputInventoryId
                foreach (var item in model.BundlePackage)
                {
                    if (item.WarehouseInputInventoryId <= 0)
                        throw new ArgumentException($"Invalid inventory ID for item: {item.Name}");
                    var getInputInventory = await _repository.GetByIdAsync(business.BusinessCode, item.WarehouseInputInventoryId);
                    if (getInputInventory == null || getInputInventory.IsAvailable == false)
                        throw new ArgumentException($"This item does not exist: {item.Name}");
                }
                model.CreatedBy = args.UserId;
                model.BelongToBusinessCode = business.BusinessCode;
                var createBundle = await _repository.CreateBundle(model);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return createBundle;

            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
        }
        #endregion bundlemgt


    }
}
