using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
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
    public enum FarmerRowOutcome { Created, AlreadyExisted }
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
    
       /** private async Task ProcessBatchAsync(
            List<BulkFarmerRowDto> batch,
            OnBoardingArgs args,
            BulkFarmerUploadResultDto result,
            HashSet<string> phoneSet,
            HashSet<string> emailSet,
            HashSet<string> bvnSet,
            HashSet<string> ninSet,
            HashSet<(string UserId, string BusinessCode)> existingFarmerScopes,
            Dictionary<(string UserId, string BusinessCode), long> existingFarmersByUserAndBusiness)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    foreach (var row in batch)
                    {
                        var phone = row.PhoneNumber.Trim();
                        var email = row.Email?.Trim();

                        // --- In-memory duplicate checks: no DB round trip ---
                        if (!string.IsNullOrWhiteSpace(email) && emailSet.Contains(email))
                        {
                            Failure.RecordFailure(result, row, $"Duplicate Email '{email}' for row {row.RowIndex}.");
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(row.BVN) && bvnSet.Contains(row.BVN))
                        {
                            Failure.RecordFailure(result, row, $"Duplicate BVN '{row.BVN}' for row {row.RowIndex}.");
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(row.NIN) && ninSet.Contains(row.NIN))
                        {
                            Failure.RecordFailure(result, row, $"Duplicate NIN '{row.NIN}' for row {row.RowIndex}.");
                            continue;
                        }

                        var userExists = phoneSet.Contains(phone);
                        User user;

                        if (!userExists)
                        {
                            var (defaultPassword, salt) = GenerateDefaultCredentials();

                            user = new User
                            {
                                UserName = phone,
                                PhoneNumber = phone,
                                Email = email,
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
                                Pin = "1234"
                            };

                            var identityResult = await _userManager.CreateAsync(user, defaultPassword);
                            if (!identityResult.Succeeded)
                            {
                                var msg = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                                Failure.RecordFailure(result, row, $"User creation failed: {msg}");
                                continue;
                            }

                            // Mark as used immediately so later rows in this same batch see it too.
                            phoneSet.Add(phone);
                            if (!string.IsNullOrWhiteSpace(email)) emailSet.Add(email);
                            if (!string.IsNullOrWhiteSpace(row.BVN)) bvnSet.Add(row.BVN);
                            if (!string.IsNullOrWhiteSpace(row.NIN)) ninSet.Add(row.NIN);
                        }
                        else
                        {
                            user = await _db.Users.AsNoTracking()
                                .FirstAsync(u => u.PhoneNumber == phone);
                        }

                        var scopeKey = (user.Id, args.businessCode);

                        if (existingFarmerScopes.Contains(scopeKey))
                        {
                            Failure.RecordFailure(result, row, $"Duplicate phone '{phone}' for row {row.RowIndex}.");
                            continue;
                        }

                        Farmer farmer;
                        if (existingFarmersByUserAndBusiness.TryGetValue(scopeKey, out var farmerId))
                        {
                            farmer = await _db.Farmers.FirstAsync(f => f.Id == farmerId);
                        }
                        else
                        {
                            farmer = new Farmer
                            {
                                UserId = user.Id,
                                BelongToBusinessCode = args.businessCode,
                                Name = row.Name.Trim(),
                                PhoneNumber = phone,
                                Email = email,
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
                            existingFarmersByUserAndBusiness[scopeKey] = 0; // placeholder until saved
                        }

                        if (row.SelectedItems.Any())
                            await AddFarmerValueChainItemsAsync(farmer, row.SelectedItems);

                        if (!string.IsNullOrWhiteSpace(args.organizationCode))
                            await LinkToOrganizationAsync(user, farmer, args.organizationCode, args.UserId);

                        if (args.CooperativeIds.Count > 0)
                            await LinkToCooperativeAsync(farmer, args.CooperativeIds, args.organizationCode, args.UserId);

                        if (args.AgentIds.Count > 0)
                            await LinkToAgentAsync(farmer, args.AgentIds, args.organizationCode, args.UserId);

                        result.Succeeded++;
                    }

                    // ONE SaveChanges + ONE commit per batch, instead of per row.
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Batch failed — falling back to per-row processing.");

                    // Fallback: something in this batch broke the whole SaveChanges (e.g. a race-condition
                    // duplicate that slipped past the in-memory check). Retry the batch one row at a time
                    // so we isolate exactly which row failed instead of losing the whole batch.
                    await ProcessRowByRowFallbackAsync(batch, args, result);
                }
            });
        }
        **/
        
        /**private async Task AddFarmerValueChainItemsAsync(
    Farmer farmer,
    List<ValueChainSelectionDto> selections)
        {
            var incomingItems = selections
                .Where(v => !string.IsNullOrWhiteSpace(v.ValueChainType) && v.Items != null)
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

            HashSet<string> existingKeys;

            if (farmer.Id != 0)
            {
                // Existing farmer already in the DB — check what's already there to avoid duplicate inserts.
                existingKeys = (await _db.FarmerValueChainItems
                    .Where(x => x.FarmerId == farmer.Id)
                    .Select(x => x.ValueChainType + "|" + x.ItemName)
                    .ToListAsync())
                    .ToHashSet();
            }
            else
            {
                // Brand new farmer, not yet saved — nothing can exist for it yet, skip the query entirely.
                existingKeys = new HashSet<string>();
            }

            var toAdd = incomingItems
                .Where(i => !existingKeys.Contains($"{i.ValueChainType}|{i.ItemName}"))
                .Select(i => new FarmerValueChainItem
                {
                    Farmer = farmer, // navigation property — EF resolves FarmerId at SaveChanges time,
                                     // even before farmer.Id has been generated
                    ValueChainType = i.ValueChainType,
                    ItemName = i.ItemName
                })
                .ToList();

            if (toAdd.Any())
                _db.FarmerValueChainItems.AddRange(toAdd);
        }
        private async Task ProcessRowByRowFallbackAsync(
    List<BulkFarmerRowDto> batch,
    OnBoardingArgs args,
    BulkFarmerUploadResultDto result)
        {
            // Whatever got added to the tracker during the failed batch attempt must be cleared,
            // or retrying will try to re-insert entities that are still (invalidly) tracked.
            _db.ChangeTracker.Clear();

            var strategy = _db.Database.CreateExecutionStrategy();

            foreach (var row in batch)
            {
                // Rows already flagged by the in-memory checks earlier never reached SaveChanges —
                // no need to retry those.
                if (result.Errors.Any(e => e.RowIndex == row.RowIndex))
                    continue;

                var rowSucceeded = false;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        await OnboardSingleFarmerWithDbChecksAsync(row, args);
                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();
                        rowSucceeded = true;

                        _logger.LogInformation(
                            "Row {Row}: farmer {Phone} onboarded successfully (fallback path).",
                            row.RowIndex, row.PhoneNumber);
                    }
                    catch (DuplicatePhoneException ex)
                    {
                        await tx.RollbackAsync();
                        Failure.RecordFailure(result, row, ex.Message);
                        _logger.LogWarning("Row {Row}: duplicate phone (fallback) – {Msg}", row.RowIndex, ex.Message);
                    }
                    catch (DuplicateFarmerFieldException ex)
                    {
                        await tx.RollbackAsync();
                        Failure.RecordFailure(result, row, ex.Message);
                        _logger.LogWarning("Row {Row}: duplicate {Field} (fallback) – {Msg}", row.RowIndex, ex.FieldName, ex.Message);
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        await tx.RollbackAsync();
                        var msg = ExtractDuplicateFieldMessage(ex);
                        Failure.RecordFailure(result, row, msg);
                        _logger.LogWarning("Row {Row}: unique constraint violation (fallback) – {Msg}", row.RowIndex, msg);
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Row {Row}: unhandled exception (fallback).", row.RowIndex);
                        Failure.RecordFailure(result, row, $"Unexpected error: {ex.Message}");
                    }
                });

                if (rowSucceeded)
                    result.Succeeded++;
            }
        }
       **/
        
        /// <param name="rows">Parsed rows from the uploaded file.</param>
        public async Task<BulkFarmerUploadResultDto> ProcessAsync(
    IReadOnlyList<BulkFarmerRowDto> rows, OnBoardingArgs args)
        {
            var result = new BulkFarmerUploadResultDto { TotalRows = rows.Count };

            var strategy = _db.Database.CreateExecutionStrategy();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.RowIndex = i + 1;

                var validationErrors = BulkFarmerRowValidator.Validate(row);
                if (validationErrors.Any())
                {
                    Failure.RecordFailure(result, row, string.Join(" | ", validationErrors));
                    continue;
                }

                var currentRow = row;

                var rowSucceeded = false;
                FarmerRowOutcome? outcome = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        outcome = await OnboardSingleFarmerWithDbChecksAsync(currentRow, args);
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
                        Failure.RecordFailure(result, currentRow, ex.Message); // ← already correct, nothing to change
                        _logger.LogWarning("Row {Row}: duplicate – {Msg}", currentRow.RowIndex, ex.Message);
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        await tx.RollbackAsync();
                        var msg = ExtractDuplicateFieldMessage(ex); // pull the field/value out of ex.InnerException.Message
                        Failure.RecordFailure(result, currentRow, msg);
                        _logger.LogWarning("Row {Row}: unique constraint violation – {Msg}", currentRow.RowIndex, msg);
                    }
                    catch (DuplicateFarmerFieldException ex)
                    {
                        await tx.RollbackAsync();
                        Failure.RecordFailure(result, currentRow, ex.Message);
                        _logger.LogWarning("Row {Row}: duplicate {Field} – {Msg}", currentRow.RowIndex, ex.FieldName, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Row {Row}: unhandled exception.", currentRow.RowIndex);
                        Failure.RecordFailure(result, currentRow, $"Unexpected error: {ex.Message}");

                        throw;
                    }
                });

                if (rowSucceeded)
                {
                    if (outcome == FarmerRowOutcome.AlreadyExisted)
                        result.AlreadyExisted++;
                    else
                        result.Succeeded++;
                }
                else if (!result.Errors.Any(e => e.RowIndex == currentRow.RowIndex))
                    Failure.RecordFailure(result, currentRow, "Unexpected error after retries exhausted.");
            }

            return result;
        }


        private async Task<FarmerRowOutcome> OnboardSingleFarmerWithDbChecksAsync(BulkFarmerRowDto row, OnBoardingArgs args)
        {
            var phone = row.PhoneNumber.Trim();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

            if (user == null)
            {
                if (!string.IsNullOrWhiteSpace(row.Email))
                {
                    var email = row.Email.Trim();
                    var emailExists = await _db.Users.AnyAsync(u =>
                        u.Email != null && u.Email.ToLower() == email.ToLower());

                    if (emailExists)
                        throw new DuplicateFarmerFieldException("Email", email, row.RowIndex);
                }

                if (!string.IsNullOrWhiteSpace(row.BVN))
                {
                    var bvnExists = await _db.Users.AnyAsync(u => u.BVN == row.BVN);
                    if (bvnExists)
                        throw new DuplicateFarmerFieldException("BVN", row.BVN, row.RowIndex);
                }

                if (!string.IsNullOrWhiteSpace(row.NIN))
                {
                    var ninExists = await _db.Users.AnyAsync(u => u.NIN == row.NIN);
                    if (ninExists)
                        throw new DuplicateFarmerFieldException("NIN", row.NIN, row.RowIndex);
                }

                var (defaultPassword, salt) = GenerateDefaultCredentials();

                user = new User
                {
                    UserName = phone,
                    PhoneNumber = phone,
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
                    Pin = "1234"
                };

                var identityResult = await _userManager.CreateAsync(user, defaultPassword);
                if (!identityResult.Succeeded)
                {
                    var msg = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"User creation failed: {msg}");
                }
            }

               var farmer = await FindExistingFarmerInScopeAsync(user.Id, args.businessCode, args.organizationCode);
            var isNewFarmer = farmer == null;

            if (isNewFarmer)
            {
                farmer = new Farmer
                {
                    UserId = user.Id,
                    BelongToBusinessCode = args.businessCode,
                    Name = row.Name.Trim(),
                    PhoneNumber = phone,
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
            }
            
            if (row.SelectedItems.Any())
                await AddFarmerValueChainItemsAsync(farmer.Id, row.SelectedItems, args.UserId);

            if (!string.IsNullOrWhiteSpace(args.organizationCode))
                await LinkToOrganizationAsync(user, farmer, args.organizationCode, args.UserId);

            if (args.CooperativeIds.Count > 0)
                await LinkToCooperativeAsync(farmer, args.CooperativeIds, args.organizationCode, args.UserId);

            if (args.AgentIds.Count > 0)
                await LinkToAgentAsync(farmer, args.AgentIds, args.organizationCode, args.UserId);

            return isNewFarmer ? FarmerRowOutcome.Created : FarmerRowOutcome.AlreadyExisted;

        }
        private async Task<Farmer?> FindExistingFarmerInScopeAsync(
    string userId, string businessCode, string? organizationCode)
        {
            var farmerQuery = _db.Farmers
                .Where(f => f.UserId == userId && f.BelongToBusinessCode == businessCode);

            if (string.IsNullOrWhiteSpace(organizationCode))
            {
                return await farmerQuery.FirstOrDefaultAsync();
            }

            return await farmerQuery
                .Where(f => _db.FarmerOrganizations.Any(fo =>
                    fo.FarmerId == f.Id &&
                    fo.OrganizationCode == organizationCode &&
                    fo.IsActive))
                .FirstOrDefaultAsync();
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
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx &&
                   (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }

        private static string ExtractDuplicateFieldMessage(DbUpdateException ex)
        {
            return ex.InnerException?.Message ?? "A duplicate value violated a unique constraint.";
        }
    }
    public class DuplicateFarmerFieldException : Exception
    {
        public string FieldName { get; }
        public string Value { get; }
        public int RowIndex { get; }

        public DuplicateFarmerFieldException(string fieldName, string value, int rowIndex)
            : base($"Duplicate {fieldName} '{value}' for row {rowIndex}.")
        {
            FieldName = fieldName;
            Value = value;
            RowIndex = rowIndex;
        }
    }

  }
