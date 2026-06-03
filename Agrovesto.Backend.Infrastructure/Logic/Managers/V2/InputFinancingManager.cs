using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IInputFinancingManager
    {
        Task<EopResponseDto> CreateEopAsync(CreateEopRequest request, ListQueryParams query);
        Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, ListQueryParams query);
        Task<bool> DeactivateInputPackage(ReplacePackageItemRequestArgs query);
        Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query);
        Task<EopDto> GetEopByIdAsync(ListQueryParams query);
        Task<PagedResult<EopDto>> GetEopsAsync(EopFilterRequest request, ListQueryParams query);
        Task<InputPackageDto> GetPackageByIdAsync(ListQueryParams query);
        Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query);
        Task<bool> ReactivatePackageAsync(ReplacePackageItemRequestArgs query);
        Task<bool> RemoveItemFromPackage(ReplacePackageItemRequestArgs query);
        Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs query);
        Task<bool> UpdateInputPackageConfig(long packageId, UpdateInputPackageConfigRequest request, InputPackageConfigArgs args);
    }

    public class InputFinancingManager : IInputFinancingManager
    {
        private readonly IUserWalletRepository _userWalletRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IInputFinancingRepository _inputFinancingRepository;
        private readonly IWalletProviderRepository _walletproviderRepo;
        public InputFinancingManager(IUserWalletRepository userWalletRepository,
            IInputFinancingRepository inputFinancingRepository, IBusinessRepository businessRepository,
            IWalletProviderRepository walletProviderRepository)
        {
            _userWalletRepository = userWalletRepository;
            _inputFinancingRepository = inputFinancingRepository;
            _businessRepository = businessRepository;
            _walletproviderRepo = walletProviderRepository;
        }
        private async Task FetchOrganizationDetails(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.Id = hasOrg.OrganizaionId;
            }
        }

        public async Task<EopResponseDto> CreateEopAsync(CreateEopRequest request, ListQueryParams query)
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

        public async Task<bool> CreateInputFinancingPackageConfig(CreateInputPackageRequest request, ListQueryParams query)
        {
            if (request == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Request cannot be null");

            if (query == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Query cannot be null");

            var args = new InputPackageConfigArgs
            {
                createdByUserId = query.UserId
            };

            var organization = await _businessRepository
          .GetOrganizationByCodeAsync(query.organizationCode);

            if (organization == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Organization not found");

            args.organizationId = organization.OrganizaionId;

            if (request.PaybackWalletId <= 0)
            {
                return await _inputFinancingRepository.CreateInputFinancingPackageConfig(request, args);
            }

            var provider = await _walletproviderRepo.Get(request.PaybackWalletId);

            if (provider == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Wallet Provider not found");

            var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
            {
                Id = request.PaybackWalletId,
                UserId = query.UserId,
                businessCode = query.businessCode
            });

            if (userWallet != null)
            {
                args.isSelectedWalletProvisioned = true;
                request.UserWalletId = userWallet.Id;
            }
            else
            {
                args.isSelectedWalletProvisioned = false;
            }

         
            return await _inputFinancingRepository.CreateInputFinancingPackageConfig(request, args);
        }

        public async Task<bool> GenerateInputPackageFromEopAsync(GenerateInputPackageRequest request, ListQueryParams query)
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
            var package = await _inputFinancingRepository.GetPackageByIdAsync(query.Id);

            if (package == null)
                throw new Exception($"Package not found: {query.Id}");

            if (package.IsRepaymentWalletConfigured)
            {
                if (package.WalletProviderId == null)
                    throw new Exception("Wallet provider not configured for this package.");

                var userWallet = await _userWalletRepository.GetUserWalletByProviderId(new UserWalletParams
                {
                    Id = package.WalletProviderId,
                    UserId = query.UserId,
                    businessCode = query.businessCode
                });

                if (userWallet == null)
                    throw new Exception("User wallet not found for repayment configuration.");

                package.RepaymentWallet = new InputPackageRepaymentWallet
                {
                    WalletId = userWallet.Id,
                    WalletName = userWallet.WalletNumber,
                    WalletNumber = userWallet.WalletNumber,
                    WalletProvider = userWallet.ProviderName
                };
            }

            return package;
        }
        public async Task<bool> DeactivateInputPackage(ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.DeactivatePackageAsync(query.PackageId, query.OrganizationId);
        }
        public async Task<bool> UpdateInputPackageConfig(
        long packageId,
        UpdateInputPackageConfigRequest request,
        InputPackageConfigArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (packageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(packageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(args.OrganizationCode);
            if (hasOrg != null)
            {
                args.organizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.UpdateInputPackageConfig(packageId, request, args);
        }

        public async Task<bool> ReactivatePackageAsync(ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.ReactivatePackageAsync(query.PackageId, query.OrganizationId);
        }
        public async Task<PagedResult<InputPackageListModel>> GetPackagesAsync(ListQueryParams query)
        {
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.organizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.GetPackagesAsync(query);
        }

        public async Task<bool> RemoveItemFromPackage(ReplacePackageItemRequestArgs query)
        {
            if(query == null) throw new ArgumentNullException(nameof(query));
            if(query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if(query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
            return await _inputFinancingRepository.RemoveItemFromPackage(query.PackageId, query.InputId);
        }

        public async Task<bool> ReplacePackageItemAsync(ReplacePackageItemRequest request, ReplacePackageItemRequestArgs query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.PackageId <= 0) throw new ArgumentException("Invalid PackageId", nameof(query.PackageId));
            if (query.InputId <= 0) throw new ArgumentException("Invalid InputId", nameof(query.InputId));
           if (request == null) throw new ArgumentNullException(nameof(request));
           if(request.OrganizationProductId <= 0) throw new ArgumentException("Invalid OrganizationProductId", nameof(request.OrganizationProductId));
            var hasOrg = await _businessRepository.GetOrganizationByCodeAsync(query.OrganizationCode);
            if (hasOrg != null)
            {
                query.OrganizationId = hasOrg.OrganizaionId;
            }
            return await _inputFinancingRepository.ReplacePackageItemAsync(request, query);
        }
    }
}
