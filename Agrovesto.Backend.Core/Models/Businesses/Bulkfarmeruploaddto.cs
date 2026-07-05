using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    // Input row parsed from each Excel row
    public class BulkMembershipRowDto
    {
        public int RowNumber { get; set; }
        public string MembershipName { get; set; }
        public string MembershipType { get; set; }
        public string State { get; set; }
        public string LGA { get; set; }
        public string Location { get; set; }
        public string LeaderName { get; set; }
        public string LeaderPhoneNumber { get; set; }

        // Resolved at upload time from request context / headers
       // public string BusinessCode { get; set; }
        //public string? OrganizationCode { get; set; }
        //public long? AgentId { get; set; }
        //public string OnboardedById { get; set; }
    }

    public class BulkUploadResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<MembershipRowError> Errors { get; set; } = new();
    }

    public class MembershipRowError
    {
        public int RowNumber { get; set; }
        public string MembershipName { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    /// Represents a single farmer row from the bulk upload file.
    /// All relationship fields are optional — a farmer may belong to
    /// an organisation, cooperative, and/or agent, or none at all.
    /// </summary>
    public class BulkFarmerRowDto
    {
        // ── Core identity ──────────────────────────────────────────────
        public string PhoneNumber { get; set; } = default!;
        public string? Email { get; set; }
        public string Name { get; set; } = default!;
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }

        // ── Location ───────────────────────────────────────────────────
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }

        // ── Agric profile ──────────────────────────────────────────────
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }

        // ── Relationships (all optional) ───────────────────────────────
       // public string? OrganizationCode { get; set; }   // → UserOrganization + FarmerOrganization
        //public long? CooperativeId { get; set; }         // → FarmerCooperative
        //public long? AgentId { get; set; }               // → FarmerAgent
                                                         // ── Value chain selections (optional) ─────────────────────────
        /// <summary>
        /// Each entry = one (CategoryId, TypeId, ItemId) triple.
        /// Populated by the CSV parser from a JSON cell or flat columns.
        /// </summary>
        public List<ValueChainSelectionDto> SelectedItems { get; set; } = new();
        // ── Upload metadata ────────────────────────────────────────────
        /// <summary>1-based row number from the source file; used in error reporting.</summary>
        public int RowIndex { get; set; }
    }

    /// <summary>Aggregated result returned to the caller after a bulk upload attempt.</summary>
    public class BulkFarmerUploadResultDto
    {
        public int TotalRows { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<FarmerRowErrorDto> Errors { get; set; } = new();
    }

    /// <summary>Details of a single failed row.</summary>
    public class FarmerRowErrorDto
    {
        public int RowIndex { get; set; }
        public string PhoneNumber { get; set; } = default!;
        public string Reason { get; set; } = default!;
    }
}
