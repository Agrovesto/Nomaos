using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface ISMSService
    {
        Task<(SMSResponseModel response, bool status)> HandleAfricasTalkingSMS(WhisperSMSModel request);
        Task<(TermiiResponse response, bool status)> HandleTermiiSMS(TermiiSmsRequest request);
        Task<(SMSResponseModel response, bool status)> HandleWhisperSMS(WhisperSMSModel request);

    }
}
