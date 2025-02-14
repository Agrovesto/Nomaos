using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.Models.Common.Constants;

namespace Agrovesto.Backend.Core.Models.Businesses
{

    public class ListQueryParams
    {
        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Location { get; set; }
        public string? UserId { get; set; }
    }
    public class BusinessRegistrationArgs
    {
        public string UserType { get; set; }
        public string RoleName { get; set; }
        public string? UserId { get; set; }
        public string? DefaultPassword { get; set; }
        public string businessCode { get; set; }
        public string SubAccountType { get; set; }
        public string Env { get; set; }
    }
    public class BusinessLoadModel
    {
        public string BusinessCode { get; set; }
        public string BusinessName { get; set; }
        public string PhoneNumber { get; set; }
        public string BusinessEmail { get; set; }
        public string BusinessLogo { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
    }

    public class BusinessModel :Model
    {
        public string? BusinessCodeSignageSalt { get; set; }
        [Range(int.MinValue, int.MaxValue)]
        public long BusinessId { get; set; }
        [RegularExpression(ValidationRegex.BusinessCode)]
        [MaxLength(50)]
        public string BusinessCode { get; set; }

        [Required]
        [RegularExpression(ValidationRegex.General)]
        [MaxLength(150)]
        public string BusinessName { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        public string ContactEmail { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        public string SupportEmail { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        public string DisputeEmail { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        [Required]
        public string BusinessEmail { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        [Required]
        public string PhoneNumber { get; set; }


        [Required]
        [RegularExpression(ValidationRegex.General)]
        [MaxLength(500)]
        public string Address { get; set; }


        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        public string BVN { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(13)]
        public string NIN { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(50)]
        public string Website { get; set; }
        public BusinessStatus BusinessStatus { get; set; }
        public Category Category { get; set; } = Category.Not_Live;
        public ICollection<BusinessWorkerModel> BusinessWorkers { get; set; } = new List<BusinessWorkerModel>();

        //   public ICollection<SubAccount> SubAccounts { get; set; } = new HashSet<SubAccount>(); //subaccounts are the business users
        //  public List<Product> Products { get; set; } // product maps agrovesto offerings to the business configuration of selected products
        public string BusinessLogo { get; set; }
        public string? UserId { get; set; }
        public string? BusinessCodeSignage { get; set; }
    }
    public enum BusinessStatus
    {
        Deactivated, Registered, AwaitingApproval, Live, ApprovalRejected,
    }
    public enum Category
    {
        Not_Live, Beginner, Consolidated
    }
}
