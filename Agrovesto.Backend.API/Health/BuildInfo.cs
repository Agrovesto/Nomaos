using System.Diagnostics;
using System.Reflection;

namespace Agrovesto.Backend.API.Health;

/// <summary>
/// Identity of the running build, for <c>/status</c>.
///
/// The git SHA is the thing on-call actually needs at 3am ("is the fix
/// deployed?"), so it is resolved from three places in order: the GIT_SHA env
/// var Compose sets, the COMMIT_SHA build arg, and finally the
/// <c>/app/.commit-sha</c> file the Dockerfile stamps into the image. The file
/// fallback means the SHA is correct even when someone starts the container by
/// hand without the env block.
/// </summary>
public sealed class BuildInfo
{
    public const string ApplicationName = "nomaos-onboarding-api";
    public const string CommitShaFile = "/app/.commit-sha";

    public BuildInfo(
        string application,
        string version,
        string gitSha,
        string environment,
        string machineName,
        DateTimeOffset startedAtUtc)
    {
        Application = application;
        Version = version;
        GitSha = gitSha;
        Environment = environment;
        MachineName = machineName;
        StartedAtUtc = startedAtUtc;
    }

    public string Application { get; }

    public string Version { get; }

    public string GitSha { get; }

    public string Environment { get; }

    public string MachineName { get; }

    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>Process start time, captured once at first use.</summary>
    public static DateTimeOffset ProcessStartedAtUtc { get; } = ResolveProcessStart();

    public static BuildInfo Create(
        IConfiguration configuration,
        string environmentName,
        Func<string, string?>? fileReader = null,
        DateTimeOffset? startedAtUtc = null,
        Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var reader = fileReader ?? ReadFileOrNull;

        var sha = FirstNonEmpty(
            configuration["GIT_SHA"],
            configuration["COMMIT_SHA"],
            reader(CommitShaFile),
            "unknown");

        return new BuildInfo(
            application: FirstNonEmpty(configuration["OTEL_SERVICE_NAME"], ApplicationName),
            version: ResolveVersion(assembly ?? typeof(BuildInfo).Assembly),
            gitSha: sha,
            environment: FirstNonEmpty(environmentName, "Production"),
            machineName: System.Environment.MachineName,
            startedAtUtc: startedAtUtc ?? ProcessStartedAtUtc);
    }

    public double UptimeSeconds(DateTimeOffset now)
    {
        var seconds = (now - StartedAtUtc).TotalSeconds;
        return seconds < 0 ? 0 : Math.Round(seconds, 3);
    }

    /// <summary>Human-readable uptime, e.g. "3d 04h 07m 12s".</summary>
    public string UptimeHuman(DateTimeOffset now)
    {
        var elapsed = now - StartedAtUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return $"{(int)elapsed.TotalDays}d {elapsed.Hours:D2}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";
    }

    internal static string ResolveVersion(Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static DateTimeOffset ResolveProcessStart()
    {
        try
        {
            return new DateTimeOffset(Process.GetCurrentProcess().StartTime.ToUniversalTime(), TimeSpan.Zero);
        }
        catch (Exception)
        {
            // Some hardened containers deny /proc access. Uptime is a nicety,
            // never a reason to fail a health probe.
            return DateTimeOffset.UtcNow;
        }
    }

    private static string? ReadFileOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}

/// <summary>Point-in-time memory/thread usage for <c>/status</c>.</summary>
public sealed record MemorySnapshot(long WorkingSetBytes, long GcHeapBytes, int ThreadCount)
{
    public double WorkingSetMb => Math.Round(WorkingSetBytes / 1024d / 1024d, 2);

    public double GcHeapMb => Math.Round(GcHeapBytes / 1024d / 1024d, 2);

    public static MemorySnapshot Capture()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return new MemorySnapshot(process.WorkingSet64, GC.GetTotalMemory(forceFullCollection: false), process.Threads.Count);
        }
        catch (Exception)
        {
            return new MemorySnapshot(0, GC.GetTotalMemory(forceFullCollection: false), 0);
        }
    }
}
