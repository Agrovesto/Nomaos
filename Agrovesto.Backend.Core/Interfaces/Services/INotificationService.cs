using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<(SMSResponseModel response, bool status)> HandleAfricasTalkingSMS(WhisperSMSModel request);
        Task<(TermiiResponse response, bool status)> HandleTermiiSMS(TermiiSmsRequest request,string businessCode);
        Task<(SMSResponseModel response, bool status)> HandleWhisperSMS(WhisperSMSModel request);
        Task SendEmailAsync(string destination, string subject, string message);
        Task SendSmsAsync(string destination, string message, string businessCode);
    }
}
