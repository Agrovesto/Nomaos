using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Onboarding
{
    public class OrganizationResponseModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        
        public string? ContactEmail { get; set; }
        public string? OrganizationCode { get; set; }
        public string? CompanyEmail { get; set; }
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OrganizationPhone { get; set; }
      
        public string? CompanyAddress { get; set; }

       public DateTimeOffset OnboardedOn { get; set; }
        public string CompanyType { get; set; } //Business, llc, public limited, ngo
        public string? CACNumber { get; set; }

        public string RoleInOrganization { get; set; }


    }
 
    public class OrganizationOnboardingModel
    {
   
        public string? ContactEmail { get; set; }
        public string? OrganizationCode { get; set; }
        public string? CompanyEmail { get; set; }
    
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OrganizationPhone { get; set; }
        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? CompanyAddress { get; set; }

           [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]

        public CompanyType? CompanyType { get; set; } //Business, llc, public limited, ngo
        public string? CACNumber { get; set; }
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]

        public RoleInOrganization? RoleInOrganization { get; set; }


    }
    public class OrganizationOnboardingArgs
    {

        public string businessCode { get; set; }
        public string? userId { get; set; }
      
        public string userType { get; set; }
        public string env { get; set; }

        public string? onboardedByUserId { get; set; }
    }
    public enum CompanyType
    {
        Business,
        LLC,
        PLC,
        NGO
    }

    public enum RoleInOrganization
    {
        Admin,
        Director,
        COO,
        CEO,
        CTO
    }
}
