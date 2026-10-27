using System.Security.Claims;
using Agrovesto.Backend.API.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace NomaOS.Test.Observability;

/// <summary>
/// The dashboard authorization rule, tested as the pure function it is — same
/// reason <c>ObservabilityExtensionsTests</c> tests <c>ShouldTraceRequest</c>
/// rather than spinning up a host: the dangerous part is the predicate, not the
/// Hangfire plumbing around it.
/// </summary>
public class HangfireDashboardAuthorizationFilterTests
{
    private static ClaimsPrincipal Principal(string? authenticationType, params string[] roles)
    {
        var identity = new ClaimsIdentity(authenticationType);

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, role));
        }

        return new ClaimsPrincipal(identity);
    }

    private static IReadOnlyList<string> Roles(params string[] roles) => roles;

    private const string Valid = "0123456789abcdef0123456789abcdef";
    private static readonly string ValidToken = new('a', HangfireDashboardAuthorizationFilter.MinEdgeTokenLength);

    [Fact]
    public void IsAuthorized_DeniesAnAnonymousPrincipal()
    {
        // authenticationType == null is ASP.NET Core's "not authenticated",
        // which is what every unauthenticated caller of /hangfire produces today.
        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal(authenticationType: null), Roles())
            .Should().BeFalse();

        HangfireDashboardAuthorizationFilter
            .IsAuthorized(null, Roles())
            .Should().BeFalse();
    }

    [Fact]
    public void IsAuthorized_AllowsAnyAuthenticatedUserWhenNoRolesAreConfigured()
    {
        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies"), Roles())
            .Should().BeTrue();
    }

    [Fact]
    public void IsAuthorized_RequiresOneOfTheConfiguredRoles()
    {
        var allowed = Roles("Admin", "PlatformOps");

        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies", "Admin"), allowed)
            .Should().BeTrue();

        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies", "PlatformOps"), allowed)
            .Should().BeTrue();

        // A farmer-facing role, or a role that was renamed but not rotated out
        // of the env file, must not reach the job surface.
        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies", "CooperativeFieldOfficer"), allowed)
            .Should().BeFalse();

        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies"), allowed)
            .Should().BeFalse();
    }

    [Fact]
    public void IsAuthorized_IgnoresRoleClaimsOnAnUnauthenticatedIdentity()
    {
        // A cookie that presents role claims without a authenticated identity
        // (e.g. a hand-crafted JWT validated with a different scheme) must not
        // satisfy the role check on its own.
        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal(authenticationType: null, "Admin"), Roles("Admin"))
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData(",", 0)]
    [InlineData("Admin", 1)]
    [InlineData("Admin,PlatformOps", 2)]
    [InlineData(" admin , ops ,, admin ", 2)]
    public void ParseRoles_SplitsTrimsAndDeduplicatesCaseInsensitively(string? raw, int expectedCount)
    {
        HangfireDashboardAuthorizationFilter.ParseRoles(raw).Should().HaveCount(expectedCount);
    }

    [Fact]
    public void ParseRoles_NormalisesEntriesSoTrailingSpacesCannotSilentlyMiss()
    {
        var roles = HangfireDashboardAuthorizationFilter.ParseRoles("Admin, Platform Ops");

        roles.Should().Equal("Admin", "Platform Ops");
        roles.Should().NotContain(" Admin");
    }

    [Fact]
    public void ParseRoles_ReturnsAReadOnlyEmptyListWhenUnset()
    {
        HangfireDashboardAuthorizationFilter.ParseRoles(null).Should().BeEmpty();
    }

    // ── edge token ───────────────────────────────────────────────────────────
    [Fact]
    public void HasValidEdgeToken_IsDisabledWhenNoTokenIsConfigured()
    {
        // The failure mode this prevents: an unset secret that quietly accepts an
        // empty or guessed header value.
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(
            HangfireDashboardAuthorizationFilter.EdgeTokenHeaderName, string.Empty);

        HangfireDashboardAuthorizationFilter
            .HasValidEdgeToken(context.Request, expectedToken: null)
            .Should().BeFalse();

        HangfireDashboardAuthorizationFilter
            .HasValidEdgeToken(context.Request, ValidToken)
            .Should().BeFalse();
    }

    [Fact]
    public void HasValidEdgeToken_AcceptsExactlyOneMatch()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(
            HangfireDashboardAuthorizationFilter.EdgeTokenHeaderName, ValidToken);

        HangfireDashboardAuthorizationFilter
            .HasValidEdgeToken(context.Request, ValidToken)
            .Should().BeTrue();
    }

    [Fact]
    public void HasValidEdgeToken_IgnoresDuplicatedOrNearMissHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(
            HangfireDashboardAuthorizationFilter.EdgeTokenHeaderName, ValidToken + "x");
        context.Request.Headers.Append(
            HangfireDashboardAuthorizationFilter.EdgeTokenHeaderName, ValidToken);

        // The first value wins and is wrong: a second, correct value must not be
        // smuggled in behind it.
        HangfireDashboardAuthorizationFilter
            .HasValidEdgeToken(context.Request, ValidToken)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(null, Valid, false)]
    [InlineData("", Valid, false)]
    [InlineData(Valid, Valid, true)]
    [InlineData(Valid, Valid + "x", false)]
    [InlineData(Valid, null, false)]
    public void TokenMatches_ComparesExactly(string? provided, string? expected, bool expected_)
    {
        HangfireDashboardAuthorizationFilter
            .TokenMatches(provided, expected)
            .Should().Be(expected_);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("too-short", false)]
    public void IsUsableEdgeToken_RejectsAnythingGuessable(string? candidate, bool expected)
    {
        HangfireDashboardAuthorizationFilter
            .IsUsableEdgeToken(candidate)
            .Should().Be(expected);
    }

    [Fact]
    public void IsUsableEdgeToken_AcceptsTheMinimumLength()
    {
        HangfireDashboardAuthorizationFilter
            .IsUsableEdgeToken(new string('a', HangfireDashboardAuthorizationFilter.MinEdgeTokenLength))
            .Should().BeTrue();
    }

    [Fact]
    public void Constructor_TrimsTheEdgeTokenAndArmsTheMechanism()
    {
        var filter = new HangfireDashboardAuthorizationFilter(Roles(), "  " + ValidToken + "  ");

        filter.EdgeTokenConfigured.Should().BeTrue();

        // Short values never arm it, so a typo in the env file cannot quietly
        // turn into "secret = 3 chars".
        new HangfireDashboardAuthorizationFilter(Roles(), "abc")
            .EdgeTokenConfigured.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ToleratesANullAllowList()
    {
        var filter = new HangfireDashboardAuthorizationFilter(allowedRoles: null);

        var act = () => filter.Authorize(null!);

        // ArgumentNullException, not a denial: Authorize(null) is a programming
        // error, and the guard is what keeps the filter from swallowing it.
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_TrimsAndDeduplicatesTheSuppliedRoles()
    {
        var filter = new HangfireDashboardAuthorizationFilter(new[] { " Admin ", "ADMIN", "", "  ", "Ops" });

        // Exercised through the public surface: Admin matches, and the duplicate
        // blank entries cannot make an empty allow-list look permissive.
        HangfireDashboardAuthorizationFilter
            .IsAuthorized(Principal("Cookies", "Ops"), filter.AllowedRoles)
            .Should().BeTrue();
    }
}
