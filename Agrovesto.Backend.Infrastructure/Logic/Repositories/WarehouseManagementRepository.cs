using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class WarehouseManagementRepository : IWarehouseManagementRepository
    {
        private readonly DataContext _context;
        public WarehouseManagementRepository(DataContext context)
        {
            _context = context;
        }
        #region warehousemgt
        public async Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel data)
        {
            var entity = data.Map();
            entity.IsActive = true;
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<(bool status, WarehouseModel data)> UpdateWarehouse(WarehouseModel data)
        {
            var warehouse = await _context.Warehouses.Where(x => x.BelongToBusinessCode == data.BelongToBusinessCode && x.Id == data.Id).FirstOrDefaultAsync();
           if (warehouse == null) { throw new Exception("No record found"); }
            warehouse.ManagedByUserId = data.ManagedByUserId;
            warehouse.HasbeenAssignedManager = data.HasbeenAssignedManager;
            warehouse.Capacity = data.Capacity;
            await _context.SaveChangesAsync();

            return (true, warehouse.Map());
        }

        public async Task<List<WarehouseModel>> GetWarehousesByBusiness(string belongToBusiness)
        {
            var warehouse = await _context.Warehouses.Where(x => x.BelongToBusinessCode == belongToBusiness ).ToListAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Select(x=>x.Map()).ToList();
        }

        public async Task<WarehouseModel> GetWarehouseByBusiness(string belongToBusiness, long warehouseId)
        {
            var warehouse = await _context.Warehouses.Where(x => x.BelongToBusinessCode == belongToBusiness && x.Id == warehouseId).FirstOrDefaultAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Map();
        }

        public async Task<List<WarehouseModel>> GetWarehouses()
        {
            var warehouse = await _context.Warehouses.ToListAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Select(x => x.Map()).ToList();
        }

        public async Task<(bool status, WarehouseUserModel data)> CreateWarehouseManager(WarehouseUserModel data)
        {

            var entity = data.Map();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<(bool status, WarehouseUserModel data)> UpdateWarehouseManager(WarehouseUserModel data)
        {
            var warehouse = await _context.WarehouseUsers.Where(x => x.BelongToBusinessCode == data.BelongToBusinessCode && x.Id == data.Id).FirstOrDefaultAsync();
            if (warehouse == null) { throw new Exception("No record found"); }
            warehouse.UserId = data.UserId;
            warehouse.Name = !string.IsNullOrEmpty(data.Name) ? data.Name : warehouse.Name;
            warehouse.PhoneNumber = !string.IsNullOrEmpty(data.PhoneNumber) ? data.PhoneNumber : warehouse.PhoneNumber;
            await _context.SaveChangesAsync();

            return (true, warehouse.Map());
        }

        public async Task<List<WarehouseUserModel>> GetWarehousesManagerByBusiness(string belongToBusiness)
        {
            var warehouse = await _context.WarehouseUsers.Where(x => x.BelongToBusinessCode == belongToBusiness).ToListAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Select(x => x.Map()).ToList();
        }

        public async Task<WarehouseUserModel> GetWarehouseManagerByBusiness(string belongToBusiness, long warehouseId)
        {
            var warehouse = await _context.WarehouseUsers.Where(x => x.BelongToBusinessCode == belongToBusiness && x.WarehouseId == warehouseId).FirstOrDefaultAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Map();
        }

        public async Task<List<WarehouseUserModel>> GetWarehousesManagersByBusiness(string belongToBusiness, long warehouseId)
        {
            var warehouse = await _context.WarehouseUsers.Where(x => x.BelongToBusinessCode == belongToBusiness && x.WarehouseId == warehouseId).ToListAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Select(x => x.Map()).ToList();
        }
        #endregion warehousemgt

        #region BundleManagement
        public async Task<BundleModel> CreateBundle(BundleModel model)
        {
            var bundle = new Bundle
            {
                Name = model.Name,
                Description = model.Description,
                BundlePrice = model.BundlePrice,
                BundlePackage = model.BundlePackage.Select(i => new BundleItems
                {
                    Name = i.Name,
                    Unit = i.Unit,
                    WarehouseInputInventoryId = i.WarehouseInputInventoryId,
                    
                }).ToList(),
                BelongToBusinessCode=model.BelongToBusinessCode,
                CreatedBy=model.CreatedBy,
                CreatedOn= DateTimeOffset.Now,
                TotalBundlePackagePacked = 0,
                AvailableBundlePackagePacked = 0,
                TotalBundlePackageRequested = 0,
                TotalBundlePackageDisbursed = 0
            };
            _context.Set<Bundle>().Add(bundle);
            await _context.SaveChangesAsync();

            return bundle.Map();
        }


        public async Task<List<BundleModel>> GetBundleByBusiness(string belongToBusiness)
        {
            var bundles = await _context.Bundles.Include(b=>b.BundlePackage).Where(x => x.BelongToBusinessCode == belongToBusiness
            ).ToListAsync();
            if (bundles == null) { throw new Exception("No record found"); }

            return bundles.Select(x => x.Map()).ToList();
        }

        public async Task<BundleModel> GetBundleByIdAsync(string belongToBusiness, long bundleId)
        {
            var bundle = await _context.Bundles.Include(b => b.BundlePackage).Where(x => x.BelongToBusinessCode == belongToBusiness
         && x.Id == bundleId).FirstOrDefaultAsync();
            if (bundle == null) { throw new Exception("No record found"); }

            return bundle.Map();
        }
        #endregion BundleManagement
        #region inputmgt
        public async Task<WarehouseInputInventoryModel> CreateAsync(WarehouseInputInventoryModel inventory)
        {
            var entity = inventory.Map();
            entity.UnitBalance = inventory.UnitStockedUp;
            entity.IsAvailable = true;
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Map();
        }


        //public async Task<WarehouseInputInventoryModel> UpdateAsync(WarehouseInputInventoryModel inventory)
        //{
        //    var entity = inventory.Map();
        //    entity.UnitAdded += inventory.UnitStockedUp;
        //    entity.UnitBalance += inventory.UnitStockedUp;
        //    entity.ModifiedOn = DateTimeOffset.Now;
        //    entity.ManagedByUserId = inventory.ManagedByUserId;
        //    entity.

        //    _context.Set<WarehouseInputInventory>().Update(entity);
        //    await _context.SaveChangesAsync();
        //    return entity.Map();
        //}

        public async Task<List<WarehouseInputInventoryModel>> GetInputByWarehouse(string belongToBusiness, long warehouseId)
        {
            var input = await _context.WarehouseInputInventories.Where(x => x.BelongToBusinessCode == belongToBusiness 
            && x.WarehouseId == warehouseId).ToListAsync();
            if (input == null) { throw new Exception("No record found"); }

            return input.Select(x=>x.Map()).ToList();
        }

        public async Task<WarehouseInputInventoryModel> GetByIdAsync(string belongToBusiness, long id)
        {
            var warehouse = await _context.WarehouseInputInventories.Where(x => x.BelongToBusinessCode == belongToBusiness && x.Id == id).FirstOrDefaultAsync();
            if (warehouse == null) { throw new Exception("No record found"); }

            return warehouse.Map();
        }

        public async Task<WarehouseInputInventory> GetByInputNameAsync(string inputName)
        {
            return await _context.Set<WarehouseInputInventory>().FirstOrDefaultAsync(i => i.InputName == inputName);
        }

        public async Task<bool> DepleteAsync(long id, long unitsToDeplete)
        {
            var inventory = await _context.Set<WarehouseInputInventory>().FindAsync(id);
            if (inventory == null || inventory.UnitBalance < unitsToDeplete) return false;

            inventory.UnitDepleted += unitsToDeplete;
            inventory.UnitBalance -= unitsToDeplete;
            inventory.ModifiedOn = DateTimeOffset.Now;

            if (inventory.UnitBalance == 0) { inventory.IsAvailable = false; }
         //   _context.Set<WarehouseInputInventory>().Update(inventory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddMoreAsync(long id, long unitstoadd)
        {
            var inventory = await _context.Set<WarehouseInputInventory>().FindAsync(id);
            if (inventory == null ||  unitstoadd == 0) return false;

            inventory.UnitAdded += unitstoadd;
            inventory.UnitBalance += unitstoadd;
            inventory.ModifiedOn = DateTimeOffset.Now;

            if (inventory.UnitBalance > 0) { inventory.IsAvailable = true; }
            //_context.Set<WarehouseInputInventory>().Update(inventory);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion inputmgt
    }
}
