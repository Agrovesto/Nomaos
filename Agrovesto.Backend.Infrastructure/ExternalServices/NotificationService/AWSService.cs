using Agrovesto.Backend.Core.Interfaces.Services;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public interface IAWSService
    {
        Task SendEmailAsync(string senderEmail, string recipientEmail, string subject, string bodyHtml, string bodyText);
        Task SendSignupSms(string phoneNumber, string userName);
    }
    public class AWSService : IAWSService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IAmazonSimpleNotificationService _sns;
        private readonly IConfiguration _config;
        public AWSService(IConfiguration config)
        {
            var awsAccessKey = config["AWS:AccessKey"];
            var awsSecretKey = config["AWS:SecretKey"];

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

            _sesClient = new AmazonSimpleEmailServiceClient(credentials, RegionEndpoint.EUWest1);
            _sns = new AmazonSimpleNotificationServiceClient(credentials, RegionEndpoint.EUWest1);

        }
        public async Task SendEmailAsync(string senderEmail,
            string recipientEmail, 
            string subject,
            string bodyHtml,
            string bodyText
            )
        {
            var sendRequest = new SendEmailRequest
            {
                Source = senderEmail, // Must be verified in SES
                Destination = new Destination
                {
                    ToAddresses = new List<string> { recipientEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content(bodyHtml),
                        Text = new Content(bodyText)
                    }
                }
            };
            try
            {
                var response = await _sesClient.SendEmailAsync(sendRequest);
                Console.WriteLine($"Email sent! Message ID: {response.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
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
