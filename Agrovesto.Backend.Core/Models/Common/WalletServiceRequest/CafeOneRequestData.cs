using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.WalletServiceRequest
{
   
    public class CafeOneCustomerWalletCreationRequest
    {
        public string WalletGroupId { get; set; }
        public string CustomerId { get; set; }
        public string CurrencyId { get; set; } //= "fd5e474d-bb42-4db1-ab74-e8d2a01047e9"; //move to db
        public string Name { get; set; }

    }

    public class CafeoneCustomerRequest
    {
        public string mobileNumber { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string EmailAddress { get; set; }
        public string dob { get; set; }
        public string bvnNumber { get; set; }
        public string nin { get; set; }
        public string CustomerTypeId { get; set; }// = "f671da57-e281-4b40-965f-a96f4205405e"; //put in the db
        public string CountryId { get; set; } //= "c15ad9ae-c4d7-4342-b70f-de5508627e3b"; //put in the db
        public string City { get; set; }
        public string Address { get; set; }
        public string Alias { get; set; }
        public string? CustomerId { get; set; }

    }

    public class CafeOneCustomerKYCRequest
    {
        public string CustomerId { get; set; }
        public string Nin { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string dob { get; set; }
    }

}
