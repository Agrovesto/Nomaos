using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class Wallet
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string BankName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WalletCode { get; set; }
        public string WalletProviderCode { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? OrganizationCode { get; set; }
    }
}
