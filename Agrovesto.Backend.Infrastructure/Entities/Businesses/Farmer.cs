using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{

    //public class FarmerValueChain
    //{
    //    public long Id { get; set; }
    //    public long FarmerId { get; set; }
    //    public FarmerCategoryModel Category { get; set; }
    //    public ValueChainTypeModel ValueChainType { get; set; }
    //    public string ItemName { get; set; }
    //}
    public class FarmerOrganization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long FarmerId { get; set; }   // FK to Farmer
        public Farmer Farmer { get; set; }
        public string OrganizationCode { get; set; }  // FK to Organization
       // public Organization Organization { get; set; }  
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? OnboardedByUserId { get; set; } // user/admin who onboarded
        public bool IsActive { get; set; } = true;

        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
        public DateTimeOffset? DeactivatedOn { get; set; }
    }

    public class Farmer
    {
        public string BelongToBusinessCode { get; set; }  //which business
        [StringLength(50)]
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }       
        public string UserId { get; set; }
        public User User { get; set; }   // FK is already Farmer.UserId

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }       
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? OnboardedByUserId { get; set; } //if individual farmers register, automatically assign them to Agrovesto
     //   public long BelongToCooperativeId { get; set; }
        //public string? OrganizationCode { get; set; }
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public FarmerLevel Level { get; set; }
        [NotMapped]
        public List<ValueChainSelectionDto> SelectedItems { get; set; }

        public FarmerStatus FarmerStatus { get; set; } = FarmerStatus.Active;
    }


    /// <summary>
    /// Persisted form of Farmer.SelectedItems (which is [NotMapped]).
    ///
    /// One row = one value-chain item selected by a farmer.
    /// A farmer can have many selections (e.g. Crop > Cereals > Maize,
    /// Livestock > Poultry > Broiler, etc.)
    /// </summary>
    public class FarmerValueChain
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; } = default!;

        // ── Category (top-level grouping e.g. "Crop", "Livestock") ──────
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;

        // ── Type (mid-level e.g. "Cereals", "Poultry") ──────────────────
        public long TypeId { get; set; }
        public string TypeName { get; set; } = default!;

        // ── Item (leaf e.g. "Maize", "Broiler") ─────────────────────────
        public long ItemId { get; set; }
        public string ItemName { get; set; } = default!;

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public string? CreatedByUserId { get; set; }
    }

    public class FarmerCooperative
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }
        public long CooperativeId { get; set; }
       // public MemberShip Cooperative { get; set; }
        public DateTimeOffset JoinedOn { get; set; } = DateTimeOffset.Now;
        public string? OnboardedByUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }

        public string? OrganizationCode { get; set; }
        public DateTimeOffset? DeactivatedOn { get; set; }
       
    }
    public class FarmerAgent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long FarmerId { get; set; }
        public Farmer Farmer { get; set; }
        public long AgentId { get; set; }
        public Agent Agent { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? AssignedByUserId { get; set; }
        public bool IsActive { get; set; } = true;

        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
        public string? OrganizationCode { get; set; }

        public DateTimeOffset? DeactivatedOn { get; set; }
    }


    public class BulkRowAggregate
    {
        public long RowNumber { get; set; }
        public User User { get; set; }
        public Farmer Farmer { get; set; }
        public List<ValueChainSelectionDto> ValueChains { get; set; }
    }
}
