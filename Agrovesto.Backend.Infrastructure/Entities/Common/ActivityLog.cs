using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public ModuleAccessed ModuleAccessed { get; set; }
        public ActivityLogAction ActionType { get; set; } = ActivityLogAction.Create;
        public string PerformedByUserId { get; set; }
        public string Description { get; set; }
        public string? ActionPerformeed { get; set; }
        public ActivityStatus Status { get; set; } = ActivityStatus.Success;
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Browser { get; set; }
        public string? AffectedRecord { get; set; } //userId
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public enum ActivityStatus
    {
        [Description("Success")]
        Success,
        [Description("Failed")]
        Failed,
        [Description("PartialUpdate")]
        PartialUpdate
    }
    public enum ModuleAccessed
    {
        [Description("Onboarding")]
        Onboarding,
        [Description("FarmMapping")]
        FarmMapping,
        [Description("FarmDocumentUpload")]
        FarmDocumentUpload,
        [Description("BVNverification")]
        BVNverification,
        [Description("NINVerification")]
        NINVerification,
        [Description("WalletManagement")]
        WalletManagement,
        [Description("InputManagement")]
        InputManagement,
        [Description("Warehousing")]
        Warehousing,
        [Description("MandE")]
        MandE,
        [Description("ProfileUpdate")]
        ProfileUpdate
    }
    public enum ActivityLogAction
    {
        [Description("Create")]
        Create,
        [Description("Update")]
        Update,
        [Description("Delete")]
        Delete,
        [Description("View")]
        View,
        [Description("View")]
        Approve,
        [Description("Reject")]
        Reject,

    }
}
