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
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public interface IAWSService
    {
        Task CreateTemplateAsync();
        Task SendEmailAsync(string senderEmail, string recipientEmail, string subject, string bodyHtml, string bodyText);
        Task SendSignupSms(string phoneNumber, string userName);
        Task SendTemplatedEmailAsync(string senderEmail, string recipientEmail, string templateName, object templateData);
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
        public async Task CreateTemplateAsync()
        {
            var templateName = "AgrovestoWelcomeTemplate";

            try
            {
                var listResponse = await _sesClient.ListTemplatesAsync(new ListTemplatesRequest());
                if (listResponse.TemplatesMetadata.Exists(t => t.Name == templateName))
                {
                    Console.WriteLine("Template already exists.");
                    return;
                }

                var createRequest = new CreateTemplateRequest
                {
                    Template = new Template 
                    { 
                        TemplateName = templateName,
                        HtmlPart = @"
<!DOCTYPE html>
<html> 
<head> <meta charset=""UTF-8"">
<title>Welcome to Agrovesto</title> </head> 
<body style=""font-family: Arial, sans-serif; background-color: #f6f9fc; padding: 20px;"">
<div style=""max-width: 600px; margin: auto; background: #ffffff; padding: 20px; border-radius: 8px;"">
<h2 style=""color: #2E7D32;"">Welcome to Agrovesto 🌱</h2> 
<p>Hello <strong>{{name}}</strong>,</p> 
<p>Your account has been successfully created. We're excited to have you onboard!</p>
<p>Your default password is: <strong>{{password}}</strong></p>
<p>With Agrovesto, you can manage your agricultural operations, track activities, and grow your business efficiently.</p>
<div style=""margin: 30px 0;""> 
<a href=""{{loginLink}}"" style=""background-color: #2E7D32; color: white; padding: 12px 20px; text-decoration: none; border-radius: 5px;""> 
Login to your account </a> 
</div>
<p>If you have any questions, feel free to reach out to our support team.</p>
<p>Cheers,<br/>The Agrovesto Team</p> 
</div> 
</body> 
</html>",                        TextPart = "Hello {{name}}, your default password is {{password}}. Login here: {{loginLink}}",
                        SubjectPart = "Welcome to Agrovesto, {{name}}!"
                    },                    
                };

                await _sesClient.CreateTemplateAsync(createRequest);
                Console.WriteLine("Template created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating template: {ex.Message}");
            }
        }

        public async Task GetTemplateAsync()
        {
            var templateName = "AgrovestoWelcomeTemplate";

            try
            {
                var listResponse = await _sesClient.ListTemplatesAsync(new ListTemplatesRequest());
                if (listResponse.TemplatesMetadata.Exists(t => t.Name == templateName))
                {
                    Console.WriteLine("Template already exists.");
                    return;
                }


                await _sesClient.GetTemplateAsync(new GetTemplateRequest
                {
                    TemplateName=templateName
                });
                Console.WriteLine("Template created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating template: {ex.Message}");
            }
        }

        public async Task SendTemplatedEmailAsync(string senderEmail, string recipientEmail, string templateName, object templateData)
        {
            var request = new Amazon.SimpleEmail.Model.SendTemplatedEmailRequest
            {
                
                Source = senderEmail,
                Destination = new Amazon.SimpleEmail.Model.Destination
                {
                    ToAddresses = new List<string> { recipientEmail }
                },
                Template = templateName,
                TemplateData = JsonSerializer.Serialize(templateData)
            };

            try
            {
                var response = await _sesClient.SendTemplatedEmailAsync(request);
                Console.WriteLine($"Email sent! MessageId: {response.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
        public async Task SendEmailAsync(string senderEmail,string recipientEmail,  string subject,  string bodyHtml,  string bodyText)
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
