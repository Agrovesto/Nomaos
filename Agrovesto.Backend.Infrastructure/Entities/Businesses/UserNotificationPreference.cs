using Agrovesto.Backend.Core.V2.Onboarding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class UserNotificationPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string UserId { get; set; }

        public NotificationUserRole UserRole { get; set; }

        public bool MappingStatusUpdates { get; set; }

        public bool AgentVisitReminders { get; set; }

        public bool MembershipAnnouncements { get; set; }

        public bool DocumentApprovalUpdates { get; set; }

        public bool GeneralPlatformUpdates { get; set; }

        public bool NewFarmerAssigned { get; set; }

        public bool NewFarmCreated { get; set; }

        public bool BulkUploadCompleted { get; set; }

        public bool WalletRepaymentDue { get; set; }

        public bool Sms { get; set; }

        public bool Email { get; set; }

        public bool PushNotification { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? DateModified { get; set; }
    }
}
