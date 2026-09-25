namespace Agrovesto.Backend.Core.V2.Onboarding
{
    public enum NotificationUserRole
    {
        Farmer = 1,
        Agent = 2,
        Organization = 3,
        MemberShip = 4
    }

    public class NotificationPreferenceRequest
    {
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
    }

    public class NotificationPreferenceResponse : NotificationPreferenceRequest
    {
        public string UserId { get; set; }

        public NotificationUserRole UserRole { get; set; }

        public bool IsDefault { get; set; }

        public DateTimeOffset? LastUpdatedOn { get; set; }
    }
}
