using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure
{
    public static class Mapper
    {
        #region FarmMgt
        public static FarmPlotDetailsModel Map(this FarmPlotDetails entity)
        {
            if(entity == null)
            {
                return null;
            }
            return new FarmPlotDetailsModel
            {
                FarmState = entity.FarmState,
                FarmerId = entity.FarmerId,
                FarmerName = entity.FarmerName,
                FarmLga = entity.FarmLga,
                FarmLocation = entity.FarmLocation,
                Id = entity.Id,
                Latitude1 = entity.Latitude1,
                Latitude2 = entity.Latitude2,
                Latitude3 = entity.Latitude3,
                Latitude4 = entity.Latitude4,
                Longitude1 = entity.Longitude1,
                Longitude2 = entity.Longitude2,
                Longitude3 = entity.Longitude3, 
                Longitude4 = entity.Longitude4 ,

                CoordinateTakenBy = entity.CoordinateTakenBy,
                CoordinateTakenByUserId = entity.CoordinateTakenByUserId,
            };
        }

        public static FarmPlotDetails Map(this FarmPlotDetailsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new FarmPlotDetails
            {
                FarmState = model.FarmState,
                FarmerId = model.FarmerId,
                FarmerName = model.FarmerName,
                FarmLga = model.FarmLga,
                FarmLocation = model.FarmLocation,
                Latitude1 = model.Latitude1,
                Latitude2 = model.Latitude2,
                Latitude3 = model.Latitude3,
                Latitude4 = model.Latitude4,
                Longitude1 = model.Longitude1,
                Longitude2 = model.Longitude2,
                Longitude3 = model.Longitude3,
                Longitude4 = model.Longitude4,
                CoordinateTakenBy = model.CoordinateTakenBy,
                CoordinateTakenByUserId = model.CoordinateTakenByUserId,
            };
        }
        #endregion FarmMgt

        #region Bundle
        public static BundleModel Map(this Bundle entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new BundleModel
            {
                Description = entity.Description,
                TotalBundlePackageDisbursed = entity.TotalBundlePackageDisbursed,
                BundlePrice = entity.BundlePrice,
                Id = entity.Id,
                Name = entity.Name,
                TotalBundlePackagePacked = entity.TotalBundlePackagePacked,
                TotalBundlePackageRequested = entity.TotalBundlePackageRequested,
                AvailableBundlePackagePacked = entity.AvailableBundlePackagePacked,
            };
        }

        public static Bundle Map(this BundleModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new Bundle
            {
                Description = model.Description,
                TotalBundlePackageDisbursed = model.TotalBundlePackageDisbursed,
                BundlePrice = model.BundlePrice,
             
                Name = model.Name,
                TotalBundlePackagePacked = model.TotalBundlePackagePacked,
                TotalBundlePackageRequested = model.TotalBundlePackageRequested,
                AvailableBundlePackagePacked = model.AvailableBundlePackagePacked,
            };
        }

        public static BundleItemsModel Map (this BundleItems entity)
        {
            if(entity == null)
            {
                return null;
            }
            return new BundleItemsModel
            {
                BundleId = entity.BundleId,
                Id = entity.Id,
                Name=entity.Name,
                Unit=entity.Unit,
                WarehouseInputInventoryId = entity.WarehouseInputInventoryId,
            };
        }

        public static BundleItems Map(this BundleItemsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new BundleItems
            {
                BundleId = model.BundleId,
                Name = model.Name,
                Unit = model.Unit,
                WarehouseInputInventoryId = model.WarehouseInputInventoryId,
            };
        }
        #endregion Bundle

        #region warehouse

        public static WarehouseUserModel Map(this WarehouseUser entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseUserModel
            {
                State = entity.State,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                Id = entity.Id,
                Lga = entity.Lga,
                Location = entity.Location,
                Name = entity.Name,
                PhoneNumber = entity.PhoneNumber,
            };
        }

        public static WarehouseUser Map(this WarehouseUserModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WarehouseUser
            {
                State = model.State,
                BelongToBusinessCode = model.BelongToBusinessCode,
                Lga = model.Lga,
                Location = model.Location,
                Name = model.Name,
                PhoneNumber = model.PhoneNumber,
            };
        }
        public static WarehouseInputInventoryModel Map(this WarehouseInputInventory entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseInputInventoryModel
            {
                IsAvailable = entity.IsAvailable,
                Size = entity.Size,
                InventoryStockedOn = entity.InventoryStockedOn,
                UnitStockedUp = entity.UnitStockedUp,
                Brand = entity.Brand,
                Id = entity.Id,
                InputDescription = entity.InputDescription,
                InputName = entity.InputName,
                ManagedByUserId = entity.ManagedByUserId,
                ModifiedOn = entity.ModifiedOn,
                Price = entity.Price,
                Type = entity.Type,
                Unit = entity.Unit,
                UnitBalance = entity.UnitBalance,
                UnitDepleted = entity.UnitDepleted,
            };
        }

        public static WarehouseInputInventory Map(this WarehouseInputInventoryModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WarehouseInputInventory
            {
                IsAvailable = model.IsAvailable,
                Size = model.Size,
                InventoryStockedOn = model.InventoryStockedOn,
                UnitStockedUp = model.UnitStockedUp,
                Brand = model.Brand,
                InputDescription = model.InputDescription,
                InputName = model.InputName,
                ManagedByUserId = model.ManagedByUserId,
                ModifiedOn = model.ModifiedOn,
                Price = model.Price,
                Type = model.Type,
                Unit = model.Unit,
                UnitBalance = model.UnitBalance,
                UnitDepleted = model.UnitDepleted,
            };
        }

        public static Warehouse Map(this WarehouseModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new Warehouse
            {
                HasbeenAssignedManager = model.HasbeenAssignedManager,
                BelongToBusinessCode = model.BelongToBusinessCode,
                CreatedOn = model.CreatedOn,
                Lga = model.Lga,
                State = model.State,
                Location = model.Location,
                ManagedByUserId = model.ManagedByUserId,
                Name = model.Name,
            };
        }

        public static WarehouseModel Map(this Warehouse entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseModel
            {
                HasbeenAssignedManager = entity.HasbeenAssignedManager,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                CreatedOn = entity.CreatedOn,
                Lga = entity.Lga,
                State = entity.State,
                Location = entity.Location,
                ManagedByUserId = entity.ManagedByUserId,
                Name = entity.Name,
                Id = entity.Id
            };
        }
        #endregion warehouse
        #region userMgt
        public static FarmerModel Map(this Farmer entities)
        {
            if (entities == null) { return null; }
            return new FarmerModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.PhoneNumber,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,

                DateOfBirth = entities.DateOfBirth,
                Gender = entities.Gender,
                BelongToBusinessCode = entities.BelongToBusinessCode,
                State = entities.State,
                Lga = entities.Lga,
                OnboardedByUserId = entities.OnboardedByUserId,
            };
        }

        public static Farmer Map(this FarmerModel model)
        {
            if (model == null) { return null; }
            return new Farmer
            {
                //Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                UserId = model.UserId,
                OnBoardedOn = model.OnBoardedOn,
                BelongToBusinessCode = model.BelongToBusinessCode,
                State = model.State,
                Lga = model.Lga,
                OnboardedByUserId = model.OnboardedByUserId,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender
            };
        }

        public static CooperativeModel Map(this Cooperative entities)
        {
            if (entities == null) { return null; }
            return new CooperativeModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.ContactPerson,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,
                State = entities.State,
                Lga = entities.Lga,
                BelongToBusinessCode = entities.BelongToBusinessCode,
                ContactPerson = entities.ContactPerson,
                IsAssignedToAgentId = entities.IsAssignedToAgentId,
                HasbeenAssignedAgent = entities.HasbeenAssignedAgent
            };
        }

        public static Cooperative Map(this CooperativeModel model)
        {
            if (model == null) { return null; }
            return new Cooperative
            {
                // Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                ContactPerson = model.PhoneNumber,
                UserId = model.UserId,
                State = model.State,
                Lga = model.Lga,
                BelongToBusinessCode = model.BelongToBusinessCode
                ,
                IsAssignedToAgentId = model.IsAssignedToAgentId,
                PhoneNumber = model.PhoneNumber,
                OnBoardedOn = model.OnBoardedOn,
                HasbeenAssignedAgent = model.HasbeenAssignedAgent
            };
        }


        public static AgentModel Map(this Agent entities)
        {
            if (entities == null) { return null; }
            return new AgentModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.PhoneNumber,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,
                State = entities.State,
                Lga = entities.Lga,
                BelongToBusinessCode = entities.BelongToBusinessCode,

            };
        }

        public static Agent Map(this AgentModel model)
        {
            if (model == null) { return null; }
            return new Agent
            {
                // Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                UserId = model.UserId,
                State = model.State,
                Lga = model.Lga,
                BelongToBusinessCode = model.BelongToBusinessCode
                ,
                PhoneNumber = model.PhoneNumber,
                OnBoardedOn = model.OnBoardedOn,

            };
        }


        #endregion userMgt

        #region walletProvider


        public static SubAccount Map(this SubAccountModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubAccount()
            {
                DOB = model.DOB,
                Status = model.Status,
                SubAccountName = model.SubAccountName,
                BankAccount = model.BankAccount,
                BankName = model.BankName,
                BusinessId = model.BusinessId,
                Bvn = model.Bvn,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn,
                IsMainAccount = model.IsMainAccount,
                NIN = model.NIN,
                PhoneNumber = model.PhoneNumber,
                UserId = model.UserId,
                WalletCode = model.WalletCode,
                FirstName = model.FirstName,
                LastName = model.LastName,
                SubAccountType = Enum.GetName(typeof(SubAccountType), model.SubAccountType)

            };
        }

        public static SubAccountModel Map(this SubAccount entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new SubAccountModel()
            {
                DOB = entity.DOB,
                Status = entity.Status,
                SubAccountId = entity.SubAccountId,
                SubAccountName = entity.SubAccountName,
                BankAccount = entity.BankAccount,
                BankName = entity.BankName,
                BusinessId = entity.BusinessId,
                Bvn = entity.Bvn,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                IsMainAccount = entity.IsMainAccount,
                NIN = entity.NIN,
                PhoneNumber = entity.PhoneNumber,
                UserId = entity.UserId,
                WalletCode = entity.WalletCode,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                SubAccountType = entity.SubAccountType.ToEnum<SubAccountType>(),
            };
        }

        public static SterlingBankWallet Map(this SterlingBankWalletModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SterlingBankWallet()
            {
                WalletId = model.WalletId,
                AccountNumber = model.AccountNumber,
                AgrovestoUserId = model.AgrovestoUserId,
                CreatedOn = model.CreatedOn,
                InternalAccountId = model.InternalAccountId,
                IsABusinessAccount = model.IsABusinessAccount,
                LedgerAccountNumber = model.LedgerAccountNumber,
                OrganizationId = model.OrganizationId,
                OwnerId = model.OwnerId,
                UserId = model.UserId,
            };
        }

        public static SterlingBankWalletModel Map(this SterlingBankWallet entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new SterlingBankWalletModel()
            {
                WalletId = entity.WalletId,
                AccountNumber = entity.AccountNumber,
                AgrovestoUserId = entity.AgrovestoUserId,
                CreatedOn = entity.CreatedOn,
                InternalAccountId = entity.InternalAccountId,
                IsABusinessAccount = entity.IsABusinessAccount,
                LedgerAccountNumber = entity.LedgerAccountNumber,
                OrganizationId = entity.OrganizationId,
                OwnerId = entity.OwnerId,
                UserId = entity.UserId,
            };
        }


        public static WalletProvider Map(this WalletProviderModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WalletProvider()
            {
                Code = model.Code,
                Name = model.Name,
                ProviderId = model.ProviderId,
                WalletProviderParameters = model.WalletProviderParameters.Select(x => x.Map()).ToList(),
                Status = model.Status.ToString()
            };
        }

        public static WalletProviderModel Map(this WalletProvider entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WalletProviderModel()
            {
                Code = entity.Code,
                Name = entity.Name,
                ProviderId = entity.ProviderId,
                WalletProviderParameters = entity.WalletProviderParameters.Select(x => x.Map()).ToList(),
                Status = entity.Status.ToEnum<ProviderStatus>()
            };
        }
        public static WalletProviderParameter Map(this WalletProviderParameterModel model)
        {
            return new WalletProviderParameter()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                WalletProviderProviderId = model.ProviderId,
                Value = model.Value,
                Env = model.Env.ToString()
            };
        }
        public static WalletProviderParameterModel Map(this WalletProviderParameter model)
        {
            return new WalletProviderParameterModel()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                ProviderId = model.WalletProviderProviderId,
                Value = model.Value,
                Env = model.Env.ToEnum<Env>()
            };
        }


        #endregion walletProvider
        #region Business
        public static Business Map(this BusinessModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new Business
            {

                Address = model.Address,

                BusinessCode = model.BusinessCode,
                BusinessEmail = model.BusinessEmail,
                BusinessName = model.BusinessName,
                BusinessStatus = model.BusinessStatus.ToString(),
                ContactEmail = model.ContactEmail,
                DisputeEmail = model.DisputeEmail,
                PhoneNumber = model.PhoneNumber,
                SupportEmail = model.SupportEmail,
                BusinessId = model.BusinessId,
                Category = model.Category.ToString(),
                Website = model.Website,
                BVN = model.BVN,
                NIN = model.NIN,
                BusinessLogo = model.BusinessLogo,
                KycCategory = Enum.GetName(typeof(Category), model.Category),
                BusinessCodeSignage = model.BusinessCodeSignage,
                BusinessCodeSignageSalt = model.BusinessCodeSignageSalt
            };
        }

        public static BusinessLoadModel ViewMap(this BusinessModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new BusinessLoadModel
            {

                Address = model.Address,

                BusinessCode = model.BusinessCode,
                BusinessEmail = model.BusinessEmail,
                BusinessName = model.BusinessName,

                PhoneNumber = model.PhoneNumber,
                Website = model.Website,

                BusinessLogo = model.BusinessLogo,

            };
        }



        public static BusinessModel Map(this Business entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new BusinessModel
            {
                BusinessId = entity.BusinessId,
                Address = entity.Address,
                BusinessCode = entity.BusinessCode,
                BusinessEmail = entity.BusinessEmail,
                BusinessName = entity.BusinessName,
                BusinessStatus = entity.BusinessStatus.ToEnum<BusinessStatus>(),
                ContactEmail = entity.ContactEmail,
                DisputeEmail = entity.DisputeEmail,
                PhoneNumber = entity.PhoneNumber,
                Category = entity.Category.ToEnum<Category>(),
                SupportEmail = entity.SupportEmail,
                Website = entity.Website,
                BVN = entity.BVN,
                BusinessLogo = entity.BusinessLogo,
                NIN = entity.NIN,
                UserId = entity.UserId,
                BusinessCodeSignage = entity.BusinessCodeSignage,
                BusinessCodeSignageSalt = entity.BusinessCodeSignageSalt
            };
        }
        public static BusinessWorker Map(this BusinessWorkerModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new BusinessWorker
            {
                BusinessId = model.BusinessId,
                UserId = model.UserId,
                BusinessWorkerId = model.BusinessWorkerId,
            };
        }

        public static BusinessWorkerModel Map(this BusinessWorker entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new BusinessWorkerModel
            {

                BusinessWorkerId = entity.BusinessWorkerId,
                BusinessId = entity.BusinessId,
                UserId = entity.UserId,
            };
        }



        #endregion Business


        #region FarmerSalesMgt
        #endregion FarmerSalesMgt
        #region RoleMgt
        public static Role Map(this RoleModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new Role
            {
                Descriptions = model.RoleDescription,
                Name = model.RoleName
            };
        }

        public static RoleModel Map(this Role entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new RoleModel
            {
                RoleId = entity.Id,
                RoleDescription = entity.Descriptions,
                RoleName = entity.Name
            };
        }
        #endregion RoleMgt

        #region Admin Mgt
        public static User Map(this UserModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new User
            {
                OTP = model.OTP,
                Salt = model.Salt,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn,
                DateModified = model.DateModified,
                Email = model.Email,
                LastLoginDate = model.LastLoginDate,
                ModifiedBy = model.ModifiedBy,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                Pin = model.Pin,
                UserName = model.Email,
                Name = model.Name,
                //UserType=model.UserType.ToString()
                UserType = Enum.GetName(typeof(UserType), model.UserType)
            };
        }

        public static UserModel Map(this User entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new UserModel
            {
                OTP = entity.OTP,
                Salt = entity.Salt,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                DateModified = entity.DateModified,
                Email = entity.Email,
                LastLoginDate = entity.LastLoginDate,
                ModifiedBy = entity.ModifiedBy,
                Password = entity.Password,
                PhoneNumber = entity.PhoneNumber,
                Pin = entity.Pin,
                UserType = Enum.GetName(typeof(UserType), entity.UserType),//entity.UserType.ToEnum<UserType>(),
                IsActive = entity.IsActive,
                Id = entity.Id,
            };
        }
        #endregion Mgt
    
    }
}
