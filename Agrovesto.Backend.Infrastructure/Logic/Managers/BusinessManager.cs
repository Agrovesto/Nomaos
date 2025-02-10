using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class BusinessManager : IBusinessManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly IWalletRepository _walletRepository;
        public BusinessManager(IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletRepository walletRepository)
        {
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _walletRepository = walletRepository;

        }

        public async Task<BusinessModel> CreateBusiness(BusinessModel business, BusinessRegistrationArgs args) //userId is same as onboarded by{
        {
            business.Validate();
            business.BusinessStatus = BusinessStatus.Registered;

            using (var dbTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var bModel = await _businessRepository.GetBusinessAsync(business.BusinessName);
                    if (bModel != null)
                    {
                        throw new Exception($"{business.BusinessName} already exist, Kindly contact administrator");
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
                        UserType = UserType.Business,
                        CreatedBy = args.UserId,
                        CreatedOn = DateTimeOffset.Now,

                    };
                    var registerBusiness = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*role name should be business admin role*/);
                    if (!registerBusiness.status)
                    {
                        throw new Exception($"Could not register business");
                    }

                    var newBusiness = await _businessRepository.CreateBusiness(business, registerBusiness.model.Id);


                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();


                    //send email to business about sucessful onboarding
                    //DynamicBusinessOnboarding
                    var accountOfficer = await _authenticationRepository.GetUserById(args.UserId);
                    var propagationUrl = AppSettingsHelper.Setting("propagation:url");
                    var mailRequest = new WelcomeMailBody
                    {
                        To = business.BusinessEmail,
                        ToName = $"{business.BusinessName}",
                        Subject = "Welcome OnBoard",
                        AccountOfficer = accountOfficer.model.Name,
                        Email = accountOfficer.model.Email,
                        PhoneNumber = accountOfficer.model.PhoneNumber,
                        PropagateUrl = $"{propagationUrl}/{business.BusinessCode}"
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
        }

        public async Task<BusinessWorkerModel> CreateBusinessWorkerAsync(BusinessWorkerModel bWorker, BusinessRegistrationArgs args)
        {
            var user = await _authenticationRepository.GetUserById(args.UserId);
            var business = await _businessRepository.GetBusinessByUserIdAsync(user.model.Id);

            if (business == null) { throw new Exception($"Business does not exist"); }

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
                        UserType = UserType.Business,
                        CreatedBy = args.UserId,
                        CreatedOn = DateTimeOffset.Now,

                    };
                    var registerBusinessWorker = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*rolename should be roles from businesses*/);
                    if (!registerBusinessWorker.status)
                    {
                        throw new Exception($"Could not register business");
                    }

                    bWorker.BusinessId = business.BusinessId;
                    bWorker.UserId = registerBusinessWorker.model.Id;
                    var newBusinessWorker = await _businessRepository.CreateBusinessWorker(bWorker);


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
        }


        public async Task<UserModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);

            if (business == null) { throw new Exception($"Business does not exist"); }

            using (var dbTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var registrationMode = new UserModel
                    {
                        Email = model.Email,
                        Name = model.Name,
                        Password = model.Password,
                        PhoneNumber = model.PhoneNumber,
                        UserType = args.UserType.ToEnum<UserType>(),//UserType.User,
                        CreatedBy = args.UserId,
                        CreatedOn = DateTimeOffset.Now,

                    };
                    var registerUser = await _authenticationRepository.RegisterBusinessUser(registrationMode, args.RoleName /*rolename should be roles from default to user*/);
                    if (!registerUser.status)
                    {
                        throw new Exception($"Could not register user");
                    }

                    var subaccount = new SubAccountModel
                    {
                        BusinessId = business.BusinessId,
                        UserId = registerUser.model.Id,
                        FirstName = model.Name,
                        LastName = model.Name,
                        SubAccountType = args.SubAccountType.ToEnum<SubAccountType>(),
                        PhoneNumber = model.PhoneNumber,
                    };

                    var createSubAccount = await _walletRepository.CreateSubAccount(subaccount);
                    //create subaccount
                    if (subaccount.SubAccountType == SubAccountType.Cooperative)
                    {
                        var cooperative = new Cooperative
                        {
                            BelongToBusinessCode = business.BusinessCode,
                            State = model.State,
                            Lga = model.Lga,
                            ContactPerson = "",
                            Email = model.Email,
                            Location = model.Location,
                            Name = model.Name,
                            OnBoardedOn = DateTimeOffset.Now,
                            PhoneNumber = model.PhoneNumber,
                            UserId = registerUser.model.Id
                        };
                        //create cooperative profile
                    }
                    else if (subaccount.SubAccountType == SubAccountType.Agent)
                    {
                        var agent = new Agent
                        {
                            BelongToBusinessCode = business.BusinessCode,
                            State = model.State,
                            Lga = model.Lga,
                            Email = model.Email,
                            Location = model.Location,
                            Name = model.Name,
                            OnBoardedOn = DateTimeOffset.Now,
                            PhoneNumber = model.PhoneNumber,
                            UserId = registerUser.model.Id,                            
                        };
                        //create agent profile
                    }
                    else if (subaccount.SubAccountType == SubAccountType.Farmer)
                    {
                        //get agent or cooperative user loggedin profile

                        //create farmer profile user the loggedin user
                        
                        var farmer = new Farmer
                        {
                            OnboardedByUserId = args.UserId, //if it's null, refactor to use our userId  
                            BelongToBusinessCode= business.BusinessCode,
                            Email = model.Email,
                            Gender = model.Gender,
                            State = model.State,
                            Lga = model.Lga,
                            Location = model.Location,
                            Name = model.Name,
                            OnBoardedOn = DateTimeOffset.Now,
                            PhoneNumber = model.PhoneNumber,
                            UserId= registerUser.model.Id,
                            DateOfBirth= GetYearOfBirth(int.Parse(model.DateOfBirth))
                        };
                        //create farmer
                    }
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();


                    //send email to user to reset their password
                    return registerUser.model;
                }
                catch (Exception ex)
                {

                    await dbTransaction.RollbackAsync();

                    throw new Exception(ex.Message);
                }
            }
        }
        public string GetYearOfBirth(int age)
        {
            int currentYear = DateTime.Now.Year;
            int yearOfBirth = currentYear - age;
            return yearOfBirth.ToString();
        }
    }
}
