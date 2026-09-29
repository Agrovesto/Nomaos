using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class CredentialResetAttempt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public string PurposeKey { get; set; } = default!; // e.g. "PasswordReset", "PinReset"
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = default!;
    }
}
