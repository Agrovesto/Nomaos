using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Google;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.DB
{
    public class InventoryTransactionBackfillService
    {
        private readonly DataContext _context;

        public InventoryTransactionBackfillService(
            DataContext context)
        {
            _context = context;
        }

        public async Task BackfillInventoryTransactionsAsync()
        {
            var validOrganizationIds = (await _context.BusinessOrganizations
       .Select(x => x.OrganizationId)
       .ToListAsync())
       .ToHashSet();

            var inventories = await _context.LocationInventories
                .Include(x => x.Location)
                .Where(x => x.IsActive)
                .Where(x => !_context.InventoryTransactions
                    .Any(t => t.LocationInventoryId == x.Id))
                .ToListAsync();

            if (!inventories.Any())
                return;

            var transactions = new List<InventoryTransaction>();

            foreach (var inventory in inventories)
            {
                if (inventory.Location == null)
                    continue;

                if (!validOrganizationIds.Contains(
                        inventory.Location.OrganizationId))
                    continue;

                transactions.Add(new InventoryTransaction
                {
                    LocationInventoryId = inventory.Id,

                    OrganizationProductId = inventory.OrganizationProductId,

                    OrganizationId = inventory.Location.OrganizationId,

                    ToLocationId = inventory.LocationId,

                    TransactionType =
                        Core.Enums.InventoryTransactionType.OpeningBalance,

                    Quantity = inventory.Quantity,

                    QuantityType = inventory.QuantityType ?? "Bag",

                    QuantityBefore = 0,

                    QuantityAfter = inventory.Quantity,

                    BatchNumber = inventory.BatchNumber,

                    Reason = "System migration opening balance",

                    PerformedByUserId =
                        inventory.RecordByUserId ?? "SYSTEM",

                    TransactionDate =
                        inventory.RecordedOn,

                    BusinessCode = inventory.BusinessCode
                });
            }

            _context.InventoryTransactions.AddRange(transactions);

            await _context.SaveChangesAsync();
        }
    }
}
