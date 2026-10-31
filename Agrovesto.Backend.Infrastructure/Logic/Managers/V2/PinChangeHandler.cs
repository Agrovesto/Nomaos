using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Google;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public class PinChangeHandler : ICredentialChangeHandler
    {
        private readonly DataContext _context;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOtpService _otpService;
        private readonly INotificationService _smsService;

        public PinChangeHandler(DataContext context,
            IOnboardingRepository onboardingRepository, IOtpService otpService, INotificationService smsService)
        {
            _context = context;
            _otpService = otpService;
            _smsService = smsService;
            _onboardingRepository = onboardingRepository;
        }
        public CredentialType Type => CredentialType.Pin;
        public string RequiredFieldMissingMessage => "Your current PIN is required.";

        public async Task<(User? user, string message)> FindUserAsync(ChangeCredentialModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Phone number is required.");

            var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber);
            if (user == null)
                return (null, "Reset pin has been sent to your registered phone number.");

            return (user, "Reset pin has been sent to your registered phone number.");
        }

        public void ValidateNewValueFormat(string newValue)
        {
            if (!Regex.IsMatch(newValue, @"^\d{4}$"))
                throw new BaseException(ExceptionType.BAD_REQUEST, "PIN must be exactly 4 digits.");
        }

        public async Task ChangeAsync(User user, string oldValue, string newValue)
        {
            if (!await _onboardingRepository.ValidateUserPinAsync(user.Id, oldValue))
                throw new BaseException(ExceptionType.BAD_REQUEST, "The current PIN you entered is incorrect.");

            await _onboardingRepository.ResetPin(user, newValue); // already saves + signs out internally
        }

        public async Task<ResetArtifact> GenerateArtifactAsync(User user, ForgotCredentialModel model)
        {
            var otp = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserId = user.Id,
                Target = OtpTarget.Phone,
                Destination = user.PhoneNumber,
                Purpose = OtpPurpose.PhoneNumberPinReset,
                ExpiryMinutes = 5
            });

            return new ResetArtifact { Value = otp.Code, ExpiresAt = otp.ExpiresAt };
        }

        public async Task DeliverLiveAsync(User user, ResetArtifact artifact, ForgotCredentialModel model)
        {
            await _smsService.SendSmsAsync(
                user.PhoneNumber,
                $"Your verification code is {artifact.Value}. Expires in 5 minutes.",
                model.BusinessCode);
        }

        public string BuildNonLiveMessage(ResetArtifact artifact)
        => $"Your verification code is {artifact.Value}. Expires in 5 minutes.";


        public async Task ConfirmAsync(User user, ConfirmCredentialResetModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Verification code is required.");

            var validated = await _otpService.VerifyOtpAsync(user.Id, model.Code, OtpPurpose.PhoneNumberPinReset);
            if (validated == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid or expired OTP.");

            await _onboardingRepository.ResetPin(user, model.NewValue); // already saves + signs out internally
        }
    }
}
