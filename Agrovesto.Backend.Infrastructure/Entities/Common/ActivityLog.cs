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
        public ActivityType ActivityType { get; set; }
        public string PerformedByUserId { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }

    public enum ActivityType
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
}
