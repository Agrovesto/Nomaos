using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class FarmerTransactionManager : IFarmerTransactionManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly IFarmerTransactionRepository _repository;
        private readonly ILogger<FarmerTransactionManager> _log;
        private readonly IAgentRepository _agentRepository;
        private readonly IFarmerRepository _farmerRepository;

        private readonly IConfiguration _configuration;
        public FarmerTransactionManager(IFarmerTransactionRepository repository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
           ILogger<FarmerTransactionManager> log,
           IConfiguration configuration,
            IAgentRepository agentRepository, DataContext context, IFarmerRepository farmerRepository)
        {
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository; ;
            _context = context;
            _repository = repository;
            _log = log;
            _configuration = configuration;
            _agentRepository = agentRepository;
            _farmerRepository = farmerRepository;
        }

        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmerByUserId(
         TransactionQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var farmer = await _farmerRepository.GetFarmerByUserId(query.UserId);
                if (farmer == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found For Farmer");
                var transactions = await _repository.GetTransactionByFarmer(farmer.UserId,
                    query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null) return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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

        //TODO: get farmer transactions by userId

        //
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmer(long farmerId,
          TransactionQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var farmer = await _farmerRepository.GetFarmerById(farmerId);
                if (farmer == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found For Farmer");
                var transactions = await _repository.GetTransactionByFarmer(farmer.UserId,
                    query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null) return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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
        //TODO: log farmer transaction

        public async Task<FarmerSalesTrackerModel> LogOfflineTransaction(
    List<FarmerSalesTrackerModel> request,
    TransactionQueryParams args,
    long farmerId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.BusinessCode)
                ?? throw new Exception("Business does not exist");

            var totalPriceSold = 0m;
            var estimate = 0m;

            (bool status, FarmerSalesTrackerModel data) update = new(false, new FarmerSalesTrackerModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                       var farmer = await _farmerRepository.GetFarmerById(farmerId);

                    if (farmer == null)
                    {
                        farmer = await _farmerRepository.GetFarmerByUserId(args.UserId);
                    }

                    if (farmer == null)
                    {
                        throw new BaseException(
                            Core.Enums.ExceptionType.NOTFOUND,
                            "Farmer record not found"
                        );
                    }

                    var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                    if (!getUserCred.status)
                        throw new Exception("User taking log not recognized");

           
                    foreach (var model in request)
                    {
                        model.CollectionOfficerId = getUserCred.model.Id;
                        model.BelongToBusinessCode = business.BusinessCode;
                        model.FarmerUserId = farmer.UserId;
                        model.SalesType = SalesType.Offline;
                        if (model.UnitType != model.SoldCropUnitType)
                        {
                            model.LowestUnitQuntity = (double)(model.TotalPriceSold / model.PriceOfLowestUnit);

                            totalPriceSold = (decimal)model.LowestUnitQuntity * model.PriceOfLowestUnit;

                            estimate =
                                (
                                (model.CostOfPackagingMaterial / (decimal)model.LowestUnitQuntity)
                                + (model.TransportCost / (decimal)model.LowestUnitQuntity)
                                + (model.OtherCost / (decimal)model.LowestUnitQuntity)
                                ) * (decimal)model.LowestUnitQuntity;
                        }
                        else
                        {
                            totalPriceSold = (decimal)model.QuantitySoldToday * model.CropPricePerUnitSold;

                            estimate =
                                (
                                (model.CostOfPackagingMaterial / (decimal)model.QuantitySoldToday)
                                + (model.TransportCost / (decimal)model.QuantitySoldToday)
                                + (model.OtherCost / (decimal)model.QuantitySoldToday)
                                ) * (decimal)model.QuantitySoldToday;
                        }

                        if (model.TotalPriceSold != totalPriceSold)
                        {
                            throw new BaseException(
                                Core.Enums.ExceptionType.CONFLICT,
                                $"Total Price sold indicated {model.TotalPriceSold} does not tally with Price per unit {model.CropPricePerUnitSold} and quantity {model.QuantitySoldToday}"
                            );
                        }

                        model.supervisorId = args.supervisorId;

                        update = await _repository.LogTransaction(model);
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return update.data;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });

            return update.data;
        }

        //TODO: get all farmertransaction by agent
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmersTransactionByAgent(TransactionQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode)
               ?? throw new Exception("Business does not exist");
            try
            {
                var agent = await _agentRepository.GetAgentDetailByUserId(query.UserId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var transactions = await _repository.GetTransactionByAgent(query.UserId, query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null)
                    return new PagedListResponse<List<FarmerSalesTrackerModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Count = 0,
                        Message = "No Record Found"
                    };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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

        //TODO: get all farmertransaction by cooperative
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerSalesTransactionsByCooperativeAsync(TransactionQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode)
                ?? throw new Exception("Business does not exist");
            try
            {

                var transactions = await _repository.GetFarmerSalesTransactionsByCooperativeAsync(query.CooperativeId, query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null)
                    return new PagedListResponse<List<FarmerSalesTrackerModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Count = 0,
                        Message = "No Record Found"
                    };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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

        //TODO: get all farmertransaction
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionsByBusinessCode(TransactionQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode)
                ?? throw new Exception("Business does not exist");
            try
            {

                var transactions = await _repository.GetTransactionsByBusinessCode(query.BusinessCode, query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null)
                    return new PagedListResponse<List<FarmerSalesTrackerModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Count = 0,
                        Message = "No Record Found"
                    };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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
        public async Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerTransactions(TransactionQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.BusinessCode)
                ?? throw new Exception("Business does not exist");
            try
            {

                var transactions = await _repository.GetTransactions(query.PageNumber, query.PageSize, query.transactionType,
                    query.startDate, query.endDate, query.Location);
                if (transactions == null)
                    return new PagedListResponse<List<FarmerSalesTrackerModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Count = 0,
                        Message = "No Record Found"
                    };
                return new PagedListResponse<List<FarmerSalesTrackerModel>>
                {
                    Succeeded = true,
                    Data = transactions.Data,
                    Count = transactions.Count,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,

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

        //TODO: calculate total transaction volume
        //TODO: calculate total transaction volumen based on transaction type
        //TODO: calculate transaction performance (daily, weekly, monthly, yearly)
        //TODO: get most transacted product
        //TODO: get most transacted product by farmer
        //TODO: get most transacted product by location

    }
}
