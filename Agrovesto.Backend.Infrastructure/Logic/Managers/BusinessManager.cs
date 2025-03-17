using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private readonly IFaceManager _faceManager;
        private readonly ISMSService _whisperSMSService;
        public BusinessManager(IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletManager walletManager, IFarmerRepository farmerRepository,
            IAgentRepository agentRepository, ISMSService whisperSMSService,
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

            _faceManager = faceManager;
            _whisperSMSService = whisperSMSService;
        }
        public async Task<PagedListResponse<List<BusinessModel>>> GetBusinessesAsync(ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
             
                var businesses = await _businessRepository.GetBusinessesAsync(query);
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
                    Message = "Retrieved Successfulluy"
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

        public async Task<BusinessModel> CreateBusiness(BusinessModel business, BusinessRegistrationArgs args) //userId is same as onboarded by{
        {
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
                        throw new BaseException(Core.Enums.ExceptionType.ALREADY_EXIST,$"{business.BusinessName} already exist, Kindly contact administrator");
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
                        Name = business.BusinessName,
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
                        AccountOfficer = accountOfficer.model.Name,
                        Email = accountOfficer.model.Email,
                        PhoneNumber = accountOfficer.model.PhoneNumber,
                        PropagateUrl = $"{propagationUrl}/login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}",
                        logincred = $"login?bsCd={business.BusinessCode}&signage={newBusiness.BusinessCodeSignageSalt}&pwd={args.DefaultPassword}"
                    };
                    var mail = SendGridService.DynamicBusinessOnboarding(mailRequest);

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
            var user = await _authenticationRepository.GetUserById(args.UserId);
            var business = await _businessRepository.GetBusinessByUserIdAsync(user.model.Id);

            if (business == null) { throw new Exception($"Business does not exist"); }
            BusinessWorkerModel newBusinessWorker = new BusinessWorkerModel();
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
                            Name = bWorker.FullName,
                            Password = args.DefaultPassword,
                            PhoneNumber = bWorker.PhoneNumber,
                            UserType = Enum.GetName(typeof(UserType), UserType.Business),
                            //UserType.Business,
                            CreatedBy = args.UserId,
                            CreatedOn = DateTimeOffset.Now,
                            BusinessCode = business.BusinessCode

                        };
                        var registerBusinessWorker = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*rolename should be roles from businesses*/);
                        if (!registerBusinessWorker.status)
                        {
                            throw new Exception(registerBusinessWorker.message);
                        }

                        bWorker.BusinessId = business.BusinessId;
                        bWorker.UserId = registerBusinessWorker.model.Id;
                         newBusinessWorker = await _businessRepository.CreateBusinessWorker(bWorker);


                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();


                        //send email to user to reset their password
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

        public async Task<CooperativeModel> AssignCooperativeToAgent(long agentId, long cooperativeId)
        {
            return await _cooperativeRepository.AssignCooperativeToAgent(agentId, cooperativeId);
        }
        public async Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId)
        {
            return await _cooperativeRepository.ViewAgentAssignedToCooperative(cooperativeId);
        }
        public async Task<SubAccountModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            SubAccountModel subaccount = new SubAccountModel();
          
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    var registeredUser = await RegisterUserAsync(model, args);
                    if (!registeredUser.status)
                        throw new Exception($"Could not register user {registeredUser.message}");

                    subaccount = await CreateSubAccountAsync(model, args, business, registeredUser.model.Id);
                  

                    switch (subaccount.SubAccountType)
                    {
                        case SubAccountType.Agent:
                            await CreateAgentProfileAsync(model, business, registeredUser.model.Id);
                            break;

                        case SubAccountType.Cooperative:
                            model.Id = registeredUser.model.Id;
                                //create first as cooperative
                            var coop=    await CreateCooperativeProfileAsync(model, business, args.UserId);
                            args.UserId = registeredUser.model.Id;
                            //then create user as a farmer as well

                            await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, coop.model.Id);

                            break;

                        case SubAccountType.Farmer:
                            model.Id = registeredUser.model.Id;
                            await CreateFarmerProfileAsync(model, args, business, registeredUser.model.Id, subaccount, args.CooperativeId);
                            break;
                    }

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    //using(var newTransaction = await _context.Database.BeginTransactionAsync())
                    //{
                    //    await _walletManager.UpdateUserProfile(subaccount, new ParameterArgs
                    //    {
                    //        UserId = registeredUser.Id,
                    //        Env = args.Env
                    //    });
                    //    await newTransaction.CommitAsync();
                    //}
                    subaccount.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : GenerateEmail(model.Name, business.BusinessEmail);
                   subaccount.UserId = registeredUser.model.Id;
                    //var messageBody = $"You are welcome onboard. ";

                    //var sms = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
                    //{
                    //    campaign_name = "Agrovesto Notification",
                    //    contacts = new List<string> { model.PhoneNumber },
                    //    message = messageBody,
                    //});
                    //var sms2 = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
                    //{
                    //    to = $"234{model.PhoneNumber.GetLast(10)}",
                    //    sms = messageBody
                    //});
                    return subaccount;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return subaccount;
        }
        public async Task<SubAccountModel> UpdateUserAccountProfile(SubAccountModel subaccount, BusinessRegistrationArgs args, string userId)
        {
          return  await _walletManager.UpdateUserProfile(subaccount, new ParameterArgs
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
                Name = model.Name,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                UserType = args.UserType,//Enum.GetName(typeof(UserType), args.UserType),//args.UserType.ToEnum<UserType>(),
                CreatedBy = args.UserId,
                CreatedOn = DateTimeOffset.UtcNow,
                Pin = model.Pin,

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
                FirstName = model.Name,
                LastName = model.Name,
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
                Status = BusinessStatus.Registered.ToString()
            };

            return await _walletManager.CreateSubAccount(subaccount);
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
                UserId = userId
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
                agent = await _agentRepository.GetAgentDetailByUserId(userId);
             
            }
            var cooperative = new CooperativeModel
            {
                BelongToBusinessCode = business.BusinessCode,
                State = model.State,
                Lga = model.Lga,
                ContactPerson = model.Name,//string.Empty,
                Email = model.Email,
                Location = model.Location,
                Name = model.Name,
                OnBoardedOn = DateTimeOffset.UtcNow,
                PhoneNumber = model.PhoneNumber,
                UserId = model.Id,
                IsAssignedToAgentId= agent.Id,
                HasbeenAssignedAgent=isAssignedagent
            };

         return  await _cooperativeRepository.CreateCooperative(cooperative);
        }

    
        private async Task CreateFarmerProfileAsync(UserModel model, BusinessRegistrationArgs args, BusinessModel business, string userId, SubAccountModel subaccount,
            long cooperativeId)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                model.Email = GenerateEmail(model.Name, business.BusinessEmail);
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
                BelongToCooperativeId= cooperativeId
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

        public async Task<PagedListResponse<List<AgentModel>>> GetAgentsByBusinessCode(string userId,
            ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var agent = await _agentRepository.GetAgentsByBusinessCode(business.BusinessCode, query);
                if (agent == null) return new PagedListResponse<List<AgentModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<AgentModel>>
                {
                    Succeeded = true,
                    Data = agent,
                    Count = agent.Count,
                    Message = "Retrieved Successfulluy"
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

        public async Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffByBusinessCode(string businessCode)
        {
            try
            {
              //  query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(businessCode);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var staff = await _businessRepository.GetStaffByBusinessCode(business.BusinessId);
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
                    Message = "Retrieved Successfulluy"
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

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

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
                    Message = "Retrieved Successfulluy"
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

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

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
        //            Message = "Retrieved Successfulluy"
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
                    Message = "Retrieved Successfulluy"
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

                var staff = await _businessRepository.GetStaffByBusinessCode(business.BusinessId);
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
                    Message = "Retrieved Successfulluy"
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

                var cooperative = await _cooperativeRepository.GetCooperativeByAgent(agent.Id, query);
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
                    Message = "Retrieved Successfulluy"
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

                var farmers = await _farmerRepository.GetFarmersByAgent(agent.Id, query);
                if (farmers == null) return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = farmers,
                    Count = farmers.Count,
                    Message = "Retrieved Successfulluy"
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
                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = farmers,
                    Count = farmers.Count,
                    Message = "Retrieved Successfulluy"
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

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var bizEntities = await _farmerRepository.GetBusinessEntitiesMetricsAsync(business.BusinessCode);
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
                    Message = "Retrieved Successfulluy"
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
                    saveFace = await _faceManager.UploadImage(model.File, model.FarmerUserId, args.businessCode);

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

       

        }
}
