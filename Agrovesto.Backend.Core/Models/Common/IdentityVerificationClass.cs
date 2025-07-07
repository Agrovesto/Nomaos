using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    internal class IdentityVerificationClass
    {
    }
    public class BVNDataModel
    {
        public string number { get; set; }
        public string? image { get; set; } = null;
    }
    public class Verification
    {
        public string status { get; set; }
        public string reference { get; set; }
    }

    public class BVNVerificationResponse<T, V>
    {
        public bool status { get; set; }
        public string detail { get; set; }
        public string response_code { get; set; }
        public T data { get; set; }
        public V verification { get; set; }
    }
    public class BVN_data
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string dateOfBirth { get; set; }
        public string phoneNumber1 { get; set; }
        public string phoneNumber2 { get; set; }
        public string registrationDate { get; set; }
        public string enrollmentBank { get; set; }
        public string enrollmentBranch { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string levelOfAccount { get; set; }
        public string lgaOfOrigin { get; set; }
        public string lgaOfResidence { get; set; }
        public string maritalStatus { get; set; }
        public string nin { get; set; }
        public string nameOnCard { get; set; }
        public string nationality { get; set; }
        public string residentialAddress { get; set; }
        public string stateOfOrigin { get; set; }
        public string stateOfResidence { get; set; }
        public string title { get; set; }
        public string watchListed { get; set; }
        public string bvn { get; set; }
        public string base64Image { get; set; }
    }

}
