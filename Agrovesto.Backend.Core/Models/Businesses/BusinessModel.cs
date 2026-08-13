using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.Models.Common.Constants;
using Agrovesto.Backend.Core.Enums.Wallet;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class CreateWalletRequest
    {
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }    
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public string? DOB { get; set; }
        public string TransactionPin { get; set; }
        public string? WalletProviderCode { get; set; }
        public bool IsDefault { get; set; }
    }
    public class WalletQueryParams
    {
        public string businessCode { get; set; }
        public string? UserId { get; set; }
        public string? organizationCode { get; set; }
        public long Id { get; set; }
        public string? env { get; set; }
      
    }
    public class TransactionQueryParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? transactionType { get; set; }
        public DateTimeOffset? startDate { get; set; }
        public DateTimeOffset? endDate { get; set; }
        public string? Location { get; set; }
        public string? organizationCode { get; set; }
        public string BusinessCode { get; set; }
        public string? supervisorId { get; set; }
        public string? UserId { get; set; } //logging officerId
        public long CooperativeId { get; set; }
        public long FarmerId { get; set; }
        public string? MappingStatus { get; set; } // "Mapped" or "Unmapped"
        public string? CooperativeStatus { get; set; } // "Linked" or "Not Linked"
    }
    public class ListQueryParams
    {
        public bool? IsActive { get; set; }
        public long OrganizationId { get; set; }
        public string? FilterBy { get; set; }
        public long farmerId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? UserId { get; set; }
        public long warehouseId { get; set; }
        public string? businessCode { get; set; }
        public string? organizationCode { get; set; }
        public string? sortBy { get; set; }
        public DateTimeOffset? startDate { get; set; }
        public DateTimeOffset? endDate { get; set; }
        public long? CooperativeId { get; set; }
        public long? AgentId { get; set; }
        public long Id { get; set; }
        public string? env { get; set; }
        public string? SelectedProviderCode { get; set; }
        public bool? Status { get; set; } = null;
        // filters
        public string? MappingStatus { get; set; } // "Mapped" or "Unmapped"
        public string? CooperativeStatus { get; set; } // "Linked" or "Not Linked"
        public string? PaymentStatus { get; set; } // "Linked" or "Not Linked"
        public string? SalesContext { get; set; } // "Linked" or "Not Linked"
        public string? FarmerUserId { get; set; } // "Linked" or "Not Linked"
        public long? LocationTypeId { get; set; }
        public string? LocationStatus { get; set; }
        public string? TransactionReference { get; set; }
        public DateTimeOffset? TransactionDate { get; set; }
        public WalletTransactionType? TransactionType { get; set; }
        // Not named Status - that is already taken above by the bool? flag other endpoints filter on.
        public WalletTransactionStatus? TransactionStatus { get; set; }
    }
    public class BusinessRegistrationArgs
    {
        public string? SelectedProviderCode { get; set; }
        public long SubscribedBundleId { get; set; }
        public DateTimeOffset? startDate { get; set; }
        public DateTimeOffset? endDate { get; set; }
        public string? Location { get; set; }
        public string? WarehouseManger { get; set; }
        public string? UserType { get; set; }
        public string? RoleName { get; set; }
        public string? UserId { get; set; }
        public string? DefaultPassword { get; set; }
        public string? businessCode { get; set; }
        public string? businessEmail { get; set; }
        public string? organizationCode { get; set; }
        public string? SubAccountType { get; set; }
        public string? Env { get; set; }
        public long WarehouseId { get; set; }
        public long CooperativeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public long Id { get; set; }
        public string? status { get; set; }
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
        public string? PropagateUrl { get; set; }
    }

    public class BusinessModel :Model
    {
        public string? BusinessCodeSignageSalt { get; set; }
        [Range(int.MinValue, int.MaxValue)]
        public long BusinessId { get; set; }
        [RegularExpression(ValidationRegex.BusinessCode)]
        [MaxLength(50)]
        public string? BusinessCode { get; set; }

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
        public string? BVN { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(13)]
        public string? NIN { get; set; }

        [RegularExpression(ValidationRegex.General)]
        [MaxLength(200)]
        public string Website { get; set; }
        public BusinessStatus BusinessStatus { get; set; }
        public Category Category { get; set; } = Category.Not_Live;
        public ICollection<BusinessWorkerModel> BusinessWorkers { get; set; } = new List<BusinessWorkerModel>();

        //   public ICollection<SubAccount> SubAccounts { get; set; } = new HashSet<SubAccount>(); //subaccounts are the business users
        //  public List<Product> Products { get; set; } // product maps agrovesto offerings to the business configuration of selected products
        public string BusinessLogo { get; set; }
        public string? UserId { get; set; }
        public string? BusinessCodeSignage { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public long TotalStaffCount { get; set; }
        public string? PropagateUrl { get; set; }
    }
    public enum BusinessStatus
    {
        Deactivated, Registered, AwaitingApproval, Live, ApprovalRejected,
    }
    public enum Category
    {
        Not_Live, Beginner, Consolidated
    }

    public class BusinessClaimModel : Model
    {
        public long BusinessId { get; set; }
        
        public string? BusinessCode { get; set; }

        public string BusinessName { get; set; }

    }

}
