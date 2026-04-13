using System;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class BulkUploadJob
    {
        public Guid Id { get; set; }
        public string FilePath { get; set; }
        public string OrganizationCode { get; set; }
        public string CreatedByUserId { get; set; }
        public string OnboardedByUserId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalRows { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
    }
}
