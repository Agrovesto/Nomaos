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
                       propagateUrl =model.PropagateUrl
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

    public class WelcomeMailBody
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string ToName { get; set; }
        public string AccountOfficer { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PropagateUrl { get; set; }
    }

}
