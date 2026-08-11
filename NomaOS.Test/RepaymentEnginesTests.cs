using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using FluentAssertions;

namespace NomaOS.Test;

public class RepaymentEnginesTests
{
    [Fact]
    public void Allocate_AppliesPaymentToPenaltyThenInterestThenPrincipal()
    {
        var result = new RepaymentAllocationEngine().Allocate(100m, 20m, 30m, 100m);

        result.PenaltyPortion.Should().Be(20m);
        result.InterestPortion.Should().Be(30m);
        result.PrincipalPortion.Should().Be(50m);
    }

    [Fact]
    public void Allocate_DoesNotAllocateMoreThanOutstandingBalances()
    {
        var result = new RepaymentAllocationEngine().Allocate(100m, 5m, 10m, 20m);

        result.PenaltyPortion.Should().Be(5m);
        result.InterestPortion.Should().Be(10m);
        result.PrincipalPortion.Should().Be(20m);
    }

    [Fact]
    public void CalculatePenalty_ReturnsZeroDuringGracePeriod()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var policy = new PenaltyPolicy { GracePeriodDays = 3, FlatFee = 10m, DailyInterestRate = 0.01m, MaxPenaltyCap = 100m };
        var schedule = new RepaymentSchedule { DueDate = dueDate, Amount = 1_000m };

        new PenaltyEngine().CalculatePenalty(policy, schedule, dueDate.AddDays(3)).Should().Be(0m);
    }

    [Fact]
    public void CalculatePenalty_AppliesFlatFeeAndDailyInterestThenCapsIt()
    {
        var dueDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var policy = new PenaltyPolicy { GracePeriodDays = 2, FlatFee = 10m, DailyInterestRate = 0.01m, MaxPenaltyCap = 25m };
        var schedule = new RepaymentSchedule { DueDate = dueDate, Amount = 1_000m };

        new PenaltyEngine().CalculatePenalty(policy, schedule, dueDate.AddDays(5)).Should().Be(25m);
    }

    [Fact]
    public void GenerateSchedule_CreatesEqualMonthlyInstallmentsAndGraceFlags()
    {
        var start = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var request = new RepaymentEngineRequest
        {
            StartDate = start,
            GracePeriod = start.AddMonths(1),
            Duration = 3,
            TotalAmount = 300m,
            Frequency = RepaymentFrequency.Monthly
        };

        var schedule = new RepaymentEngine().GenerateSchedule(request);

        schedule.Should().HaveCount(3);
        schedule.Select(x => x.InstallmentNumber).Should().ContainInOrder(1, 2, 3);
        schedule.Select(x => x.Amount).Should().OnlyContain(x => x == 100m);
        schedule.Select(x => x.DueDate).Should().ContainInOrder(start.AddMonths(1), start.AddMonths(2), start.AddMonths(3));
        schedule[0].IsWithinGracePeriod.Should().BeTrue();
        schedule.Skip(1).Should().OnlyContain(x => !x.IsWithinGracePeriod);
    }

    [Theory]
    [InlineData(RepaymentFrequency.Daily, 7)]
    [InlineData(RepaymentFrequency.Weekly, 14)]
    public void GenerateSchedule_UsesExpectedDayFrequency(RepaymentFrequency frequency, int days)
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new RepaymentEngine().GenerateSchedule(new RepaymentEngineRequest
        {
            StartDate = start,
            GracePeriod = start,
            Duration = 1,
            TotalAmount = 99m,
            Frequency = frequency
        });

        schedule[0].DueDate.Should().Be(start.AddDays(days));
    }
}
