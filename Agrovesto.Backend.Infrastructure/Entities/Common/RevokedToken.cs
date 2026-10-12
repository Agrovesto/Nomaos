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
}
