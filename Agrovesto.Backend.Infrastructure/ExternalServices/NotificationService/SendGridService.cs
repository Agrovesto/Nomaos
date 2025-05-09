using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Infrastructure.Utilities;
using SendGrid.Helpers.Mail;
using SendGrid;
using Agrovesto.Backend.Core.Models.Businesses;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService
{
    public class SendGridService//:ISendGridService
    {
        //private readonly ISendGridClient _sendGridClient;

        //public SendGridService(ISendGridClient sendGridClient)
        //{
        //    _sendGridClient = sendGridClient;
        //}
        private static string RetrieveAPIKey()
        {
            return AppSettingsHelper.Setting("SendGrid:key");
            //Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");

        }
        public static async Task SendDynamicForgotPasswordMail(MailBody model)
        {
            var emailMessage = MailHelper.CreateSingleTemplateEmail(
                new EmailAddress("info@agrovesto.com", "Agrovesto"),
                new EmailAddress(model.To, model.ToName),
                "d-48e35e82727e4148adba1953ed51a5c6",
                new
                {
                    name = model.ToName,
                    // email=model.To,
                    token = model.Token
                });


            var _sendGridClient = new SendGridClient(RetrieveAPIKey());

            var response = await _sendGridClient.SendEmailAsync(emailMessage);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                await response.Body.ReadAsStringAsync();
            }

        }
        public static async Task DynamicStockInputDelivery(StockMailBody model)
        {
            var key = AppSettingsHelper.Setting("SendGrid:key");
            var apiKey = key;//Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");

            var emailMessage = MailHelper.CreateSingleTemplateEmail(
                   new EmailAddress(model.From, model.FromName),
                   new EmailAddress(model.To, model.ToName),
                   "d-9892ab1d12494eaf8d4f6ff3e2af6493",
                   new
                   {
                       input=model.input,
                       Reference = model.Reference,
                       DeliveredBy = model.DeliveredBy,
                       DeliveryContact = model.PhoneNumber,
                       PhoneNumber = model.PhoneNumber,
                       businessName = model.BusinessName
                   });

            // emailMessage = AddBcc(emailMessage);
            var _sendGridClient = new SendGridClient(apiKey);

            var response = await _sendGridClient.SendEmailAsync(emailMessage);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                await response.Body.ReadAsStringAsync();
            }
        }


        public static async Task DynamicInputDisbursementApproval(InputDisbursementApprovalMailBody model)
        {
            var key = AppSettingsHelper.Setting("SendGrid:key");
            var apiKey = key;//Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");

            var emailMessage = MailHelper.CreateSingleTemplateEmail(
                   new EmailAddress(model.From, model.FromName),
                   new EmailAddress(model.To, model.ToName),
                   "d-d89c73d0530d4106b600178cafc368a1",
                   new
                   {
                       logo= model.CompanyLogo,
                       merchantName = model.FromName,
                       ToName = model.ToName,
                       Quantity =model.Quantity,
                       BundlePackage= model.BundlePackage,
                       Reference = model.Reference,

                       propagateUrl = model.PropagateUrl
                   });

            // emailMessage = AddBcc(emailMessage);
            var _sendGridClient = new SendGridClient(apiKey);

            var response = await _sendGridClient.SendEmailAsync(emailMessage);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                await response.Body.ReadAsStringAsync();
            }
        }

        public static async Task DynamicBusinessStaffOnboarding(WelcomeMailBody model)
        {
            var key = AppSettingsHelper.Setting("SendGrid:key");
            var apiKey = key;//Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");

            var emailMessage = MailHelper.CreateSingleTemplateEmail(
                   new EmailAddress(model.From, model.FromName),
                   new EmailAddress(model.To, model.ToName),
                   "d-b678eddcbe474d2b8f454bc5b95830ff",
                   new
                   {
                       merchantName = model.ToName,
                       accountOfficer = model.AccountOfficer,
                       phoneNumber = model.PhoneNumber,
                       email = model.Email,
                       propagateUrl = model.PropagateUrl,
                       logincred = model.logincred,
                       defaultPassword = model.DefaultPassword,
                       businessName = model.BusinessName,
                       fullName = model.FullName,
                       platform = model.Platform,
                   });

           // emailMessage = AddBcc(emailMessage);
            var _sendGridClient = new SendGridClient(RetrieveAPIKey());

            var response = await _sendGridClient.SendEmailAsync(emailMessage);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                await response.Body.ReadAsStringAsync();
            }
        }
        public static async Task DynamicBusinessOnboarding(WelcomeMailBody model)
        {
            var key = AppSettingsHelper.Setting("SendGrid:key");
            var apiKey = key;//Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");

            var emailMessage = MailHelper.CreateSingleTemplateEmail(
                   new EmailAddress("info@agrovesto.com", "Agrovesto"),
                   new EmailAddress(model.To, model.ToName),
                   "d-bf735659a34643e788c15a7b8b712907",
                   new
                   {
                       merchantName = model.ToName,
                       accountOfficer =model.AccountOfficer,
                       phoneNumber =model.PhoneNumber,
                       email = model.Email,
                       propagateUrl =model.PropagateUrl,
                       logincred = model.logincred,
                       defaultPassword = model.DefaultPassword

                   });

            emailMessage = AddBcc(emailMessage);
            var _sendGridClient = new SendGridClient(RetrieveAPIKey());

            var response = await _sendGridClient.SendEmailAsync(emailMessage);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                await response.Body.ReadAsStringAsync();
            }
        }
        private static SendGridMessage AddBcc(SendGridMessage message)
        {
            var bccs = new List<EmailAddress>
            {
                //new EmailAddress("agrovesto@gmail.com"),
                new EmailAddress( AppSettingsHelper.Setting("bcc:email")),
            };

            foreach (var bcc in bccs)
            {
                if (!message.Personalizations.Any(x => x.Tos.Any(a => a == bcc)))
                {
                    message.AddBcc(bcc);
                }
            }

            return message;
        }
        public static void AddContactToSendGrid(WelcomeMailBody model)
        {

        }
    }
    public class MailBody
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ToName { get; set; }
        public string Token { get; set; }
    }
    public class StockMailBody
    {
        public string From { get; set; }
        public string FromName { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ToName { get; set; }
        public string DeliveredBy { get; set; }
        public string input { get; set; }
        public string PhoneNumber { get; set; }
        public string Reference { get; set; }
        public string Quantity { get; set; }
        public string BusinessName { get; set; }
    }
    public class WelcomeMailBody
    {
        public string From { get; set; }
        public string FromName { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ToName { get; set; }
        public string AccountOfficer { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PropagateUrl { get; set; }
        public string logincred { get; set; }
        public string DefaultPassword { get; set; }
        public string FullName { get; set; }
        public string Platform { get; set; }
        public string BusinessName { get; set; }
    }


    public class InputDisbursementApprovalMailBody
    {
        public string From { get; set; }
        public string CompanyLogo { get; set; }
        public string FromName { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ToName { get; set; }
        public string DeliveredTo { get; set; }
        public string Bundle { get; set; }
        public string PhoneNumber { get; set; }
        public string Reference { get; set; }
        public string Quantity { get; set; }
        public string BusinessName { get; set; }
        public string PropagateUrl { get; set; }
        public List<BundleItemsModel>? BundlePackage { get; set; }
    }
}
