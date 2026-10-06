using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class WarehouseManagementRepository : IWarehouseManagementRepository
    {
        private readonly DataContext _context;
        private readonly IOutboxWriter _outboxWriter;

        public WarehouseManagementRepository(DataContext context, IOutboxWriter outboxWriter)
        {
            _context = context;
            _outboxWriter = outboxWriter;
        }
        #region warehousemgt
        public async Task<(bool status, WarehouseModel data)> CreateWarehouse(WarehouseModel data)
        {
            var entity = data.Map();
            entity.IsActive = true;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            _outboxWriter.Enqueue("warehouse.created", new
            {
                warehouseId = entity.Id,
                entity.BelongToBusinessCode,
                entity.OrganizationCode
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

        public async Task<List<WarehouseModel>> GetWarehousesByBusiness(string belongToBusiness, BusinessRegistrationArgs query)
        {
            var warehouse =  _context.Warehouses
                .Where(x => x.BelongToBusinessCode == belongToBusiness)
                .AsQueryable();
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                warehouse = warehouse.Where(x => x.OrganizationCode == query.organizationCode);
            }
            var result = await warehouse
              .OrderByDescending(s => s.CreatedOn)
              .Skip(query.PageNumber)
              .Take(query.PageSize)
               .Select(s => s.Map()).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
        public async Task<List<WarehouseModel>> GetWarehousesByAgent(string belongToBusiness, string agentId, ListQueryParams query)
        {
            var warehouseQuery = _context.Warehouses
                .AsNoTracking()
                .Where(x => x.BelongToBusinessCode == belongToBusiness && x.ManagedByUserId == agentId);

            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                warehouseQuery = warehouseQuery.Where(x => x.OrganizationCode == query.organizationCode);
            }

            var warehouses = await warehouseQuery.ToListAsync();

            if (!warehouses.Any())
            {
                throw new Exception("No record found");
            }

            return warehouses.Select(x => x.Map()).ToList();
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
            if (warehouse == null) { return null; }//throw new Exception("No record found"); }

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
        public async Task<List<WarehouseInputTransitModel>> GetWarehouseInputTransit(ListQueryParams args)
        {
            var queryable = _context.InputDeliveryTrackers
                .Where(w => w.BusinessCode == args.businessCode);

            // Conditionally apply organizationCode filter
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                queryable = queryable.Where(w => w.OrganizationCode == args.organizationCode);
            }

            var query = await queryable
                .Select(w => new WarehouseInputTransitModel
                {
                    WarehouseId = w.WarehouseId,
                    WarehouseInputInventoryId = w.Id,
                    Status = w.Status,
                    InputName = w.InputName,
                    QuantityLoaded = w.QuantityLoaded,
                    QuantityDelivered = w.QuantityDelivered,
                    QuantityReceived = w.QuantityReceived,
                    DeliveredTo = w.DeliveredTo,
                    DeliveredBy = w.DeliveredBy,
                    Reference = w.Reference,
                    TransitStatus = w.Status,
                    DeliveryContact = w.DeliveryContact,
                    PickupLocation = w.PickupLocation,
                    DeliveryLocation = w.DeliveryLocation,
                    LogisticNumber = w.LogisticNumber,
                    DateReceived = w.DeliveryDate,
                    DateShipped = w.Shipmentdate,
                    From = w.PickupLocation,
                    To = w.DeliveredTo,
                    TrackingId = w.Reference
                })
                .ToListAsync();

            return query;
        }

        public async Task<List<WarehouseSummaryMetrics>> StockSummaryByWarehouse(string businessCode, long warehouseId)
        {
            var query = await _context.Warehouses
     .Where(w => w.BelongToBusinessCode == businessCode && w.Id == warehouseId)
     .Select(w => new WarehouseSummaryMetrics
     {
         WarehouseName = w.Name,
         Location = w.Location,
         StockSummary = _context.WarehouseInputInventories
             .Where(wi => wi.WarehouseId == w.Id)
             .GroupBy(wi => wi.InputName)
             .Select(g => new WarehouseSummary
             {
                 Name = g.Key,
                 Value = g.Sum(wi => wi.UnitBalance),
                 InitialStockUp = g.Sum(wi => wi.UnitStockedUp)
             })
             .ToList()
     })
     .ToListAsync();
            if (query.Count == 0) { return null; }
            return query;
        }
        public async Task<List<WarehouseSummaryMetrics>> StockSummaryByWarehouseByAgent(string businessCode, string userId)
        {
            var query = await _context.Warehouses
     .Where(w => w.BelongToBusinessCode == businessCode && w.ManagedByUserId== userId)
     .Select(w => new WarehouseSummaryMetrics
     {
         WarehouseName = w.Name,
         Location = w.Location,
         StockSummary = _context.WarehouseInputInventories
             .Where(wi => wi.WarehouseId == w.Id)
             .GroupBy(wi => wi.InputName)
             .Select(g => new WarehouseSummary
             {
                 Name = g.Key,
                 Value = g.Sum(wi => wi.UnitBalance),
                 InitialStockUp = g.Sum(wi => wi.UnitStockedUp)
             })
             .ToList()
     })
     .ToListAsync();
            if (query.Count == 0) { return null; }
            return query;
        }

        //    public async Task<List<WarehouseInputTransitModel>> GetWarehouseInputTransit(string businessCode)
        //    {
        //        var query = await _context.WarehouseInputInventories
        //.Where(w => w.BelongToBusinessCode == businessCode)
        //.Select(w => new WarehouseInputTransitModel
        //{
        //    To=w.DeliveredTo,
        //    From=w.InputComingFrom,
        //    Status=w.TransitStatus,
        //    TrackingId= w.Reference,
        //    DateReceived=w.InventoryStockedOn,
        //    DateShipped=w.Shipmentdate

        //}).ToListAsync();

        //        if (query.Count == 0) { return null; }
        //        return query;
        //    }
        public async Task<List<WarehouseSummaryMetrics>> GetWarehouseSummaryByBusiness(ListQueryParams args)
        {
            var queryable = _context.Warehouses
                .Where(w => w.BelongToBusinessCode == args.businessCode);

            // Conditionally apply organizationCode filter
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                queryable = queryable.Where(w => w.OrganizationCode == args.organizationCode);
            }

            var query = await queryable
                .Select(w => new WarehouseSummaryMetrics
                {
                    WarehouseName = w.Name,
                    Location = w.Location,
                    StockSummary = _context.WarehouseInputInventories
                        .Where(wi => wi.WarehouseId == w.Id)
                        .GroupBy(wi => wi.InputName)
                        .Select(g => new WarehouseSummary
                        {
                            Name = g.Key,
                            Value = g.Sum(wi => wi.UnitBalance),
                            InitialStockUp = g.Sum(wi => wi.UnitStockedUp)
                        })
                        .ToList()
                })
                .ToListAsync();

            if (query.Count == 0)
            {
                return null;
            }

            return query;
        }
        public async Task<BundleModel> CreateBundle(BundleModel model)
        {
            var bundle = new Bundle
            {
                Name = model.Name,
                Description = model.Description,
                BundlePrice = model.BundlePrice,
                WarehouseId = model.WarehouseId,

                BundlePackage = model.BundlePackage.Select(i => new BundleItems
                {
                    Name = i.Name,
                    Unit = i.Unit,
                    WarehouseInputInventoryId = i.WarehouseInputInventoryId,

                }).ToList(),
                OtherBundlePaymentCosts = model.OtherBundlePaymentCosts.Select(o => new OtherBundlePaymentCost
                {
                    Description = o.Description,
                    Amount = o.Amount,
                    Name = o.Name,
                }).ToList(),
                BelongToBusinessCode = model.BelongToBusinessCode,
                CreatedBy = model.CreatedBy,
                CreatedOn = DateTimeOffset.Now,
                TotalBundlePackagePacked = 0,
                AvailableBundlePackagePacked = 0,
                TotalBundlePackageRequested = 0,
                TotalBundlePackageDisbursed = 0,
                CommissionPrice = model.CommissionPrice,
                ShouldAddCommission = model.ShouldAddCommission,
                MaturityDate=model.MaturityDate,
                BundleEquityPrice = model.BundleEquityPrice,
                OrganizationCode=model.OrganizationCode,
                BundleEquityPlusCommission = model.BundleEquityPlusCommission,
            };
            _context.Set<Bundle>().Add(bundle);
            await _context.SaveChangesAsync();

            return bundle.Map();
        }

        public async Task<BundleModel> UpdateBundleEquitPrice(BundleModel model)
        {
            var bundle = await _context.Bundles.Include(x => x.BundlePackage)

                .Include(b => b.OtherBundlePaymentCosts).Where(x => x.Id == model.Id && x.BelongToBusinessCode == model.BelongToBusinessCode).FirstOrDefaultAsync();
            if (bundle != null)
            {
                bundle.BundleEquityPrice = model.BundleEquityPrice;
                bundle.CommissionPrice = model.CommissionPrice;
                bundle.BundleEquityPlusCommission = model.BundleEquityPrice + model.CommissionPrice;

                bundle.ShouldAddCommission = model.ShouldAddCommission;

                await _context.SaveChangesAsync();
                return bundle.Map();
            }
            return null;
        }
        public async Task<BundleModel> UpdateBundleSubscriptions(BundleModel model)
        {
            var bundle = await _context.Bundles.Include(x => x.BundlePackage)
                .Include(b => b.OtherBundlePaymentCosts).Where(x => x.Id == model.Id && x.BelongToBusinessCode == model.BelongToBusinessCode).FirstOrDefaultAsync();
            if (bundle != null)
            {
                bundle.TotalBundlePackageRequested += model.TotalBundlePackageRequested;
                bundle.TotalBundlePackageDisbursed += model.TotalBundlePackageDisbursed;
                bundle.TotalBundlePackagePacked += model.TotalBundlePackagePacked;

                await _context.SaveChangesAsync();
                return bundle.Map();
            }
            return null;
        }
        public async Task<List<BundleModel>> GetBundleByBusiness(ListQueryParams query)
        {
            

            var bundlequery = _context.Bundles
                .AsNoTracking()
                .Include(b => b.BundlePackage)
                .Include(b => b.OtherBundlePaymentCosts)
                .Where(x => x.BelongToBusinessCode == query.businessCode);

            //    .ToListAsync();
            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                bundlequery = bundlequery.Where(x => x.OrganizationCode == query.organizationCode);
            }
            var bundles = await bundlequery.ToListAsync();

            if (bundles == null || !bundles.Any())
            {
                return null;
            }

            // Fetch warehouse names for all distinct WarehouseIds in bundles
            var warehouseIds = bundles.Select(b => b.WarehouseId).Distinct().ToList();
            var warehouses = await _context.Warehouses
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            return bundles.Select(bundle =>
            {
                var mappedBundle = bundle.Map();
                if (warehouses.TryGetValue(bundle.WarehouseId, out var warehouseName))
                {
                    mappedBundle.WarehouseName = warehouseName;
                }
                return mappedBundle;
            }).ToList();
        }


        public async Task<List<BundleModel>> GetBundleByWarehouseManager(string belongToBusiness, string warehouseManagerId, ListQueryParams query)
        {
            var bundlequery = _context.Bundles
                .AsNoTracking()
                .Include(b => b.BundlePackage)
                .Include(b => b.OtherBundlePaymentCosts)
                .Where(x => x.BelongToBusinessCode == query.businessCode);

            //    .ToListAsync();
            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                bundlequery = bundlequery.Where(x => x.OrganizationCode == query.organizationCode);
            }
            var bundles = await bundlequery.ToListAsync();

            if (bundles == null || !bundles.Any())
            {
                return null;
            }

            var warehouseIds = bundles.Select(b => b.WarehouseId).Distinct().ToList();

            var warehouseQuery = _context.Warehouses
                .Where(w => warehouseIds.Contains(w.Id) && w.ManagedByUserId == warehouseManagerId);

            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                warehouseQuery = warehouseQuery.Where(w => w.OrganizationCode == query.organizationCode);
            }

            var warehouses = await warehouseQuery
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            return bundles
                .Where(b => warehouses.ContainsKey(b.WarehouseId)) // Only include bundles whose warehouse is managed by the user (and match org filter)
                .Select(bundle =>
                {
                    var mapped = bundle.Map();
                    mapped.WarehouseName = warehouses[bundle.WarehouseId];
                    return mapped;
                })
                .ToList();
        }

        public async Task<BundleModel> GetBundleByIdAsync(string belongToBusiness, long bundleId)
        {
            var bundle = await _context.Bundles
                .Include(b => b.BundlePackage)
                .Include(b => b.OtherBundlePaymentCosts)
                .Where(x => x.BelongToBusinessCode == belongToBusiness && x.Id == bundleId)
                .FirstOrDefaultAsync();

            if (bundle == null)
            {
                throw new Exception("No record found");
            }

            // Fetch warehouse name for the bundle
            var warehouseName = await _context.Warehouses
                .Where(w => w.Id == bundle.WarehouseId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync();

            var mappedBundle = bundle.Map();
            mappedBundle.WarehouseName = warehouseName;

            return mappedBundle;
        }
        //public async Task<BundleModel> GetBundleByIdAsync(string belongToBusiness, long bundleId)
        //{
        //    var bundle = await _context.Bundles.Include(b => b.BundlePackage).Where(x => x.BelongToBusinessCode == belongToBusiness
        // && x.Id == bundleId).FirstOrDefaultAsync();
        //    if (bundle == null) { throw new Exception("No record found"); }

        //    return bundle.Map();
        //}
        #endregion BundleManagement
        #region inputmgt
        public async Task<WarehouseInputInventoryModel> CreateAsync(WarehouseInputInventoryModel inventory)
        {
            var entity = inventory.Map();
            // entity.UnitBalance = inventory.UnitStockedUp;
            entity.IsAvailable = true;
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Map();
        }




        public async Task<List<WarehouseInputInventoryModel>> GetInputByWarehouse(string belongToBusiness, long warehouseId)
        {
            var input = await _context.WarehouseInputInventories.Where(x => x.BelongToBusinessCode == belongToBusiness
            && x.WarehouseId == warehouseId).ToListAsync();
            if (input.Count == 0) { return null; }

            return input.Select(x => x.Map()).ToList();
        }

        public async Task<WarehouseInputInventoryModel> GetInputBYIdAsync(string belongToBusiness, long id)
        {
            var warehouse = await _context.WarehouseInputInventories.Where(x => x.BelongToBusinessCode == belongToBusiness && x.Id == id).FirstOrDefaultAsync();
            if (warehouse == null) { return null; }

            return warehouse.Map();
        }

        public async Task<WarehouseInputInventory> GetByInputNameAsync(string inputName)
        {
            return await _context.Set<WarehouseInputInventory>().FirstOrDefaultAsync(i => i.InputName == inputName);
        }
        public async Task<bool> UpdateInputReceiptStatus(string belongToBusiness, long Id, string status, string receivedBy, DateTimeOffset date, long unitstoadd)
        {
            var warehouse = await _context.WarehouseInputInventories.Where(x => x.BelongToBusinessCode == belongToBusiness && x.Id == Id).FirstOrDefaultAsync();
            if (warehouse == null) { throw new Exception("No record found"); }
            if (!string.IsNullOrEmpty(receivedBy))
            {
                warehouse.InventoryStockedOn = date;
            }
            else
            {
                warehouse.Shipmentdate = date;
            }
            warehouse.ReceivedBy = receivedBy;
            warehouse.UnitAdded += unitstoadd;
            warehouse.UnitBalance += unitstoadd;
            warehouse.TransitStatus = status;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<InputDeliveryTrackerModel> GetInputTracker(string reference)
        {
            var input = await _context.Set<InputDeliveryTracker>().FirstOrDefaultAsync(i => i.Reference == reference);
            if (input == null) return null;
            return input.Map();
        }

        public async Task<bool> LogDeliveryTracker(InputDeliveryTrackerModel model)
        {


            var delivery = await _context.InputDeliveryTrackers.Where(x => x.BusinessCode == model.BusinessCode && x.Reference == model.Reference).FirstOrDefaultAsync();
            if (delivery == null)
            {
                var newDelivery = model.Map();
                _context.Add(newDelivery);
            }
            else
            {
                delivery.QuantityDelivered = model.QuantityDelivered;
                delivery.QuantityReceived = model.QuantityReceived;
                delivery.DeliveryDate = DateTimeOffset.Now;
                delivery.Status = Enum.GetName(typeof(InputTransitStatus), model.Status);
                

            }
            await _context.SaveChangesAsync();

            return true;
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
            if (inventory == null || unitstoadd == 0) return false;

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
