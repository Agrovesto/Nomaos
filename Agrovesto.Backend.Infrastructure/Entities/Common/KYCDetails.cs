using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public  class KYCDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTimeOffset LoggedOn { get; set; } = DateTime.Now;
        public string? VerifiedBy { get; set; }      
        public string Birthcountry { get; set; }       
        public string Birthlga { get; set; }
        public string Birthstate { get; set; }
        public string Birthdate { get; set; }
        public string Gender { get; set; }
        public string Maidenname { get; set; }
        public string Middlename { get; set; }
        public string Nin { get; set; }
        public string Firstname { get; set; }
        public string Surname { get; set; }
        public string Phonenumber { get; set; }
        public string Residentialaddress { get; set; }
        
        //public string educationallevel { get; set; }
        //public string email { get; set; }
        //public string emplymentstatus { get; set; }
        
        
        //public string heigth { get; set; }
        //public string maritalstatus { get; set; }
        
        public string Photourl { get; set; }
        //public string religion { get; set; }
        //public string residence_address { get; set; }
        //public string residence_Town { get; set; }
        //public string residence_lga { get; set; }
        //public string residence_state { get; set; }
        //public string residencestatus { get; set; }
        
        //public string title { get; set; }
    }
}
