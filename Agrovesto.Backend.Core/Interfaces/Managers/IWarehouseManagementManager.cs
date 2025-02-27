using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IWarehouseManagementManager
    {
        Task<bool> AddMoreInputToStock(InputUpdateModel model, BusinessRegistrationArgs args);
        Task<BundleModel> CreateBundleAsync(BundleModel model, BusinessRegistrationArgs args);
        Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel model, BusinessRegistrationArgs args);
        Task<(bool status, WarehouseUserModel data)> CreateWarehouseManager(WarehouseUserModel model, BusinessRegistrationArgs args);
        Task<bool> DepleteFromInput(InputUpdateModel model, BusinessRegistrationArgs args);
        Task<Response<BundleModel>> GetBundle(string userId, long bundleId);
        Task<PagedListResponse<List<BundleModel>>> GetBundlesByBusiness(string userId, ListQueryParams query);
        Task<Response<WarehouseInputInventoryModel>> GetInput(string userId, long warehouseId);
        Task<PagedListResponse<List<WarehouseInputInventoryModel>>> GetInputByWarehouse(string userId, ListQueryParams query);
        Task<Response<WarehouseModel>> GetWarehouseByBusiness(string userId, long warehouseId);
        Task<Response<WarehouseUserModel>> GetWarehouseManagerByBusiness(string userId, long warehouseId);
           Task<PagedListResponse<List<WarehouseModel>>> GetWarehousesByBusiness(string userId, ListQueryParams query);
        Task<PagedListResponse<List<WarehouseUserModel>>> GetWarehousesManagerByBusiness(string userId, ListQueryParams query);
        Task<PagedListResponse<List<WarehouseUserModel>>> GetWarehousesManagersByBusiness(string userId, long warehouseId, ListQueryParams query);
        Task<Response<List<WarehouseSummaryMetrics>>> GetWarehouseSummaryByBusiness(string userId);
        Task<WarehouseInputInventoryModel> StockUpInput(WarehouseInputInventoryModel model, BusinessRegistrationArgs args);
        Task<(bool status, WarehouseModel data)> UpdateWarehouse(WarehouseModel model, BusinessRegistrationArgs args);
        Task<(bool status, WarehouseUserModel data)> UpdateWarehouseManager(WarehouseUserModel model, BusinessRegistrationArgs args);
    }
}
