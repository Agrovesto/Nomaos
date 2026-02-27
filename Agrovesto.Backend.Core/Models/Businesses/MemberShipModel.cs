using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class MemberShipTypeModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
    }
    public class MemberShipRequestModel
    {
        public long Id { get; set; }
        public bool IsLeaderAFarmer { get; set; }
        public long MemberShipTypeId { get; set; }
        public string Name { get; set; }
        public string? Password { get; set; }
        public string? Pin { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string? ContactPerson { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MembershipLeaderFirstName { get; set; }
        public string? MembershipLeaderMiddleName { get; set; }
        public string? MembershipLeaderLastName { get; set; }
        public string? MembershipLeaderGender { get; set; }
    }
    public class MemberShipResponseModel
    {
        public bool IsLeaderAFarmer { get; set; }
        public long MemberShipTypeId { get; set; }
        public string Name { get; set; }
        public string? MembershipType { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }
        public string BelongToBusinessCode { get; set; }  //which business
        public string? OrganizationCode { get; set; }  //which business
        public string? MembershipLeaderFirstName { get; set; }
        public string? MembershipLeaderMiddleName { get; set; }
        public string? MembershipLeaderLastName { get; set; }
        public string? MembershipLeaderGender { get; set; }
        public bool HasbeenAssignedAgent { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string IsAssignedToAgentId { get; set; }
        public long TotalFarmers { get; set; }
        public string? OnboardedById { get; set; }
        public string Tier { get; set; }
 }

    public class MemberShipModel
    {
        public bool IsLeaderAFarmer { get; set; }
        public long MemberShipTypeId { get; set; }
        public string Name { get; set; }
        public string? Password { get; set; }
        public string? Pin { get; set; }
        public string? MembershipType { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }
        public string BelongToBusinessCode { get; set; }  //which business
        public string? OrganizationCode { get; set; }  //which business
        public string? MembershipLeaderFirstName { get; set; }
        public string? MembershipLeaderMiddleName { get; set; }
        public string? MembershipLeaderLastName { get; set; }
        public string? MembershipLeaderGender { get; set; }
        public bool HasbeenAssignedAgent { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string IsAssignedToAgentId { get; set; }
        public long TotalFarmers { get; set; }
        public List<FarmerModel> Farmers { get; set; }
        public WalletModel Wallet { get; set; }
        public string? OnboardedById { get; set; }
        public string Tier { get; set; }

        public List<long> AgentIds { get; set; } //= new List<AgentModel>();
    }
}
