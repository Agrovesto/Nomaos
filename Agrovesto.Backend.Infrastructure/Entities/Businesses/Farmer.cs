using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;

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
        public string OrganizationCode { get; set; }  // FK to Organization

        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? OnboardedByUserId { get; set; } // user/admin who onboarded
    }

    public class Farmer
    {
        public string BelongToBusinessCode { get; set; }  //which business
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }       
        public string UserId { get; set; }
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
    }
    public class FarmerCooperative
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long FarmerId { get; set; }
        public long CooperativeId { get; set; }

        public DateTimeOffset JoinedOn { get; set; } = DateTimeOffset.Now;
        public string? OnboardedByUserId { get; set; }
    }
    public class FarmerAgent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long FarmerId { get; set; }
        public long AgentId { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? AssignedByUserId { get; set; }
    }

}
