namespace Agrovesto.Backend.Core.V2.Onboarding
{
    public class NotificationPreferenceRequest
    {
        public bool MappingStatusUpdates { get; set; }

        public bool AgentVisitReminders { get; set; }

        public bool MembershipAnnouncements { get; set; }

        public bool DocumentApprovalUpdates { get; set; }

        public bool GeneralPlatformUpdates { get; set; }

        public bool Sms { get; set; }

        public bool Email { get; set; }

        public bool InAppNotification { get; set; }
    }

    public class NotificationPreferenceResponse : NotificationPreferenceRequest
    {
        public string UserId { get; set; }

        public bool IsDefault { get; set; }

        public DateTimeOffset? LastUpdatedOn { get; set; }
    }
}
