using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IInputFinancingManager
    {
        Task<EconomicsOfProduction> CreateEopAsync(CreateEopRequest request, ListQueryParams query);
        Task<InputPackage> CreateManualInputPackageAsync(CreateInputPackageRequest request, ListQueryParams query);
        Task<InputPackage> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query);
        Task<EopDto> GetEopByIdAsync(ListQueryParams query);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request, ListQueryParams query);
        Task<InputPackageDto> GetPackageByIdAsync(ListQueryParams query);
        Task<PagedResult<InputPackageDto>> GetPackagesAsync(InputPackageFilterRequest request, ListQueryParams query);
    }

    public class InputFinancingManager : IInputFinancingManager
    {

        private readonly IBusinessRepository _businessRepository;
        private readonly IInputFinancingRepository _inputFinancingRepository;

        public InputFinancingManager(IInputFinancingRepository inputFinancingRepository, IBusinessRepository businessRepository)
        {
            _inputFinancingRepository = inputFinancingRepository;
            _businessRepository = businessRepository;
        }
        private async Task FetchOrganizationDetails(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
        }

        public async Task<EconomicsOfProduction> CreateEopAsync(CreateEopRequest request, ListQueryParams query)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
            if (business == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Business Record not found"); }
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }

            return await _inputFinancingRepository.CreateEopAsync(request);
        }

        public async Task<InputPackage> CreateManualInputPackageAsync(CreateInputPackageRequest request, ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }

            return await _inputFinancingRepository.CreateManualInputPackageAsync(request);
        }

        public async Task<InputPackage> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }

            return await _inputFinancingRepository.GenerateInputPackageFromEopAsync(request);
        }

        public async Task<EopDto> GetEopByIdAsync(ListQueryParams query)
        {
            await FetchOrganizationDetails(query);
            return await _inputFinancingRepository.GetEopByIdAsync(query.Id);
        }

        public async Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request, ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                request.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetEopsAsync(request);
        }

        public async Task<InputPackageDto> GetPackageByIdAsync(ListQueryParams query)
        {
            return await _inputFinancingRepository.GetPackageByIdAsync(query.Id);
        }

        public async Task<PagedResult<InputPackageDto>> GetPackagesAsync(InputPackageFilterRequest request, ListQueryParams query)
        {
            await FetchOrganizationDetails(query);
            return await _inputFinancingRepository.GetPackagesAsync(request);
        }
    }
}
