using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
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
        Task<BusinessWorkerModel> CreateBusinessWorkerAsync(BusinessWorkerModel bWorker, BusinessRegistrationArgs args);
        Task<object> SaveFarmerImage(ImageModel model, BusinessRegistrationArgs args);
    }
}
