using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using FluentAssertions;

namespace NomaOS.Test;

public class ConfigurationValidatorTests
{
    [Fact]
    public void ValidateConfiguration_AcceptsCompatibleConfiguration()
    {
        var result = ConfigurationValidator.ValidateConfiguration(CreateValidConfiguration());

        result.IsValid.Should().BeTrue();
        result.Message.Should().Be("Configuration is valid.");
    }

    [Fact]
    public void ValidateConfiguration_RejectsNullConfiguration()
    {
        var result = ConfigurationValidator.ValidateConfiguration(null!);

        result.Should().Be((false, "Configuration cannot be null."));
    }

    [Theory]
    [InlineData("{invalid json", "Invalid JSON format")]
    [InlineData("{}", "missing or empty")]
    [InlineData("{\"MemberShip\":50}", "must equal 100%")]
    [InlineData("{\"Unknown\":100}", "Invalid contributors")]
    public void ValidateConfiguration_RejectsInvalidContributionSplits(string split, string expectedMessage)
    {
        var config = CreateValidConfiguration();
        config.SubscriptionConfig.ContributionSplitJson = split;

        var result = ConfigurationValidator.ValidateConfiguration(config);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public void ValidateConfiguration_DoesNotRequireSplitWhenEquityIsNotRequired()
    {
        var config = CreateValidConfiguration();
        config.SubscriptionConfig.RequiresEquity = false;
        config.SubscriptionConfig.ContributionSplitJson = null;

        ConfigurationValidator.ValidateConfiguration(config).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 12, true, false, true, "Cash", "greater than zero")]
    [InlineData(100, 0, true, false, true, "Cash", "at least one month")]
    [InlineData(100, 12, true, true, true, "Cash", "both percentage-based and static fee")]
    [InlineData(100, 12, false, false, false, "Cash", "At least one repayment option")]
    [InlineData(100, 12, true, false, true, "Crypto", "Invalid repayment mode")]
    public void ValidateConfiguration_RejectsInvalidRepaymentRules(decimal percentage, long duration, bool percentageEnabled, bool staticFeeEnabled, bool installments, string mode, string expectedMessage)
    {
        var config = CreateValidConfiguration();
        config.RepaymentConfig.ExpectedRepaymentPercentage = percentage;
        config.RepaymentConfig.RepaymentDurationInMonths = duration;
        config.RepaymentConfig.IsRepaymentInPercentage = percentageEnabled;
        config.RepaymentConfig.IsRepaymentStaticFee = staticFeeEnabled;
        config.RepaymentConfig.AllowInstallments = installments;
        config.RepaymentConfig.RepaymentMode = mode;

        var result = ConfigurationValidator.ValidateConfiguration(config);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public void ValidateConfiguration_RejectsDisbursementThatDoesNotMatchSubscriptionTarget()
    {
        var config = CreateValidConfiguration();
        config.DisbursementConfig.DisbursementTarget = "MemberShip";

        var result = ConfigurationValidator.ValidateConfiguration(config);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("doesn't allow cooperative subscription");
    }

    private static InputFinancingConfigurationModel CreateValidConfiguration() => new()
    {
        SubscriptionConfig = new SubscriptionConfigModel
        {
            RequiresEquity = true,
            CooperativeContributes = true,
            ContributionSplitJson = "{\"MemberShip\":100}",
            AllowFarmerSelfSubscription = true
        },
        RepaymentConfig = new RepaymentConfigModel
        {
            ExpectedRepaymentPercentage = 100,
            RepaymentDurationInMonths = 12,
            IsRepaymentInPercentage = true,
            RepaymentMode = "Cash"
        },
        DisbursementConfig = new DisbursementConfigModel
        {
            DisbursementMode = "Wallet",
            DisbursementTarget = "Farmer"
        }
    };
}
