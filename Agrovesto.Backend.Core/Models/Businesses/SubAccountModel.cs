using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class SubAccountModel
    {
        public long SubAccountId { get; set; }
        public string? UserId { get; set; }
        public string? BankAccount { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? BankName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SubAccountName { get; set; }
        public string? WalletCode { get; set; }
        public long BusinessId { get; set; }
        public string? Status { get; set; }
        public bool IsMainAccount { get; set; } //is main account is true for business, where as users account under the business is false
        public DateTimeOffset OnBoardedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? Bvn { get; set; }
        public string? DOB { get; set; }
        public string? NIN { get; set; }
        public string City { get; set; }
        public string Alias { get; set; }
        public string Address { get; set; }
        public SubAccountType SubAccountType { get; set; }  
    }
    public enum SubAccountType
    {
        Farmer,
        Cooperative,
        Agent,
        Trader,
        Processor,
        Buyer,
        Business


    }
}
