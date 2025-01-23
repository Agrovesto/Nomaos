using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure
{
    public static class Mapper
    {
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
                BusinessStatus = model.Status.ToString(),
                ContactEmail = model.ContactEmail,
                DisputeEmail = model.DisputeEmail,
                PhoneNumber = model.PhoneNumber,
                SupportEmail = model.SupportEmail,
                BusinessId = model.BusinessId,
                Category = model.Category.ToString(),
                Website = model.Website,
                SwitchCategoryRequest = model.Request.ToString(),
                BusinessTypeId = model.BusinessTypeId,
                BVN = model.BVN,
                NIN = model.NIN,
                RegistrationTypeId = model.RegistrationTypeId,
                BusinessCategoryType = model.BusinessCategoryType.ToString()
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
                Status = entity.BusinessStatus.ToEnum<BusinessStatus>(),
                ContactEmail = entity.ContactEmail,
                DisputeEmail = entity.DisputeEmail,
                PhoneNumber = entity.PhoneNumber,
                Category = entity.Category.ToEnum<Category>(),
                SupportEmail = entity.SupportEmail,
                Website = entity.Website,
                Request = entity.SwitchCategoryRequest.ToEnum<SwitchCategoryRequest>(),
                BusinessTypeId = entity.BusinessTypeId,
                KycCategory = entity.KycCategory,
                BVN = entity.BVN,
                NIN = entity.NIN,
                RegistrationTypeId = entity.RegistrationTypeId,
                BusinessCategoryType = entity.BusinessCategoryType.ToEnum<BusinessCategoryType>(),
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
                IsOwner = model.IsOwner,
                UserId = model.UserId,
                UserBusinessId = model.UserBusinessId,
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
                
                UserBusinessId = entity.UserBusinessId,
                BusinessId = entity.BusinessId,
                IsOwner = entity.IsOwner,
                UserId = entity.UserId,
            };
        }

        #endregion Business

    }
}
