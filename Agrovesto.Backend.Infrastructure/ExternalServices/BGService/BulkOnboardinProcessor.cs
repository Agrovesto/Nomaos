using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using EFCore.BulkExtensions;
using Hangfire;
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

        // Basic phone normalization: remove spaces, hyphens, parentheses and leading plus signs.
        // This function keeps only digits and, if present, a leading country code without the '+' sign.
        private static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            var sb = new System.Text.StringBuilder();
            foreach (var c in phone)
            {
                if (char.IsDigit(c)) sb.Append(c);
            }
            var normalized = sb.ToString();

            // Remove leading zeros introduced by local formats: keep as-is because business rules vary.
            // Optionally, you could add logic to prepend country code if missing.
            return string.IsNullOrWhiteSpace(normalized) ? phone : normalized;
        }

        [AutomaticRetry(Attempts = 5)]
        public async Task ProcessAsync(string filePath, OnBoardingArgs args, Guid jobId, CancellationToken token)
        {
            await ProcessFileAsync(filePath, args, jobId, token);
        }
        private async Task ProcessFileAsync(
    string filePath,
    OnBoardingArgs args,
    Guid jobId,
    CancellationToken token)
        {
            args.userType = "Farmer";

            BulkUploadJob? job = null;

            try
            {
                _logger.LogInformation(
                    "Bulk processor starting for JobId {jobId}",
                    jobId);

                // ── 1. LOAD JOB ─────────────────────────────────────────────────────
                job = await _context.BulkUploadJobs
                    .FirstOrDefaultAsync(x => x.Id == jobId, token);

                if (job == null)
                {
                    _logger.LogWarning(
                        "Bulk upload job not found: {jobId}",
                        jobId);

                    return;
                }

                job.Status = "Processing";

                await _context.SaveChangesAsync(token);

                // ── 2. PARSE FILE ───────────────────────────────────────────────────
                using var stream = File.OpenRead(filePath);

                var rows = _excelConverterService.BulkParseExcel(stream);

                if (rows == null || !rows.Any())
                {
                    _logger.LogWarning(
                        "No rows found in file {file}",
                        filePath);

                    await FinalizeJobAsync(
                        job,
                        status: "Failed",
                        successful: 0,
                        failed: 0,
                        token);

                    return;
                }

                job.TotalRows = rows.Count;

                await _context.SaveChangesAsync(token);

                // ── 3. VALIDATE ROWS & BUILD AGGREGATES ────────────────────────────
                var aggregates = new List<BulkRowAggregate>();

                int rowNumber = 2;

                int parseFailures = 0;

                foreach (var row in rows)
                {
                    try
                    {
                        ValidateRow(row, rowNumber);

                        var salt = EncryptionUtil.GetSalt();

                        var user = BuildUserEntity(
                            new OnboardingRequestModel
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
                            },
                            args,
                            salt);

                        var farmer = new Farmer
                        {
                            Name = $"{row.FirstName} {row.MiddleName} {row.LastName}".Trim(),
                            Email = row.Email?.Trim().ToLowerInvariant(),
                            PhoneNumber = row.PhoneNumber?.Trim(),
                            Gender = row.Gender,
                            State = row.StateOfResidence,
                            Lga = row.LGA,
                            Ward = row.Ward,
                            Location = row.ResidentialAddress,
                            FarmerCategory = row.CategoryOfFarmer,
                            YearsInAgric = int.Parse(row.YearsInAgric),
                            Level = FarmerLevel.Level1,
                            HighestLevelOfEducation = row.HighestLevelOfEducation,
                            OnBoardedOn = DateTimeOffset.UtcNow
                        };

                        if (!string.IsNullOrWhiteSpace(user.Email))
                            user.Email = user.Email
                                .Trim()
                                .ToLowerInvariant();

                        user.PhoneNumber =
                            NormalizePhoneNumber(user.PhoneNumber);

                        farmer.PhoneNumber =
                            NormalizePhoneNumber(farmer.PhoneNumber);

                        aggregates.Add(new BulkRowAggregate
                        {
                            RowNumber = rowNumber,
                            User = user,
                            Farmer = farmer,
                            ValueChains = GetValueChains(row)
                        });
                    }
                    catch (Exception ex)
                    {
                        parseFailures++;

                        _logger.LogWarning(
                            "Row {row} skipped: {reason}",
                            rowNumber,
                            ex.Message);
                    }
                    finally
                    {
                        rowNumber++;
                    }
                }

                if (!aggregates.Any())
                {
                    _logger.LogWarning(
                        "No valid rows after validation for file {file}",
                        filePath);

                    await FinalizeJobAsync(
                        job,
                        "Completed",
                        successful: 0,
                        failed: rows.Count,
                        token);

                    return;
                }

                // ── 4. DUPLICATE CHECKS ────────────────────────────────────────────
                var emails = aggregates
                    .Where(a => !string.IsNullOrWhiteSpace(a.User.Email))
                    .Select(a => a.User.Email)
                    .Distinct()
                    .ToList();

                var phones = aggregates
                    .Where(a => !string.IsNullOrWhiteSpace(a.User.PhoneNumber))
                    .Select(a => a.User.PhoneNumber)
                    .Distinct()
                    .ToList();

                var existingUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        emails.Contains(u.Email) ||
                        phones.Contains(u.PhoneNumber))
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.PhoneNumber
                    })
                    .ToListAsync(token);

                var existingFarmers = await _context.Farmers
                    .AsNoTracking()
                    .Where(f =>
                        emails.Contains(f.Email) ||
                        phones.Contains(f.PhoneNumber))
                    .Select(f => new
                    {
                        f.Id,
                        f.UserId,
                        f.Email,
                        f.PhoneNumber
                    })
                    .ToListAsync(token);

                var existingUserByEmail = existingUsers
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .ToDictionary(x => x.Email, x => x.Id);

                var existingUserByPhone = existingUsers
                    .Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                    .ToDictionary(
                        x => NormalizePhoneNumber(x.PhoneNumber),
                        x => x.Id);

                var existingFarmerEmails = existingFarmers
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => x.Email)
                    .ToHashSet();

                var existingFarmerPhones = existingFarmers
                    .Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                    .Select(x => NormalizePhoneNumber(x.PhoneNumber))
                    .ToHashSet();

                // ── 5. SPLIT USERS ─────────────────────────────────────────────────
                var newUsers = new List<User>();

                var validAggregates = new List<BulkRowAggregate>();

                foreach (var agg in aggregates)
                {
                    var email = agg.User.Email;
                    var phone = agg.User.PhoneNumber;

                    if (!string.IsNullOrWhiteSpace(email) &&
                        existingFarmerEmails.Contains(email))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(phone) &&
                        existingFarmerPhones.Contains(phone))
                    {
                        continue;
                    }

                    agg.User.UserType = args.userType;
                    agg.User.IsActive = true;

                    string matchedUserId =
                        (!string.IsNullOrWhiteSpace(email) &&
                         existingUserByEmail.TryGetValue(email, out var eid))
                            ? eid
                            : (!string.IsNullOrWhiteSpace(phone) &&
                               existingUserByPhone.TryGetValue(phone, out var pid))
                                ? pid
                                : null;

                    if (matchedUserId != null)
                    {
                        agg.Farmer.UserId = matchedUserId;
                        agg.User.Id = matchedUserId;
                    }
                    else
                    {
                        newUsers.Add(agg.User);
                    }

                    validAggregates.Add(agg);
                }

                if (!validAggregates.Any())
                {
                    await FinalizeJobAsync(
                        job,
                        "Completed",
                        successful: 0,
                        failed: rows.Count,
                        token);

                    return;
                }

                // ── 6. BULK INSERT USERS ───────────────────────────────────────────
                var bulkConfig = new BulkConfig
                {
                    BatchSize = 5000,
                    BulkCopyTimeout = 300,
                    UseTempDB = true,
                    SetOutputIdentity = true
                };

                var emailToUserId = new Dictionary<string, string>(
                    existingUserByEmail,
                    StringComparer.OrdinalIgnoreCase);

                var phoneToUserId = new Dictionary<string, string>(
                    existingUserByPhone);

                if (newUsers.Any())
                {
                    await BulkInsertWithFallbackAsync(
                        bulkInsert: () =>
                            _context.BulkInsertAsync(
                                newUsers,
                                bulkConfig,
                                cancellationToken: token),

                        fallback: async () =>
                        {
                            foreach (var batch in newUsers.Chunk(500))
                            {
                                _context.Users.AddRange(batch);

                                await _context.SaveChangesAsync(token);
                            }
                        },

                        logger: _logger,
                        context: "Users");
                }

                foreach (var u in newUsers)
                {
                    if (string.IsNullOrWhiteSpace(u.Id))
                        continue;

                    if (!string.IsNullOrWhiteSpace(u.Email))
                        emailToUserId[u.Email] = u.Id;

                    if (!string.IsNullOrWhiteSpace(u.PhoneNumber))
                        phoneToUserId[u.PhoneNumber] = u.Id;
                }

                // ── 7. WIRE FARMERS TO USERS ───────────────────────────────────────
                foreach (var agg in validAggregates)
                {
                    if (!string.IsNullOrWhiteSpace(agg.User.Id))
                    {
                        agg.Farmer.UserId = agg.User.Id;

                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(agg.User.Email) &&
                        emailToUserId.TryGetValue(
                            agg.User.Email,
                            out var uid))
                    {
                        agg.Farmer.UserId = uid;
                    }
                    else if (!string.IsNullOrWhiteSpace(agg.User.PhoneNumber) &&
                             phoneToUserId.TryGetValue(
                                 agg.User.PhoneNumber,
                                 out var pid))
                    {
                        agg.Farmer.UserId = pid;
                    }
                }

                // ── 8. INSERT FARMERS ──────────────────────────────────────────────
                var farmersToInsert = validAggregates
                    .Where(x => !string.IsNullOrWhiteSpace(x.Farmer.UserId))
                    .Select(x => x.Farmer)
                    .ToList();

                if (!farmersToInsert.Any())
                {
                    await FinalizeJobAsync(
                        job,
                        "Completed",
                        successful: 0,
                        failed: rows.Count,
                        token);

                    return;
                }

                await BulkInsertWithFallbackAsync(
                    bulkInsert: () =>
                        _context.BulkInsertAsync(
                            farmersToInsert,
                            bulkConfig,
                            cancellationToken: token),

                    fallback: async () =>
                    {
                        foreach (var batch in farmersToInsert.Chunk(500))
                        {
                            _context.Farmers.AddRange(batch);

                            await _context.SaveChangesAsync(token);
                        }
                    },

                    logger: _logger,
                    context: "Farmers");

                int insertedFarmerCount = farmersToInsert.Count(x => x.Id > 0);

                // ── 9. FINALIZE ────────────────────────────────────────────────────
                _logger.LogInformation(
                    "Bulk file {file} complete — Total={total} Inserted={inserted}",
                    filePath,
                    rows.Count,
                    insertedFarmerCount);

                await FinalizeJobAsync(
                    job,
                    "Completed",
                    successful: insertedFarmerCount,
                    failed: rows.Count - insertedFarmerCount,
                    token);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Bulk onboarding failed for file {file}",
                    filePath);

                if (job != null)
                {
                    job.Status = "Failed";
                    job.ErrorMessage = ex.Message;
                    job.CompletedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync(token);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not delete temp file {file}",
                        filePath);
                }
            }
        }
        //private async Task ProcessFileAsync(string filePath, OnBoardingArgs args, BulkUploadJob job, CancellationToken token)
        //{
        //    args.userType = "Farmer";

        //    try
        //    {
        //        _logger.LogInformation("Bulk processor starting for file {file}", filePath);

        //        // ── 1. PARSE ────────────────────────────────────────────────────────
        //        using var stream = File.OpenRead(filePath);
        //        var rows = _excelConverterService.BulkParseExcel(stream);

        //        if (rows == null || !rows.Any())
        //        {
        //            _logger.LogWarning("No rows found in file {file}", filePath);
        //            await FinalizeJobAsync(job, status: "Completed", successful: 0, failed: 0, token);
        //            return;
        //        }

        //        job = await EnsureJobAsync(job, filePath, args, rows.Count, token);

        //        // ── 3. VALIDATE ROWS & BUILD AGGREGATES ─────────────────────────────
        //        var aggregates = new List<BulkRowAggregate>();
        //        int rowNumber = 2; // row 1 = header
        //        int parseFailures = 0;

        //        foreach (var row in rows)
        //        {
        //            try
        //            {
        //                ValidateRow(row, rowNumber);

        //                var salt = EncryptionUtil.GetSalt();

        //                var user = BuildUserEntity(new OnboardingRequestModel
        //                {
        //                    Email = row.Email,
        //                    PhoneNumber = row.PhoneNumber,
        //                    Password = "Admin123@",
        //                    Pin = "0000",
        //                    FirstName = row.FirstName,
        //                    MiddleName = row.MiddleName,
        //                    LastName = row.LastName,
        //                    Bvn = row.BVN,
        //                    Nin = row.NIN,
        //                }, args, salt);

        //                var farmer = new Farmer
        //                {
        //                    Name = $"{row.FirstName} {row.MiddleName} {row.LastName}".Trim(),
        //                    Email = row.Email?.Trim().ToLowerInvariant(),
        //                    PhoneNumber = row.PhoneNumber?.Trim(),
        //                    Gender = row.Gender,
        //                    State = row.StateOfResidence,
        //                    Lga = row.LGA,
        //                    Ward = row.Ward,
        //                    Location = row.ResidentialAddress,
        //                    FarmerCategory = row.CategoryOfFarmer,
        //                    YearsInAgric = int.Parse(row.YearsInAgric),
        //                    Level = FarmerLevel.Level1,
        //                    HighestLevelOfEducation = row.HighestLevelOfEducation,
        //                    OnBoardedOn = DateTimeOffset.UtcNow
        //                };

        //                // Normalize email on user too
        //                if (!string.IsNullOrWhiteSpace(user.Email))
        //                    user.Email = user.Email.Trim().ToLowerInvariant();

        //                // Normalize phone numbers to a canonical form for comparison/lookup
        //                user.PhoneNumber = NormalizePhoneNumber(user.PhoneNumber);
        //                farmer.PhoneNumber = NormalizePhoneNumber(farmer.PhoneNumber);

        //                aggregates.Add(new BulkRowAggregate
        //                {
        //                    RowNumber = rowNumber,
        //                    User = user,
        //                    Farmer = farmer,
        //                    ValueChains = GetValueChains(row)
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                parseFailures++;
        //                _logger.LogWarning("Row {row} skipped: {reason}", rowNumber, ex.Message);
        //            }
        //            finally
        //            {
        //                rowNumber++;
        //            }
        //        }

        //        if (!aggregates.Any())
        //        {
        //            _logger.LogWarning("No valid rows after validation for file {file}", filePath);
        //            await FinalizeJobAsync(job, "Completed", successful: 0, failed: rows.Count, token);
        //            return;
        //        }

        //        // ── 4. BATCH DUPLICATE CHECK ─────────────────────────────────────────
        //        var emails = aggregates
        //            .Where(a => !string.IsNullOrWhiteSpace(a.User.Email))
        //            .Select(a => a.User.Email)
        //            .Distinct()
        //            .ToList();

        //        var phones = aggregates
        //            .Where(a => !string.IsNullOrWhiteSpace(a.User.PhoneNumber))
        //            .Select(a => a.User.PhoneNumber)
        //            .Distinct()
        //            .ToList();

        //        // Single query each — no N+1
        //        var existingUsers = await _context.Users.AsNoTracking()
        //            .Where(u => emails.Contains(u.Email) || phones.Contains(u.PhoneNumber))
        //            .Select(u => new { u.Id, u.Email, u.PhoneNumber })
        //            .ToListAsync(token);

        //        var existingFarmers = await _context.Farmers.AsNoTracking()
        //            .Where(f => emails.Contains(f.Email) || phones.Contains(f.PhoneNumber))
        //            .Select(f => new { f.Id, f.UserId, f.Email, f.PhoneNumber })
        //            .ToListAsync(token);

        //        var existingUserByEmail = existingUsers
        //            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
        //            .ToDictionary(u => u.Email, u => u.Id);

        //        var existingUserByPhone = existingUsers
        //            .Where(u => !string.IsNullOrWhiteSpace(u.PhoneNumber))
        //            .ToDictionary(u => NormalizePhoneNumber(u.PhoneNumber), u => u.Id);

        //        var existingFarmerEmails = existingFarmers
        //            .Where(f => !string.IsNullOrWhiteSpace(f.Email))
        //            .Select(f => f.Email)
        //            .ToHashSet();

        //        var existingFarmerPhones = existingFarmers
        //            .Where(f => !string.IsNullOrWhiteSpace(f.PhoneNumber))
        //            .Select(f => NormalizePhoneNumber(f.PhoneNumber))
        //            .ToHashSet();

        //        // ── 5. SPLIT: NEW USERS vs EXISTING USERS ────────────────────────────
        //        var newUsers = new List<User>();
        //        var validAggregates = new List<BulkRowAggregate>(); // aggregates that will actually be inserted

        //        foreach (var agg in aggregates)
        //        {
        //            var email = agg.User.Email;
        //            var phone = agg.User.PhoneNumber;

        //            // Skip if farmer already exists
        //            if (!string.IsNullOrWhiteSpace(email) && existingFarmerEmails.Contains(email))
        //            {
        //                _logger.LogInformation("Row {row}: farmer already exists by email {email}, skipping", agg.RowNumber, email);
        //                continue;
        //            }

        //            if (!string.IsNullOrWhiteSpace(phone) && existingFarmerPhones.Contains(phone))
        //            {
        //                _logger.LogInformation("Row {row}: farmer already exists by phone {phone}, skipping", agg.RowNumber, phone);
        //                continue;
        //            }

        //            agg.User.UserType = args.userType;
        //            agg.User.IsActive = true;

        //            // Attach existing user account if found
        //            string matchedUserId =
        //                (!string.IsNullOrWhiteSpace(email) && existingUserByEmail.TryGetValue(email, out var eid)) ? eid :
        //                (!string.IsNullOrWhiteSpace(phone) && existingUserByPhone.TryGetValue(phone, out var pid)) ? pid :
        //                null;

        //            if (matchedUserId != null)
        //            {
        //                agg.Farmer.UserId = matchedUserId;
        //                agg.User.Id = matchedUserId; // flag: don't insert user
        //            }
        //            else
        //            {
        //                newUsers.Add(agg.User);
        //            }

        //            validAggregates.Add(agg);
        //        }

        //        if (!validAggregates.Any())
        //        {
        //            _logger.LogInformation("All rows were duplicates for file {file}", filePath);
        //            await FinalizeJobAsync(job, "Completed", successful: 0, failed: rows.Count, token);
        //            return;
        //        }

        //        // ── 6. BULK INSERT USERS ─────────────────────────────────────────────
        //        var bulkConfig = new BulkConfig
        //        {
        //            BatchSize = 5000,
        //            BulkCopyTimeout = 300,
        //            UseTempDB = true,
        //            SetOutputIdentity = true
        //        };

        //        // email→id and phone→id maps — seed with already-known existing users
        //        var emailToUserId = new Dictionary<string, string>(existingUserByEmail, StringComparer.OrdinalIgnoreCase);
        //        var phoneToUserId = new Dictionary<string, string>(existingUserByPhone);

        //        if (newUsers.Any())
        //        {
        //            await BulkInsertWithFallbackAsync(
        //                bulkInsert: () => _context.BulkInsertAsync(newUsers, bulkConfig, cancellationToken: token),
        //                fallback: async () =>
        //                {
        //                    foreach (var u in newUsers)
        //                    {
        //                        try
        //                        {
        //                            _context.Users.Add(u);
        //                            await _context.SaveChangesAsync(token);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogWarning(ex, "Failed individual user insert {email}/{phone}, attempting recovery", u.Email, u.PhoneNumber);

        //                            // Race condition: another process inserted between our check and now
        //                            var existing = await _context.Users.AsNoTracking()
        //                                .Where(x =>
        //                                    (!string.IsNullOrWhiteSpace(u.Email) && x.Email == u.Email) ||
        //                                    (!string.IsNullOrWhiteSpace(u.PhoneNumber) && x.PhoneNumber == u.PhoneNumber))
        //                                .FirstOrDefaultAsync(token);

        //                            if (existing != null)
        //                            {
        //                                u.Id = existing.Id; // use recovered id for downstream mapping
        //                            }
        //                        }
        //                    }
        //                },
        //                logger: _logger,
        //                context: "Users"
        //            );

        //            // Populate lookup maps from newly inserted / recovered users
        //            foreach (var u in newUsers)
        //            {
        //                if (string.IsNullOrWhiteSpace(u.Id)) continue;
        //                if (!string.IsNullOrWhiteSpace(u.Email)) emailToUserId[u.Email] = u.Id;
        //                if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) phoneToUserId[u.PhoneNumber] = u.Id;
        //            }
        //        }

        //        // ── 7. WIRE FARMER → USER IDs ────────────────────────────────────────
        //        foreach (var agg in validAggregates)
        //        {
        //            if (!string.IsNullOrWhiteSpace(agg.User.Id))
        //            {
        //                agg.Farmer.UserId = agg.User.Id;
        //                continue;
        //            }

        //            if (!string.IsNullOrWhiteSpace(agg.User.Email) && emailToUserId.TryGetValue(agg.User.Email, out var uid))
        //                agg.Farmer.UserId = uid;
        //            else if (!string.IsNullOrWhiteSpace(agg.User.PhoneNumber) && phoneToUserId.TryGetValue(agg.User.PhoneNumber, out var pid))
        //                agg.Farmer.UserId = pid;
        //        }

        //        // ── 8. BULK INSERT FARMERS ───────────────────────────────────────────
        //        var farmersToInsert = validAggregates
        //            .Where(a => !string.IsNullOrWhiteSpace(a.Farmer.UserId))
        //            .Select(a => a.Farmer)
        //            .ToList();

        //        if (!farmersToInsert.Any())
        //        {
        //            _logger.LogWarning("No farmers had a resolved UserId — nothing inserted for file {file}", filePath);
        //            await FinalizeJobAsync(job, "Completed", successful: 0, failed: rows.Count, token);
        //            return;
        //        }

        //        await BulkInsertWithFallbackAsync(
        //            bulkInsert: () => _context.BulkInsertAsync(farmersToInsert, bulkConfig, cancellationToken: token),
        //            fallback: async () =>
        //            {
        //                foreach (var f in farmersToInsert)
        //                {
        //                    try
        //                    {
        //                        _context.Farmers.Add(f);
        //                        await _context.SaveChangesAsync(token);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogWarning(ex, "Failed individual farmer insert {name}", f.Name);
        //                    }
        //                }
        //            },
        //            logger: _logger,
        //            context: "Farmers"
        //        );

        //        // Build farmer lookup by UserId for downstream steps
        //        var farmerByUserId = farmersToInsert
        //            .Where(f => !string.IsNullOrWhiteSpace(f.UserId) && f.Id > 0)
        //            .ToDictionary(f => f.UserId, f => f);

        //        int insertedFarmerCount = farmersToInsert.Count(f => f.Id > 0);

        //        // ── 9. VALUE CHAIN ITEMS ─────────────────────────────────────────────
        //        var valueChainItems = new List<FarmerValueChainItem>();

        //        foreach (var agg in validAggregates)
        //        {
        //            if (!farmerByUserId.TryGetValue(agg.Farmer.UserId ?? "", out var insertedFarmer)) continue;
        //            if (agg.ValueChains == null) continue;

        //            foreach (var vc in agg.ValueChains)
        //            {
        //                foreach (var item in vc.Items ?? new())
        //                {
        //                    valueChainItems.Add(new FarmerValueChainItem
        //                    {
        //                        FarmerId = insertedFarmer.Id,
        //                        ValueChainType = vc.ValueChainType,
        //                        ItemName = item
        //                    });
        //                }
        //            }
        //        }

        //        if (valueChainItems.Any())
        //        {
        //            await BulkInsertWithFallbackAsync(
        //                bulkInsert: () => _context.BulkInsertAsync(valueChainItems, bulkConfig, cancellationToken: token),
        //                fallback: async () =>
        //                {
        //                    foreach (var vc in valueChainItems)
        //                    {
        //                        try { _context.FarmerValueChainItems.Add(vc); await _context.SaveChangesAsync(token); }
        //                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to insert value chain item for farmer {id}", vc.FarmerId); }
        //                    }
        //                },
        //                logger: _logger,
        //                context: "ValueChains"
        //            );
        //        }

        //        // ── 10. ORGANIZATION MAPPINGS ────────────────────────────────────────
        //        if (!string.IsNullOrWhiteSpace(args.organizationCode))
        //        {
        //            var userIds = farmersToInsert.Where(f => !string.IsNullOrWhiteSpace(f.UserId)).Select(f => f.UserId).Distinct().ToList();
        //            var farmerIds = farmersToInsert.Where(f => f.Id > 0).Select(f => f.Id).ToList();

        //            // UserOrganizations
        //            var existingUserOrgList = await _context.UserOrganizations.AsNoTracking()
        //                .Where(uo => userIds.Contains(uo.UserId) && uo.OrganizationCode == args.organizationCode)
        //                .Select(uo => uo.UserId)
        //                .ToListAsync(token);

        //            var existingUserOrgSet = new HashSet<string>(existingUserOrgList);

        //            var userOrgsToInsert = userIds
        //                .Where(uid => !existingUserOrgSet.Contains(uid))
        //                .Select(uid => new UserOrganization
        //                {
        //                    UserId = uid,
        //                    OrganizationCode = args.organizationCode,
        //                    IsActive = true
        //                })
        //                .ToList();

        //            if (userOrgsToInsert.Any())
        //            {
        //                await BulkInsertWithFallbackAsync(
        //                    bulkInsert: () => _context.BulkInsertAsync(userOrgsToInsert, bulkConfig, cancellationToken: token),
        //                    fallback: async () =>
        //                    {
        //                        foreach (var uo in userOrgsToInsert)
        //                        {
        //                            try { _context.UserOrganizations.Add(uo); await _context.SaveChangesAsync(token); }
        //                            catch { }
        //                        }
        //                    },
        //                    logger: _logger,
        //                    context: "UserOrganizations"
        //                );
        //            }

        //            // FarmerOrganizations
        //            var existingFarmerOrgList = await _context.FarmerOrganizations.AsNoTracking()
        //                .Where(fo => farmerIds.Contains(fo.FarmerId) && fo.OrganizationCode == args.organizationCode)
        //                .Select(fo => fo.FarmerId)
        //                .ToListAsync(token);

        //            var existingFarmerOrgSet = new HashSet<long>(existingFarmerOrgList);

        //            var farmerOrgsToInsert = farmersToInsert
        //                .Where(f => f.Id > 0 && !existingFarmerOrgSet.Contains(f.Id))
        //                .Select(f => new FarmerOrganization
        //                {
        //                    FarmerId = f.Id,
        //                    OrganizationCode = args.organizationCode,
        //                    OnboardedByUserId = args.OnboardedByUserId,
        //                    IsActive = true
        //                })
        //                .ToList();

        //            if (farmerOrgsToInsert.Any())
        //            {
        //                await BulkInsertWithFallbackAsync(
        //                    bulkInsert: () => _context.BulkInsertAsync(farmerOrgsToInsert, bulkConfig, cancellationToken: token),
        //                    fallback: async () =>
        //                    {
        //                        foreach (var fo in farmerOrgsToInsert)
        //                        {
        //                            try { _context.FarmerOrganizations.Add(fo); await _context.SaveChangesAsync(token); }
        //                            catch { }
        //                        }
        //                    },
        //                    logger: _logger,
        //                    context: "FarmerOrganizations"
        //                );
        //            }
        //        }

        //        // ── 11. COOPERATIVE ASSIGNMENTS ──────────────────────────────────────
        //        if (args.CooperativeIds?.Any() == true)
        //        {
        //            _logger.LogInformation("Assigning {count} farmers to cooperatives", farmersToInsert.Count);

        //            // Fire cooperative assignments concurrently per farmer, bounded to avoid thread exhaustion
        //            var semaphore = new SemaphoreSlim(10);
        //            var cooperativeTasks = farmersToInsert
        //                .Where(f => !string.IsNullOrWhiteSpace(f.UserId))
        //                .Select(async f =>
        //                {
        //                    await semaphore.WaitAsync(token);
        //                    try
        //                    {
        //                        await AssignFarmerToCooperatives(new AssignmentArgs
        //                        {
        //                            Id = f.Id,
        //                            UserId = f.UserId,
        //                            CooperativeIds = args.CooperativeIds,
        //                            businessCode = args.businessCode,
        //                            organizationCode = args.organizationCode,
        //                            OnboardedByUserId = args.OnboardedByUserId
        //                        }, token);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogWarning(ex, "Failed to assign farmer {userId} to cooperatives", f.UserId);
        //                    }
        //                    finally
        //                    {
        //                        semaphore.Release();
        //                    }
        //                });

        //            await Task.WhenAll(cooperativeTasks);
        //        }

        //        // ── 12. AGENT ASSIGNMENTS ─────────────────────────────────────────────
        //        if (args.AgentIds?.Any() == true)
        //        {
        //            _logger.LogInformation("Assigning {count} farmers to agents", farmersToInsert.Count);

        //            var semaphore = new SemaphoreSlim(10);
        //            var agentTasks = farmersToInsert
        //                .Where(f => !string.IsNullOrWhiteSpace(f.UserId))
        //                .Select(async f =>
        //                {
        //                    await semaphore.WaitAsync(token);
        //                    try
        //                    {
        //                        await AssignFarmerToAgents(new AssignmentArgs
        //                        {
        //                            Id = f.Id,
        //                            UserId = f.UserId,
        //                            AgentIds = args.AgentIds,
        //                            businessCode = args.businessCode,
        //                            organizationCode = args.organizationCode,
        //                            OnboardedByUserId = args.OnboardedByUserId
        //                        }, token);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogWarning(ex, "Failed to assign farmer {userId} to agents", f.UserId);
        //                    }
        //                    finally
        //                    {
        //                        semaphore.Release();
        //                    }
        //                });

        //            await Task.WhenAll(agentTasks);
        //        }

        //        // ── 13. FINALIZE ──────────────────────────────────────────────────────
        //        _logger.LogInformation(
        //            "Bulk file {file} complete — Total={total} Valid={valid} Inserted={inserted} Skipped={skipped}",
        //            filePath, rows.Count, aggregates.Count, insertedFarmerCount, rows.Count - aggregates.Count + parseFailures);

        //        await FinalizeJobAsync(job, "Completed", successful: insertedFarmerCount, failed: rows.Count - insertedFarmerCount, token);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Bulk onboarding processing failed for file {file}", filePath);
        //        await SafeMarkJobFailedAsync(filePath, ex.Message, token);
        //    }
        //    finally
        //    {
        //        // ── 14. CLEANUP TEMP FILE ─────────────────────────────────────────────
        //        try
        //        {
        //            if (File.Exists(filePath))
        //                File.Delete(filePath);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogWarning(ex, "Could not delete temp file {file}", filePath);
        //        }
        //    }
        //}

        // ── HELPERS ───────────────────────────────────────────────────────────────────

        private static void ValidateRow(BulkOnboardingExcelRow row, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName))
                throw new Exception($"Row {rowNumber}: FirstName and LastName are required.");

            if (!int.TryParse(row.YearsInAgric, out var years) || years < 0 || years > 100)
                throw new Exception($"Row {rowNumber}: Invalid YearsInAgric '{row.YearsInAgric}'.");

            if (string.IsNullOrWhiteSpace(row.Email) && string.IsNullOrWhiteSpace(row.PhoneNumber))
                throw new Exception($"Row {rowNumber}: Email or PhoneNumber is required.");
        }

        private async Task<BulkUploadJob> EnsureJobAsync(
            BulkUploadJob job, string filePath, OnBoardingArgs args, int totalRows, CancellationToken token)
        {
            if (job != null) return job;

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
                TotalRows = totalRows
            };

            _context.BulkUploadJobs.Add(job);
            await _context.SaveChangesAsync(token);
            return job;
        }

        private async Task FinalizeJobAsync(
            BulkUploadJob job, string status, int successful, int failed, CancellationToken token)
        {
            if (job == null) return;
            try
            {
                var tracked = await _context.BulkUploadJobs.FindAsync(new object[] { job.Id }, token);
                if (tracked == null) return;

                tracked.Status = status;
                tracked.Successful = successful;
                tracked.Failed = failed;
                tracked.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not finalize job {jobId}", job.Id);
            }
        }

        private async Task SafeMarkJobFailedAsync(string filePath, string errorMessage, CancellationToken token)
        {
            try
            {
                var job = await _context.BulkUploadJobs
                    .FirstOrDefaultAsync(j => j.FilePath == filePath, token);

                if (job == null) return;

                job.Status = "Failed";
                job.ErrorMessage = errorMessage;
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(token);
            }
            catch { /* best-effort, do not throw from error handler */ }
        }

        private static async Task BulkInsertWithFallbackAsync(
            Func<Task> bulkInsert,
            Func<Task> fallback,
            ILogger logger,
            string context)
        {
            try
            {
                await bulkInsert();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bulk insert failed for {context}, falling back to individual inserts", context);
                await fallback();
            }
        }
        private async Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args, CancellationToken token)
        {
            if (args.CooperativeIds == null || !args.CooperativeIds.Any())
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "No Cooperative selected.");
            }
            if (args.Id <= 0)
            {
                throw new BaseException(
    Core.Enums.ExceptionType.BAD_REQUEST,
    "Farmer Id is required.");
            }
            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == args.Id, token);

            if (farmer == null)
                return null;

            // Get existing assignments for this farmer
            var existingAssignments = await _context.FarmerCooperatives
                .Where(fa => fa.FarmerId == args.Id && args.CooperativeIds.Contains(fa.CooperativeId))
                .Select(fa => fa.CooperativeId)
                .ToListAsync(token);

            // Filter out agents that are already assigned
            var newCooperativeIds = args.CooperativeIds
       .Where(x => x.HasValue)
       .Select(x => x.Value)
       .Except(existingAssignments)
       .ToList();

            // Create new assignments
            if (newCooperativeIds.Any())
            {
                var newAssignments = newCooperativeIds.Select(cooperativeId => new FarmerCooperative
                {
                    FarmerId = args.Id,
                    CooperativeId = cooperativeId,
                    OnboardedByUserId = args.OnboardedByUserId,
                    JoinedOn = DateTimeOffset.UtcNow,
                    IsActive = true,
                    OrganizationCode = args.organizationCode
                }).ToList();

                await _context.FarmerCooperatives.AddRangeAsync(newAssignments, token);
                await _context.SaveChangesAsync(token);
            }

            // Reload the farmer with updated data
            var updatedFarmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == args.Id, token);

            return updatedFarmer.Map();
        }

        private async Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args, CancellationToken token)
        {
            if (args.AgentIds == null || !args.AgentIds.Any())
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "No Agent selected.");
            }
            if (args.Id <= 0)
            {
                throw new BaseException(
    Core.Enums.ExceptionType.BAD_REQUEST,
    "Farmer Id is required.");
            }
            // Check if the farmer exists
            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == args.Id, token);

            if (farmer == null)
                return null;

            // Get existing assignments for this farmer
            var existingAssignments = await _context.FarmerAgents
                .Where(fa => fa.FarmerId == args.Id && args.AgentIds.Contains(fa.AgentId))
                .Select(fa => fa.AgentId)
                .ToListAsync(token);

            // Filter out agents that are already assigned

            var newAgentIds = args.AgentIds
    .Where(x => x.HasValue)
    .Select(x => x.Value)
    .Except(existingAssignments)
    .ToList();

            // Create new assignments
            if (newAgentIds.Any())
            {
                var newAssignments = newAgentIds.Select(agentId => new FarmerAgent
                {
                    FarmerId = args.Id,
                    AgentId = agentId,
                    AssignedOn = DateTimeOffset.UtcNow
                }).ToList();

                await _context.FarmerAgents.AddRangeAsync(newAssignments, token);
                await _context.SaveChangesAsync(token);
            }

            // Reload the farmer with updated data
            var updatedFarmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == args.Id, token);

            return updatedFarmer.Map();
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
            await ProcessFileAsync(job.FilePath, args, job.Id, token);
        }

        //private async Task ProcessFileAsync(string filePath, OnBoardingArgs args, BulkUploadJob job, CancellationToken token)
        //{
        //    args.userType = "Farmer";
        //    try
        //    {
        //        _logger.LogInformation("Bulk processor starting for file {file}", filePath);

        //        using var stream = File.OpenRead(filePath);
        //        var rows = _excelConverterService.BulkParseExcel(stream);

        //        if (rows == null || !rows.Any())
        //        {
        //            _logger.LogWarning("No rows found in file {file}", filePath);

        //            if (job != null)
        //            {
        //                job.Status = "Completed";
        //                job.CompletedAt = DateTime.UtcNow;
        //                job.Successful = 0;
        //                job.Failed = rows?.Count ?? 0;
        //                await _context.SaveChangesAsync(token);
        //            }

        //            return;
        //        }

        //        // Ensure job exists
        //        if (job == null)
        //        {
        //            job = new BulkUploadJob
        //            {
        //                Id = Guid.NewGuid(),
        //                FilePath = filePath,
        //                OrganizationCode = args.organizationCode,
        //                CreatedByUserId = args.UserId ?? args.OnboardedByUserId ?? "system",
        //                OnboardedByUserId = args.OnboardedByUserId ?? args.UserId ?? "system",
        //                ErrorMessage = string.Empty,
        //                Status = "Processing",
        //                CreatedAt = DateTime.UtcNow,
        //                TotalRows = rows.Count
        //            };

        //            _context.BulkUploadJobs.Add(job);
        //            await _context.SaveChangesAsync(token);
        //        }

        //        var aggregates = new List<BulkRowAggregate>();
        //        int rowNumber = 2;

        //        foreach (var row in rows)
        //        {
        //            try
        //            {
        //                if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName))
        //                    throw new Exception("FirstName and LastName are required.");

        //                if (!int.TryParse(row.YearsInAgric, out var years) || years < 0 || years > 100)
        //                    throw new Exception($"Invalid YearsInAgric: {row.YearsInAgric}");

        //                if (string.IsNullOrWhiteSpace(row.Email) && string.IsNullOrWhiteSpace(row.PhoneNumber))
        //                    throw new Exception("Email or PhoneNumber is required.");

        //                var salt = EncryptionUtil.GetSalt();

        //                var user = BuildUserEntity(new OnboardingRequestModel
        //                {
        //                    Email = row.Email,
        //                    PhoneNumber = row.PhoneNumber,
        //                    Password = "Admin123@",
        //                    Pin = "0000",
        //                    FirstName = row.FirstName,
        //                    MiddleName = row.MiddleName,
        //                    LastName = row.LastName,
        //                    Bvn = row.BVN,
        //                    Nin = row.NIN,
                            
        //                }, args, salt);

        //                var farmer = new Farmer
        //                {
        //                    Name = $"{row.FirstName} {row.MiddleName} {row.LastName}".Trim(),
        //                    Email = row.Email,
        //                    PhoneNumber = row.PhoneNumber,
        //                    Gender = row.Gender,
        //                    State = row.StateOfResidence,
        //                    Lga = row.LGA,
        //                    Ward = row.Ward,
        //                    Location = row.ResidentialAddress,
        //                    FarmerCategory = row.CategoryOfFarmer,
        //                    YearsInAgric = years,
        //                    Level = FarmerLevel.Level1,
        //                    HighestLevelOfEducation = row.HighestLevelOfEducation,
        //                    OnBoardedOn = DateTimeOffset.UtcNow
        //                };

        //                aggregates.Add(new BulkRowAggregate
        //                {
        //                    User = user,
        //                    Farmer = farmer,
        //                    ValueChains = GetValueChains(row)
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogWarning(ex, "Skipping invalid row {rowNumber}", rowNumber);
        //            }

        //            rowNumber++;
        //        }

        //        if (!aggregates.Any())
        //        {
        //            _logger.LogInformation("No valid rows to process for file {file}", filePath);

        //            job.Status = "Completed";
        //            job.CompletedAt = DateTime.UtcNow;
        //            job.Successful = 0;
        //            job.Failed = rows.Count;
        //            await _context.SaveChangesAsync(token);

        //            return;
        //        }

        //        var bulkConfig = new BulkConfig
        //        {
        //            BatchSize = 10000,
        //            BulkCopyTimeout = 300,
        //            UseTempDB = true
        //        };

        //        // Duplicate checks: email, phone
        //        var emails = aggregates.Where(a => !string.IsNullOrWhiteSpace(a.User.Email)).Select(a => a.User.Email).Distinct().ToList();
        //        var phones = aggregates.Where(a => !string.IsNullOrWhiteSpace(a.User.PhoneNumber)).Select(a => a.User.PhoneNumber).Distinct().ToList();

        //        var existingUsers = await _context.Users.AsNoTracking()
        //            .Where(u => emails.Contains(u.Email) || phones.Contains(u.PhoneNumber))
        //            .Select(u => new { u.Id, u.Email, u.PhoneNumber })
        //            .ToListAsync(token);

        //        var existingUserMapByEmail = existingUsers.Where(x => !string.IsNullOrWhiteSpace(x.Email)).ToDictionary(x => x.Email, x => x.Id);
        //        var existingUserMapByPhone = existingUsers.Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber)).ToDictionary(x => x.PhoneNumber, x => x.Id);

        //        // Also check existing farmers by same keys to skip
        //        var existingFarmers = await _context.Farmers.AsNoTracking()
        //            .Where(f => emails.Contains(f.Email) || phones.Contains(f.PhoneNumber))
        //            .Select(f => new { f.Id, f.UserId, f.Email, f.PhoneNumber })
        //            .ToListAsync(token);

        //        var existingFarmerEmails = existingFarmers.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToHashSet();
        //        var existingFarmerPhones = existingFarmers.Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber)).Select(x => x.PhoneNumber).ToHashSet();

        //        var newUsers = new List<User>();
        //        var farmers = new List<Farmer>();
        //        var valueChains = new List<FarmerValueChainItem>();

        //        foreach (var agg in aggregates)
        //        {
        //            var email = agg.User.Email;
        //            var phone = agg.User.PhoneNumber;

        //            agg.User.UserType = args.userType;
        //            agg.User.IsActive = true;
                   
        //            // if farmer exists (email or phone), skip entire row
        //            if (!string.IsNullOrWhiteSpace(email) && existingFarmerEmails.Contains(email))
        //            {
        //                _logger.LogInformation("Skipping existing farmer by email {email}", email);
        //                continue;
        //            }

        //            if (!string.IsNullOrWhiteSpace(phone) && existingFarmerPhones.Contains(phone))
        //            {
        //                _logger.LogInformation("Skipping existing farmer by phone {phone}", phone);
        //                continue;
        //            }

        //            // attach existing user if present by any key
        //            string matchedUserId = null;
        //            if (!string.IsNullOrWhiteSpace(email) && existingUserMapByEmail.TryGetValue(email, out var uidE)) matchedUserId = uidE;
        //            else if (!string.IsNullOrWhiteSpace(phone) && existingUserMapByPhone.TryGetValue(phone, out var uidP)) matchedUserId = uidP;

        //            if (matchedUserId != null)
        //            {
        //                agg.Farmer.UserId = matchedUserId;
        //            }
        //            else
        //            {
        //                newUsers.Add(agg.User);
        //            }

        //            farmers.Add(agg.Farmer);
        //        }

        //        // Bulk insert new users
        //        var emailToIdMap = new Dictionary<string, string>(existingUserMapByEmail);
        //        var phoneToIdMap = new Dictionary<string, string>(existingUserMapByPhone);
        //        var insertedUserIds = new HashSet<string>();

        //        if (newUsers.Any())
        //        {
        //            try
        //            {
        //                await _context.BulkInsertAsync(newUsers, bulkConfig, cancellationToken: token);

        //                foreach (var u in newUsers)
        //                {
        //                    if (!string.IsNullOrWhiteSpace(u.Id))
        //                    {
        //                        insertedUserIds.Add(u.Id);
        //                        if (!string.IsNullOrWhiteSpace(u.Email)) emailToIdMap[u.Email] = u.Id;
        //                        if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) phoneToIdMap[u.PhoneNumber] = u.Id;
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogWarning(ex, "Bulk insert for users failed, falling back to single inserts");

        //                foreach (var u in newUsers)
        //                {
        //                    try
        //                    {
        //                        _context.Users.Add(u);
        //                        await _context.SaveChangesAsync(token);

        //                        if (!string.IsNullOrWhiteSpace(u.Id))
        //                        {
        //                            insertedUserIds.Add(u.Id);
        //                            if (!string.IsNullOrWhiteSpace(u.Email)) emailToIdMap[u.Email] = u.Id;
        //                            if (!string.IsNullOrWhiteSpace(u.PhoneNumber)) phoneToIdMap[u.PhoneNumber] = u.Id;
        //                        }
        //                    }
        //                    catch (Exception inner)
        //                    {
        //                        // Log and attempt to resolve by finding existing user (duplicate key case)
        //                        _logger.LogWarning(inner, "Failed to insert user {email}/{phone} individually, attempting to find existing record", u.Email, u.PhoneNumber);
        //                        try
        //                        {
        //                            User existing = null;
        //                            if (!string.IsNullOrWhiteSpace(u.Email))
        //                                existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == u.Email, token);
        //                            if (existing == null && !string.IsNullOrWhiteSpace(u.PhoneNumber))
        //                                existing = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.PhoneNumber == u.PhoneNumber, token);

        //                            if (existing != null)
        //                            {
        //                                insertedUserIds.Add(existing.Id);
        //                                if (!string.IsNullOrWhiteSpace(existing.Email)) emailToIdMap[existing.Email] = existing.Id;
        //                                if (!string.IsNullOrWhiteSpace(existing.PhoneNumber)) phoneToIdMap[existing.PhoneNumber] = existing.Id;
        //                            }
        //                        }
        //                        catch { }
        //                    }
        //                }
        //            }
        //        }

        //        // set user ids on farmers for newly inserted or matched users
        //        foreach (var agg in aggregates)
        //        {
        //            if (agg.User == null) continue;

        //            if (!string.IsNullOrWhiteSpace(agg.User.Email) && emailToIdMap.TryGetValue(agg.User.Email, out var idByEmail))
        //            {
        //                agg.Farmer.UserId = idByEmail;
        //            }
        //            else if (!string.IsNullOrWhiteSpace(agg.User.PhoneNumber) && phoneToIdMap.TryGetValue(agg.User.PhoneNumber, out var idByPhone))
        //            {
        //                agg.Farmer.UserId = idByPhone;
        //            }
        //        }

        //        // Only insert farmers that have a UserId (either matched or newly inserted)
        //        var farmersToInsert = farmers.Where(f => !string.IsNullOrWhiteSpace(f.UserId)).ToList();
        //        var insertedFarmerIds = new List<long>();

        //        if (farmersToInsert.Any())
        //        {
        //            try
        //            {
        //                await _context.BulkInsertAsync(farmersToInsert, bulkConfig, cancellationToken: token);

        //                insertedFarmerIds.AddRange(farmersToInsert.Select(f => f.Id));
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogWarning(ex, "Bulk insert for farmers failed, falling back to single inserts");

        //                foreach (var f in farmersToInsert)
        //                {
        //                    try
        //                    {
        //                        _context.Farmers.Add(f);
        //                        await _context.SaveChangesAsync(token);
        //                        insertedFarmerIds.Add(f.Id);
        //                    }
        //                    catch (Exception inner)
        //                    {
        //                        _logger.LogWarning(inner, "Failed to insert farmer {name} individually, attempting to find existing by email/phone", f.Name);
        //                        try
        //                        {
        //                            var existing = await _context.Farmers.AsNoTracking()
        //                                .FirstOrDefaultAsync(x => (!string.IsNullOrWhiteSpace(f.Email) && x.Email == f.Email) || (!string.IsNullOrWhiteSpace(f.PhoneNumber) && x.PhoneNumber == f.PhoneNumber), token);
        //                            if (existing != null)
        //                            {
        //                                insertedFarmerIds.Add(existing.Id);
        //                            }
        //                        }
        //                        catch { }
        //                    }
        //                }
        //            }

        //            // create value chain items for inserted farmers
        //            foreach (var agg in aggregates)
        //            {
        //                var f = farmersToInsert.FirstOrDefault(x => x.Name == agg.Farmer.Name && (x.Email == agg.Farmer.Email || x.PhoneNumber == agg.Farmer.PhoneNumber));
        //                if (f == null || !insertedFarmerIds.Contains(f.Id)) continue;

        //                foreach (var vc in agg.ValueChains ?? new())
        //                {
        //                    foreach (var item in vc.Items ?? new())
        //                    {
        //                        valueChains.Add(new FarmerValueChainItem
        //                        {
        //                            FarmerId = f.Id,
        //                            ValueChainType = vc.ValueChainType,
        //                            ItemName = item
        //                        });
        //                    }
        //                }
        //            }

        //            if (valueChains.Any())
        //            {
        //                try
        //                {
        //                    await _context.BulkInsertAsync(valueChains, bulkConfig, cancellationToken: token);
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogWarning(ex, "Bulk insert for value chains failed, falling back to single inserts");
        //                    foreach (var vc in valueChains)
        //                    {
        //                        try
        //                        {
        //                            _context.FarmerValueChainItems.Add(vc);
        //                            await _context.SaveChangesAsync(token);
        //                        }
        //                        catch { }
        //                    }
        //                }
        //            }

        //            // Insert UserOrganization mappings (avoid duplicates)
        //            var userOrgs = farmersToInsert
        //                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
        //                .Select(x => new UserOrganization { UserId = x.UserId, OrganizationCode = args.organizationCode })
        //                .DistinctBy(x => new { x.UserId, x.OrganizationCode })
        //                .ToList();

        //            if (userOrgs.Any())
        //            {
        //                var existingUserOrgs = await _context.UserOrganizations.AsNoTracking()
        //                    .Where(uo => userOrgs.Select(u => u.UserId).Contains(uo.UserId) && uo.OrganizationCode == args.organizationCode)
        //                    .Select(uo => new { uo.UserId, uo.OrganizationCode })
        //                    .ToListAsync(token);

        //                var existingSet = new HashSet<string>(existingUserOrgs.Select(x => $"{x.UserId}-{x.OrganizationCode}"));

        //                var toInsertUserOrgs = userOrgs.Where(uo => !existingSet.Contains($"{uo.UserId}-{uo.OrganizationCode}")).ToList();

        //                if (toInsertUserOrgs.Any())
        //                {
        //                    try
        //                    {
        //                        await _context.BulkInsertAsync(toInsertUserOrgs, bulkConfig, cancellationToken: token);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logger.LogWarning(ex, "Bulk insert for user-orgs failed, falling back to single inserts");
        //                        foreach (var uo in toInsertUserOrgs)
        //                        {
        //                            uo.IsActive = true;
        //                            try { _context.UserOrganizations.Add(uo); await _context.SaveChangesAsync(token); } catch { }
        //                        }
        //                    }
        //                }
        //            }

        //            // Insert FarmerOrganization mappings (avoid duplicates)
        //            var farmerOrgCandidates = farmersToInsert.Select(f => new FarmerOrganization
        //            {
        //                FarmerId = f.Id,
        //                IsActive=true,
        //                OrganizationCode = args.organizationCode,
        //                OnboardedByUserId = args.OnboardedByUserId
        //            }).ToList();

        //            var farmerIds = farmerOrgCandidates.Select(f => f.FarmerId).ToList();
        //            var existingFarmerOrgs = await _context.FarmerOrganizations.AsNoTracking()
        //                .Where(fo => farmerIds.Contains(fo.FarmerId) && fo.OrganizationCode == args.organizationCode)
        //                .Select(fo => new { fo.FarmerId, fo.OrganizationCode })
        //                .ToListAsync(token);

        //            var existingFarmerOrgSet = new HashSet<string>(existingFarmerOrgs.Select(x => $"{x.FarmerId}-{x.OrganizationCode}"));

        //            var toInsertFarmerOrgs = farmerOrgCandidates.Where(f => !existingFarmerOrgSet.Contains($"{f.FarmerId}-{f.OrganizationCode}")).ToList();

        //            if (toInsertFarmerOrgs.Any())
        //            {
        //                try
        //                {
        //                    await _context.BulkInsertAsync(toInsertFarmerOrgs, bulkConfig, cancellationToken: token);
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogWarning(ex, "Bulk insert for farmer-orgs failed, falling back to single inserts");
        //                    foreach (var fo in toInsertFarmerOrgs)
        //                    {
        //                        fo.IsActive = true;
        //                        try { _context.FarmerOrganizations.Add(fo); await _context.SaveChangesAsync(token); } catch { }
        //                    }
        //                }
        //            }

        //            _logger.LogInformation("Bulk file {file} processed: UsersAttempted={u} FarmersAttempted={f} FarmersInserted={fi}", filePath, newUsers.Count, farmersToInsert.Count, insertedFarmerIds.Count);

        //            // update job
        //            var existingJob = await _context.BulkUploadJobs.FindAsync(job.Id);
        //            if (existingJob != null)
        //            {
        //                existingJob.Status = "Completed";
        //                existingJob.CompletedAt = DateTime.UtcNow;
        //                existingJob.Successful = insertedFarmerIds.Count;
        //                existingJob.Failed = rows.Count - insertedFarmerIds.Count;
        //                await _context.SaveChangesAsync(token);
        //            }
        //        }
        //        else
        //        {
        //            _logger.LogInformation("No new farmers to insert for file {file}", filePath);
        //            // update job as completed with zero successful
        //            var existingJob = job != null ? await _context.BulkUploadJobs.FindAsync(job.Id) : null;
        //            if (existingJob != null)
        //            {
        //                existingJob.Status = "Completed";
        //                existingJob.CompletedAt = DateTime.UtcNow;
        //                existingJob.Successful = 0;
        //                existingJob.Failed = rows.Count;
        //                await _context.SaveChangesAsync(token);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Bulk onboarding processing failed for file {file}", filePath);

        //        // update job as failed
        //        try
        //        {
        //            var existingJob = await _context.BulkUploadJobs.FirstOrDefaultAsync(j => j.FilePath == filePath, token);
        //            if (existingJob != null)
        //            {
        //                existingJob.Status = "Failed";
        //                existingJob.ErrorMessage = ex.Message;
        //                existingJob.CompletedAt = DateTime.UtcNow;
        //                await _context.SaveChangesAsync(token);
        //            }
        //        }
        //        catch { }
        //    }
        //}

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
