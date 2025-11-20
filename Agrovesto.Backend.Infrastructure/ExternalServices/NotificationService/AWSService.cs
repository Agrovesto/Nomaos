using Agrovesto.Backend.Core.Interfaces.Services;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public interface IAWSService
    {
        Task SendSignupEmail(string toEmail, string userName);
        Task SendSignupSms(string phoneNumber, string userName);
    }
    public class AWSService : IAWSService
    {
        private readonly IAmazonSimpleEmailService _ses;
        private readonly IAmazonSimpleNotificationService _sns;

        public AWSService()
        {
            _ses = new AmazonSimpleEmailServiceClient(RegionEndpoint.EUWest1);
            _sns = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USEast1);

        }
        public async Task SendSignupEmail(string toEmail, string userName)
        {
            var sendRequest = new SendEmailRequest
            {
                Source = "no-reply@yourdomain.com", // Must be verified in SES
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content("Welcome to Agrovesto!"),
                    Body = new Body
                    {
                        Html = new Content($@"
                        <h2>Hello {userName},</h2>
                        <p>Your account has been created successfully.</p>
                        <p>Thank you for joining!</p>
                    ")
                    }
                }
            };

            await _ses.SendEmailAsync(sendRequest);
        }

        public async Task SendSignupSms(string phoneNumber, string userName)
        {
            var request = new PublishRequest
            {
                Message = $"Hello {userName}, your account on Agrovesto has been created successfully.",
                PhoneNumber = phoneNumber // Example: +2348012345678
            };

            await _sns.PublishAsync(request);
        }
    }
}
