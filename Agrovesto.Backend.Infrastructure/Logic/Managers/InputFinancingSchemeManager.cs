using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface IInputFinancingSchemeManager
    {
        Task<(bool Success, string Message)> CreateSchemeAsync(InputFinancingSchemeDto model, BusinessRegistrationArgs args);
        Task<IEnumerable<InputFinancingSchemeDto>> GetAllAsync();
        Task<InputFinancingSchemeDto> GetByIdAsync(long id);
        Task UpdateAsync(long id, CreateInputFinancingSchemeDto dto);
        Task DeleteAsync(long id);
    }
    public class InputFinancingSchemeManager : IInputFinancingSchemeManager
    {
        private readonly IInputFinancingSchemeRepository _repository;
        private readonly IOrganizationConfigurationRepository _configRepository;
        private readonly IBusinessRepository _businessRepository;

        public InputFinancingSchemeManager(
            IInputFinancingSchemeRepository repository,
            IBusinessRepository businessRepository,
            IOrganizationConfigurationRepository configRepository)
        {
            _repository = repository;
            _configRepository = configRepository;
            _businessRepository = businessRepository;
        }

        public async Task<(bool Success, string Message)> CreateSchemeAsync(InputFinancingSchemeDto model, BusinessRegistrationArgs args )
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");
            var org = await _businessRepository.GetOrganizationAsync(args.organizationCode);
            if (org == null) { throw new Exception($"Organization not found"); }

            // 🔹 Basic validation
            if (string.IsNullOrWhiteSpace(model.SchemeName))
                return (false, "Scheme name is required.");

            if (model.TotalValue <= 0)
                return (false, "Total amount must be greater than zero.");

            if (model.RepaymentStartDate <= DateTime.UtcNow)
                return (false, "Repayment start date must be in the future.");

            if (await _repository.ExistsByNameAsync(model.SchemeName))
                return (false, $"A scheme with the name '{model.SchemeName}' already exists.");

            // 🔹 Validate configuration compatibility
            var config = await _configRepository.GetInputConfigByCodeAsync(model.ConfigurationCode);
            if (config == null)
                return (false, "Invalid configuration code.");

            var subscription = config.SubscriptionConfig;
            if (subscription == null)
                return (false, "Invalid or missing subscription configuration.");

            // ✅ Validate Contribution Split
            var (isValid, message) = ConfigurationValidator.ValidateConfiguration(config);
            if (!isValid)
                return (false, message);

            // ✅ Validate compatibility with beneficiary type
            if (model.BeneficiaryType == "Farmer" && !subscription.AllowFarmerSelfSubscription)
                return (false, "This configuration does not support farmer-based subscription.");

            if (model.BeneficiaryType == "Cooperative" && !subscription.AllowCooperativeSubscription)
                return (false, "This configuration does not support cooperative-based subscription.");
            // 🔹 Save
            var entity = new InputFinancingScheme
            {
                SchemeCode = model.SchemeCode,
                Season=model.Season,
                SubscriptionCloseDate=model.SubscriptionCloseDate,
                SubscriptionOpenDate=model.SubscriptionOpenDate,
                RepaymentEndDate=model.RepaymentEndDate,
                BusinessCode=args.businessCode,
                OrganizationCode=args.organizationCode,
                FundingSource= model.FundingSource,
                InputFinancingConfigurationId=config.Id,
                IsActive=true,
                //Targets= model.Targets?.Select(t => new SchemeTarget
                //{
                //    TargetId = t.TargetId,
                //    TargetType = t.TargetType
                //}).ToList(),    
                UnitValuePerHectare = model.UnitValuePerHectare,
              //  WarehouseId = model.WarehouseId,
                SchemeName = model.SchemeName,
                Description = model.Description,
                BeneficiaryType = model.BeneficiaryType,
                TotalValue = model.TotalValue,
                TotalArea = model.TotalArea,
                Commodity = model.Commodity,
                RepaymentStartDate = model.RepaymentStartDate,
                InputFinancingConfigurationCode = model.ConfigurationCode,
                CreatedBy = args.UserId,
                CreatedOn = model.CreatedOn
            };

            await _repository.AddAsync(entity);
            return (true, "Input financing scheme created successfully.");
        }
        public async Task<IEnumerable<InputFinancingSchemeDto>> GetAllAsync()
        {
            var schemes = await _repository.GetAllAsync();
            return schemes.Select(MapToDto);
        }

        public async Task<InputFinancingSchemeDto> GetByIdAsync(long id)
        {
            var scheme = await _repository.GetByIdAsync(id);
            return MapToDto(scheme);
        }

        public async Task UpdateAsync(long id, CreateInputFinancingSchemeDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) throw new Exception("Scheme not found");

            existing.SchemeName = dto.SchemeName;
            existing.Description = dto.Description;
            existing.Commodity = dto.Commodity;
            existing.TotalValue = dto.TotalValue;
            existing.IsActive = dto.IsActive;

            await _repository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(long id)
        {
            await _repository.DeleteAsync(id);
        }

        private InputFinancingSchemeDto MapToDto(InputFinancingScheme scheme)
        {
            if (scheme == null) return null;

            return new InputFinancingSchemeDto
            {
                Id = scheme.Id,
                SchemeName = scheme.SchemeName,
                SchemeCode = scheme.SchemeCode,
                ConfigurationCode=scheme.InputFinancingConfigurationCode,
                CreatedBy = scheme.CreatedBy,
                CreatedOn=scheme.CreatedOn,
                Description = scheme.Description,
                Commodity = scheme.Commodity,
                BeneficiaryType = scheme.BeneficiaryType,
                TotalArea = scheme.TotalArea,
                TotalValue = scheme.TotalValue,
                FundingSource = scheme.FundingSource,
                Season = scheme.Season,
                //WarehouseId = scheme.WarehouseId,
                InputFinancingConfigurationId = scheme.InputFinancingConfigurationId,
                IsActive = scheme.IsActive,
                UnitValuePerHectare = scheme.UnitValuePerHectare,
                RepaymentStartDate = scheme.RepaymentStartDate,
                RepaymentEndDate = scheme.RepaymentEndDate,
                SubscriptionOpenDate = scheme.SubscriptionOpenDate,
                SubscriptionCloseDate = scheme.SubscriptionCloseDate,
                OrganizationCode=scheme.OrganizationCode,
                BusinessCode=scheme.BusinessCode,
                //Targets = scheme.Targets?.Select(t => new SchemeTargetDto
                //{
                //    TargetId = t.TargetId,
                //    TargetType = t.TargetType
                //}).ToList()
            };
        }
    }
}
