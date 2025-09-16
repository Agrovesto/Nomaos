using Agrovesto.Backend.Core.CoreUtils;
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
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
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
        private readonly ICooperativeRepository _cooperativeRepository;
        private readonly ILogger<BusinessManager> _log;
        private readonly IIdentityVerificationManager _identityVerificationManager;
        private readonly IFarmPlotDetailRepository _farmPlotDetailRepository;
        private readonly IFaceManager _faceManager;
        private readonly ISMSService _whisperSMSService;
        private readonly IInputFinancingManagementRepository _inputRepository;
        private readonly ITraderRepository _traderRepository;
        private readonly IExcelConverterService _excelConverterService;

        public BusinessManager(IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletManager walletManager, IFarmerRepository farmerRepository,
            IIdentityVerificationManager identityVerificationManager,
            IFarmPlotDetailRepository farmPlotDetailRepository,
            IAgentRepository agentRepository, ISMSService whisperSMSService,
            IInputFinancingManagementRepository inputRepository,
            ITraderRepository traderRepository, IExcelConverterService excelConverterService,

                IFaceManager faceManager, ILogger<BusinessManager> log, ICooperativeRepository cooperativeRepository)
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
                contacts = new List<string> { "09061562512"},
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
                            OrganizationCode=bWorker.OrganizationCode
                        
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
                Url= business.PropagateUrl,
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

        public async Task<CooperativeModel> AssignCooperativeToAgent(string agentId, long cooperativeId)
        {
            var assignAgentToCooperative = new CooperativeModel();
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
                    var cooperative = await _cooperativeRepository.GetCooperativeDetailByUserId(args.UserId) ?? throw new Exception("Could not find user");
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
                    user = users.Where(u => u.UserType == UserType.Cooperative.ToString()).FirstOrDefault();
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

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

            args.UserId ??= business.UserId;
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                   // UserModel finalUser = existingUser;
                    bool isNewUser = false;

                    if (existingUser != null)
                    {
                        // Check if user already linked to organization
                        bool alreadyInOrg = await _context.UserOrganizations
                            .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode);

                        if (alreadyInOrg)
                        {
                            throw new BaseException(Core.Enums.ExceptionType.CONFLICT, $"A user with phone number {model.PhoneNumber} already exists in this organization.");
                        }

                        // Add user to new organization
                        if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                        }

                        _log.LogInformation($"User with phone {model.PhoneNumber} added to new organization {args.organizationCode}.");
                        await NotifyUserOfOrganizationAddition(existingUser.PhoneNumber, args.organizationCode);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(model.Name))
                        {
                            model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                        }

                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, $"User registration failed: {registrationResult.message}");

                        existingUser = registrationResult.model;
                        isNewUser = true;

                        // Add user to organization
                        if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                        {
                            _context.UserOrganizations.Add(new UserOrganization
                            {
                                UserId = existingUser.Id,
                                OrganizationCode = args.organizationCode
                            });
                        }
                    }

                    // Check for existing subaccount
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(existingUser.Id);
                    SubAccountModel subAccount;
                    model.Email = !string.IsNullOrWhiteSpace(model.Email)
                        ? model.Email
                        : GenerateEmail(model.FirstName, business.BusinessEmail);

                    if (existingSubAccount != null)
                    {
                        subAccount = existingSubAccount;
                    }
                    else
                    {
                        subAccount = await CreateSubAccountAsync(model, args, business, existingUser.Id);
                    }

                    // Track role assignments (multi-role support)
                    await _authenticationRepository.TrackUserRoleAssignment(existingUser, model.UserType);

                    // Create business worker
                    var newBusinessWorker = await CreateUserCategory(model, args, new BusinessWorkerModel(), new BusinessWorkerModel(), business, subAccount, (existingUser, !isNewUser, ""), args.UserType);

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
                    throw;
                }
            });
        }

        /**public async Task<SubAccountModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length != 11)
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Phone number must be exactly 11 digits.");

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                            ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

            args.UserId ??= business.UserId;
            model.DateOfBirth = ConvertedDate(model.DateOfBirth);

            
            model.Lga = !string.IsNullOrEmpty(model.Lga) ? model.Lga : model.Location;
            model.OrganizationCode = args.organizationCode;
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingUser = await FindByPhoneNumberAsync(model.PhoneNumber);
                    UserModel finalUser = existingUser;
                    bool isNewUser = false;
                    bool reusedUserForDifferentRole = false;

                    if (existingUser != null)
                    {
                        if (existingUser.OrganizationCode == model.OrganizationCode && existingUser.UserType.Equals(model.UserType, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new BaseException(Core.Enums.ExceptionType.CONFLICT, $"A user with phone number {model.PhoneNumber} and user type '{model.UserType}' already exists.");
                        }

                        reusedUserForDifferentRole = true;

                        // Log reuse
                        _log.LogInformation($"User with phone {model.PhoneNumber} exists with a different role ('{existingUser.UserType}'), reusing user for role '{model.UserType}'.");

                        // Optional: send notification
                        await NotifyUserOfRoleReuse(existingUser.PhoneNumber, model.UserType);

                    }
                    else
                    {
                        if (string.IsNullOrEmpty(model.Name))
                        {

                            model.Name = model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                        }
                        var registrationResult = await RegisterUserAsync(model, args);
                        if (!registrationResult.status)
                            throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, $"User registration failed: {registrationResult.message}");

                        finalUser = registrationResult.model;
                        isNewUser = true;
                    }

                    // Check for existing subaccount
                    var existingSubAccount = await _walletManager.GetSubAccountByUserId(finalUser.Id);
                    SubAccountModel subAccount;
                    model.Email = !string.IsNullOrWhiteSpace(model.Email)
            ? model.Email
            : GenerateEmail(model.FirstName, business.BusinessEmail);
                    if (existingSubAccount != null)
                    {
                        subAccount = existingSubAccount;
                    }
                    else
                    {

                        subAccount = await CreateSubAccountAsync(model, args, business, finalUser.Id);
                    }

                    // Track role assignments (multi-role support)
                    await _authenticationRepository.TrackUserRoleAssignment(finalUser, model.UserType);

                    // Create business worker
                    var newBusinessWorker = await CreateUserCategory(model, args, new BusinessWorkerModel(), new BusinessWorkerModel(), business, subAccount, (finalUser, !isNewUser, ""), args.UserType);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();


                    subAccount.UserId = finalUser.Id;
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
                    _log.LogInformation("error occurred", ex.Message);
                    await transaction.RollbackAsync();
                    throw new Exception(ex.Message);
                }
            });
        }










        **/

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

        private async Task NotifyUserOfRoleReuse(string phoneNumber, string newUserType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11)
                {
                    _log.LogWarning("Invalid phone number for SMS notification: {PhoneNumber}", phoneNumber);
                    return;
                }

                string message = $"You've been added as a '{newUserType}' on the Agrovesto platform. Login to access your dashboard.";

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
                    model.OrganizationCode= args.organizationCode;
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
                    await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, coop.model.Id);

                    break;

                case SubAccountType.Farmer:
                  //  model.Name = $"{model.FirstName} {model.MiddleName} {model.LastName}";
                    model.Id = registeredUser.model.Id;
                    model.OrganizationCode = args.organizationCode;
                    await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, args.CooperativeId);
                    break;
            }

            return newBusinessWorker;
        }

        public async Task<SubAccountModel> UpdateUserAccountProfile(SubAccountModel subaccount, BusinessRegistrationArgs args, string userId)
        {
            return await _walletManager.UpdateUserProfile(subaccount, new ParameterArgs
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
                Name= model.Name,
                NIN=model.NIN,
                Bvn=model.Bvn,
                
                OrganizationCode=args.organizationCode,
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
                LastName = model.FirstName,
                MiddleName=model.MiddleName,
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
                OrganizationCode=args.organizationCode,
                Status = BusinessStatus.Registered.ToString()
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
                Ward=model.Ward,
                
                OrganizationCode=model.OrganizationCode
            };

            await _agentRepository.CreateAgent(agent);
        }

        private async Task<(bool status, CooperativeModel model)> CreateCooperativeProfileAsync(UserModel model, BusinessModel business, string userId)
        {
            var getUserType = await _authenticationRepository.GetUserById(userId);
            var isAssignedagent = false;
            var agent = new AgentModel();
            if (getUserType.model.UserType == Enum.GetName(typeof(UserType), UserType.Agent))
            {
                isAssignedagent = true;
                // agent = await _agentRepository.GetAgentDetailByUserId(userId);

            }
            var cooperative = new CooperativeModel
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


        private async Task CreateFarmerProfileAsync(UserModel model, BusinessRegistrationArgs args, BusinessModel business, string userId, SubAccountModel subaccount,
            long cooperativeId)
        {
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
                Name = model.Name,
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                DateOfBirth = model.DateOfBirth,
                BelongToCooperativeId = cooperativeId,
                Ward = model.Ward,
                OrganizationCode =args.organizationCode,
                FarmerCategory=model.FarmerCategory,
                ValueChainSelections=model.ValueChainSelections,
                YearsInAgric=model.YearsInAgric,
                HighestLevelOfEducation=model.HighestLevelOfEducation
            };

            var farer = await _farmerRepository.CreateFarmer(farmer);


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

        public async Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffByBusinessCode(string businessCode,string organizationCode)
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

        public async Task<PagedListResponse<List<CooperativeModel>>> GetCooperativesByBusinessCode(string userId,
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
                if (agent == null) return new PagedListResponse<List<CooperativeModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<CooperativeModel>>
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
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);
                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
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
        public async Task<CooperativeModel> GetCooperativeDetailByUserId(string userId)
        {

            return await _cooperativeRepository.GetCooperativeDetailByUserId(userId);
        }


        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeUserId(string userId,
                ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var cooperative = await _cooperativeRepository.GetCooperativeDetailByUserId(userId);

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

        public async Task<PagedListResponse<List<CooperativeModel>>> GetCooperativesByAgent(string userId,
       ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var cooperative = await _cooperativeRepository.GetCooperativeByAgent(userId, query);
                if (cooperative == null) return new PagedListResponse<List<CooperativeModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<CooperativeModel>>
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
        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByAgent(string userId,

      ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var farmers = await _farmerRepository.GetFarmersByAgent(userId, query);
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
        public async Task<OrganizationModel> CreateOrganization(OrganizationModel org, BusinessRegistrationArgs args) //userId is same as onboarded by{
        {   
            OrganizationModel newOrg = new OrganizationModel();
            var parentBusiness = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
            if (parentBusiness == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"Parent business not found or has been deactivated, kindly contact platform administrator");
            }
            var bModel = await _businessRepository.GetOrganizationAsync(org.OrganizationCode);
            if (bModel != null)
            {
                throw new BaseException(Core.Enums.ExceptionType.ALREADY_EXIST, $"{org.OrganizationName} already exist, Kindly contact administrator");
            }
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var name = org.OrganizationName.Replace(" ", string.Empty).ToUpper();
                        var businessCount = await _businessRepository.OrganizationCount();
                        if (name.Length > 3)
                        {
                            name = $"{name.Substring(0, 3)}";
                        }
                        org.BusinessCode = args.businessCode;
                        org.OrganizationCode= $"{name}{DateTimeOffset.Now.Month}{businessCount + 1}";

                        //register organization
                        newOrg = await _businessRepository.CreateOrganization(org);

                        var registrationMode = new UserModel
                        {
                            Email = org.ContactEmail,
                            FirstName = org.OrganizationName,
                            Password = org.Password,
                            PhoneNumber = org.Contact,
                            UserType = Enum.GetName(typeof(UserType), UserType.Organization),//UserType.Business,
                            CreatedBy = args.UserId,
                            CreatedOn = DateTimeOffset.Now,
                            BusinessCode = args.businessCode,
                            OrganizationCode= org.OrganizationCode,
                            Name= org.OrganizationName

                        };
                        var registerBusinessUser = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*role name should be business admin role*/);
                        if (!registerBusinessUser.status)
                        {
                            throw new Exception(registerBusinessUser.message);
                        }

                        


                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();


                        //send email to business about sucessful onboarding
                       
                        var accountOfficer = await _authenticationRepository.GetUserById(args.UserId);

                        var propagationUrl = AppSettingsHelper.Setting("propagation:url");
                        var mailRequest = new WelcomeMailBody
                        {
                            To = org.ContactEmail,
                            ToName = $"{org.OrganizationName}",
                            Subject = $"Welcome OnBoard {org.OrganizationName}",
                            AccountOfficer = accountOfficer.model.FirstName,
                            DefaultPassword = args.DefaultPassword,
                            Email = accountOfficer.model.Email,
                            PhoneNumber = accountOfficer.model.PhoneNumber,
                            //PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}",
                            logincred = $"login?bsCd={org.BusinessCode}&org={org.OrganizationCode}&pwd={args.DefaultPassword}"
                        };
                        var mail = SendGridService.DynamicBusinessOnboarding(mailRequest);
                        var sendSms = SendOnboardingSMSM(org.OrganizationName, org.Contact);
                        return newOrg;
                    }
                    catch (Exception ex)
                    {

                        await dbTransaction.RollbackAsync();

                        throw new Exception(ex.Message);
                    }
                }
            });

            return newOrg;
        }

        public async Task<FarmerDetailDto> GetFarmerDetails(

    ListQueryParams query)
        {
            try
            {
                
                var farmers = await _farmerRepository.GetFarmerDetails( query.UserId);
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

                var farmers = await _farmerRepository.GetAllFarmerDetails(query.businessCode,query.organizationCode,query.CooperativeId,query.UserId, query.Location,query.sortBy);
                if (farmers.Data.Count ==0) return null;

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
}
