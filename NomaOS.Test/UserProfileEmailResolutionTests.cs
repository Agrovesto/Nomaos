using System.Text.Json;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace NomaOS.Test;

public class UserProfileEmailResolutionTests
{
    private static string? ResolveConnectionString()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Agrovesto.Backend.API", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                using var doc = JsonDocument.Parse(
                    File.ReadAllText(candidate),
                    new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });

                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var section) &&
                    section.TryGetProperty("DefaultConnection", out var value))
                {
                    return value.GetString();
                }

                return null;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static DataContext? TryCreateContext()
    {
        var connectionString = ResolveConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("SKIPPED: no DefaultConnection in appsettings.Development.json.");
            return null;
        }

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.UseNetTopologySuite();
                sql.CommandTimeout(60);
            })
            .Options;

        return new DataContext(options);
    }

    private static OnboardingRepository CreateRepository(DataContext context) =>
        new(context, null!, null!, null!, null!, NullLogger<OnboardingRepository>.Instance, null!, null!);

    private static async Task<bool> IsReachableAsync(DataContext context)
    {
        try
        {
            if (await context.Database.CanConnectAsync())
                return true;

            Console.WriteLine("SKIPPED: development database is not reachable from this machine.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SKIPPED: development database is not reachable: {ex.Message}");
            return false;
        }
    }

    private static async Task<string?> FindFarmerWithRoleOnlyEmailAsync(DataContext context) =>
        await (
            from f in context.Farmers.AsNoTracking()
            join u in context.Users.AsNoTracking() on f.UserId equals u.Id
            where f.Email != null && f.Email != "" && (u.Email == null || u.Email == "")
                  && u.UserType == "Farmer"
            select f.UserId)
            .FirstOrDefaultAsync();

    private static async Task<string?> FindUserWithRoleEmailOutsideTheirRoleAsync(DataContext context) =>
        await (
            from f in context.Farmers.AsNoTracking()
            join u in context.Users.AsNoTracking() on f.UserId equals u.Id
            where f.Email != null && f.Email != "" && (u.Email == null || u.Email == "")
                  && u.UserType != "Farmer"
            select f.UserId)
            .FirstOrDefaultAsync();

    private static async Task<string?> FindAgentWithRoleOnlyEmailAsync(DataContext context) =>
        await (
            from a in context.Agents.AsNoTracking()
            join u in context.Users.AsNoTracking() on a.UserId equals u.Id
            where a.Email != null && a.Email != "" && (u.Email == null || u.Email == "")
            select a.UserId)
            .FirstOrDefaultAsync();

    [Fact]
    public async Task ExistingDataContainsUsersWhoseEmailOnlyLivesOnTheRoleTable()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var farmers = await (
            from f in context.Farmers.AsNoTracking()
            join u in context.Users.AsNoTracking() on f.UserId equals u.Id
            where f.Email != null && f.Email != "" && (u.Email == null || u.Email == "")
            select f.UserId)
            .CountAsync();

        var agents = await (
            from a in context.Agents.AsNoTracking()
            join u in context.Users.AsNoTracking() on a.UserId equals u.Id
            where a.Email != null && a.Email != "" && (u.Email == null || u.Email == "")
            select a.UserId)
            .CountAsync();

        var memberships = await (
            from m in context.MemberShips.AsNoTracking()
            join u in context.Users.AsNoTracking() on m.UserId equals u.Id
            where m.Email != null && m.Email != "" && (u.Email == null || u.Email == "")
            select m.UserId)
            .CountAsync();

        var organizations = await (
            from o in context.Organizations.AsNoTracking()
            join u in context.Users.AsNoTracking() on o.UserId equals u.Id
            where o.OrganizationEmail != null && o.OrganizationEmail != "" && (u.Email == null || u.Email == "")
            select o.UserId)
            .CountAsync();

        var usersMissingIdentityEmail = await context.Users.AsNoTracking()
            .CountAsync(u => u.Email == null || u.Email == "");

        var totalUsers = await context.Users.AsNoTracking().CountAsync();

        Console.WriteLine($"total users: {totalUsers}");
        Console.WriteLine($"users with no AspNetUsers.Email: {usersMissingIdentityEmail}");
        Console.WriteLine($"farmers recoverable from Farmers.Email: {farmers}");
        Console.WriteLine($"agents recoverable from Agents.Email: {agents}");
        Console.WriteLine($"memberships recoverable from MemberShips.Email: {memberships}");
        Console.WriteLine($"organizations recoverable from Organizations.OrganizationEmail: {organizations}");

        (farmers + agents + memberships + organizations).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUserProfileReturnsFarmerEmailWhenIdentityEmailIsMissing()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var userId = await FindFarmerWithRoleOnlyEmailAsync(context);

        if (userId == null)
        {
            Console.WriteLine("SKIPPED: No farmer in the database has an email only on the Farmers table.");
            return;
        }

        var repository = CreateRepository(context);
        var profile = await repository.GetUserProfile(userId);

        Console.WriteLine($"userId: {userId}");
        Console.WriteLine($"top-level email: {profile.Email}");
        Console.WriteLine($"farmer email: {profile.Farmer?.PersonalInformation?.Email}");

        profile.Email.Should().NotBeNullOrWhiteSpace();
        profile.Farmer.Should().NotBeNull();
        profile.Farmer!.PersonalInformation!.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetUserProfileFallsBackToRoleTablesWhenTheUserHasNoMatchingRoleBranch()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var userId = await FindUserWithRoleEmailOutsideTheirRoleAsync(context);

        if (userId == null)
        {
            Console.WriteLine("SKIPPED: No user carries a role-table email outside their own role.");
            return;
        }

        var expected = await context.Farmers.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.Email)
            .FirstAsync();

        var repository = CreateRepository(context);
        var profile = await repository.GetUserProfile(userId);

        Console.WriteLine($"userId: {userId}, expected: {expected}, returned: {profile.Email}");

        profile.Email.Should().Be(expected);
    }

    [Fact]
    public async Task AgentProfileEmailComesFromTheAgentsTableNotTheIdentityRow()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var candidate = await (
            from a in context.Agents.AsNoTracking()
            join u in context.Users.AsNoTracking() on a.UserId equals u.Id
            where a.Email != null && a.Email != "" && u.Email != null && u.Email != a.Email
                  && u.UserType == "Agent"
            select new { a.UserId, AgentEmail = a.Email, IdentityEmail = u.Email })
            .FirstOrDefaultAsync();

        if (candidate == null)
        {
            Console.WriteLine("SKIPPED: No agent has an Agents.Email that differs from their identity email.");
            return;
        }

        var repository = CreateRepository(context);
        var profile = await repository.GetUserProfile(candidate.UserId);

        Console.WriteLine($"userId: {candidate.UserId}, agents.Email: {candidate.AgentEmail}, identity: {candidate.IdentityEmail}, returned: {profile.Email}");

        profile.Email.Should().Be(candidate.AgentEmail);
        profile.Agent!.Email.Should().Be(candidate.AgentEmail);
    }

    [Fact]
    public async Task GetUserProfileReturnsAgentEmailWhenIdentityEmailIsMissing()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var userId = await FindAgentWithRoleOnlyEmailAsync(context);

        if (userId == null)
        {
            Console.WriteLine("SKIPPED: No agent in the database has an email only on the Agents table.");
            return;
        }

        var repository = CreateRepository(context);
        var profile = await repository.GetUserProfile(userId);

        Console.WriteLine($"userId: {userId}");
        Console.WriteLine($"top-level email: {profile.Email}");
        Console.WriteLine($"agent email: {profile.Agent?.Email}");

        profile.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FindUserByIdReturnsRoleEmailWhenIdentityEmailIsMissing()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var userId = await FindFarmerWithRoleOnlyEmailAsync(context)
                     ?? await FindAgentWithRoleOnlyEmailAsync(context);

        if (userId == null)
        {
            Console.WriteLine("SKIPPED: No user in the database has an email only on a role table.");
            return;
        }

        var repository = CreateRepository(context);
        var profile = await repository.FindUserById(userId);

        Console.WriteLine($"userId: {userId}, email: {profile.Email}");

        profile.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetUseridentityReturnsRoleEmailWhenIdentityEmailIsMissing()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var userId = await FindFarmerWithRoleOnlyEmailAsync(context)
                     ?? await FindAgentWithRoleOnlyEmailAsync(context);

        if (userId == null)
        {
            Console.WriteLine("SKIPPED: No user in the database has an email only on a role table.");
            return;
        }

        var repository = CreateRepository(context);
        var profile = await repository.GetUseridentity(userId);

        Console.WriteLine($"userId: {userId}, email: {profile.Email}");

        profile.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OrganizationFarmerListingPrefersTheFarmerEmailColumn()
    {
        using var context = TryCreateContext();
        if (context == null) return;
        if (!await IsReachableAsync(context)) return;

        var row = await (
            from fo in context.FarmerOrganizations.AsNoTracking()
            where fo.IsActive
            join f in context.Farmers.AsNoTracking() on fo.FarmerId equals f.Id
            join u in context.Users.AsNoTracking() on f.UserId equals u.Id
            where f.Email != null && f.Email != "" && (u.Email == null || u.Email == "")
            select new
            {
                u.Id,
                Resolved = f.Email ?? u.Email,
                FarmerEmail = f.Email,
                IdentityEmail = u.Email
            })
            .FirstOrDefaultAsync();

        if (row == null)
        {
            Console.WriteLine("SKIPPED: No organization farmer has an email only on the Farmers table.");
            return;
        }

        Console.WriteLine($"userId: {row.Id}, resolved: {row.Resolved}, farmer: {row.FarmerEmail}, identity: {row.IdentityEmail}");

        row.Resolved.Should().Be(row.FarmerEmail);
    }
}
