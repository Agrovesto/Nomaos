using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;


namespace Agrovesto.Backend.Infrastructure.Logic.Services
{

    public interface IBulkFarmerUploadService
    {
        Task<BulkFarmerUploadResultDto> ProcessAsync(
           IReadOnlyList<BulkFarmerRowDto> rows, OnBoardingArgs args);
    }

    public class BulkFarmerUploadService : IBulkFarmerUploadService
    {
        private readonly DataContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<BulkFarmerUploadService> _logger;

        private const int BatchSize = 50;

        public BulkFarmerUploadService(
            DataContext db,
            UserManager<User> userManager,
            ILogger<BulkFarmerUploadService> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }


        /// <param name="rows">Parsed rows from the uploaded file.</param>
        public async Task<BulkFarmerUploadResultDto> ProcessAsync(
    IReadOnlyList<BulkFarmerRowDto> rows, OnBoardingArgs args)
        {
            var result = new BulkFarmerUploadResultDto { TotalRows = rows.Count };
            \
            var strategy = _db.Database.CreateExecutionStrategy();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.RowIndex = i + 1;

                var validationErrors = BulkFarmerRowValidator.Validate(row);
                if (validationErrors.Any())
                {
                    RecordFailure(result, row, string.Join(" | ", validationErrors));
                    continue;
                }

                var currentRow = row;

                var rowSucceeded = false;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        await OnboardSingleFarmerAsync(currentRow, args);
                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();
                        rowSucceeded = true;

                        _logger.LogInformation(
                            "Row {Row}: farmer {Phone} onboarded successfully.",
                            currentRow.RowIndex, currentRow.PhoneNumber);
                    }
                    catch (DuplicatePhoneException ex)
                    {
                        await tx.RollbackAsync();
                        RecordFailure(result, currentRow, ex.Message);
                        _logger.LogWarning(
                            "Row {Row}: duplicate – {Msg}", currentRow.RowIndex, ex.Message);
                  
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Row {Row}: unhandled exception.", currentRow.RowIndex);
                      
                        throw;
                    }
                });

                if (rowSucceeded)
                    result.Succeeded++;
                else if (!result.Errors.Any(e => e.RowIndex == currentRow.RowIndex))
                    RecordFailure(result, currentRow, "Unexpected error after retries exhausted.");
            }

            return result;
        }
        private async Task OnboardSingleFarmerAsync(
            BulkFarmerRowDto row, OnBoardingArgs args)
        {
            bool phoneExists = await _db.Users
                .AnyAsync(u => u.PhoneNumber == row.PhoneNumber.Trim());

            if (phoneExists)
                throw new DuplicatePhoneException(
                    $"Phone number '{row.PhoneNumber}' already exists.");

            var (defaultPassword, salt) = GenerateDefaultCredentials();

            var user = new User
            {
                UserName = row.PhoneNumber.Trim(),
                PhoneNumber = row.PhoneNumber.Trim(),
                Email = row.Email?.Trim(),
                Name = row.Name.Trim(),
                NIN = row.NIN,
                BVN = row.BVN,
                Salt = salt,
                UserType = "Farmer",
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                CreatedOn = DateTimeOffset.UtcNow,
                IsActive = true,
                FarmerOnboardingStatus = FarmerOnboardingStatus.Pending,
                HasDefaultPassword = true,
                Pin = "0000"
            };

            var identityResult = await _userManager.CreateAsync(user, defaultPassword);
            if (!identityResult.Succeeded)
            {
                var msg = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"User creation failed: {msg}");
            }

            var farmer = new Farmer
            {
                UserId = user.Id,
                BelongToBusinessCode = args.businessCode,
                Name = row.Name.Trim(),
                PhoneNumber = row.PhoneNumber.Trim(),
                Email = row.Email?.Trim(),
                Gender = row.Gender,
                DateOfBirth = row.DateOfBirth,
                Location = row.Location,
                State = row.State,
                Lga = row.Lga,
                Ward = row.Ward,
                FarmerCategory = row.FarmerCategory,
                YearsInAgric = row.YearsInAgric,
                HighestLevelOfEducation = row.HighestLevelOfEducation,
                OnboardedByUserId = args.UserId,
                OnBoardedOn = DateTimeOffset.UtcNow,
                Level = FarmerLevel.Level1
            };
            _db.Farmers.Add(farmer);

            await _db.SaveChangesAsync();
           
            if (row.SelectedItems.Any())
            {
                await AddFarmerValueChainItemsAsync(farmer.Id, row.SelectedItems, args.UserId);
            }
            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                await LinkToOrganizationAsync(
                    user, farmer, args.organizationCode, args.UserId);
            }

          
            if (args.CooperativeIds.Count > 0)
            {

                await LinkToCooperativeAsync(
                    farmer, args.CooperativeIds,
                    args.organizationCode, args.UserId);
            }

            if (args.AgentIds.Count > 0)
            {
                await LinkToAgentAsync(
                    farmer, args.AgentIds,
                    args.organizationCode, args.UserId);
            }
        }
        private async Task AddFarmerValueChainItemsAsync(
            long farmerId,
            List<ValueChainSelectionDto> selections,
            string createdByUserId)
        {
            var incomingItems = selections.Where(v => !string.IsNullOrWhiteSpace(v.ValueChainType)
            && v.Items != null)
                    .SelectMany(v => v.Items
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => new
                    {
                        ValueChainType = v.ValueChainType.Trim(),
                        ItemName = item.Trim()
                    }))
                    .Distinct()
                    .ToList();

            if (!incomingItems.Any()) return;

            var existingKeys = (await _db.FarmerValueChainItems
                .Where(x => x.FarmerId == farmerId)
                .Select(x => x.ValueChainType + "|" + x.ItemName)
                .ToListAsync())
                .ToHashSet();

            var toAdd = incomingItems
      .Where(i => !existingKeys.Contains($"{i.ValueChainType}|{i.ItemName}"))
      .Select(i => new FarmerValueChainItem
      {
          FarmerId = farmerId,
          ValueChainType = i.ValueChainType,
          ItemName = i.ItemName
      })
      .ToList();

            if (toAdd.Any())
                _db.FarmerValueChainItems.AddRange(toAdd);


        }
       
        private async Task LinkToOrganizationAsync(
            User user, Farmer farmer, string orgCode, string onboardedBy)
        {
            bool userOrgExists = await _db.UserOrganizations
                .AnyAsync(uo => uo.UserId == user.Id && uo.OrganizationCode == orgCode);

            if (!userOrgExists)
            {
                _db.UserOrganizations.Add(new UserOrganization
                {
                    UserId = user.Id,
                    OrganizationCode = orgCode,
                    IsActive = true,

                });
            }

            bool farmerOrgExists = await _db.FarmerOrganizations
                .AnyAsync(fo => fo.FarmerId == farmer.Id && fo.OrganizationCode == orgCode);

            if (!farmerOrgExists)
            {
                _db.FarmerOrganizations.Add(new FarmerOrganization
                {
                    FarmerId = farmer.Id,
                    OrganizationCode = orgCode,
                    OnboardedByUserId = onboardedBy,
                    OnBoardedOn = DateTimeOffset.UtcNow,
                    IsActive = true,

                });
            }
        }

        private async Task LinkToCooperativeAsync(
       Farmer farmer,
       List<long?> cooperativeIds,
       string? orgCode,
       string onboardedBy)
        {
            var validCooperativeIds = cooperativeIds?
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList() ?? new List<long>();

            foreach (var cooperativeId in validCooperativeIds)
            {
                bool exists = await _db.FarmerCooperatives
                    .AnyAsync(fc =>
                        fc.FarmerId == farmer.Id &&
                        fc.CooperativeId == cooperativeId);

                if (!exists)
                {
                    _db.FarmerCooperatives.Add(new FarmerCooperative
                    {
                        FarmerId = farmer.Id,
                        CooperativeId = cooperativeId,
                        OrganizationCode = orgCode,
                        OnboardedByUserId = onboardedBy,
                        JoinedOn = DateTimeOffset.UtcNow,
                        IsActive = true
                    });
                }
            }
        }

        private async Task LinkToAgentAsync(
            Farmer farmer,
            List<long?> agentIds,
            string? orgCode,
            string onboardedBy)
        {
            var validAgentIds = agentIds?
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToList() ?? new List<long>();

            foreach (var agentId in validAgentIds)
            {
                bool exists = await _db.FarmerAgents
                    .AnyAsync(fa =>
                        fa.FarmerId == farmer.Id &&
                        fa.AgentId == agentId);

                if (!exists)
                {
                    _db.FarmerAgents.Add(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agentId,
                        OrganizationCode = orgCode,
                        AssignedByUserId = onboardedBy,
                        AssignedOn = DateTimeOffset.UtcNow,
                        IsActive = true
                    });
                }
            }
        }
        private static (string password, string salt) GenerateDefaultCredentials()
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var passwordChars = RandomNumberGenerator.GetItems<char>(chars, 12);
            var password = new string(passwordChars);

            return (password, salt);
        }

        private static void RecordFailure(
            BulkFarmerUploadResultDto result, BulkFarmerRowDto row, string reason)
        {
            result.Failed++;
            result.Errors.Add(new FarmerRowErrorDto
            {
                RowIndex = row.RowIndex,
                PhoneNumber = row.PhoneNumber ?? "(missing)",
                Reason = reason,
            });
        }


        private List<ValueChainSelectionDto> GetValueChains(BulkOnboardingExcelRow row)
        {
            if (string.IsNullOrWhiteSpace(row.PartOfAgriculture))
                return new List<ValueChainSelectionDto>();

            var items = string.IsNullOrWhiteSpace(row.ProductTypes)
                ? new List<string>()
                : row.ProductTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            return new List<ValueChainSelectionDto>
            {
                new() { ValueChainType = row.PartOfAgriculture, Items = items }
            };
        }
    }
    public sealed class DuplicatePhoneException : Exception
    {
        public DuplicatePhoneException(string message) : base(message) { }
    }
}
