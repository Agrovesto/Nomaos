using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class InputFinancingManagement : IInputFinancingManagement
    {
        private readonly IInputFinancingManagementRepository _repository;
        private readonly IWarehouseManagementRepository _warehouseManagementRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly ILogger<InputFinancingManagement> _log;
        public InputFinancingManagement(IInputFinancingManagementRepository repository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, ILogger<InputFinancingManagement> log, IWarehouseManagementRepository warehouseManagementRepository,IFarmerRepository farmerRepository)
        {
            _repository = repository;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _log = log;
            _warehouseManagementRepository = warehouseManagementRepository; 
            _farmerRepository = farmerRepository;
        }

        public async Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterest(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                    ?? throw new Exception("Bundle does not exist");
                var farmer= await _farmerRepository.GetFarmerByUserId(model.FarmerUserId) ?? throw new Exception("Farmer does not exist");

                model.BelongToBusinessCode = args.businessCode;
                model.OfficerInChargeUserId = args.UserId;
                model.InterestedBundleName = getInputInventory.Name;
                model.PaymentStatus = EquityPaymentStatus.Initial;
                model.FarmerName = farmer.Name;
                if (getInputInventory.ShouldAddCommission)
                {
                    model.ShouldAddCommission = true;
                    model.CommissionPrice = getInputInventory.CommissionPrice;//(getInputInventory.CommissionPrice * model.QuantityOfInterest);
                    //model.EquityPlusCommission = (getInputInventory.BundlePrice * model.QuantityOfInterest) + getInputInventory.CommissionPrice
                }
                model.ExpectedEquityPricePerBundle = getInputInventory.BundlePrice;
                model.ExpectedTotalEquityPrice = (getInputInventory.BundlePrice * model.QuantityOfInterest);

                var createWarehouse = await _repository.CreateInputFinancingInterestThroughBundle(model);

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
    }
}
