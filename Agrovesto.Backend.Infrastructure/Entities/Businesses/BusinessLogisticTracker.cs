using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class BusinessLogisticTracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string InputReference { get; set; }
        public string BelongToBusinessCode { get; set; }
        public string LogisticName { get; set; }
        public string LogisticType { get; set; }
        public string Driver { get; set; }
        public string DriverContact
        { get; set; }
        public long UnitTransported { get; set; }
        public long UnitDelivered { get; set; }
        public string? ReceivedBy { get; set; }
        public long UnitReceived { get; set; }
        public string ReceiverUserId { get; set; }
        public string? OrganizationCode { get; set; }
    }
}
