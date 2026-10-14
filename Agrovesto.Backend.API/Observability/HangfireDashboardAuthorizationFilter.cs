using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Locks the Hangfire dashboard down: either an authenticated API principal in an
/// allowed role, or a request that presents the edge token nginx is configured to
/// add.
///
/// <para>
/// Before this, <c>Program.cs</c> called <c>app.UseHangfireDashboard("/hangfire")</c>
/// with no <see cref="DashboardOptions"/>, which in Hangfire 1.8 means "allow
/// everybody". That surface lists every background job with its full serialised
/// arguments — farmer phone numbers, wallet references, cooperative ids, KYC
/// payloads — and offers buttons to retry, trigger and delete jobs. Reachable from
/// the public internet that is a data-exposure and a financial-integrity finding,
/// not a cosmetic one.
/// </para>
///
/// <para>
/// <b>Why two mechanisms.</b> This API's only authentication scheme is JWT bearer
/// (<c>DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme</c>), so
/// a browser visiting <c>/hangfire</c> has no way to become "authenticated" — a
/// filter that required <see cref="HttpContext"/> user identity and nothing else
/// would not harden the dashboard, it would brick it. The supported way to put a
/// human in front of it on this deployment is the edge: nginx
/// <c>auth_basic</c>/<c>auth_request</c> in front of a location that sets
/// <c>X-Nomaos-Dashboard-Token</c>. That works because the container port is not
/// published to the host (see deploy/env/app.env.example, <c>APP_PORT</c>), so the
/// token cannot be bypassed by going around nginx; if a deployment ever publishes
/// the port directly, set no token and rely on roles alone.
/// </para>
///
/// <para>
/// Both knobs are fail-closed: an unset <c>HANGFIRE_DASHBOARD_ROLES</c> means "any
/// authenticated principal" (not "anyone"), and an unset or too-short
/// <c>HANGFIRE_DASHBOARD_EDGE_TOKEN</c> disables the edge-token path entirely
/// rather than accepting an empty header.
/// </para>
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Comma-separated allow-list of roles. Empty = any authenticated user.
    /// </summary>
    public const string RolesConfigurationKey = "HANGFIRE_DASHBOARD_ROLES";

    /// <summary>
    /// Shared secret nginx attaches after it has authenticated a human. Blank,
    /// whitespace or shorter than <see cref="MinEdgeTokenLength"/> disables the
    /// mechanism.
    /// </summary>
    public const string EdgeTokenConfigurationKey = "HANGFIRE_DASHBOARD_EDGE_TOKEN";

    /// <summary>Header the edge sets. Case-insensitive, like every ASP.NET Core header key.</summary>
    public const string EdgeTokenHeaderName = "X-Nomaos-Dashboard-Token";

    /// <summary>
    /// Floor for the configured secret. 32 characters is ~128 bits of a
    /// lowercase-alphanumeric alphabet; below that a guessable value in front of
    /// a job-control surface is worse than no value at all.
    /// </summary>
    public const int MinEdgeTokenLength = 32;

    private readonly IReadOnlyList<string> _allowedRoles;
    private readonly string? _edgeToken;

    public HangfireDashboardAuthorizationFilter(IEnumerable<string>? allowedRoles, string? edgeToken = null)
    {
        _allowedRoles = Normalise(allowedRoles);
        _edgeToken = IsUsableEdgeToken(edgeToken) ? edgeToken!.Trim() : null;
    }

    /// <summary>Roles-only construction; the edge-token path stays disabled.</summary>
    public HangfireDashboardAuthorizationFilter()
        : this(allowedRoles: null)
    {
    }

    /// <summary>The normalised allow-list in effect; internal so the trimming and de-duplication rules are directly assertable.</summary>
    internal IReadOnlyList<string> AllowedRoles => _allowedRoles;

    /// <summary>True when the edge-token path is armed. Never exposes the value.</summary>
    internal bool EdgeTokenConfigured => _edgeToken is not null;

    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Deliberately NOT context.GetHttpContext(): that extension throws
        // ArgumentException whenever the dashboard context is not an
        // AspNetCoreDashboardContext, and an exception thrown from an
        // authorization filter surfaces as a 500 on the one surface whose job is
        // to say "no". Pattern-match instead, and deny when we cannot see the
        // request.
        var httpContext = context is AspNetCoreDashboardContext aspNetCore
            ? aspNetCore.HttpContext
            : null;

        if (httpContext is null)
        {
            return false;
        }

        if (IsAuthorized(httpContext.User, _allowedRoles))
        {
            return true;
        }

        return HasValidEdgeToken(httpContext.Request, _edgeToken);
    }

    /// <summary>
    /// Role rule, with no Hangfire or ASP.NET Core types in the signature so it is
    /// unit-testable the way <see cref="ObservabilityExtensions"/> makes its trace
    /// filter testable.
    /// </summary>
    internal static bool IsAuthorized(ClaimsPrincipal? user, IReadOnlyList<string> allowedRoles)
    {
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            return false;
        }

        if (allowedRoles is null || allowedRoles.Count == 0)
        {
            return true;
        }

        foreach (var role in allowedRoles)
        {
            if (user.IsInRole(role))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Edge-token rule. Reads the first header value only (a duplicated header
    /// would otherwise be joined with a comma and compared as garbage) and
    /// compares in constant time so the dashboard cannot be used to probe the
    /// secret byte by byte.
    /// </summary>
    internal static bool HasValidEdgeToken(HttpRequest? request, string? expectedToken)
    {
        if (expectedToken is null || request is null)
        {
            return false;
        }

        string? provided = null;

        if (request.Headers.TryGetValue(EdgeTokenHeaderName, out var values) && values.Count > 0)
        {
            provided = values[0];
        }

        return TokenMatches(provided, expectedToken);
    }

    internal static bool TokenMatches(string? provided, string? expected)
    {
        if (expected is null || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        // FixedTimeEquals returns false on a length mismatch without leaking the
        // delta, which is the behaviour we want here.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }

    internal static bool IsUsableEdgeToken(string? candidate) =>
        !string.IsNullOrWhiteSpace(candidate) && candidate.Trim().Length >= MinEdgeTokenLength;

    /// <summary>
    /// Splits, trims, drops blanks, case-insensitive de-duplication: an operator
    /// writing "Admin, ADMIN" should get one entry, not a confusing two.
    /// </summary>
    public static IReadOnlyList<string> ParseRoles(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> Normalise(IEnumerable<string>? allowedRoles)
    {
        if (allowedRoles is null)
        {
            return Array.Empty<string>();
        }

        return allowedRoles
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
