using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition.Model;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using EFCore.BulkExtensions;
using iTextSharp.text.pdf.security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using User = Agrovesto.Backend.Infrastructure.Entities.Common.User;


namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class BusinessManager : IBusinessManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly IWalletManager _walletManager;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly IMembershipRepository _cooperativeRepository;
        private readonly ILogger<BusinessManager> _log;
        private readonly IIdentityVerificationManager _identityVerificationManager;
        private readonly IFarmPlotDetailRepository _farmPlotDetailRepository;
        private readonly IAWSRekognitionManager _faceManager;
        private readonly INotificationService _whisperSMSService;
        private readonly IInputFinancingManagementRepository _inputRepository;
        private readonly ITraderRepository _traderRepository;
        private readonly IExcelConverterService _excelConverterService;

        public BusinessManager(IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletManager walletManager, IFarmerRepository farmerRepository,
            IIdentityVerificationManager identityVerificationManager,
            IFarmPlotDetailRepository farmPlotDetailRepository,
            IAgentRepository agentRepository, INotificationService whisperSMSService,
            IInputFinancingManagementRepository inputRepository,
            ITraderRepository traderRepository, IExcelConverterService excelConverterService,

                IAWSRekognitionManager faceManager, ILogger<BusinessManager> log, IMembershipRepository cooperativeRepository)
        {
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _walletManager = walletManager;
            _farmerRepository = farmerRepository;
            _agentRepository = agentRepository;
            _cooperativeRepository = cooperativeRepository;
            _log = log;
            _farmPlotDetailRepository = farmPlotDetailRepository;
            _identityVerificationManager = identityVerificationManager;
            _faceManager = faceManager;
            _whisperSMSService = whisperSMSService;
            _inputRepository = inputRepository;
            _traderRepository = traderRepository;
            _excelConverterService = excelConverterService;

        }
        private async Task<List<UserModel>> ParseFarmerFileAsync(Stream fileContent, BusinessRegistrationArgs args)
        {
            var (allFarmers, errors) = await _excelConverterService.ExtractFarmersFromExcel(fileContent, args);

            if (errors.Any())
            {
                var errorMessages = string.Join("\n", errors.Select(e => $"Row {e.Row}: {e.ErrorMessage}"));
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, $"The following errors were found in the uploaded file:\n{errorMessages}");
            }

            if (!allFarmers.Any())
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                    $"The file contains no valid farmer data.");
            }

            return (allFarmers);
        }
        public async Task<BulkUploadFarmersResponse> BulkUploadFarmersAsync(FarmerUploadModel request, BusinessRegistrationArgs args)
        {
            var summary = new BulkUploadFarmersResponse();
            var errors = new List<BulkUploadError>();
            var createdFarmers = new List<Farmer>();
            var model = new BulkFarmerUpoad();
            model.file = request.file;
            model.FileContent = request.file.OpenReadStream();
            if (request.file == null || request.file.OpenReadStream() == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "No file was uploaded.");
            }

            // 1. Extract rows using your extractor
            var allFarmers = await ParseFarmerFileAsync(model.FileContent, args);

            if (allFarmers == null || !allFarmers.Any())
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "The uploaded file does not contain valid farmer data.");

            var upload = await BulkCreateBusinessUsersFromExcelAsync(allFarmers, args);
            summary.TotalRows = allFarmers.Count;
            summary.Successful = upload.Successful;//.Results.Count;
            summary.Failed = upload.Failed; //errors.Count;
           summary.Errors = upload.Errors;
            summary.Message = upload.Message;//"Bulk upload completed.";

            return summary;
        }
        public async Task<BulkUploadFarmersResponse> NewBulkUploadFarmersAsync(FarmerUploadModel request, BusinessRegistrationArgs args, IProgress<BulkUploadProgress> progress = null)
        {
            var summary = new BulkUploadFarmersResponse();
            var errors = new List<BulkUploadError>();
            var createdFarmers = new List<Farmer>();
            var model = new BulkFarmerUpoad();
            model.file = request.file;
            model.FileContent = request.file.OpenReadStream();
            if (request.file == null || request.file.OpenReadStream() == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "No file was uploaded.");
            }

            // 1. Extract rows using your extractor
            var allFarmers = await ParseFarmerFileAsync(model.FileContent, args);

            if (allFarmers == null || !allFarmers.Any())
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "The uploaded file does not contain valid farmer data.");

            var upload = await NewBulkCreateBusinessUsersFromExcelAsync(allFarmers, args, progress);
            summary.TotalRows = allFarmers.Count;
            summary.Successful = upload.Successful;//.Results.Count;
            summary.Failed = upload.Failed; //errors.Count;
           summary.Errors = upload.Errors;
            summary.Message = upload.Message;//"Bulk upload completed.";

            return summary;
        }
     
        public async Task<BulkUploadFarmersResponse> NewBulkCreateBusinessUsersFromExcelAsync(
    List<UserModel> users,
    BusinessRegistrationArgs args,
        IProgress<BulkUploadProgress> progress = null)
        {
            const int batchSize = 50;
            var allResults = new List<(UserModel user, bool status, string message)>();
            var totalRows = users.Count;
            var processedRows = 0;
            var existingPhones = await _farmerRepository.GetAllPhonesForOrganizationAsync(args.organizationCode);
            var phoneSet = new HashSet<string>(existingPhones, StringComparer.OrdinalIgnoreCase);

            for (int i=0; i< users.Count; i +=batchSize)
            {
                var batch = users.Skip(i).Take(batchSize).ToList();
                var batchResults = await ProcessBatchAsync(batch, args, phoneSet);
                allResults.AddRange(batchResults);

                processedRows += batch.Count;
                progress?.Report(new BulkUploadProgress
                {
                    TotalRows = totalRows,
                    ProcressedRows = processedRows,
                    SuccessfulRows = allResults.Count(r => r.status),
                    FailedRows = allResults.Count(r => !r.status),
                    Status = "Processing"
                });

                // Small delay to prevent overwhelming system
                await Task.Delay(50);
            }

            return new BulkUploadFarmersResponse
            {
                TotalRows = totalRows,
                Successful = allResults.Count(r => r.status),
                Failed = allResults.Count(r => !r.status),
                Results = allResults,
                Message = "Bulk upload completed successfully"
            };
        }

        private async Task<List<(UserModel user, bool status, string message)>> ProcessBatchAsync(
       List<UserModel> batch,
       BusinessRegistrationArgs args,
       HashSet<string> existingPhoneSet)
        {
            var results = new List<(UserModel user, bool status, string message)>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var user in batch)
                {
                    try
                    {
                        // Skip if phone already exists in organization
                        if (existingPhoneSet.Contains(user.PhoneNumber))
                        {
                            results.Add((user, false, $"Phone number {user.PhoneNumber} already exists"));
                            continue;
                        }

                        var response = await CreateBusinessUserOptimizedAsync(user, args);
                        results.Add((user, true, "Successfully processed."));

                        // Add to existing set to prevent duplicates in same batch
                        existingPhoneSet.Add(user.PhoneNumber);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, $"Error processing user {user.PhoneNumber}");
                        results.Add((user, false, ex.Message));
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _log.LogError(ex, "Batch transaction failed");

                // Mark all users in batch as failed
                foreach (var user in batch.Where(u => !results.Any(r => r.user == u)))
                {
                    results.Add((user, false, $"Batch processing failed: {ex.Message}"));
                }
            }

            return results;
        }

        private async Task<SubAccountModel> CreateBusinessUserOptimizedAsync(
            UserModel model,
            BusinessRegistrationArgs args)
        {
            // Validate phone number
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length != 11)
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                    "Phone number must be exactly 11 digits.");

            // Fetch business once per batch
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

            args.UserId ??= business.UserId;
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;

            // NIN verification if required
            if (model.shouldVerifyNin)
            {
                try
                {
                    var ninVerification = await _identityVerificationManager.VerifyNIN(
                        new NINDataModel { number = model.NIN });

                    if (ninVerification.status)
                    {
                        model.shouldVerifyNin = true;
                        model.NIN = ninVerification.nin_data.nin;
                        model.FirstName = ninVerification.nin_data.firstname;
                        model.LastName = ninVerification.nin_data.surname;
                        model.MiddleName = ninVerification.nin_data.middlename;
                        _log.LogInformation($"NIN verification succeeded for NIN: {model.NIN}");
                    }
                    else
                    {
                        model.shouldVerifyNin = false;
                        _log.LogWarning($"NIN verification failed: {ninVerification.detail}");
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"NIN verification error for {model.NIN}");
                    model.shouldVerifyNin = false;
                }
            }

            // Use execution strategy for database resilience
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                    UserModel userToUse;
                    bool isNewUser = false;

                    if (existingUser != null)
                    {
                        userToUse = existingUser;
                    }
                    else
                    {
                        // Generate full name if empty
                        if (string.IsNullOrEmpty(model.Name))
                            model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";

                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                                $"User registration failed: {registrationResult.message}");

                        userToUse = registrationResult.model;
                        isNewUser = true;
                    }

                    // Handle organization linkage
                    if (!string.IsNullOrEmpty(args.organizationCode))
                    {
                        bool alreadyInOrg = await _context.UserOrganizations
                            .AnyAsync(x => x.UserId == userToUse.Id && x.OrganizationCode == args.organizationCode);

                        if (!alreadyInOrg)
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = userToUse.Id,
                                OrganizationCode = args.organizationCode
                            });

                            if (isNewUser)
                            {
                                await NotifyUserOfOrganizationAddition(userToUse.PhoneNumber, args.organizationCode);
                            }
                        }
                    }

                    // Ensure subaccount exists
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(userToUse.Id);
                    SubAccountModel subAccount;

                    if (string.IsNullOrEmpty(model.Email))
                        model.Email = GenerateEmail(model.FirstName, business.BusinessEmail);

                    if (existingSubAccount != null)
                    {
                        subAccount = existingSubAccount;
                    }
                    else
                    {
                        subAccount = await CreateSubAccountAsync(model, args, business, userToUse.Id);
                    }

                    // Track role assignments
                    await _authenticationRepository.TrackUserRoleAssignment(userToUse, model.UserType);

                    // Create user category/profile
                    await CreateUserCategory(model, args, new BusinessWorkerModel(),
                        new BusinessWorkerModel(), business, subAccount,
                        (userToUse, !isNewUser, ""), args.UserType);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Populate subaccount response
                    subAccount.UserId = userToUse.Id;
                    subAccount.Address = model.Location;
                    subAccount.City = model.Lga;
                    subAccount.FirstName = model.FirstName;
                    subAccount.LastName = model.LastName;
                    subAccount.MiddleName = model.MiddleName;
                    subAccount.Alias = !string.IsNullOrEmpty(model.Alias) ? model.Alias : "Mr";

                    return subAccount;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _log.LogError(ex, "Error occurred during CreateBusinessUserOptimized");
                    throw new Exception($"Unable to register user {model.PhoneNumber}: {ex.Message}");
                }
            });
        }

        public async Task<SubAccountModel> NewCreateBusinessUser(UserModel model, BusinessRegistrationArgs args)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length != 11)
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number must be exactly 11 digits.");

            // Fetch the business
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

            args.UserId ??= business.UserId;
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;

            // NIN verification if required
            if (model.shouldVerifyNin)
            {
                var ninVerification = await _identityVerificationManager.VerifyNIN(new NINDataModel { number = model.NIN });
                if (!ninVerification.status)
                {
                    _log.LogError($"NIN verification failed: {ninVerification.detail}");
                    model.shouldVerifyNin = false;
                }
                else
                {
                    model.shouldVerifyNin = true;
                    model.NIN = ninVerification.nin_data.nin;
                    model.FirstName = ninVerification.nin_data.firstname;
                    model.LastName = ninVerification.nin_data.surname;
                    model.MiddleName = ninVerification.nin_data.middlename;
                    _log.LogInformation($"NIN verification succeeded for NIN: {model.NIN}");
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                    bool isNewUser = false;

                    if (existingUser != null)
                    {
                        // Only handle organization linkage if organizationCode is provided
                        if (!string.IsNullOrEmpty(args.organizationCode))
                        {
                            bool alreadyInOrg = await _context.UserOrganizations
                                .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode);

                            if (alreadyInOrg)
                                throw new BaseException(Core.Enums.ExceptionType.CONFLICT,
                                    $"A user with phone number {model.PhoneNumber} already exists in this organization.");

                            if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                            {
                                _context.UserOrganizations.Add(new UserOrganization
                                {
                                    UserId = existingUser.Id,
                                    OrganizationCode = args.organizationCode
                                });
                                _log.LogInformation($"User {model.PhoneNumber} added to organization {args.organizationCode}.");
                                await NotifyUserOfOrganizationAddition(existingUser.PhoneNumber, args.organizationCode);
                            }
                        }
                        else
                        {
                            _log.LogInformation($"User {model.PhoneNumber} is a direct business user (no organization).");
                        }
                    }
                    else
                    {
                        // Generate full name if empty
                        if (string.IsNullOrEmpty(model.Name))
                            model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";

                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                                $"User registration failed: {registrationResult.message}");

                        existingUser = registrationResult.model;
                        isNewUser = true;

                        // Add to organization if applicable
                        if (!string.IsNullOrEmpty(args.organizationCode) &&
                            !_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                            _log.LogInformation($"New user {model.PhoneNumber} added to organization {args.organizationCode}.");
                        }
                    }

                    // Ensure subaccount exists
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(existingUser.Id);
                    SubAccountModel subAccount;
                    model.Email ??= GenerateEmail(model.FirstName, business.BusinessEmail);

                    subAccount = existingSubAccount ?? await CreateSubAccountAsync(model, args, business, existingUser.Id);

                    // Track role assignments
                    await _authenticationRepository.TrackUserRoleAssignment(existingUser, model.UserType);

                    // Create business worker
                    await CreateUserCategory(
                        model,
                        args,
                        new BusinessWorkerModel(),
                        new BusinessWorkerModel(),
                        business,
                        subAccount,
                        (existingUser, !isNewUser, ""),
                        args.UserType
                    );

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Populate subaccount response
                    subAccount.UserId = existingUser.Id;
                    subAccount.Address = model.Location;
                    subAccount.City = model.Lga;
                    subAccount.FirstName = model.FirstName;
                    subAccount.LastName = model.LastName;
                    subAccount.MiddleName = model.MiddleName;
                    subAccount.Alias = !string.IsNullOrEmpty(model.Alias) ? model.Alias : "Mr";

                    return subAccount;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error occurred during CreateBusinessUser");
                    await transaction.RollbackAsync();
                    throw new Exception("Unable to register user at this time. Please try again later.");
                }
            });
        }

        public async Task<SubAccountModel> CreateBulkBusinessUser(UserModel model, BusinessModel business, BusinessRegistrationArgs args)
        {
            // 1. Initial Validation
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length != 11)
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number must be exactly 11 digits.");

              // Assuming ConvertedDate is a local utility method
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;

            // 4. NIN Verification (I/O operation outside the transaction)
            if (model.shouldVerifyNin)
            {
                var ninVerification = await _identityVerificationManager.VerifyNIN(new NINDataModel { number = model.NIN });
                if (!ninVerification.status)
                {
                    _log.LogError($"NIN verification failed: {ninVerification.detail}");
                    // Keep shouldVerifyNin=false to prevent subsequent retries if verification failed
                    model.shouldVerifyNin = false;
                }
                else
                {
                    model.shouldVerifyNin = true; // Mark as successfully verified
                    model.NIN = ninVerification.nin_data.nin;
                    model.FirstName = ninVerification.nin_data.firstname;
                    model.LastName = ninVerification.nin_data.surname;
                    model.MiddleName = ninVerification.nin_data.middlename;
                    _log.LogInformation($"NIN verification succeeded for NIN: {model.NIN}");
                }
            }

            // 5. Execution Strategy with Transaction (Ensures atomicity for database writes)
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                    bool isNewUser = false;

                    if (existingUser != null)
                    {
                        // Logic for EXISTING User

                        // Organization Linkage
                        if (!string.IsNullOrEmpty(args.organizationCode))
                        {
                            // Check database if user is already linked (1st and only DB check)
                            bool alreadyInOrg = await _context.UserOrganizations
                                .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode);

                            if (alreadyInOrg)
                                throw new BaseException(Core.Enums.ExceptionType.CONFLICT,
                                    $"A user with phone number {model.PhoneNumber} already exists in this organization.");

                            // Optimization: Directly add the organization link since the DB check confirmed no duplicate.
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                            _log.LogInformation($"User {model.PhoneNumber} added to organization {args.organizationCode}.");
                            await NotifyUserOfOrganizationAddition(existingUser.PhoneNumber, args.organizationCode);
                        }
                        else
                        {
                            _log.LogInformation($"User {model.PhoneNumber} is a direct business user (no organization).");
                        }
                    }
                    else
                    {
                        // Logic for NEW User

                        // Generate full name if empty
                        if (string.IsNullOrEmpty(model.Name))
                            model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";

                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                                $"User registration failed: {registrationResult.message}");

                        existingUser = registrationResult.model;
                        isNewUser = true;

                        // Add to organization if applicable (New user logic)
                        if (!string.IsNullOrEmpty(args.organizationCode) &&
                            !_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                            _log.LogInformation($"New user {model.PhoneNumber} added to organization {args.organizationCode}.");
                        }
                    }

                    // 6. Ensure Subaccount Exists (Wallet/Account Manager operations)
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(existingUser.Id);
                    SubAccountModel subAccount;
                    model.Email ??= GenerateEmail(model.FirstName, args.businessEmail);

                    subAccount = existingSubAccount ?? await CreateSubAccountAsync(model, args, business, existingUser.Id);

                    // 7. Track Role and Create Category
                    await _authenticationRepository.TrackUserRoleAssignment(existingUser, model.UserType);

                    // Create business worker/category
                    await CreateUserCategory(
                        model,
                        args,
                        new BusinessWorkerModel(), // Assuming these are mock/empty models for the method
                        new BusinessWorkerModel(),
                        business,
                        subAccount,
                        (existingUser, !isNewUser, ""),
                        args.UserType
                    );

                    // 8. Commit Changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 9. Populate and Return Subaccount Response
                    subAccount.UserId = existingUser.Id;
                    subAccount.Address = model.Location;
                    subAccount.City = model.Lga;
                    subAccount.FirstName = model.FirstName;
                    subAccount.LastName = model.LastName;
                    subAccount.MiddleName = model.MiddleName;
                    subAccount.Alias = !string.IsNullOrEmpty(model.Alias) ? model.Alias : "Mr";

                    return subAccount;
                }
                catch (Exception ex)
                {
                    // 1. Log the full exception and stack trace
                    _log.LogError(ex, "Error occurred during CreateBulkBusinessUser and transaction rollback initiated.");

                    // 2. Rollback the database transaction
                    await transaction.RollbackAsync();

                    // 3. Determine the root cause message
                    string errorMessage;

                    // Check for specific exceptions that wrap the database error (like EF Core)
                    if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                    {
                        // Extract the most specific database error message (e.g., SQL server constraint violation)
                        errorMessage = $"DB Constraint Error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                    }
                    // Check if it's already a handled business exception
                    else if (ex is BaseException bex)
                    {
                        // Keep the original, clear business message
                        errorMessage = bex.Message;
                    }
                    // Catch other internal errors, like identity creation failures (if not using BaseException)
                    else if (ex.InnerException != null)
                    {
                        errorMessage = $"System Error: {ex.InnerException.Message}";
                    }
                    else
                    {
                        errorMessage = ex.Message;
                    }

                    // 4. Wrap the root message in a BaseException and rethrow. 
                    // This ensures the outer Task.WhenAll loop catches a BaseException for clear reporting.
                    throw new BaseException(Core.Enums.ExceptionType.INTERNAL_SERVER_ERROR, errorMessage);
                }
            });
        }
        public async Task<BulkUploadFarmersResponse> BulkCreateBusinessUsersFromExcelAsync(
        List<UserModel> excelUsers,
        BusinessRegistrationArgs args)
         {
             var results = new List<(UserModel user, bool status, string message)>();

             foreach (var rawUser in excelUsers)
             {
                 try
                 {
                     var response = await CreateBusinessUser(rawUser, args);

                     if (response == null)
                     {
                         results.Add((rawUser, false, "User creation returned null."));
                         continue;
                     }

                     results.Add((rawUser, true, "Successfully processed."));
                 }
                 catch (Exception ex)
                 {
                     results.Add((rawUser, false, ex.Message));
                 }
             }

             // summary
             int successCount = results.Count(x => x.status == true);
             int failedCount = results.Count(x => x.status == false);

             return new BulkUploadFarmersResponse
             {
                 TotalRows = excelUsers.Count,
                 Successful = successCount,
                 Failed = failedCount,
                 Results = results
             };
         }
         
        public async Task<Response<UserAccountDetailsModel>> GetUserBankAccountDetails(

        ListQueryParams query)
        {
            try
            {
                var user = await _authenticationRepository.GetUserById(query.UserId);
                if (user.model == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found"); }

                var accountDetails = await _walletManager.GetUserSterlingAccount(query.UserId);
                if (accountDetails == null)
                {
                    return new Response<UserAccountDetailsModel>
                    {
                        Succeeded = true,
                        Data = null,
                        Message = "No Record Found"
                    };
                }
                var account = new UserAccountDetailsModel
                {
                    WalletId = accountDetails.WalletId,
                    CustomerId = accountDetails.CustomerId,
                    AccountName = accountDetails.AccountName,
                    AccountNumber = accountDetails.AccountNumber,
                    BankCode = accountDetails.BankCode,
                    BankName = accountDetails.BankName,
                };
                return new Response<UserAccountDetailsModel>
                {
                    Succeeded = true,
                    Data = account,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving User Account: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<Response<CafeOneCustomerWalletResponse>> RetrieveCustomerWallet(

        ListQueryParams query)
        {
            try
            {
                var user = await _authenticationRepository.GetUserById(query.UserId);
                if (user.model == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found"); }


                var walletDetails = await _walletManager.RetrieveCustomerWallet(query.UserId, query.env);

                return new Response<CafeOneCustomerWalletResponse>
                {
                    Succeeded = true,
                    Data = walletDetails,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving User Account: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<Response<UserWalletBalanceModel>> GetUserBankAccountBalance(

        ListQueryParams query)
        {
            try
            {
                var user = await _authenticationRepository.GetUserById(query.UserId);
                if (user.model == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found"); }


                var userBalance = await _walletManager.RetrieveCustomerWallet(query.UserId, query.env);
                if (userBalance == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }
                var accountDetails = new UserWalletBalanceModel
                {
                    AvailableBalance = userBalance.availableBalance,
                    LedgerBalance = userBalance.ledgerBalance,
                    //Status=userBalance,
                    WalletId = userBalance.Id,
                    Currency = userBalance.currencyId,
                    CustomerId = userBalance.customerId,
                };
                return new Response<UserWalletBalanceModel>
                {
                    Succeeded = true,
                    Data = accountDetails,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving User Account: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<AgentModel>>> AgentsBySupervisor(string userId,
      ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                //var agent = await _agentRepository.GetAgentBySuperVisor(userId);

                //if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var cooperative = await _cooperativeRepository.AgentsBySupervisor(userId, query);
                if (cooperative == null) return new PagedListResponse<List<AgentModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<AgentModel>>
                {
                    Succeeded = true,
                    Data = cooperative,
                    Count = cooperative.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId)
        {
            var assignAgentToCooperative = new AgentModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    assignAgentToCooperative = await _cooperativeRepository.AssignSupervisorToAgent(agentId, supervisorId);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return assignAgentToCooperative;
        }

        public async Task<PagedListResponse<List<BusinessModel>>> GetBusinessesAsync(ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;

                var businesses = await _businessRepository.GetBusinessesAsync();
                if (businesses == null) return new PagedListResponse<List<BusinessModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<BusinessModel>>
                {
                    Succeeded = true,
                    Data = businesses,
                    Count = businesses.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        private async Task SendOnboardingSMSM(string businessName, string phoneNumber)
        {
            var messageBody = $"A new business {businessName} has been added to your merchant list for Farmer Management Platform." +
                         $"You can always reach out to the business using {phoneNumber} ";

            var sms = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { "09061562512" },
                message = messageBody,
            });
            var sms1 = await _whisperSMSService.HandleAfricasTalkingSMS(new WhisperSMSModel
            {
                campaign_name = "Onboarding",
                contacts = new List<string> { $"+2349061562512" },
                message = messageBody,
            });
            var sms2 = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
            {
                to = $"2349061562512",
                sms = messageBody
            });
        }

        public async Task<BusinessModel> CreateBusiness(BusinessModel business, BusinessRegistrationArgs args) //userId is same as onboarded by{
        {
            if (business.PhoneNumber.Length != 11) { throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number length incorrect"); }
            business.Validate();
            business.BusinessStatus = BusinessStatus.Registered;
            BusinessModel newBusiness = new BusinessModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var bModel = await _businessRepository.GetBusinessAsync(business.BusinessName);
                        if (bModel != null)
                        {
                            throw new BaseException(Core.Enums.ExceptionType.ALREADY_EXIST, $"{business.BusinessName} already exist, Kindly contact administrator");
                        }
                        var name = business.BusinessName.Replace(" ", string.Empty).ToUpper();
                        var businessCount = await _businessRepository.BusinessCount();
                        if (name.Length > 3)
                        {
                            name = $"{name.Substring(0, 3)}";
                        }
                        business.BusinessCode = $"{name}{DateTimeOffset.Now.Year}{businessCount + 1}";

                        var registrationMode = new UserModel
                        {
                            Email = business.BusinessEmail,
                            FirstName = business.BusinessName,
                            Password = args.DefaultPassword,
                            PhoneNumber = business.PhoneNumber,
                            UserType = Enum.GetName(typeof(UserType), UserType.Business),//UserType.Business,
                            CreatedBy = args.UserId,
                            CreatedOn = DateTimeOffset.Now,
                            BusinessCode = business.BusinessCode,

                        };
                        var registerBusiness = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*role name should be business admin role*/);
                        if (!registerBusiness.status)
                        {
                            throw new Exception(registerBusiness.message);
                        }

                        newBusiness = await _businessRepository.CreateBusiness(business, registerBusiness.model.Id);


                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();


                        //send email to business about sucessful onboarding
                        //DynamicBusinessOnboarding
                        var accountOfficer = await _authenticationRepository.GetUserById(args.UserId);

                        var propagationUrl = AppSettingsHelper.Setting("propagation:url");
                        newBusiness.BusinessCodeSignageSalt = HttpUtility.UrlEncode(newBusiness.BusinessCodeSignageSalt);
                        var mailRequest = new WelcomeMailBody
                        {
                            To = business.BusinessEmail,
                            ToName = $"{business.BusinessName}",
                            Subject = $"Welcome OnBoard {business.BusinessName}",
                            AccountOfficer = accountOfficer.model.FirstName,
                            DefaultPassword = args.DefaultPassword,
                            Email = accountOfficer.model.Email,
                            PhoneNumber = accountOfficer.model.PhoneNumber,
                            PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}",
                            logincred = $"login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}&pwd={args.DefaultPassword}"
                        };
                        var mail = SendGridService.DynamicBusinessOnboarding(mailRequest);
                        var sendSms = SendOnboardingSMSM(business.BusinessName, business.PhoneNumber);
                        return newBusiness;
                    }
                    catch (Exception ex)
                    {

                        await dbTransaction.RollbackAsync();

                        throw new Exception(ex.Message);
                    }
                }
            });

            return newBusiness;
        }

        public async Task<BusinessWorkerModel> CreateBusinessWorkerAsync(BusinessWorkerModel bWorker, BusinessRegistrationArgs args)
        {
            if (bWorker.PhoneNumber.Length != 11) { throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number length incorrect"); }

            var user = await _authenticationRepository.GetUserById(args.UserId);
            var business = await _businessRepository.GetBusinessByUserIdAsync(user.model.Id);

            if (business == null) { throw new Exception($"Business does not exist"); }
            BusinessWorkerModel newBusinessWorker = new BusinessWorkerModel();
            //verify user bvn details

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {


                        var registrationMode = new UserModel
                        {
                            Email = bWorker.Email,
                            FirstName = bWorker.FullName,
                            Password = args.DefaultPassword,
                            PhoneNumber = bWorker.PhoneNumber,
                            UserType = Enum.GetName(typeof(UserType), UserType.Business),
                            //UserType.Business,
                            CreatedBy = args.UserId,
                            CreatedOn = DateTimeOffset.Now,
                            BusinessCode = business.BusinessCode,
                            Name = bWorker.FullName,
                            OrganizationCode = bWorker.OrganizationCode

                        };
                        var registerBusinessWorker = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*rolename should be roles from businesses*/);
                        if (!registerBusinessWorker.status)
                        {
                            throw new Exception(registerBusinessWorker.message);
                        }

                        bWorker.BusinessId = business.BusinessId;
                        bWorker.UserId = registerBusinessWorker.model.Id;
                        bWorker.OrganizationCode = bWorker.OrganizationCode;
                        newBusinessWorker = await _businessRepository.CreateBusinessWorker(bWorker);


                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();
                        SendWorkerMail(bWorker, args, business);

                        return newBusinessWorker;
                    }
                    catch (Exception ex)
                    {

                        await dbTransaction.RollbackAsync();

                        throw new Exception(ex.Message);
                    }
                }

            });
            return newBusinessWorker;
        }

        private static void SendWorkerMail(BusinessWorkerModel bWorker, BusinessRegistrationArgs args, BusinessModel business)
        {
            var propagationUrl = AppSettingsHelper.Setting("propagation:url");
            var mailRequest = new WelcomeMailBody
            {
                To = bWorker.Email,
                ToName = $"{bWorker.FullName}",
                Subject = $"Welcome OnBoard {business.BusinessName}",
                From = business.BusinessEmail,
                FromName = business.BusinessName,
                FullName = bWorker.FullName,
                Platform = business.BusinessName,
                Url = business.PropagateUrl,
                DefaultPassword = args.DefaultPassword,
                BusinessName = business.BusinessName,
                PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={business.BusinessCodeSignageSalt}",
                logincred = $"login?bsCd={business.BusinessCode}&signage={business.BusinessCodeSignageSalt}&pwd={args.DefaultPassword}"
            };
            var mail = SendGridService.DynamicBusinessStaffOnboarding(mailRequest);
        }

        private string ConvertedDate(string date)
        {
            string[] dateFormats = {
            "dd-MMM-yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd",
            "MM/dd/yyyy", "MMM dd, yyyy", "yyyy/MM/dd"
        };

            string dateOfBirth = date; // Example input, change to test other formats

            if (DateTime.TryParseExact(dateOfBirth, dateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                string formattedDate = parsedDate.ToString("yyyy-MM-dd");
                return formattedDate;
            }
            else
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Invalid date of birth");
            }
        }
        public async Task<BusinessLoadModel> GetBusinessBySinageSaltAsync(string businessCode, string signagesalt)
        {

            var getbusiness = await _businessRepository.GetBusinessBySinageSaltAsync(signagesalt);
            if (getbusiness == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Invalid Businesscode");
            byte[] salt = Convert.FromBase64String(getbusiness.BusinessCodeSignageSalt);
            string code = businessCode.HashSecret(salt);
            if (getbusiness.BusinessCodeSignage.Equals(code))
            {

                return getbusiness.ViewMap();
            }
            else
            {
                return null;
            }
        }

        public async Task<MemberShipModel> AssignCooperativeToAgent(string agentId, long cooperativeId)
        {
            var assignAgentToCooperative = new MemberShipModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    assignAgentToCooperative = await _cooperativeRepository.AssignCooperativeToAgent(agentId, cooperativeId);

                    var assignAgentToFarmer = await _cooperativeRepository.AssignFarmerToAgent(agentId, cooperativeId);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return assignAgentToCooperative;
        }
        public async Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId)
        {
            return await _cooperativeRepository.ViewAgentAssignedToCooperative(cooperativeId);
        }

        public async Task<UserModel> MakeUserAgent(BusinessRegistrationArgs args)
        {
            var user = new UserModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                  ?? throw new Exception("Business does not exist");

                    args.UserId ??= business.UserId;
                    var cooperative = await _cooperativeRepository.GetMembershipDetailByUserId(args.UserId) ?? throw new Exception("Could not find user");
                    var changeRole = await _authenticationRepository.ChangeUserRole(args.UserId, args.RoleName);
                    changeRole.model.Lga = cooperative.Lga;
                    changeRole.model.State = cooperative.State;
                    changeRole.model.Location = cooperative.Location;
                    changeRole.model.Email = cooperative.Email;
                    changeRole.model.PhoneNumber = cooperative.PhoneNumber;
                    changeRole.model.FirstName = cooperative.Name;
                    changeRole.model.OrganizationCode = args.organizationCode;
                    await CreateAgentProfileAsync(changeRole.model, business, args.UserId);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    user = changeRole.model;
                    return user;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return user;
        }
        public async Task<UserModel> FindByPhoneNumberAsync(string phoneNumber)
        {
            var user = new User();
            var users = await _context.Set<User>().Where(u => u.PhoneNumber == phoneNumber).ToListAsync();
            if (users.Count > 0)
            {
                if (users.Count > 1)
                {
                    user = users.Where(u => u.UserType == UserType.MemberShip.ToString()).FirstOrDefault();
                }
                else
                {
                    user = users[0];
                }
            }
            else
            {
                return null;
            }


            return user.Map();
        }
        public async Task<SubAccountModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length != 11)
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number must be exactly 11 digits.");

            // Fetch the business
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

            args.UserId ??= business.UserId;
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;

            // NIN verification if required
            if (model.shouldVerifyNin)
            {
                var ninVerification = await _identityVerificationManager.VerifyNIN(new NINDataModel { number = model.NIN });
                if (!ninVerification.status)
                {
                    _log.LogError($"NIN verification failed: {ninVerification.detail}");
                    model.shouldVerifyNin = false;
                }
                else
                {
                    model.shouldVerifyNin = true;
                    model.NIN = ninVerification.nin_data.nin;
                    model.FirstName = ninVerification.nin_data.firstname;
                    model.LastName = ninVerification.nin_data.surname;
                    model.MiddleName = ninVerification.nin_data.middlename;
                    _log.LogInformation($"NIN verification succeeded for NIN: {model.NIN}");
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                    bool isNewUser = false;

                    if (existingUser != null)
                    {
                        // Only handle organization linkage if organizationCode is provided
                        if (!string.IsNullOrEmpty(args.organizationCode))
                        {
                            bool alreadyInOrg = await _context.UserOrganizations
                                .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode);

                            if (alreadyInOrg)
                                throw new BaseException(Core.Enums.ExceptionType.CONFLICT,
                                    $"A user with phone number {model.PhoneNumber} already exists in this organization.");

                            if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                            {
                                _context.UserOrganizations.Add(new UserOrganization
                                {
                                    UserId = existingUser.Id,
                                    OrganizationCode = args.organizationCode
                                });
                                _log.LogInformation($"User {model.PhoneNumber} added to organization {args.organizationCode}.");
                                await NotifyUserOfOrganizationAddition(existingUser.PhoneNumber, args.organizationCode);
                            }
                        }
                        else
                        {
                            _log.LogInformation($"User {model.PhoneNumber} is a direct business user (no organization).");
                        }
                    }
                    else
                    {
                        // Generate full name if empty
                        if (string.IsNullOrEmpty(model.Name))
                            model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";

                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST,
                                $"User registration failed: {registrationResult.message}");

                        existingUser = registrationResult.model;
                        isNewUser = true;

                        // Add to organization if applicable
                        if (!string.IsNullOrEmpty(args.organizationCode) &&
                            !_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                            _log.LogInformation($"New user {model.PhoneNumber} added to organization {args.organizationCode}.");
                        }
                    }

                    // Ensure subaccount exists
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(existingUser.Id);
                    SubAccountModel subAccount;
                    model.Email ??= GenerateEmail(model.FirstName, business.BusinessEmail);

                    subAccount = existingSubAccount ?? await CreateSubAccountAsync(model, args, business, existingUser.Id);

                    // Track role assignments
                    await _authenticationRepository.TrackUserRoleAssignment(existingUser, model.UserType);

                    // Create business worker
                    await CreateUserCategory(
                        model,
                        args,
                        new BusinessWorkerModel(),
                        new BusinessWorkerModel(),
                        business,
                        subAccount,
                        (existingUser, !isNewUser, ""),
                        args.UserType
                    );

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Populate subaccount response
                    subAccount.UserId = existingUser.Id;
                    subAccount.Address = model.Location;
                    subAccount.City = model.Lga;
                    subAccount.FirstName = model.FirstName;
                    subAccount.LastName = model.LastName;
                    subAccount.MiddleName = model.MiddleName;
                    subAccount.Alias = !string.IsNullOrEmpty(model.Alias) ? model.Alias : "Mr";

                    return subAccount;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error occurred during CreateBusinessUser");
                    await transaction.RollbackAsync();
                    throw new Exception("Unable to register user at this time. Please try again later.");
                }
            });
        }

        private async Task NotifyUserOfOrganizationAddition(string phoneNumber, string orgCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11)
                {
                    _log.LogWarning("Invalid phone number for SMS notification: {PhoneNumber}", phoneNumber);
                    return;
                }

                string message = $"You've been added as on the Agrovesto platform to a new organization. Login to access your dashboard.";

                var termiiResult = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
                {
                    to = $"234{phoneNumber[^10..]}",
                    sms = message
                });

                if (termiiResult.response != null && termiiResult.status)
                {
                    _log.LogInformation("Role notification SMS successfully sent to {PhoneNumber}", phoneNumber);
                }
                else
                {
                    _log.LogWarning("Termii SMS failed for {PhoneNumber}. Attempting WhisperSMS fallback...", phoneNumber);

                    var whisperResult = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
                    {
                        campaign_name = "Agrovesto Notification",
                        contacts = new List<string> { phoneNumber },
                        message = message
                    });

                    if (whisperResult.response is not null && whisperResult.status)
                    {
                        _log.LogInformation("Fallback WhisperSMS sent to {PhoneNumber}", phoneNumber);
                    }
                    else
                    {
                        _log.LogError("All SMS attempts failed for {PhoneNumber}", phoneNumber);
                    }
                }

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to send role assignment SMS to {PhoneNumber}", phoneNumber);
            }
        }

         private async Task<BusinessWorkerModel> CreateUserCategory(UserModel model, BusinessRegistrationArgs args, BusinessWorkerModel bWorker, BusinessWorkerModel newBusinessWorker, BusinessModel business, SubAccountModel subaccount, (UserModel model, bool status, string message) registeredUser, string userType)
        {
            if (userType != null)
            {
                subaccount.SubAccountType = userType.ToEnum<SubAccountType>();
            }
            switch (subaccount.SubAccountType)
            {
                case SubAccountType.Trader:
                    //model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                    await CreateTraderProfile(model, business, registeredUser.model.Id);
                    break;
                case SubAccountType.Agent:
                    model.OrganizationCode = args.organizationCode;
                    //model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                    await CreateAgentProfileAsync(model, business, registeredUser.model.Id);

                    bWorker.BusinessId = business.BusinessId;
                    bWorker.UserId = registeredUser.model.Id;
                    bWorker.CreatedOn = DateTimeOffset.Now;
                    bWorker.Email = model.Email;
                    bWorker.FullName = model.FirstName;
                    bWorker.PhoneNumber = model.PhoneNumber;
                    bWorker.OrganizationCode = args.organizationCode;

                    newBusinessWorker = await _businessRepository.CreateBusinessWorker(bWorker);

                    SendWorkerMail(bWorker, args, business);
                    break;

                case SubAccountType.Cooperative:
                    model.Id = registeredUser.model.Id;
                    model.OrganizationCode = args.organizationCode;
                    //create first as cooperative
                    var coop = await CreateCooperativeProfileAsync(model, business, args.UserId);
                    args.UserId = registeredUser.model.Id;
                    //then create user as a farmer as well
                    model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                    //  model.FarmerCategory = 
                    await CreateFarmerProfileAsync(
    model,
    args,
    business,
    registeredUser.model.Id,
    subaccount,
    cooperativeIds: new List<long> { coop.model.Id } // wrap single cooperative ID
);

                    //await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, coop.model.Id);

                    break;

                case SubAccountType.Farmer:
                    //  model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                    model.Id = registeredUser.model.Id;
                    model.OrganizationCode = args.organizationCode;
                    await CreateFarmerProfileAsync(
    model,
    args,
    business,
    registeredUser.model.Id,
    subaccount,
    cooperativeIds: new List<long> {args.CooperativeId } // wrap single cooperative ID
);

                    //   await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, args.CooperativeId);
                    break;
            }

            return newBusinessWorker;
        }

        public async Task<SubAccountModel> UpdateUserAccountProfile(SubAccountModel subaccount, BusinessRegistrationArgs args, string userId)
        {
            return await _walletManager.UpdateUserSterlingWalletProfile(subaccount, new ParameterArgs
            {
                UserId = subaccount.UserId,
                Env = args.Env
            });
        }

        private async Task<(UserModel model, bool status, string message)> RegisterUserAsync(UserModel model, BusinessRegistrationArgs args)
        {

            var user = new UserModel
            {
                Email = model.Email,
                FirstName = model.FirstName,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                UserType = args.UserType,//Enum.GetName(typeof(UserType), args.UserType),//args.UserType.ToEnum<UserType>(),
                CreatedBy = args.UserId,
                CreatedOn = DateTimeOffset.UtcNow,
                Pin = model.Pin,
                Name = model.Name,
                NIN = model.NIN,
                Bvn = model.Bvn,
                shouldVerifyNin = model.shouldVerifyNin,

                OrganizationCode = args.organizationCode,
                BusinessCode = args.businessCode,
            };
            //send mail

            var result = await _authenticationRepository.RegisterBusinessUser(user, args.RoleName);
            return result;
        }


        private async Task<SubAccountModel> CreateSubAccountAsync(UserModel model, BusinessRegistrationArgs args, BusinessModel business, string userId)
        {
            var subaccount = new SubAccountModel
            {
                BusinessId = business.BusinessId,
                UserId = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                SubAccountType = args.SubAccountType.ToEnum<SubAccountType>(),
                PhoneNumber = model.PhoneNumber,
                DOB = model.DateOfBirth,
                Bvn = model.Bvn,
                NIN = model.NIN,
                BankAccount = model.BankAccount,
                BankName = model.BankName,
                CreatedBy = args.UserId,
                OnBoardedOn = DateTimeOffset.UtcNow,
                IsMainAccount = false,
                OrganizationCode = args.organizationCode,
                Status = BusinessStatus.Registered.ToString(),
                IsNinVerified = model.shouldVerifyNin
            };

            return await _walletManager.CreateSubAccount(subaccount);
        }

        private async Task CreateTraderProfile(UserModel model, BusinessModel business, string userId)
        {
            var trader = new TraderModel
            {
                BelongToBusinessCode = business.BusinessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                Name = model.Name,
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,

            };

            await _traderRepository.CreateTrader(trader);
        }

        private async Task CreateAgentProfileAsync(UserModel model, BusinessModel business, string userId)
        {
            var agent = new AgentModel
            {
                BelongToBusinessCode = business.BusinessCode,
                State = model.State,
                Lga = model.Lga,
                Email = model.Email,
                Location = model.Location,
                Name = model.Name,
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = userId,
                Ward = model.Ward,

                OrganizationCode = model.OrganizationCode
            };

            await _agentRepository.CreateAgent(agent);
        }

        private async Task<(bool status, MemberShipModel model)> CreateCooperativeProfileAsync(UserModel model, BusinessModel business, string userId)
        {
            var getUserType = await _authenticationRepository.GetUserById(userId);
            var isAssignedagent = false;
            var agent = new AgentModel();
            if (getUserType.model.UserType == Enum.GetName(typeof(UserType), UserType.Agent))
            {
                isAssignedagent = true;
                // agent = await _agentRepository.GetAgentDetailByUserId(userId);

            }
            var cooperative = new MemberShipModel
            {
                BelongToBusinessCode = business.BusinessCode,
                State = model.State,
                Lga = model.Lga,
                ContactPerson = model.FirstName,//string.Empty,
                Email = model.Email,
                Location = model.Location,
                Name = model.Name,
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                IsAssignedToAgentId = userId,
                HasbeenAssignedAgent = isAssignedagent,
                Ward = model.Ward,
                OrganizationCode = model.OrganizationCode
            };

            return await _cooperativeRepository.CreateCooperative(cooperative);
        }


        //private async Task CreateFarmerProfileAsync(UserModel model, BusinessRegistrationArgs args, BusinessModel business, string userId, SubAccountModel subaccount,
        //    long cooperativeId)
        //{
        //    if (string.IsNullOrEmpty(model.Email))
        //    {
        //        model.Email = GenerateEmail(model.FirstName, business.BusinessEmail);
        //    }
        //    var farmer = new FarmerModel
        //    {
        //        OnboardedByUserId = args.UserId,
        //        BelongToBusinessCode = business.BusinessCode,
        //        Email = model.Email,
        //        Gender = model.Gender,
        //        State = model.State,
        //        Lga = model.Lga,
        //        Location = model.Location,
        //        Name = !string.IsNullOrEmpty(model.Name) ? model.Name : $"{model.FirstName} {model.MiddleName} {model.LastName}",
        //        OnBoardedOn = DateTimeOffset.UtcNow,
        //        PhoneNumber = model.PhoneNumber,
        //        UserId = model.Id,
        //        DateOfBirth = model.DateOfBirth,
        //       // BelongToCooperativeId = cooperativeId,
        //        Ward = model.Ward,
        //        OrganizationCode = args.organizationCode,
        //        FarmerCategory = model.FarmerCategory,
        //        SelectedItems = model.ValueChainDto,
        //        YearsInAgric = model.YearsInAgric,
        //        HighestLevelOfEducation = model.HighestLevelOfEducation,
        //        // ValueChainItems= model.ValueChainSelections
        //    };

        //    var farer = await _farmerRepository.CreateFarmer(farmer);


        //}
        private async Task CreateFarmerProfileAsync(
    UserModel model,
    BusinessRegistrationArgs args,
    BusinessModel business,
    string userId,
    SubAccountModel subaccount,
    List<long> cooperativeIds = null,
    List<long> agentIds = null)
        {
            // Generate email if missing
            if (string.IsNullOrEmpty(model.Email))
            {
                model.Email = GenerateEmail(model.FirstName, business.BusinessEmail);
            }

            var farmer = new FarmerModel
            {
                OnboardedByUserId = args.UserId,
                BelongToBusinessCode = business.BusinessCode,
                Email = model.Email,
                Gender = model.Gender,
                State = model.State,
                Lga = model.Lga,
                Location = model.Location,
                Name = !string.IsNullOrEmpty(model.Name)
                    ? model.Name
                    : $"{model.FirstName} {model.MiddleName} {model.LastName}",
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                DateOfBirth = model.DateOfBirth,
                Ward = model.Ward,
                OrganizationCode = args.organizationCode,
                FarmerCategory = model.FarmerCategory,
                SelectedItems = model.ValueChainDto,
                YearsInAgric = model.YearsInAgric,
                HighestLevelOfEducation = model.HighestLevelOfEducation,
                CooperativeIds = cooperativeIds ?? new List<long>(), // assign multiple cooperatives
                AgentIds = agentIds ?? new List<long>(),             // assign multiple agents
            };

            // Create the farmer and handle linking to organizations, cooperatives, agents
            var (status, createdFarmer) = await _farmerRepository.CreateFarmer(farmer);

           
        }

        private static string GenerateEmail(string fullName, string domain)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException("Invalid input provided.");
            }
            domain = ExtractDomain(domain);
            fullName = Regex.Replace(fullName.Trim(), @"[^a-zA-Z\s]", "");

            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                throw new ArgumentException("Name must contain at least one word.");
            }

            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "user";

            firstName = firstName.ToLower();
            lastName = lastName.ToLower();

            return $"{firstName}.{lastName}@{domain}";
        }

        private static string ExtractDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return string.Empty;
            }
            string[] parts = email.Split('@');
            return parts.Length == 2 ? parts[1] : string.Empty;
        }
        public string GetYearOfBirth(int age)
        {
            int currentYear = DateTime.Now.Year;
            int yearOfBirth = currentYear - age;
            return yearOfBirth.ToString();
        }

        public async Task<PagedListResponse<List<AgentModel>>> GetAgentsByBusinessCode(string userId, ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);
                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

                var agents = await _agentRepository.GetAgentsByBusinessCode(business.BusinessCode, query);


                return new PagedListResponse<List<AgentModel>>
                {
                    Succeeded = true,
                    Data = agents,
                    Count = agents?.Count ?? 0,
                    Message = agents == null || agents.Count == 0 ? "No Record Found" : "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError($"Unexpected error retrieving Agents: {ex.Message}");
                throw new NotFoundException("No Record Found");
            }
        }

        public async Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffByBusinessCode(string businessCode, string organizationCode)
        {
            try
            {
                //  query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var staff = await _businessRepository.GetStaffByBusinessCode(business.BusinessId, organizationCode);
                if (staff == null) return new PagedListResponse<List<BusinessWorkerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<BusinessWorkerModel>>
                {
                    Succeeded = true,
                    Data = staff,
                    Count = staff.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        //
        public async Task<Response<MemberShipModel>> GetCooperativeDetailById(long Id)
        {
            try
            {
                var cooperativeDetail = await _cooperativeRepository.GetCooperativeDetailById(Id);
                if (cooperativeDetail == null) return new Response<MemberShipModel>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<MemberShipModel>
                {
                    Succeeded = true,
                    Data = cooperativeDetail,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<MemberShipModel>>> GetCooperativesByBusinessCode(string userId,
          ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }
                var agent = await _cooperativeRepository.GetCooperativesByBusinessCode(business.BusinessCode, query);
                if (agent == null) return new PagedListResponse<List<MemberShipModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<MemberShipModel>>
                {
                    Succeeded = true,
                    Data = agent,
                    Count = agent.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByBusinessCode(string userId,
     ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);

                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }
                var farmers = await _farmerRepository.GetFarmersByBusinessCode(business.BusinessCode, query);

                if (farmers == null || !farmers.Any())
                {
                    return new PagedListResponse<List<FarmerModel>>
                    {
                        Succeeded = true,
                        Data = null,
                        Count = 0,
                        Message = "No Record Found"
                    };
                }

                // Fetch actual image URLs from Amazon S3 using the stored image keys
                foreach (var farmer in farmers)
                {
                    if (!string.IsNullOrEmpty(farmer.FarmerImage))
                    {
                        farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                    }
                }

                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = farmers,
                    Count = farmers.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Farmers: {ex.Message}");
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        //public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByBusinessCode(string userId,
        //        ListQueryParams query)
        //{
        //    try
        //    {
        //        query.PageNumber = (query.PageNumber - 1) * query.PageSize;
        //        var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

        //        if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

        //        var farmers = await _farmerRepository.GetFarmersByBusinessCode(business.BusinessCode, query);
        //        if (farmers == null) return new PagedListResponse<List<FarmerModel>>
        //        {
        //            Succeeded = true,
        //            Data = null,
        //            Count = 0,
        //            Message = "No Record Found"
        //        };
        //        return new PagedListResponse<List<FarmerModel>>
        //        {
        //            Succeeded = true,
        //            Data = farmers,
        //            Count = farmers.Count,
        //            Message = "Retrieved Successfully"
        //        };
        //    }
        //    catch (BaseException ex)
        //    {
        //        _log.LogError($"Error retrieving Agents: {ex.Message}");

        //        throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new NotFoundException($"No Record Found");
        //    }
        //}
        public async Task<MemberShipModel> GetCooperativeDetailByUserId(string userId)
        {

            return await _cooperativeRepository.GetMembershipDetailByUserId(userId);
        }
        public async Task<AgentModel> GetAgentDetailByUserId(string userId)
        {

            return await _agentRepository.GetAgentDetailByUserId(userId);
        }


        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeUserId(string userId,
                ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var cooperative = await _cooperativeRepository.GetMembershipDetailByUserId(userId);

                if (cooperative == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var agent = await _farmerRepository.GetFarmersByOnboardedUserId(cooperative.UserId, query);
                if (agent == null) return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = agent,
                    Count = agent.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffbyBusiness(string userId,
              ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var staff = await _businessRepository.GetStaffByBusinessCode(business.BusinessId, query.organizationCode);
                if (staff == null) return new PagedListResponse<List<BusinessWorkerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<BusinessWorkerModel>>
                {
                    Succeeded = true,
                    Data = staff,
                    Count = staff.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<PagedListResponse<List<MemberShipModel>>> GetCooperativesByAgent(string userId,
       ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var cooperative = await _cooperativeRepository.GetCooperativeByAgent(userId, query);
                if (cooperative == null) return new PagedListResponse<List<MemberShipModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<MemberShipModel>>
                {
                    Succeeded = true,
                    Data = cooperative,
                    Count = cooperative.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<PagedListResponse<List<FarmerAgentDto>>> GetFarmersByAgent(string userId,

      ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var farmers = await _farmerRepository.GetFarmersByAgent(userId, query);
                if (farmers == null) return new PagedListResponse<List<FarmerAgentDto>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };

                //foreach (var farmer in farmers)
                //{
                //    if (!string.IsNullOrEmpty(farmer.FarmerImage))
                //    {
                //        farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                //    }
                //}

                return new PagedListResponse<List<FarmerAgentDto>>
                {
                    Succeeded = true,
                    Data = farmers.Data,
                    Count = farmers.Data.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeId(string userId,

      ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;

                var farmers = await _farmerRepository.GetFarmersByCooperativeId(query.CooperativeId, query);
                if (farmers == null) return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                foreach (var farmer in farmers)
                {
                    if (!string.IsNullOrEmpty(farmer.FarmerImage))
                    {
                        farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                    }
                }

                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = farmers,
                    Count = farmers.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<Response<BusinessEntitiesMetrics>> GetBusinessEntitiesMetricsAsync(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }
                var bizEntities = await _farmerRepository.GetBusinessEntitiesMetricsAsync(business.BusinessCode, query.organizationCode);
                if (bizEntities == null) return new Response<BusinessEntitiesMetrics>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<BusinessEntitiesMetrics>
                {
                    Succeeded = true,
                    Data = bizEntities,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<Response<BusinessEntitiesMetrics>> GetBusinessEntitiesMetricsAsyncByAgent(ListQueryParams query)
        {
            try
            {
                var agent = await _agentRepository.GetAgentDetailByUserId(query.UserId);
                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var plots = await _farmPlotDetailRepository.GetVisitationLogsByOfficerInCharge(query.businessCode, query.UserId, query);

                var bizEntities = await _farmerRepository.GetBusinessEntitiesMetricsAsyncByAgent(query.UserId, query);
                bizEntities.FarmVisit = plots != null ? plots.FirstOrDefault() : null;


                if (bizEntities == null) return new Response<BusinessEntitiesMetrics>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<BusinessEntitiesMetrics>
                {
                    Succeeded = true,
                    Data = bizEntities,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<ImageUploadResponse> SaveFarmerImage(ImageModel model, BusinessRegistrationArgs args)
        {
            var saveFace = new ImageUploadResponse();
            //var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
            //    ?? throw new Exception("Business does not exist");


            //var strategy = _context.Database.CreateExecutionStrategy();

            //await strategy.ExecuteAsync(async () =>
            //{
            //    await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                saveFace = await _faceManager.UploadImage(model.File, model.FarmerUserId, args.businessCode, args.organizationCode);

                return saveFace;
            }
            catch (Exception ex)
            {
                //await dbTransaction.RollbackAsync();
                throw new Exception($"{ex.Message}", ex);
            }
            // });
            //return saveFace;
        }


        public async Task<FarmerModel> AssignFarmerToCooperative(long farmerId, long cooperativeId)
        {
            return await _farmerRepository.AssignFarmerToCooperative(farmerId, cooperativeId);
        }


        public async Task<Response<FarmerModel>> GetFarmerDetailWithImage(
ListQueryParams query, string imageKey)
        {
            try
            {

                var business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"Access Forbidden");

                var farmer = await _farmerRepository.GetFarmerByUserId(query.UserId);

                if (farmer == null)
                {
                    return new Response<FarmerModel>
                    {
                        Succeeded = true,
                        Data = null,
                        Message = "No Record Found"
                    };
                }

                // Fetch actual image URLs from Amazon S3 using the stored image keys

                if (!string.IsNullOrEmpty(farmer.FarmerImage) && farmer.FarmerImage == imageKey)
                {
                    farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                }


                return new Response<FarmerModel>
                {
                    Succeeded = true,
                    Data = farmer,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Farmer: {ex.Message}");
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        //organization
        public async Task<OrganizationModel> CreateOrganization(
    OrganizationModel org,
    BusinessRegistrationArgs args)
        {
            // validate business
            var parentBusiness = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
            if (parentBusiness == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND,
                    "Parent business not found or has been deactivated, kindly contact platform administrator");
            }

            // check if org already exists
            var existingOrg = await _businessRepository.GetOrganizationAsync(org.OrganizationEmail);
            if (existingOrg != null)
            {
                throw new BaseException(Core.Enums.ExceptionType.ALREADY_EXIST,
                    $"{org.OrganizationName} already exist, Kindly contact administrator");
            }

            OrganizationModel newOrg;

            // EF Core execution strategy for transient errors
            var strategy = _context.Database.CreateExecutionStrategy();

            newOrg = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // generate org code
                    var name = org.OrganizationName.Replace(" ", string.Empty).ToUpper();
                    var businessCount = await _businessRepository.OrganizationCount();
                    if (name.Length > 3)
                    {
                        name = name.Substring(0, 3);
                    }

                    org.BusinessCode = args.businessCode;
                    org.OrganizationCode = $"{name}{DateTimeOffset.Now.Month}{businessCount + 1}";

                    // stage org in context (do not SaveChanges inside repo)
                    newOrg = await _businessRepository.CreateOrganization(org);

                    // prepare user model
                    var registrationMode = new UserModel
                    {
                        Email = org.OrganizationEmail,
                        FirstName = org.OrganizationName,
                        Password = org.Password,
                        PhoneNumber = org.Contact,
                        UserType = Enum.GetName(typeof(UserType), UserType.Organization),
                        CreatedBy = !string.IsNullOrEmpty(args.UserId) ? args.UserId : "Self-Registration",
                        CreatedOn = DateTimeOffset.Now,
                        BusinessCode = args.businessCode,
                        OrganizationCode = org.OrganizationCode,
                        Name = org.OrganizationName
                    };

                    // stage user in context
                    var registerBusinessUser =
                        await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName);
                    if (!registerBusinessUser.status)
                    {
                        throw new Exception(registerBusinessUser.message);
                    }

                    // commit DB work
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return newOrg;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            // 🔑 non-DB work after commit
            var accountOfficer = await _authenticationRepository.GetUserById(args.UserId);

            var propagationUrl = AppSettingsHelper.Setting("propagation:url");
            var mailRequest = new WelcomeMailBody
            {
                To = org.ContactEmail,
                ToName = org.OrganizationName,
                Subject = $"Welcome OnBoard {org.OrganizationName}",
                AccountOfficer = accountOfficer.model.FirstName,
                DefaultPassword = args.DefaultPassword,
                Email = accountOfficer.model.Email,
                PhoneNumber = accountOfficer.model.PhoneNumber,
                logincred = $"login?bsCd={org.BusinessCode}&org={org.OrganizationCode}&pwd={args.DefaultPassword}"
            };

            // send notifications
            var mail = SendGridService.DynamicBusinessOnboarding(mailRequest);
            var sendSms = SendOnboardingSMSM(org.OrganizationName, org.Contact);

            return newOrg;
        }

        //public async Task<OrganizationModel> CreateOrganization(OrganizationModel org, BusinessRegistrationArgs args) //userId is same as onboarded by{
        //{   
        //    OrganizationModel newOrg = new OrganizationModel();
        //    var parentBusiness = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
        //    if (parentBusiness == null)
        //    {
        //        throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"Parent business not found or has been deactivated, kindly contact platform administrator");
        //    }
        //    var bModel = await _businessRepository.GetOrganizationAsync(org.OrganizationCode);
        //    if (bModel != null)
        //    {
        //        throw new BaseException(Core.Enums.ExceptionType.ALREADY_EXIST, $"{org.OrganizationName} already exist, Kindly contact administrator");
        //    }
        //    var strategy = _context.Database.CreateExecutionStrategy();

        //    await strategy.ExecuteAsync(async () =>
        //    {
        //        using (var dbTransaction = _context.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                var name = org.OrganizationName.Replace(" ", string.Empty).ToUpper();
        //                var businessCount = await _businessRepository.OrganizationCount();
        //                if (name.Length > 3)
        //                {
        //                    name = $"{name.Substring(0, 3)}";
        //                }
        //                org.BusinessCode = args.businessCode;
        //                org.OrganizationCode= $"{name}{DateTimeOffset.Now.Month}{businessCount + 1}";

        //                //register organization
        //                newOrg = await _businessRepository.CreateOrganization(org);

        //                var registrationMode = new UserModel
        //                {
        //                    Email = org.OrganizationEmail,
        //                    FirstName = org.OrganizationName,
        //                    Password = org.Password,
        //                    PhoneNumber = org.Contact,
        //                    UserType = Enum.GetName(typeof(UserType), UserType.Organization),//UserType.Business,
        //                    CreatedBy = !string.IsNullOrEmpty(args.UserId) ? args.UserId : "Self-Registration",
        //                    CreatedOn = DateTimeOffset.Now,
        //                    BusinessCode = args.businessCode,
        //                    OrganizationCode= org.OrganizationCode,
        //                    Name= org.OrganizationName

        //                };
        //                var registerBusinessUser = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*role name should be business admin role*/);
        //                if (!registerBusinessUser.status)
        //                {
        //                    throw new Exception(registerBusinessUser.message);
        //                }




        //                await _context.SaveChangesAsync();
        //                await dbTransaction.CommitAsync();


        //                //send email to business about sucessful onboarding

        //                var accountOfficer = await _authenticationRepository.GetUserById(args.UserId);

        //                var propagationUrl = AppSettingsHelper.Setting("propagation:url");
        //                var mailRequest = new WelcomeMailBody
        //                {
        //                    To = org.ContactEmail,
        //                    ToName = $"{org.OrganizationName}",
        //                    Subject = $"Welcome OnBoard {org.OrganizationName}",
        //                    AccountOfficer = accountOfficer.model.FirstName,
        //                    DefaultPassword = args.DefaultPassword,
        //                    Email = accountOfficer.model.Email,
        //                    PhoneNumber = accountOfficer.model.PhoneNumber,
        //                    //PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}",
        //                    logincred = $"login?bsCd={org.BusinessCode}&org={org.OrganizationCode}&pwd={args.DefaultPassword}"
        //                };
        //                var mail = SendGridService.DynamicBusinessOnboarding(mailRequest);
        //                var sendSms = SendOnboardingSMSM(org.OrganizationName, org.Contact);
        //                return newOrg;
        //            }
        //            catch (Exception ex)
        //            {

        //                await dbTransaction.RollbackAsync();
        //                _log.LogInformation("Error thrown at organization registration with ", ex.Message);
        //                throw;
        //            }
        //        }
        //    });

        //    return newOrg;
        //}

        public async Task<FarmerDetailDto> GetFarmerDetails(

    ListQueryParams query)
        {
            try
            {

                var farmers = await _farmerRepository.GetFarmerDetails(query.UserId);
                if (farmers == null) return null;

                if (farmers.FarmerImage != null && !string.IsNullOrEmpty(farmers.FarmerImage.ImageKey))
                {
                    farmers.FarmerImage.ImageKey = await _faceManager.GetImageUrlAsync(farmers.FarmerImage.ImageKey);
                }


                return farmers;
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<FileDownloadModel> GetAllFarmerDetails(

    ListQueryParams query)
        {
            try
            {
                var model = new FileDownloadModel();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var cooperativeIds = query.CooperativeId.HasValue
    ? new List<long> { query.CooperativeId.Value }
    : null;
                var AgentId = query.AgentId.HasValue
 ? new List<long> { query.AgentId.Value }
 : null;
                var farmers = await _farmerRepository.GetAllFarmerDetails(
                    query.businessCode,
                    query.organizationCode,
                    cooperativeIds,   // <-- pass list
                   AgentId,
                    query.Location,
                    query.sortBy
                );

                // var farmers = await _farmerRepository.GetAllFarmerDetails(query.businessCode, query.organizationCode, query.CooperativeId, query.UserId, query.Location, query.sortBy);
                if (farmers.Data.Count == 0) return null;

                foreach (var farmer in farmers.Data)
                {

                    if (farmer.FarmerImage != null && !string.IsNullOrEmpty(farmer.FarmerImage.ImageKey))
                    {
                        farmer.FarmerImage.ImageKey = await _faceManager.GetImageUrlAsync(farmer.FarmerImage.ImageKey);
                    }
                }
                var excelBytes = await _excelConverterService.GenerateFarmerExcel(farmers.Data);
                model.FileBytes = excelBytes;
                model.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                model.FileName = $"farmer_{timestamp}.xlsx";
                return model;
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode)
        {
            return await _businessRepository.GetBusinessOrganizationAsync(businessCode);

        }
    }
    public class UserOrganizationComparer : IEqualityComparer<UserOrganization>
    {
        public bool Equals(UserOrganization x, UserOrganization y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

            return x.UserId == y.UserId && x.OrganizationCode == y.OrganizationCode;
        }

        public int GetHashCode(UserOrganization obj)
        {
            return HashCode.Combine(obj.UserId, obj.OrganizationCode);
        }
    }
}
