using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class ProduceCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ProduceSubCategory> ProduceSubCaegories { get; set; }
    }
    public class ProduceSubCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; }    
        public string Description { get; set; }

        public long ProduceCategoryId { get; set; }
        public ProduceCategory ProduceCategory { get; set; }
    }

    public class AgriculturalProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string BusinessCode { get; set; }
        public string OrganizationCode { get; set; }
        public string Name { get; set; }
        public ProduceCategory ProduceCategory { get; set; }
        public ProduceSubCategory ProduceSubCategory { get; set; }
    }
}
