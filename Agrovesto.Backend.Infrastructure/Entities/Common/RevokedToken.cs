using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class RevokedToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Jti { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; } 
        public DateTimeOffset RevokedOn { get; set; } = DateTimeOffset.UtcNow;
    }

    public class RefreshToken
    {
        public long Id { get; set; }
        public string UserId { get; set; } = default!;
        public long BusinessId { get; set; }              // match the type on UserBusiness.BusinessId
        public string TokenHash { get; set; } = default!;
        public string FamilyId { get; set; } = default!;
        public DateTime CreatedOn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? RevokedReason { get; set; }
        public bool RequiresCredentialChange { get; set; }
        public string? CreatedByIp { get; set; }
        public bool IsAdminSession { get; set; }
    }

}
