using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BulkUploadJob = Agrovesto.Backend.Infrastructure.Entities.Common.BulkUploadJob;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public class BulkOnboardingProcessor
    {
        private readonly DataContext _context;
        private readonly IExcelConverterService _excelConverterService;
        private readonly ILogger<BulkOnboardingProcessor> _logger;

        public BulkOnboardingProcessor(
            DataContext context,
            IExcelConverterService excelConverterService,
            ILogger<BulkOnboardingProcessor> logger)
        {
            _context = context;
            _excelConverterService = excelConverterService;
            _logger = logger;
        }

        public async Task ProcessAsync(string filePath, OnBoardingArgs args, CancellationToken token)
        {
            await ProcessFileAsync(filePath, args, null, token);
        }

        public async Task ProcessJobAsync(Guid jobId, CancellationToken token)
        {
            var job = await _context.BulkUploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, token);
            if (job == null)
            {
                _logger.LogWarning("Bulk job {jobId} not found", jobId);
                return;
            }

            if (string.IsNullOrWhiteSpace(job.FilePath) || !File.Exists(job.FilePath))
            {
                job.Status = "Failed";
                job.ErrorMessage = "File not found";
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(token);
                return;
            }

            job.Status = "Processing";
            await _context.SaveChangesAsync(token);

            var args = new OnBoardingArgs
            {
                organizationCode = job.OrganizationCode,
                // resume with sensible defaults so required fields like UserType are not null
                userType = "Farmer",
                UserId = job.CreatedByUserId,
                OnboardedByUserId = job.OnboardedByUserId
            };
            await ProcessFileAsync(job.FilePath, args, job, token);
        }

        private async Task ProcessFileAsync(string filePath, OnBoardingArgs args, BulkUploadJob job, CancellationToken token)
        {
            args.userType = "Farmer";
            try
            {
                _logger.LogInformation("Bulk processor starting for file {file}", filePath);

                using var stream = File.OpenRead(filePath);
                var rows = _excelConverterService.BulkParseExcel(stream);

                if (rows == null || !rows.Any())
                {
                    _logger.LogWarning("No rows found in file {file}", filePath);

                    if (job != null)
                    {
                        job.Status = "Completed";
                        job.CompletedAt = DateTime.UtcNow;
                        job.Successful = 0;
                        job.Failed = rows?.Count ?? 0;
                        await _context.SaveChangesAsync(token);
                    }

                    return;
                }

                // Ensure job exists
                if (job == null)
                {
                    job = new BulkUploadJob
                    {
                        Id = Guid.NewGuid(),
                        FilePath = filePath,
                        OrganizationCode = args.organizationCode,
                        CreatedByUserId = args.UserId ?? args.OnboardedByUserId ?? "system",
                        OnboardedByUserId = args.OnboardedByUserId ?? args.UserId ?? "system",
                        ErrorMessage = string.Empty,
                        Status = "Processing",
                        CreatedAt = DateTime.UtcNow,
                        TotalRows = rows.Count
                    };

                    _context.BulkUploadJobs.Add(job);
                    await _context.SaveChangesAsync(token);
                }

                var aggregates = new List<BulkRowAggregate>();
                int rowNumber = 2;

                foreach (var row in rows)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName))
                            throw new Exception("FirstName and LastName are required.");

                        if (!int.TryParse(row.YearsInAgric, out var years) || years < 0 || years > 100)
                            throw new Exception($"Invalid YearsInAgric: {row.YearsInAgric}");

                        if (string.IsNullOrWhiteSpace(row.Email) && string.IsNullOrWhiteSpace(row.PhoneNumber))
                            throw new Exception("Email or PhoneNumber is required.");

                        var salt = EncryptionUtil.GetSalt();

                        var user = BuildUserEntity(new OnboardingRequestModel
                        {
                            Email = row.Email,
                            PhoneNumber = row.PhoneNumber,
                            Password = "Admin123@",
                            Pin = "0000",
                            FirstName = row.FirstName,
                            MiddleName = row.MiddleName,
                            LastName = row.LastName,
                            Bvn = row.BVN,
                            Nin = row.NIN,
                            
                        }, args, salt);

                        var farmer = new Farmer
                        {
                            Name = $"{row.FirstName} {row.MiddleName} {row.LastName}".Trim(),
                            Email = row.Email,
                            PhoneNumber = row.PhoneNumber,
                            Gender = row.Gender,
                            State = row.StateOfResidence,
                            Lga = row.LGA,
                            Ward = row.Ward,
                            Location = row.ResidentialAddress,
                            FarmerCategory = row.CategoryOfFarmer,
                            YearsInAgric = years,
                            Level = FarmerLevel.Level1,
                            HighestLevelOfEducation = row.HighestLevelOfEducation,
                            OnBoardedOn = DateTimeOffset.UtcNow
                        };

                        aggregates.Add(new BulkRowAggregate
                        {
                            User = user,
                            Farmer = farmer,
                            ValueChains = GetValueChains(row)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Skipping invalid row {rowNumber}", rowNumber);
                    }

                    rowNumber++;
                }

                if (!aggregates.Any())
                {
                    _logger.LogInformation("No valid rows to process for file {file}", filePath);

                    job.Status = "Completed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.Successful = 0;
                    job.Failed = rows.Count;
                    await _context.SaveChangesAsync(token);

                    return;
                }

                var bulkConfig = new BulkConfig
                {
                    BatchSize = 10000,
                    BulkCopyTimeout = 300,
                    UseTempDB = true
                };

                // Duplicate checks: email, phone
                var emails = aggregates.Where(a => !string.IsNullOrWhiteSpace(a.User.Email)).Select(a => a.User.Email).Distinct().ToList();
                var phones = aggregates.Where(a => !string.IsNullOrWhiteSpace(a.User.PhoneNumber)).Select(a => a.User.PhoneNumber).Distinct().ToList();

                var existingUsers = await _context.Users.AsNoTracking()
                    .Where(u => emails.Contains(u.Email) || phones.Contains(u.PhoneNumber))
                    .Select(u => new { u.Id, u.Email, u.PhoneNumber })
                    .ToListAsync(token);

                var existingUserMapByEmail = existingUsers.Where(x => !string.IsNullOrWhiteSpace(x.Email)).ToDictionary(x => x.Email, x => x.Id);
                var existingUserMapByPhone = existingUsers.Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber)).ToDictionary(x => x.PhoneNumber, x => x.Id);

                // Also check existing farmers by same keys to skip
                var existingFarmers = await _context.Farmers.AsNoTracking()
                    .Where(f => emails.Contains(f.Email) || phones.Contains(f.PhoneNumber))
                    .Select(f => new { f.Id, f.UserId, f.Email, f.PhoneNumber })
                    .ToListAsync(token);

                var existingFarmerEmails = existingFarmers.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToHashSet();
                var existingFarmerPhones = existingFarmers.Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber)).Select(x => x.PhoneNumber).ToHashSet();

                var newUsers = new List<User>();
                var farmers = new List<Farmer>();
                var valueChains = new List<FarmerValueChainItem>();

                foreach (var agg in aggregates)
                {
                    var email = agg.User.Email;
                    var phone = agg.User.PhoneNumber;

                    agg.User.UserType = args.userType;
                    agg.User.IsActive = true;
                   
                    // if farmer exists (email or phone), skip entire row
                    if (!string.IsNullOrWhiteSpace(email) && existingFarmerEmails.Contains(email))
                    {
                        _logger.LogInformation("Skipping existing farmer by email {email}", email);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(phone) && existingFarmerPhones.Contains(phone))
                    {
                        _logger.LogInformation("Skipping existing farmer by phone {phone}", phone);
                        continue;
                    }

                    // attach existing user if present by any key
                    string matchedUserId = null;
                    if (!string.IsNullOrWhiteSpace(email) && existingUserMapByEmail.TryGetValue(email, out var uidE)) matchedUserId = uidE;
                    else if (!string.IsNullOrWhiteSpace(phone) && existingUserMapByPhone.TryGetValue(phone, out var uidP)) matchedUserId = uidP;

                    if (matchedUserId != null)
                    {
                        agg.Farmer.UserId = matchedUserId;
                    }
                    else
                    {
                        newUsers.Add(agg.User);
                    }

                    farmers.Add(agg.Farmer);
                }

                // Bulk insert new users
                var emailToIdMap = new Dictionary<string, string>(existingUserMapByEmail);
                var phoneToIdMap = new Dictionary<string, string>(existingUserMapByPhone);
                var insertedUserIds = new HashSet<string>();

                if (newUsers.Any())
                {
                    try
                    {
                        await _context.BulkInsertAsync(newUsers, bulkConfig, cancellationToken: token);

                        foreach (var u in newUsers)
                        {
                            if (!string.IsNullOrWhiteSpace(u.Id))
                            {
                                insertedUserIds.Add(u.Id);
                                if (!string.IsNullOrWhiteSpace(u.Email)) emailToIdMap[u.Email] = u.Id;
                                if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) phoneToIdMap[u.PhoneNumber] = u.Id;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bulk insert for users failed, falling back to single inserts");

                        foreach (var u in newUsers)
                        {
                            try
                            {
                                _context.Users.Add(u);
                                await _context.SaveChangesAsync(token);

                                if (!string.IsNullOrWhiteSpace(u.Id))
                                {
                                    insertedUserIds.Add(u.Id);
                                    if (!string.IsNullOrWhiteSpace(u.Email)) emailToIdMap[u.Email] = u.Id;
                                    if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) phoneToIdMap[u.PhoneNumber] = u.Id;
                                }
                            }
                            catch (Exception inner)
                            {
                                // Log and attempt to resolve by finding existing user (duplicate key case)
                                _logger.LogWarning(inner, "Failed to insert user {email}/{phone} individually, attempting to find existing record", u.Email, u.PhoneNumber);
                                try
                                {
                                    User existing = null;
                                    if (!string.IsNullOrWhiteSpace(u.Email))
                                        existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == u.Email, token);
                                    if (existing == null && !string.IsNullOrWhiteSpace(u.PhoneNumber))
                                        existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.PhoneNumber == u.PhoneNumber, token);

                                    if (existing != null)
                                    {
                                        insertedUserIds.Add(existing.Id);
                                        if (!string.IsNullOrWhiteSpace(existing.Email)) emailToIdMap[existing.Email] = existing.Id;
                                        if (!string.IsNullOrWhiteSpace(existing.PhoneNumber)) phoneToIdMap[existing.PhoneNumber] = existing.Id;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                // set user ids on farmers for newly inserted or matched users
                foreach (var agg in aggregates)
                {
                    if (agg.User == null) continue;

                    if (!string.IsNullOrWhiteSpace(agg.User.Email) && emailToIdMap.TryGetValue(agg.User.Email, out var idByEmail))
                    {
                        agg.Farmer.UserId = idByEmail;
                    }
                    else if (!string.IsNullOrWhiteSpace(agg.User.PhoneNumber) && phoneToIdMap.TryGetValue(agg.User.PhoneNumber, out var idByPhone))
                    {
                        agg.Farmer.UserId = idByPhone;
                    }
                }

                // Only insert farmers that have a UserId (either matched or newly inserted)
                var farmersToInsert = farmers.Where(f => !string.IsNullOrWhiteSpace(f.UserId)).ToList();
                var insertedFarmerIds = new List<long>();

                if (farmersToInsert.Any())
                {
                    try
                    {
                        await _context.BulkInsertAsync(farmersToInsert, bulkConfig, cancellationToken: token);

                        insertedFarmerIds.AddRange(farmersToInsert.Select(f => f.Id));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bulk insert for farmers failed, falling back to single inserts");

                        foreach (var f in farmersToInsert)
                        {
                            try
                            {
                                _context.Farmers.Add(f);
                                await _context.SaveChangesAsync(token);
                                insertedFarmerIds.Add(f.Id);
                            }
                            catch (Exception inner)
                            {
                                _logger.LogWarning(inner, "Failed to insert farmer {name} individually, attempting to find existing by email/phone", f.Name);
                                try
                                {
                                    var existing = await _context.Farmers.AsNoTracking()
                                        .FirstOrDefaultAsync(x => (!string.IsNullOrWhiteSpace(f.Email) && x.Email == f.Email) || (!string.IsNullOrWhiteSpace(f.PhoneNumber) && x.PhoneNumber == f.PhoneNumber), token);
                                    if (existing != null)
                                    {
                                        insertedFarmerIds.Add(existing.Id);
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    // create value chain items for inserted farmers
                    foreach (var agg in aggregates)
                    {
                        var f = farmersToInsert.FirstOrDefault(x => x.Name == agg.Farmer.Name && (x.Email == agg.Farmer.Email || x.PhoneNumber == agg.Farmer.PhoneNumber));
                        if (f == null || !insertedFarmerIds.Contains(f.Id)) continue;

                        foreach (var vc in agg.ValueChains ?? new())
                        {
                            foreach (var item in vc.Items ?? new())
                            {
                                valueChains.Add(new FarmerValueChainItem
                                {
                                    FarmerId = f.Id,
                                    ValueChainType = vc.ValueChainType,
                                    ItemName = item
                                });
                            }
                        }
                    }

                    if (valueChains.Any())
                    {
                        try
                        {
                            await _context.BulkInsertAsync(valueChains, bulkConfig, cancellationToken: token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Bulk insert for value chains failed, falling back to single inserts");
                            foreach (var vc in valueChains)
                            {
                                try
                                {
                                    _context.FarmerValueChainItems.Add(vc);
                                    await _context.SaveChangesAsync(token);
                                }
                                catch { }
                            }
                        }
                    }

                    // Insert UserOrganization mappings (avoid duplicates)
                    var userOrgs = farmersToInsert
                        .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                        .Select(x => new UserOrganization { UserId = x.UserId, OrganizationCode = args.organizationCode })
                        .DistinctBy(x => new { x.UserId, x.OrganizationCode })
                        .ToList();

                    if (userOrgs.Any())
                    {
                        var existingUserOrgs = await _context.UserOrganizations.AsNoTracking()
                            .Where(uo => userOrgs.Select(u => u.UserId).Contains(uo.UserId) && uo.OrganizationCode == args.organizationCode)
                            .Select(uo => new { uo.UserId, uo.OrganizationCode })
                            .ToListAsync(token);

                        var existingSet = new HashSet<string>(existingUserOrgs.Select(x => $"{x.UserId}-{x.OrganizationCode}"));

                        var toInsertUserOrgs = userOrgs.Where(uo => !existingSet.Contains($"{uo.UserId}-{uo.OrganizationCode}")).ToList();

                        if (toInsertUserOrgs.Any())
                        {
                            try
                            {
                                await _context.BulkInsertAsync(toInsertUserOrgs, bulkConfig, cancellationToken: token);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Bulk insert for user-orgs failed, falling back to single inserts");
                                foreach (var uo in toInsertUserOrgs)
                                {
                                    uo.IsActive = true;
                                    try { _context.UserOrganizations.Add(uo); await _context.SaveChangesAsync(token); } catch { }
                                }
                            }
                        }
                    }

                    // Insert FarmerOrganization mappings (avoid duplicates)
                    var farmerOrgCandidates = farmersToInsert.Select(f => new FarmerOrganization
                    {
                        FarmerId = f.Id,
                        IsActive=true,
                        OrganizationCode = args.organizationCode,
                        OnboardedByUserId = args.OnboardedByUserId
                    }).ToList();

                    var farmerIds = farmerOrgCandidates.Select(f => f.FarmerId).ToList();
                    var existingFarmerOrgs = await _context.FarmerOrganizations.AsNoTracking()
                        .Where(fo => farmerIds.Contains(fo.FarmerId) && fo.OrganizationCode == args.organizationCode)
                        .Select(fo => new { fo.FarmerId, fo.OrganizationCode })
                        .ToListAsync(token);

                    var existingFarmerOrgSet = new HashSet<string>(existingFarmerOrgs.Select(x => $"{x.FarmerId}-{x.OrganizationCode}"));

                    var toInsertFarmerOrgs = farmerOrgCandidates.Where(f => !existingFarmerOrgSet.Contains($"{f.FarmerId}-{f.OrganizationCode}")).ToList();

                    if (toInsertFarmerOrgs.Any())
                    {
                        try
                        {
                            await _context.BulkInsertAsync(toInsertFarmerOrgs, bulkConfig, cancellationToken: token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Bulk insert for farmer-orgs failed, falling back to single inserts");
                            foreach (var fo in toInsertFarmerOrgs)
                            {
                                fo.IsActive = true;
                                try { _context.FarmerOrganizations.Add(fo); await _context.SaveChangesAsync(token); } catch { }
                            }
                        }
                    }

                    _logger.LogInformation("Bulk file {file} processed: UsersAttempted={u} FarmersAttempted={f} FarmersInserted={fi}", filePath, newUsers.Count, farmersToInsert.Count, insertedFarmerIds.Count);

                    // update job
                    var existingJob = await _context.BulkUploadJobs.FindAsync(job.Id);
                    if (existingJob != null)
                    {
                        existingJob.Status = "Completed";
                        existingJob.CompletedAt = DateTime.UtcNow;
                        existingJob.Successful = insertedFarmerIds.Count;
                        existingJob.Failed = rows.Count - insertedFarmerIds.Count;
                        await _context.SaveChangesAsync(token);
                    }
                }
                else
                {
                    _logger.LogInformation("No new farmers to insert for file {file}", filePath);
                    // update job as completed with zero successful
                    var existingJob = job != null ? await _context.BulkUploadJobs.FindAsync(job.Id) : null;
                    if (existingJob != null)
                    {
                        existingJob.Status = "Completed";
                        existingJob.CompletedAt = DateTime.UtcNow;
                        existingJob.Successful = 0;
                        existingJob.Failed = rows.Count;
                        await _context.SaveChangesAsync(token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk onboarding processing failed for file {file}", filePath);

                // update job as failed
                try
                {
                    var existingJob = await _context.BulkUploadJobs.FirstOrDefaultAsync(j => j.FilePath == filePath, token);
                    if (existingJob != null)
                    {
                        existingJob.Status = "Failed";
                        existingJob.ErrorMessage = ex.Message;
                        existingJob.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(token);
                    }
                }
                catch { }
            }
        }

        private string GenerateUserId(OnBoardingArgs args)
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomDigits = new Random().Next(100, 999);

            if (!string.IsNullOrEmpty(args.businessCode))
                return $"{args.businessCode}{datePart}{randomDigits}";

            if (!string.IsNullOrEmpty(args.organizationCode))
                return $"{args.organizationCode}{datePart}{randomDigits}";

            return Guid.NewGuid().ToString();
        }

        private User BuildUserEntity(OnboardingRequestModel model, OnBoardingArgs args, byte[] salt)
        {
            return new User
            {
                Id = GenerateUserId(args),
                UserName = model.Email ?? model.PhoneNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Pin = model.Pin,
                PinEncrypt = string.IsNullOrEmpty(model.Pin) ? null : model.Pin.EncryptTransactionPin(salt),
                Salt = Convert.ToBase64String(salt),
                Password = model.Password,
                CreatedOn = DateTimeOffset.Now,
                IsActive = true,
                UserType = args.userType,
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                Name = model.FullName,
                NIN = model.Nin,
                BVN = model.Bvn
            };
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
}
