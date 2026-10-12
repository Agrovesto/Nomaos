using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        Task SendCommitmentPaymentNotificationAsync(string senderEmail, string recipientEmail, string subject, string bodyHtml, string bodyText);
        Task<bool> SendEmailAsync(string senderEmail, string recipientEmail, string subject, string bodyHtml, string bodyText);
        Task SendReconciliationAlertAsync(ReconciliationAlertRequest model);
        Task SendSignupSms(string phoneNumber, string userName);
        Task SendTemplatedEmailAsync(string senderEmail, string recipientEmail, string templateName, object templateData);

        Task SendSMSAsync(string phoneE164, string message, CancellationToken ct = default);

    }
    public class AWSService : IAWSService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly IAmazonSimpleNotificationService _sns;
        private readonly IConfiguration _config;

        private readonly ReconciliationEmailSettings _settings;
        private readonly ILogger<AWSService> _logger;
        public AWSService(IConfiguration config,
        IOptions<ReconciliationEmailSettings> settings,
        ILogger<AWSService> logger)
        {
            var awsAccessKey = config["AWS:AccessKey"];
            var awsSecretKey = config["AWS:SecretKey"];

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

            _sesClient = new AmazonSimpleEmailServiceClient(credentials, RegionEndpoint.EUWest1);
            _sns = new AmazonSimpleNotificationServiceClient(credentials, RegionEndpoint.EUWest1);

            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendSMSAsync(string phoneE164, string message, CancellationToken ct = default)
        {
            var request = new PublishRequest
            {
                PhoneNumber = phoneE164, // must be E.164, e.g. +234...
                Message = message,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["AWS.SNS.SMS.SMSType"] = new() { DataType = "String", StringValue = "Transactional" },
                    ["AWS.SNS.SMS.SenderID"] = new() { DataType = "String", StringValue = "YourBrand" }
                }
            };
            await _sns.PublishAsync(request, ct);
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
                _logger.LogInformation(
                    "Sent template '{Template}' from {Sender} to {Recipient}. SES MessageId: {MessageId}",
                    templateName, senderEmail, recipientEmail, response.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send template '{Template}' from {Sender} to {Recipient}.",
                    templateName, senderEmail, recipientEmail);
            }
        }
        public async Task<bool> SendEmailAsync(string senderEmail,string recipientEmail,  string subject,  string bodyHtml,  string bodyText)
        {
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError(
                    "Cannot send '{Subject}' to {Recipient}: no sender address was supplied (check Official:Email).",
                    subject, recipientEmail);
                return false;
            }

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogError("Cannot send '{Subject}': no recipient address was supplied.", subject);
                return false;
            }

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
                _logger.LogInformation(
                    "Sent '{Subject}' from {Sender} to {Recipient}. SES MessageId: {MessageId}",
                    subject, senderEmail, recipientEmail, response.MessageId);

                return true;
            }
            catch (MessageRejectedException ex)
            {
                _logger.LogError(ex,
                    "SES rejected '{Subject}' from {Sender} to {Recipient}. Verify the sender identity in eu-west-1 and check whether the account is still in the SES sandbox.",
                    subject, senderEmail, recipientEmail);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send '{Subject}' from {Sender} to {Recipient}.",
                    subject, senderEmail, recipientEmail);

                return false;
            }
        }
            public async Task SendCommitmentPaymentNotificationAsync(string senderEmail,
            string recipientEmail, 
            string subject,  string bodyHtml,  string bodyText)
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
                _logger.LogInformation(
                    "Sent commitment payment notification to {Recipient}. SES MessageId: {MessageId}",
                    recipientEmail, response.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send commitment payment notification from {Sender} to {Recipient}.",
                    senderEmail, recipientEmail);
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

        public async Task SendReconciliationAlertAsync(ReconciliationAlertRequest request)
        {
            if (_settings.AlertRecipients == null || !_settings.AlertRecipients.Any())
            {
                _logger.LogWarning(
                    "ReconciliationAlert: No recipients configured. " +
                    "DebitRef={DebitReference} CreditRef={CreditReference}",
                    request.DebitReference,
                    request.CreditReference);
                return;
            }

            var subject = $"[ACTION REQUIRED] Wallet Reconciliation Alert — {request.DebitReference}";
            var htmlBody = BuildReconciliationEmailHtml(request);
            var textBody = BuildReconciliationEmailText(request);

            var sendRequest = new SendEmailRequest
            {
                Source = $"{_settings.FromName} <{_settings.FromEmail}>",

                Destination = new Destination
                {
                    ToAddresses = _settings.AlertRecipients
                },

                Message = new Message
                {
                    Subject = new Content
                    {
                        Charset = "UTF-8",
                        Data = subject
                    },
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = textBody
                        }
                    }
                },

                // Reply-to can be your support inbox
                ReplyToAddresses = new List<string> { _settings.FromEmail }
            };

            try
            {
                var response = await _sesClient.SendEmailAsync(sendRequest);

                _logger.LogInformation(
                    "ReconciliationAlert sent. MessageId={MessageId} " +
                    "DebitRef={DebitReference} CreditRef={CreditReference}",
                    response.MessageId,
                    request.DebitReference,
                    request.CreditReference);
            }
            catch (MessageRejectedException ex)
            {
                _logger.LogError(ex,
                    "ReconciliationAlert rejected by SES. " +
                    "DebitRef={DebitReference} CreditRef={CreditReference}",
                    request.DebitReference,
                    request.CreditReference);
                throw;
            }
            catch (MailFromDomainNotVerifiedException ex)
            {
                _logger.LogError(ex,
                    "ReconciliationAlert failed — sender domain not verified in SES. " +
                    "FromEmail={FromEmail}",
                    _settings.FromEmail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ReconciliationAlert failed unexpectedly. " +
                    "DebitRef={DebitReference} CreditRef={CreditReference}",
                    request.DebitReference,
                    request.CreditReference);
                throw;
            }
        }

        // ─── HTML Email ───────────────────────────────────────────────────────────
        private static string BuildReconciliationEmailHtml(ReconciliationAlertRequest request)
        {
            var debitReference = System.Net.WebUtility.HtmlEncode(request.DebitReference);
            var creditReference = System.Net.WebUtility.HtmlEncode(request.CreditReference);
            var reason = System.Net.WebUtility.HtmlEncode(request.Reason);
            var flaggedAt = request.FlaggedAt.ToString("dddd, dd MMMM yyyy HH:mm:ss");
            var year = DateTime.UtcNow.Year;

            var sb = new System.Text.StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='en'>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8' />");
            sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'/>");
            sb.Append("<title>Reconciliation Alert</title>");
            sb.Append("<style>");
            sb.Append("body { margin: 0; padding: 0; background-color: #f4f4f7; font-family: Arial, sans-serif; color: #333333; }");
            sb.Append(".wrapper { width: 100%; padding: 40px 0; background-color: #f4f4f7; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }");
            sb.Append(".header { background-color: #c0392b; padding: 32px 40px; text-align: center; }");
            sb.Append(".header h1 { margin: 0; color: #ffffff; font-size: 22px; letter-spacing: 0.5px; }");
            sb.Append(".header p { margin: 8px 0 0; color: #f5b7b1; font-size: 13px; }");
            sb.Append(".badge { display: inline-block; background-color: #e74c3c; color: #ffffff; font-size: 11px; font-weight: bold; letter-spacing: 1px; padding: 4px 12px; border-radius: 20px; margin-bottom: 12px; text-transform: uppercase; }");
            sb.Append(".body { padding: 36px 40px; }");
            sb.Append(".body p { font-size: 15px; line-height: 1.6; margin: 0 0 20px; }");
            sb.Append(".detail-table { width: 100%; border-collapse: collapse; margin: 24px 0; font-size: 14px; }");
            sb.Append(".detail-table th { text-align: left; padding: 10px 14px; background-color: #f9f9f9; border: 1px solid #e0e0e0; color: #555555; font-weight: 600; width: 40%; }");
            sb.Append(".detail-table td { padding: 10px 14px; border: 1px solid #e0e0e0; color: #333333; word-break: break-all; }");
            sb.Append(".reason-box { background-color: #fff8f8; border-left: 4px solid #e74c3c; padding: 16px 20px; border-radius: 4px; margin: 24px 0; font-size: 14px; line-height: 1.6; color: #555; }");
            sb.Append(".reason-box strong { display: block; margin-bottom: 6px; color: #c0392b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px; }");
            sb.Append(".action-box { background-color: #fef9e7; border: 1px solid #f9e79f; border-radius: 6px; padding: 16px 20px; margin: 24px 0; font-size: 14px; line-height: 1.6; }");
            sb.Append(".action-box strong { color: #b7950b; }");
            sb.Append(".footer { background-color: #f4f4f7; padding: 24px 40px; text-align: center; font-size: 12px; color: #999999; border-top: 1px solid #e0e0e0; }");
            sb.Append(".footer a { color: #999999; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");

            sb.Append("<div class='wrapper'>");
            sb.Append("<div class='container'>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<div class='badge'>Action Required</div>");
            sb.Append("<h1>Wallet Reconciliation Alert</h1>");
            sb.Append("<p>A wallet transaction requires immediate attention</p>");
            sb.Append("</div>");

            // Body
            sb.Append("<div class='body'>");
            sb.Append("<p>");
            sb.Append("A wallet transfer has been flagged for reconciliation. ");
            sb.Append("The subscriber's wallet was successfully debited, but ");
            sb.Append("the credit to the organization's wallet could not be confirmed. ");
            sb.Append("Immediate review is required to ensure no funds are lost.");
            sb.Append("</p>");

            // Detail table
            sb.Append("<table class='detail-table'>");
            sb.Append("<tr>");
            sb.Append("<th>Debit Reference</th>");
            sb.AppendFormat("<td>{0}</td>", debitReference);
            sb.Append("</tr>");
            sb.Append("<tr>");
            sb.Append("<th>Credit Reference</th>");
            sb.AppendFormat("<td>{0}</td>", creditReference);
            sb.Append("</tr>");
            sb.Append("<tr>");
            sb.Append("<th>Flagged At</th>");
            sb.AppendFormat("<td>{0} UTC</td>", flaggedAt);
            sb.Append("</tr>");
            sb.Append("</table>");

            // Reason box
            sb.Append("<div class='reason-box'>");
            sb.Append("<strong>Reason</strong>");
            sb.Append(reason);
            sb.Append("</div>");

            // Action box
            sb.Append("<div class='action-box'>");
            sb.Append("<strong>What to do:</strong><br/>");
            sb.Append("<ol style='margin: 8px 0 0; padding-left: 18px;'>");
            sb.Append("<li>Locate the debit and credit references in the wallet transaction logs.</li>");
            sb.Append("<li>Confirm whether the organization wallet was credited by the provider.</li>");
            sb.Append("<li>If the credit was successful at provider level, manually mark the credit as complete.</li>");
            sb.Append("<li>If the credit failed at provider level, initiate a reversal to the subscriber's wallet.</li>");
            sb.Append("<li>Update the reconciliation log with the resolution and your notes.</li>");
            sb.Append("</ol>");
            sb.Append("</div>");

            sb.Append("<p style='font-size:13px; color:#999;'>");
            sb.Append("This alert was generated automatically by the payment system. ");
            sb.Append("Do not reply to this email — contact your system administrator ");
            sb.Append("for access to the reconciliation dashboard.");
            sb.Append("</p>");

            sb.Append("</div>"); // end .body

            // Footer
            sb.Append("<div class='footer'>");
            sb.AppendFormat("&copy; {0} Payment System. All rights reserved.<br/>", year);
            sb.Append("This is an automated alert. Please do not reply to this email.");
            sb.Append("</div>");

            sb.Append("</div>"); // end .container
            sb.Append("</div>"); // end .wrapper

            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        // ─── Plain Text Fallback ──────────────────────────────────────────────────

        private static string BuildReconciliationEmailText(ReconciliationAlertRequest request)
        {
            return $"""
        [ACTION REQUIRED] Wallet Reconciliation Alert
        ==============================================

        A wallet transfer has been flagged for reconciliation.
        The subscriber's wallet was successfully debited, but the
        credit to the organization's wallet could not be confirmed.

        TRANSACTION DETAILS
        -------------------
        Debit Reference  : {request.DebitReference}
        Credit Reference : {request.CreditReference}
        Flagged At       : {request.FlaggedAt:dddd, dd MMMM yyyy HH:mm:ss} UTC

        REASON
        ------
        {request.Reason}

        WHAT TO DO
        ----------
        1. Locate the debit and credit references in the wallet transaction logs.
        2. Confirm whether the organization wallet was credited by the provider.
        3. If the credit was successful at provider level, manually mark it as complete.
        4. If the credit failed at provider level, initiate a reversal to the subscriber.
        5. Update the reconciliation log with the resolution and your notes.

        ---
        This is an automated alert. Do not reply to this email.
        Contact your system administrator for reconciliation dashboard access.
        """;
        }
    }
}
