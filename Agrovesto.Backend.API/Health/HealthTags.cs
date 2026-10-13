namespace Agrovesto.Backend.API.Health;

/// <summary>
/// The tag vocabulary that decides which check runs behind which endpoint.
///
/// Getting this wrong is the classic outage amplifier: if a dependency check
/// is tagged <see cref="Live"/>, a transient SQL blip makes Docker/Compose
/// restart-loop every container at once. So:
///
///   live   -> process self-check ONLY. Never I/O, never a dependency.
///   ready  -> SQL Server, Redis, Vault. Gates traffic, not process lifetime.
///   status -> everything else (third parties). Reported, never gating.
/// </summary>
public static class HealthTags
{
    /// <summary>Process is alive. Self-check only — no SQL, Redis, Vault or HTTP.</summary>
    public const string Live = "live";

    /// <summary>Dependency is required before this instance may take traffic.</summary>
    public const string Ready = "ready";

    /// <summary>Informational, non-gating external dependency.</summary>
    public const string Status = "status";
}

/// <summary>Canonical endpoint paths, shared by the mapper, the Dockerfile docs and the tests.</summary>
public static class HealthEndpoints
{
    public const string Live = "/health/live";
    public const string Ready = "/health/ready";
    public const string Legacy = "/health";
    public const string Status = "/status";
}
