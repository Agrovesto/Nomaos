using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class Payout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string? SourceAccountName { get; set; }
        public string? SourceAccountNumber { get; set; }
        public string? SourceBankCode { get; set; }
        public string? DestinationAccountNumber { get; set; }
        public string? DestinationAccountName { get; set; }
        public string? DestinationBank { get; set; }
        public string? Reference { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessfulTransaction { get; set; }

        public DateTimeOffset? CreatedOn { get; set; }
    }

}
