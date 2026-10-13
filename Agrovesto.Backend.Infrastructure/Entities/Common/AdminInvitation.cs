using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    // Invitations (Sprint Task 13 will write to this; Task 12 only reads it)
    public class AdminInvitation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = default!;
        public string TokenHash { get; set; } = default!;
        public DateTimeOffset SentAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string InvitedByUserId { get; set; } = default!;
    }

    // Generates ADM000124-style codes safely under concurrency (MySQL auto-increment)
    public class AdminCodeSequence { public long Id { get; set; } }

    public class AdminPanelOptions
    {
        public long BusinessId { get; set; }          // business that owns admin-panel roles
        public string PortalBaseUrl { get; set; } = default!;
        public int InviteExpiryDays { get; set; } = 7;
    }
}
