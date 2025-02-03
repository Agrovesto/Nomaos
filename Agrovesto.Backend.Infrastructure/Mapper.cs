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

        #region walletProvider
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
                KycCategory= Enum.GetName(typeof(Category), model.Category),
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
                RoleId= entity.Id,
                RoleDescription = entity.Descriptions,
                RoleName = entity.Name
            };
        }
        #endregion RoleMgt


        #region Admin Mgt
        public static User Map(this UserModel model)
        {
            if(model == null)
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
                Email= model.Email,
                LastLoginDate = model.LastLoginDate,
                ModifiedBy = model.ModifiedBy,
                Password= model.Password,
                PhoneNumber= model.PhoneNumber,
                Pin= model.Pin,
                UserName=model.Email,
                Name =model.Name,
                //UserType=model.UserType.ToString()
               UserType= Enum.GetName(typeof(UserType), model.UserType)
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
                UserType = entity.UserType.ToEnum<UserType>(),
                IsActive = entity.IsActive,
                Id = entity.Id,
            };
        }
        #endregion Mgt
    }
}
