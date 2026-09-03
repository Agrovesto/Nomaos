using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace NomaOS.Test;

public class NotificationPreferenceTests
{
    private const string UserId = "FARMER-1";

    private static DataContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new DataContext(options);
    }

    private static OnboardingRepository CreateRepository(DataContext context) =>
        new(context, null!, null!, null!, null!, NullLogger<OnboardingRepository>.Instance);

    private static NotificationPreferenceRequest AllEnabled() => new()
    {
        MappingStatusUpdates = true,
        AgentVisitReminders = true,
        MembershipAnnouncements = true,
        DocumentApprovalUpdates = true,
        GeneralPlatformUpdates = true,
        Sms = true,
        Email = true,
        InAppNotification = true
    };

    [Fact]
    public async Task GetReturnsEverythingDisabledWhenTheFarmerHasNeverSetAPreference()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var preference = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);

        preference.UserId.Should().Be(UserId);
        preference.IsDefault.Should().BeTrue();
        preference.MappingStatusUpdates.Should().BeFalse();
        preference.AgentVisitReminders.Should().BeFalse();
        preference.MembershipAnnouncements.Should().BeFalse();
        preference.DocumentApprovalUpdates.Should().BeFalse();
        preference.GeneralPlatformUpdates.Should().BeFalse();
        preference.Sms.Should().BeFalse();
        preference.Email.Should().BeFalse();
        preference.InAppNotification.Should().BeFalse();
        preference.LastUpdatedOn.Should().BeNull();
    }

    [Fact]
    public async Task ANewPreferenceRowStartsWithEveryFlagDisabled()
    {
        using var context = CreateContext();

        var entity = new UserNotificationPreference { UserId = UserId };

        entity.MappingStatusUpdates.Should().BeFalse();
        entity.AgentVisitReminders.Should().BeFalse();
        entity.MembershipAnnouncements.Should().BeFalse();
        entity.DocumentApprovalUpdates.Should().BeFalse();
        entity.GeneralPlatformUpdates.Should().BeFalse();
        entity.Sms.Should().BeFalse();
        entity.Email.Should().BeFalse();
        entity.InAppNotification.Should().BeFalse();
    }

    [Fact]
    public async Task SaveStoresEveryCategoryAndChannelExactlyAsSupplied()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var model = new NotificationPreferenceRequest
        {
            MappingStatusUpdates = true,
            AgentVisitReminders = false,
            MembershipAnnouncements = true,
            DocumentApprovalUpdates = false,
            GeneralPlatformUpdates = true,
            Sms = false,
            Email = true,
            InAppNotification = false
        };

        var saved = await repository.SaveNotificationPreference(UserId, model);

        saved.IsDefault.Should().BeFalse();
        saved.MappingStatusUpdates.Should().BeTrue();
        saved.AgentVisitReminders.Should().BeFalse();
        saved.MembershipAnnouncements.Should().BeTrue();
        saved.DocumentApprovalUpdates.Should().BeFalse();
        saved.GeneralPlatformUpdates.Should().BeTrue();
        saved.Sms.Should().BeFalse();
        saved.Email.Should().BeTrue();
        saved.InAppNotification.Should().BeFalse();

        var stored = await context.UserNotificationPreferences.SingleAsync(x => x.UserId == UserId);
        stored.AgentVisitReminders.Should().BeFalse();
        stored.Sms.Should().BeFalse();
        stored.InAppNotification.Should().BeFalse();
    }

    [Fact]
    public async Task SavedPreferenceIsReturnedByASubsequentGet()
    {
        var databaseName = Guid.NewGuid().ToString();

        using (var context = CreateContext(databaseName))
        {
            var model = AllEnabled();
            model.Sms = false;
            model.GeneralPlatformUpdates = false;

            await CreateRepository(context).SaveNotificationPreference(UserId, model);
        }

        using (var context = CreateContext(databaseName))
        {
            var preference = await CreateRepository(context).GetNotificationPreference(UserId, NotificationUserRole.Farmer);

            preference.IsDefault.Should().BeFalse();
            preference.Sms.Should().BeFalse();
            preference.GeneralPlatformUpdates.Should().BeFalse();
            preference.Email.Should().BeTrue();
            preference.LastUpdatedOn.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task SavingTwiceUpdatesTheSameRowInsteadOfCreatingASecond()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, AllEnabled());

        var second = AllEnabled();
        second.Email = false;
        second.MappingStatusUpdates = false;

        var updated = await repository.SaveNotificationPreference(UserId, second);

        updated.Email.Should().BeFalse();
        updated.MappingStatusUpdates.Should().BeFalse();

        var rows = await context.UserNotificationPreferences.CountAsync(x => x.UserId == UserId);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task UpdatingAnExistingPreferenceStampsTheModifiedDate()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, AllEnabled());

        var afterCreate = await context.UserNotificationPreferences.AsNoTracking()
            .SingleAsync(x => x.UserId == UserId);
        afterCreate.DateModified.Should().BeNull();

        await repository.SaveNotificationPreference(UserId, AllEnabled());

        var afterUpdate = await context.UserNotificationPreferences.AsNoTracking()
            .SingleAsync(x => x.UserId == UserId);
        afterUpdate.DateModified.Should().NotBeNull();
        afterUpdate.CreatedOn.Should().Be(afterCreate.CreatedOn);
    }

    [Fact]
    public async Task PreferencesAreKeptSeparatePerFarmer()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var first = AllEnabled();
        first.Sms = false;

        var second = AllEnabled();
        second.Email = false;

        await repository.SaveNotificationPreference(UserId, first);
        await repository.SaveNotificationPreference("FARMER-2", second);

        var one = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);
        var two = await repository.GetNotificationPreference("FARMER-2", NotificationUserRole.Farmer);

        one.Sms.Should().BeFalse();
        one.Email.Should().BeTrue();
        two.Sms.Should().BeTrue();
        two.Email.Should().BeFalse();
    }

    private static AgentNotificationPreferenceRequest AgentAllEnabled() => new()
    {
        NewFarmerAssigned = true,
        NewFarmCreated = true,
        BulkUploadCompleted = true,
        WalletRepaymentDue = true,
        Sms = true,
        Email = true,
        InAppNotification = true
    };

    [Fact]
    public async Task AgentSaveStoresEveryAgentCategoryExactlyAsSupplied()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var saved = await repository.SaveAgentNotificationPreference(UserId, new AgentNotificationPreferenceRequest
        {
            NewFarmerAssigned = true,
            NewFarmCreated = false,
            BulkUploadCompleted = true,
            WalletRepaymentDue = false
        });

        saved.NewFarmerAssigned.Should().BeTrue();
        saved.NewFarmCreated.Should().BeFalse();
        saved.BulkUploadCompleted.Should().BeTrue();
        saved.WalletRepaymentDue.Should().BeFalse();

        var stored = await context.UserNotificationPreferences.SingleAsync(x => x.UserId == UserId);
        stored.NewFarmerAssigned.Should().BeTrue();
        stored.BulkUploadCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ADualRoleUserGetsOneRowPerRole()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, AllEnabled());
        await repository.SaveAgentNotificationPreference(UserId, AgentAllEnabled());

        var farmerRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);
        var agentRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Agent);

        farmerRow.UserRole.Should().Be(NotificationUserRole.Farmer);
        farmerRow.MappingStatusUpdates.Should().BeTrue();
        farmerRow.NewFarmerAssigned.Should().BeFalse();

        agentRow.UserRole.Should().Be(NotificationUserRole.Agent);
        agentRow.NewFarmerAssigned.Should().BeTrue();
        agentRow.MappingStatusUpdates.Should().BeFalse();

        var rows = await context.UserNotificationPreferences.CountAsync(x => x.UserId == UserId);
        rows.Should().Be(2);
    }

    [Fact]
    public async Task EachRoleRowIsStampedWithItsOwnUserRole()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, AllEnabled());
        await repository.SaveAgentNotificationPreference(UserId, AgentAllEnabled());

        var roles = await context.UserNotificationPreferences
            .Where(x => x.UserId == UserId)
            .Select(x => x.UserRole)
            .ToListAsync();

        roles.Should().BeEquivalentTo(new[] { NotificationUserRole.Farmer, NotificationUserRole.Agent });
    }

    [Fact]
    public async Task AnUnsetPreferenceReportsTheRoleItWasAskedFor()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var farmerRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);
        var agentRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Agent);

        farmerRow.IsDefault.Should().BeTrue();
        farmerRow.UserRole.Should().Be(NotificationUserRole.Farmer);
        agentRow.IsDefault.Should().BeTrue();
        agentRow.UserRole.Should().Be(NotificationUserRole.Agent);
    }

    [Fact]
    public async Task SavingFarmerPreferencesLeavesTheAgentRowUntouched()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveAgentNotificationPreference(UserId, AgentAllEnabled());

        var farmerOnly = new NotificationPreferenceRequest { MappingStatusUpdates = true };
        await repository.SaveNotificationPreference(UserId, farmerOnly);

        var agentRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Agent);

        agentRow.NewFarmerAssigned.Should().BeTrue();
        agentRow.NewFarmCreated.Should().BeTrue();
        agentRow.BulkUploadCompleted.Should().BeTrue();
        agentRow.WalletRepaymentDue.Should().BeTrue();
    }

    [Fact]
    public async Task SavingAgentPreferencesLeavesTheFarmerRowUntouched()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, AllEnabled());

        await repository.SaveAgentNotificationPreference(UserId, new AgentNotificationPreferenceRequest
        {
            NewFarmerAssigned = true,
            Sms = true,
            Email = true,
            InAppNotification = true
        });

        var farmerRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);

        farmerRow.MappingStatusUpdates.Should().BeTrue();
        farmerRow.AgentVisitReminders.Should().BeTrue();
        farmerRow.MembershipAnnouncements.Should().BeTrue();
        farmerRow.DocumentApprovalUpdates.Should().BeTrue();
        farmerRow.GeneralPlatformUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task AnAgentCanSetTheDeliveryChannelsTheSameWayAFarmerCan()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var saved = await repository.SaveAgentNotificationPreference(UserId, new AgentNotificationPreferenceRequest
        {
            NewFarmerAssigned = true,
            Sms = true,
            Email = false,
            InAppNotification = true
        });

        saved.Sms.Should().BeTrue();
        saved.Email.Should().BeFalse();
        saved.InAppNotification.Should().BeTrue();

        var stored = await context.UserNotificationPreferences.SingleAsync(x => x.UserId == UserId);
        stored.Sms.Should().BeTrue();
        stored.Email.Should().BeFalse();
        stored.InAppNotification.Should().BeTrue();
    }

    [Fact]
    public async Task EachRoleRowCarriesItsOwnChannelChoices()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var farmer = AllEnabled();
        farmer.Sms = true;
        farmer.Email = true;
        farmer.InAppNotification = true;
        await repository.SaveNotificationPreference(UserId, farmer);

        var agent = AgentAllEnabled();
        agent.Sms = false;
        agent.Email = true;
        agent.InAppNotification = false;
        await repository.SaveAgentNotificationPreference(UserId, agent);

        var farmerRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);
        var agentRow = await repository.GetNotificationPreference(UserId, NotificationUserRole.Agent);

        farmerRow.Sms.Should().BeTrue();
        farmerRow.InAppNotification.Should().BeTrue();
        agentRow.Sms.Should().BeFalse();
        agentRow.Email.Should().BeTrue();
        agentRow.InAppNotification.Should().BeFalse();
    }

    [Fact]
    public async Task AgentSaveTogglesAnExistingRowRatherThanCreatingASecond()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveAgentNotificationPreference(UserId, AgentAllEnabled());

        var off = new AgentNotificationPreferenceRequest();
        var updated = await repository.SaveAgentNotificationPreference(UserId, off);

        updated.NewFarmerAssigned.Should().BeFalse();
        updated.NewFarmCreated.Should().BeFalse();
        updated.BulkUploadCompleted.Should().BeFalse();
        updated.WalletRepaymentDue.Should().BeFalse();

        var rows = await context.UserNotificationPreferences.CountAsync(x => x.UserId == UserId);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task UnsetPreferenceReportsTheAgentCategoriesAsDisabled()
    {
        using var context = CreateContext();

        var preference = await CreateRepository(context).GetNotificationPreference(UserId, NotificationUserRole.Agent);

        preference.IsDefault.Should().BeTrue();
        preference.NewFarmerAssigned.Should().BeFalse();
        preference.NewFarmCreated.Should().BeFalse();
        preference.BulkUploadCompleted.Should().BeFalse();
        preference.WalletRepaymentDue.Should().BeFalse();
    }

    [Fact]
    public async Task TurningEverythingOffIsPersistedRatherThanTreatedAsUnset()
    {
        using var context = CreateContext();
        var repository = CreateRepository(context);

        await repository.SaveNotificationPreference(UserId, new NotificationPreferenceRequest());

        var preference = await repository.GetNotificationPreference(UserId, NotificationUserRole.Farmer);

        preference.IsDefault.Should().BeFalse();
        preference.MappingStatusUpdates.Should().BeFalse();
        preference.AgentVisitReminders.Should().BeFalse();
        preference.MembershipAnnouncements.Should().BeFalse();
        preference.DocumentApprovalUpdates.Should().BeFalse();
        preference.GeneralPlatformUpdates.Should().BeFalse();
        preference.Sms.Should().BeFalse();
        preference.Email.Should().BeFalse();
        preference.InAppNotification.Should().BeFalse();
    }
}
