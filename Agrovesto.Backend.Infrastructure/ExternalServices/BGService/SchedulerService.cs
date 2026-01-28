using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.BGService
{
    public class SchedulerService
    {


    }

    //public interface IBulkOnboardingJob
    //{
    //    Task Process(BulkOnboardingJobArgs args);
    //}
    //public class BulkOnboardingJob : IBulkOnboardingJob
    //{
    //    private readonly IOnBoardingManager _onboarding;
    //    private readonly IInviteService _inviteService;
    //    private readonly ILogger<BulkOnboardingJob> _log;

    //    public BulkOnboardingJob(
    //        IOnBoardingManager onboarding,
    //        IInviteService inviteService,
    //        ILogger<BulkOnboardingJob> log)
    //    {
    //        _onboarding = onboarding;
    //        _inviteService = inviteService;
    //        _log = log;
    //    }

    //    //public async Task Process(BulkOnboardingJobArgs args)
    //    //{
    //    //    var result = new BulkOnboardingResult
    //    //    {
    //    //        Total = args.Rows.Count
    //    //    };

    //    //    int rowNumber = 1;

    //    //    foreach (var row in args.Rows)
    //    //    {
    //    //        rowNumber++;

    //    //        try
    //    //        {
    //    //            if (args.IsDryRun)
    //    //            {
    //    //                _onboarding.ValidateQuickOnboarding(new OnboardingRequestModel
    //    //                {
    //    //                    Email = row.Email,
    //    //                    PhoneNumber = row.PhoneNumber
    //    //                });

    //    //                continue;
    //    //            }

    //    //            var model = new OnboardingRequestModel
    //    //            {
    //    //                FirstName = row.FirstName,
    //    //                LastName = row.LastName,
    //    //                Email = row.Email,
    //    //                PhoneNumber = row.PhoneNumber
    //    //            };

    //    //            await _onboarding.CreateBusinessUser(
    //    //                model,
    //    //                new OnBoardingArgs
    //    //                {
    //    //                    businessCode = args.BusinessCode,
    //    //                    userType = row.Role,
    //    //                    DisableOtp = args.DisableOtp
    //    //                });

    //    //            if (args.DisableOtp)
    //    //            {
    //    //                await _inviteService.SendInviteAsync(
    //    //                    model.Email,
    //    //                    args.BusinessCode);
    //    //            }

    //    //            result.Successful++;
    //    //        }
    //    //        catch (Exception ex)
    //    //        {
    //    //            _log.LogError(ex, "Bulk onboarding failed");

    //    //            result.Failed++;
    //    //            result.Errors.Add(new BulkOnboardingError
    //    //            {
    //    //                RowNumber = rowNumber,
    //    //                Email = row.Email,
    //    //                Error = ex.Message
    //    //            });
    //    //        }
    //    //    }

    //    //    // persist result (DB / cache / blob)
    //    //    await _onboarding.SaveBulkResultAsync(result);
    //    //}
    
    
    //}

}
