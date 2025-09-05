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
    public class NINDataModel
    {

        public string? image { get; set; } = null;
        public string? number { get; set; } = null;
    }

    public class GlobalResponse<T, V>
    {
        public bool status { get; set; }
        public string detail { get; set; }
        public string response_code { get; set; }
        public T data { get; set; }
        public V verification { get; set; }


    }
    public class NINDataVerificationResponse<T, V>
    {
        public bool status { get; set; }
        public string detail { get; set; }
        public string response_code { get; set; }
        public T nin_data { get; set; }
        public V verification { get; set; }
    }

    public class NINData
    {
        public string birthcountry { get; set; }
        public string birthdate { get; set; }
        public string birthlga { get; set; }
        public string centralID { get; set; }
        public string educationallevel { get; set; }
        public string email { get; set; }
        public string middlename { get; set; }
        public string emplymentstatus { get; set; }
        public string firstname { get; set; }
        public string gender { get; set; }
        public string heigth { get; set; }
        public string maritalstatus { get; set; }
        public string nin { get; set; }
        public string nok_address1 { get; set; }
        public string nok_address2 { get; set; }
        public string nok_firstname { get; set; }
        public string nok_lga { get; set; }
        public string nok_middlename { get; set; }
        public string nok_postalcode { get; set; }
        public string nok_state { get; set; }
        public string nok_surname { get; set; }
        public string nok_town { get; set; }
        public string nspokenlang { get; set; }
        public string ospokenlang { get; set; }
        public string pfirstname { get; set; }
        public string photo { get; set; }
        public string pmiddlename { get; set; }
        public string profession { get; set; }
        public string psurname { get; set; }
        public string religion { get; set; }
        public string residence_address { get; set; }
        public string residence_Town { get; set; }
        public string residence_lga { get; set; }
        public string residence_state { get; set; }
        public string residencestatus { get; set; }
        public string self_origin_lga { get; set; }
        public string self_origin_place { get; set; }
        public string self_origin_state { get; set; }
        public string signature { get; set; }
        public string surname { get; set; }
        public string telephoneno { get; set; }
        public string title { get; set; }
    }

}
