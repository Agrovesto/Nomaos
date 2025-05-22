using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.WalletServiceRequest
{
   
    public class PayoutModel
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string? SourceAccountName { get; set; }
        public string? SourceAccountNumber { get; set; }
        public string? SourceBankCode { get; set; }
        public string? DestinationAccountNumber { get; set; }
        public string? DestinationAccountName { get; set; }
        public string? DestinationBank { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessfulTransaction { get; set; }
        public string? Pin { get; set; }
        public string? Reference { get; set; }
        public string? Narration { get; set; }
        public DateTimeOffset? CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    }

}
