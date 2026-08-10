using Agrovesto.Backend.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    [Index(nameof(Value), IsUnique = true)]
    public class Token
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string OwnerId { get; set; }

        [Required]
        [MaxLength(50)]
        public TokenUse Use { get; set; }

        [Required]
        [MaxLength(9)]
        public string Value { get; set; }

        public DateTimeOffset GeneratedOn { get; set; } = DateTimeOffset.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTimeOffset? LastAttemptOn { get; set; }
    }
}
