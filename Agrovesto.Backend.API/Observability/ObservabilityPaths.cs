namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Single source of truth for the request paths that must never produce a trace.
///
/// Probe traffic (Docker HEALTHCHECK every 30s, the ALB every 10s, Hangfire's
/// dashboard poller) would otherwise dominate Tempo and make the trace search
/// useless — thousands of 2ms <c>GET /health/live</c> spans per hour, none of
/// which anyone will ever look at. The exclusion is applied at the ASP.NET Core
/// instrumentation <c>Filter</c>, i.e. before the span is created, so the cost
/// is a prefix compare and nothing is sampled, buffered or exported.
///
/// Kept deliberately free of DI so both the tracing filter and the tests can
/// use it without a host.
/// </summary>
public static class ObservabilityPaths
{
    /// <summary>
    /// Prefixes excluded from tracing. A request matches when its path equals
    /// the prefix or starts with "prefix/" — so "/health" and "/health/ready"
    /// match, but "/healthz-of-my-own-controller" does not.
    /// </summary>
    public static readonly IReadOnlyList<string> ExcludedPrefixes = new[]
    {
        "/health",
        "/status",
        "/hangfire",
    };

    /// <summary>
    /// True when the path must not be traced.
    /// Null/empty paths are never excluded (they cannot be probe traffic).
    /// </summary>
    public static bool IsExcluded(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.TrimEnd('/');
        if (trimmed.Length == 0)
        {
            // "/" — the root. Not a probe path.
            return false;
        }

        foreach (var prefix in ExcludedPrefixes)
        {
            if (trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Overload for the ASP.NET Core instrumentation filter.</summary>
    public static bool IsExcluded(PathString path) => IsExcluded(path.HasValue ? path.Value : null);

    /// <summary>
    /// Overload for the HttpClient instrumentation filter. Outbound calls are
    /// filtered on their absolute path so the app's own probe calls (the
    /// ApiEndpointHealthCheck hits /health/status over HTTP) do not create
    /// client spans either.
    /// </summary>
    public static bool IsExcluded(Uri? uri) => uri is not null && IsExcluded(uri.AbsolutePath);
}
