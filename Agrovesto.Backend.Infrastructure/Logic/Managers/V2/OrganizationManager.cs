using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IOrganizationManager
    {
        Task<OrganizationLocationModel> CreateLocation(OrganizationLocationModel model, ListQueryParams query);
        Task<List<OrganizationLocationModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationById(ListQueryParams query);
    }
    public class OrganizationManager : IOrganizationManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IOrganizationRepository _organizationRepository;

        public OrganizationManager(IBusinessRepository businessRepository, IOrganizationRepository organizationRepository)
        {
            _organizationRepository= organizationRepository;
            _businessRepository= businessRepository;
        }

        public async Task<List<OrganizationLocationModel>> GetLocationAsync(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }


            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
            return await _organizationRepository.GetLocationAsync(query);

        }
        public async Task<OrganizationLocationModel> GetLocationById(ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

            return await _organizationRepository.GetLocationByIAsync(query.Id);

        }

        public async Task<OrganizationLocationModel> CreateLocation(OrganizationLocationModel model, ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }

                var locationType = await _businessRepository.GetLocationByIAsync(model.LocationTypeId);
                if (locationType == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Invalid Location Type"); }

                var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
                if (hasOrg != null)
                {
                    model.OrganizationId = hasOrg.OrganizaionId;
                }
                model.BusinessCode = business.BusinessCode;


                return await _organizationRepository.CreateLocationAsync(model);
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
