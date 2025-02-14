using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IBusinessManager
    {
        Task<BusinessModel> CreateBusiness(BusinessModel business, BusinessRegistrationArgs args);
        Task<SubAccountModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args);
        Task<BusinessWorkerModel> CreateBusinessWorkerAsync(BusinessWorkerModel bWorker, BusinessRegistrationArgs args);
        Task<BusinessLoadModel> GetBusinessBySinageSaltAsync(string businessCode, string signagesalt);
        Task UpdateUserAccountProfile(SubAccountModel subaccount, BusinessRegistrationArgs args, string userId);
    }
}
