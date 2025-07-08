using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

                string systemAdminRole = "SystemAdmin";
                string systemAdminEmail = "reloarhub@gmail.com";
                string systemAdminPassword = "Reloarhub144828@"; // Use a strong password

                await creditManager.SaveCreditScoreWeightAsync(new CreditScoreWeightsModel
                    {
                      BusinessCode= "REL20252",
                      SubcriptionWeight=15,
                      OrganizationCode=null,
                      RepaymentWeight=20,
                      TimelinessWeight=65
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

        public static async Task SeedRolesAndSuperAdminAsync(IServiceProvider serviceProvider)
        {
            // var logger = serviceProvider.GetRequiredService<ILogger<DbInitializer>>();
            try
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

                string systemAdminRole = "SystemAdmin";
                string systemAdminEmail = "reloarhub@gmail.com";
                string systemAdminPassword = "Reloarhub144828@"; // Use a strong password

                // Check and create SuperAdmin Role if not exists
                if (!await roleManager.RoleExistsAsync(systemAdminRole))
                {
                    var roleResult = await roleManager.CreateAsync(new Role { 
                        Name= "SystemAdmin",
                        Descriptions = "System Admin"
                    });
                    if (roleResult.Succeeded)
                    {
                        // logger.LogInformation("SuperAdmin role created successfully.");
                    }
                    else
                    {
                        //logger.LogWarning("Failed to create SuperAdmin role.");
                    }
                }
                else
                {
                    //logger.LogInformation("SuperAdmin role already exists. Skipping...");
                }

                // Check if SuperAdmin User exists
                var systemAdminUser = await userManager.FindByEmailAsync(systemAdminEmail);
                if (systemAdminUser == null)
                {
                    systemAdminUser = new User
                    {
                        UserName = systemAdminEmail,
                        Email = systemAdminEmail,
                        EmailConfirmed = true,
                        Name = "Reloarhub Solutions",
                        PhoneNumber="08126201565",
                        UserType = Enum.GetName(typeof(UserType), UserType.Agrovesto),
                       
                    };

                    var userResult = await userManager.CreateAsync(systemAdminUser, systemAdminPassword);
                    if (userResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(systemAdminUser, systemAdminRole);
                        // logger.LogInformation("SuperAdmin user created and assigned role successfully.");
                    }
                    else
                    {
                        //logger.LogWarning("Failed to create SuperAdmin user.");
                    }
                }
                else
                {
                    // logger.LogInformation("SuperAdmin user already exists. Skipping...");
                }
            }
            catch (Exception ex)
            {
                // logger.LogError($"Error during database seeding: {ex.Message}");
            }
        }
    }
}