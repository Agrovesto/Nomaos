using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Payment
{
    public class AgentDashboardDto
    {
        // Top-level summary
        public long TotalCooperatives { get; set; }
        public long TotalFarmers { get; set; }
        public double TotalFarmSizeHectares { get; set; }
        public long TotalFarms { get; set; }

        // Operational overview
        public long MappedFarms { get; set; }
        public long UnmappedFarms { get; set; }

        // Farmers overview
        public long FarmersLinkedToCooperatives { get; set; }
        public long FarmersNotLinkedToCooperatives { get; set; }
    }
    public class FarmMappersDto
    {
        public string Id { get; set; }
        public string FarmerId { get; set; }
        public string AgentId { get; set; }
        public string CooperativeId { get; set; }
        public double SizeHectares { get; set; }
        public bool IsMapped { get; set; }
    }
    public class FarmerDashboardDto
    {
        public long TotalFarms { get; set; }
        public decimal TotalWalletBalance { get; set; }
        public decimal TotalInputReceived { get; set; }
        public decimal OutstandingInputDebt { get; set; }

        // Operational Overview
        public long NotMapped { get; set; }
        public long InProgressFarms { get; set; }
        public long CompletedFarms { get; set; }
    }
    public class ActivityDto
    {
        public string Description { get; set; }

        public string PerformedBy { get; set; }

        public DateTimeOffset Date { get; set; }
    }
    public class MembershipDashboardFlatDto
    {
        public string UserId { get; set; }
        public long MembershipId { get; set; }
        public string Name { get; set; }
        public string MembershipType { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
        public string Location { get; set; }

        public int TotalAgents { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalFarms { get; set; }
        public double TotalFarmSize { get; set; }

        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int NotStarted { get; set; }
    }
    public class MembershipDashboardDto
    {
        public MembershipInfoDto Membership { get; set; }
        public MemberSummaryDto Summary { get; set; }
        public FarmMappingProgressDto MappingProgress { get; set; }
        public List<MonthlyFarmerLinkDto> FarmerLinkHistogram { get; set; }
        public List<MonthlyInputDisbursementDto> FarmerInputDisbursedLinkHistogram { get; set; }

        public List<ActivityDto> RecentActivities { get; set; }
        public bool CanAddFarmer { get; set; }
    }
    public class MonthlyInputDisbursementDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int InputCount { get; set; }
    }
    public class MonthlyFarmerLinkDto
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int FarmerCount { get; set; }
    }
    public class MemberSummaryDto
    {
        public int TotalFarmers { get; set; }
        public int TotalAgents { get; set; }
        public double TotalFarmSize { get; set; }
        public int TotalFarms { get; set; }
        public int FarmsMapped { get; set; }
        public int MappingProgressPercentage { get; set; }
        public int TotalVerifiedFarmers { get; set; }
        public int TotalUnverifiedFarmers { get; set; }
    }

    public class FarmMappingProgressDto
    {
        public int TotalFarms { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int NotStarted { get; set; }
    }

    public class FarmerSummaryDto
    {
        public int TotalFarmers { get; set; }
        public int TotalFarms { get; set; }
        public int FarmsMapped { get; set; }
        public int MappingProgressPercentage { get; set; }
    }

    public class MembershipInfoDto
    {
        public string UserId { get; set; }
        public long MembershipId { get; set; }
        public string Name { get; set; }
        public string MembershipType { get; set; }
        public string Status { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public string Location { get; set; }
        public string LeaderName { get; set; }
    }

}
