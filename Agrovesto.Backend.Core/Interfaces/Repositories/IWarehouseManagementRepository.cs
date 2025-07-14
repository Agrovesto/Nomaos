using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IWarehouseManagementRepository
    {
        Task<bool> AddMoreAsync(long id, long unitstoadd);
        Task<WarehouseInputInventoryModel> CreateAsync(WarehouseInputInventoryModel inventory);
        Task<BundleModel> CreateBundle(BundleModel model);
        Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel data);
        Task<(bool status, WarehouseUserModel data)> CreateWarehouseManager(WarehouseUserModel data);
        Task<bool> DepleteAsync(long id, long unitsToDeplete);
        Task<List<BundleModel>> GetBundleByBusiness(string belongToBusiness);
        Task<BundleModel> GetBundleByIdAsync(string belongToBusiness, long bundleId);
        Task<List<BundleModel>> GetBundleByWarehouseManager(string belongToBusiness, string warehouseMangerId);
        Task<WarehouseInputInventoryModel> GetInputBYIdAsync(string belongToBusiness, long id);
        Task<List<WarehouseInputInventoryModel>> GetInputByWarehouse(string belongToBusiness, long warehouseId);
        Task<InputDeliveryTrackerModel> GetInputTracker(string reference);
        Task<WarehouseModel> GetWarehouseByBusiness(string belongToBusiness, long warehouseId);
        Task<List<WarehouseInputTransitModel>> GetWarehouseInputTransit(string businessCode);
        Task<WarehouseUserModel> GetWarehouseManagerByBusiness(string belongToBusiness, long warehouseId);
        Task<List<WarehouseModel>> GetWarehouses();
        Task<List<WarehouseModel>> GetWarehousesByAgent(string belongToBusiness, string agentId);
        Task<List<WarehouseModel>> GetWarehousesByBusiness(string belongToBusiness, BusinessRegistrationArgs query);
        Task<List<WarehouseUserModel>> GetWarehousesManagerByBusiness(string belongToBusiness);
        Task<List<WarehouseUserModel>> GetWarehousesManagersByBusiness(string belongToBusiness, long warehouseId);
        Task<List<WarehouseSummaryMetrics>> GetWarehouseSummaryByBusiness(string businessCode);
        Task<bool> LogDeliveryTracker(InputDeliveryTrackerModel model);
        Task<List<WarehouseSummaryMetrics>> StockSummaryByWarehouse(string businessCode, long warehouseId);
        Task<List<WarehouseSummaryMetrics>> StockSummaryByWarehouseByAgent(string businessCode, string userId);
        Task<BundleModel> UpdateBundleEquitPrice(BundleModel model);
        Task<BundleModel> UpdateBundleSubscriptions(BundleModel model);
        Task<bool> UpdateInputReceiptStatus(string belongToBusiness, long Id, string status, string receivedBy, DateTimeOffset date, long unitstoadd);
        Task<(bool status, WarehouseModel data)> UpdateWarehouse(WarehouseModel data);
        Task<(bool status, WarehouseUserModel data)> UpdateWarehouseManager(WarehouseUserModel data);
    }
}
