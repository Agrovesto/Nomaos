using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace NomaOS.Test;

public class AgentProfileTests
{
    private const string UserId = "AGENT-1";

    private static DataContext CreateContext() =>
        new(new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AgentRepository CreateRepository(DataContext context) => new(context);

    private static Agent BuildAgent(bool isActive = true) => new()
    {
        UserId = UserId,
        Name = "Ada Agent",
        State = "Kano",
        Lga = "Nassarawa",
        Ward = "Gama",
        Location = "Kofar Mata",
        IsActive = isActive,
        OnBoardedOn = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task ProfileReturnsStatusDateRegisteredNameAndEveryLocationField()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent());
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.Should().NotBeNull();
        profile.UserId.Should().Be(UserId);
        profile.Name.Should().Be("Ada Agent");
        profile.Status.Should().Be("Active");
        profile.DateRegistered.Should().Be(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        profile.Location.State.Should().Be("Kano");
        profile.Location.Lga.Should().Be("Nassarawa");
        profile.Location.Ward.Should().Be("Gama");
        profile.Location.Community.Should().Be("Kofar Mata");
    }

    [Fact]
    public async Task StatusReflectsADeactivatedAgent()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent(isActive: false));
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.Status.Should().Be("Inactive");
    }

    [Fact]
    public async Task ProfileCarriesTheStoredImageKeyWhenOneExists()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent());
        await context.FarmerImages.AddAsync(new FarmerImages
        {
            FarmerUserId = UserId,
            ImageName = "agent.jpg",
            IMageKey = "images/agent-1.jpg",
            BelongToBusinessCode = "BIZ1"
        });
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.AgentImage.Should().Be("images/agent-1.jpg");
    }

    [Fact]
    public async Task ProfileLeavesTheImageNullWhenTheAgentHasNoneStored()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent());
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.AgentImage.Should().BeNull();
    }

    [Fact]
    public async Task ProfileIsNullForAUserWhoIsNotAnAgent()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent());
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile("SOMEONE-ELSE");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task AnotherAgentsImageIsNotReturned()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(BuildAgent());
        await context.FarmerImages.AddAsync(new FarmerImages
        {
            FarmerUserId = "OTHER-USER",
            ImageName = "other.jpg",
            IMageKey = "images/other.jpg",
            BelongToBusinessCode = "BIZ1"
        });
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.AgentImage.Should().BeNull();
    }

    [Fact]
    public async Task LocationFieldsStayNullWhenTheAgentRecordHasNoneSet()
    {
        using var context = CreateContext();
        await context.Agents.AddAsync(new Agent { UserId = UserId, Name = "Sparse Agent" });
        await context.SaveChangesAsync();

        var profile = await CreateRepository(context).GetAgentProfile(UserId);

        profile.Location.Should().NotBeNull();
        profile.Location.State.Should().BeNull();
        profile.Location.Lga.Should().BeNull();
        profile.Location.Ward.Should().BeNull();
        profile.Location.Community.Should().BeNull();
    }
}
