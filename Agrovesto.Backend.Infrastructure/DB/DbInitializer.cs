using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.DB
{
    public static class DbInitializer
    {



        public static async Task SeedWeight(IServiceProvider serviceProvider)
        {
            // var logger = serviceProvider.GetRequiredService<ILogger<DbInitializer>>();
            try
            {
                var creditManager = serviceProvider.GetRequiredService<IFarmerCreditScoreRepository>();

                await creditManager.SaveCreditScoreWeightAsync(new CreditScoreWeightsModel
                {
                    BusinessCode = "REL20252",
                    SubcriptionWeight = 15,
                    OrganizationCode = null,
                    RepaymentWeight = 20,
                    TimelinessWeight = 65
                });
                await creditManager.SaveCreditScoreWeightAsync(new CreditScoreWeightsModel
                {
                    BusinessCode = "ARN20253",
                    SubcriptionWeight = 15,
                    OrganizationCode = null,
                    RepaymentWeight = 20,
                    TimelinessWeight = 65
                });
                await creditManager.SaveCreditScoreWeightAsync(new CreditScoreWeightsModel
                {
                    BusinessCode = "ARN20253",
                    SubcriptionWeight = 15,
                    OrganizationCode = "FMC62",
                    RepaymentWeight = 20,
                    TimelinessWeight = 65
                });
            }
            catch (Exception ex)
            {
                // logger.LogError($"Error during database seeding: {ex.Message}");
            }
        }


    }
    public static class SeedData
    {
        public static async Task EnsureSeededAsync(DataContext db)
        {
            if (await db.MandEActivityTypes.AnyAsync()) return;

            // Insert activity types first
            var pre = new MandEActivityType { Name = "Pre-Planting", BelongToBusinessCode = "REL20252", OrganizationCode = "NIW73" };
            var plant = new MandEActivityType { Name = "Planting", BelongToBusinessCode = "REL20252", OrganizationCode = "NIW73" };
            var post = new MandEActivityType { Name = "Post-Harvest", BelongToBusinessCode = "REL20252", OrganizationCode = "NIW73" };

            await db.MandEActivityTypes.AddRangeAsync(pre, plant, post);
            await db.SaveChangesAsync(); // ensures IDs are generated

            // Subactivities now can safely use IDs
            var subActivities = new List<MandESubActivity>
        {
            new() { MandEActivityTypeId = pre.Id, Name = "Farm Clearing", BelongToBusinessCode="REL20252", OrganizationCode="NIW73" },
            new() { MandEActivityTypeId = pre.Id, Name = "Soil Testing", BelongToBusinessCode="REL20252", OrganizationCode="NIW73" },
            new() { MandEActivityTypeId = plant.Id, Name = "Crop Planting", BelongToBusinessCode="REL20252", OrganizationCode="NIW73" },
            new() { MandEActivityTypeId = post.Id, Name = "Harvesting", BelongToBusinessCode="REL20252", OrganizationCode="NIW73" }
        };

            await db.MandESubActivities.AddRangeAsync(subActivities);
            await db.SaveChangesAsync();

            // Lookup helper for field definitions
            long SA(string name) => subActivities.Single(x => x.Name == name).Id;

            var fields = new List<MandEFieldDefinition>
        {
            new() { MandESubActivityId = SA("Farm Clearing"), Label = "Clearing Method", Key = "clearingMethod", Type = "select", OptionsJson = "[\"Manual\",\"Mechanized\"]", IsRequired = true, Order = 1, BelongToBusinessCode="REL20252" },
            new() { MandESubActivityId = SA("Farm Clearing"), Label = "Area Cleared (ha)", Key = "areaHa", Type = "number", IsRequired = true, Order = 2, BelongToBusinessCode="REL20252" },
            new() { MandESubActivityId = SA("Soil Testing"), Label = "pH", Key = "ph", Type = "number", IsRequired = true, Order = 1, BelongToBusinessCode="REL20252" },
            new() { MandESubActivityId = SA("Crop Planting"), Label = "Planting Date", Key = "plantingDate", Type = "date", IsRequired = true, Order = 1, BelongToBusinessCode="REL20252" },
            new() { MandESubActivityId = SA("Harvesting"), Label = "Harvest Date", Key = "harvestDate", Type = "date", IsRequired = true, Order = 1, BelongToBusinessCode="REL20252" }
        };

            await db.MandEFieldDefinitions.AddRangeAsync(fields);
            await db.SaveChangesAsync();
        }
    }
}