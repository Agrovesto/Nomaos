using System.Diagnostics;
using Serilog.Context;

namespace Agrovesto.Backend.API.Observability;

/// <summary>
/// Gives every request one opaque id, echoes it on the response, and puts it on
/// every log line and span produced while that request is being handled.
///
/// <para>
/// This is the piece <see cref="ActivityTraceEnricher"/> cannot replace. That
/// enricher writes <c>trace_id</c> so an engineer can jump from a Loki line into
/// the Tempo trace — but a <c>trace_id</c> is not something you can ask a
/// customer support agent to collect, it disappears when
/// <c>OTEL_SDK_DISABLED=true</c>, and it is only meaningful inside a trace that
/// Tempo kept (probe traffic and filtered paths are deliberately not traced). A
/// correlation id exists for every request, including the ones we choose not to
/// trace, and it is handed back to the caller so an error screen, a webhook
/// payload or a screenshot of a mobile app is enough to find the server logs.
/// </para>
///
/// <para>
/// Contract, in the order it matters:
/// <list type="bullet">
///   <item><description>An inbound <c>X-Correlation-Id</c> is honoured, so one id spans every hop; it is rejected (and replaced) unless it is 1..128 characters of <c>[A-Za-z0-9._-]</c>.</description></item>
///   <item><description>The id is always generated server-side when absent or rejected — never reflected verbatim, which would make this header a log-poisoning and response-splitting vector.</description></item>
///   <item><description>Registered outermost, so <c>GlobalExceptionMiddleware</c> and the health/error writers inherit it.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// Header name in both directions. <c>IHeaderDictionary</c> compares keys
    /// case-insensitively, so clients may send <c>x-correlation-id</c>.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// <see cref="HttpContext.Items"/> key. A key rather than a property on a
    /// custom feature because handlers, filters and exception middleware all
    /// reach the same <see cref="HttpContext"/> without needing a service.
    /// </summary>
    public const string ItemKey = "Nomaos.CorrelationId";

    /// <summary>
    /// Serilog <c>LogContext</c> property name. snake_case and lowercase on
    /// purpose: it sits next to <c>trace_id</c>/<c>span_id</c> from
    /// <see cref="ActivityTraceEnricher"/>, and Loki's field extraction is
    /// case-sensitive.
    /// </summary>
    public const string LogContextProperty = "correlation_id";

    /// <summary>
    /// Ceiling for a client-supplied id. 128 keeps a sane UUID/ULID/Request-ID
    /// while bounding what a caller can push into every log line of a request —
    /// a 64 KB header would otherwise become a 64 KB field on every event.
    /// </summary>
    public const int MaxAcceptedLength = 128;

    /// <summary>Length of a generated id (<see cref="Guid.ToString(string)"/> with "N").</summary>
    public const int GeneratedLength = 32;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = AcquireOrGenerate(ReadInboundId(context.Request));

        context.Items[ItemKey] = correlationId;

        // Safe to assign directly: we are before the handler, so the headers
        // have not been sent. An OnStarting callback would be the defensive
        // choice but costs an allocation and a closure per request.
        context.Response.Headers[HeaderName] = correlationId;

        // The hosting Activity already exists when middleware runs (the
        // AspNetCore instrumentation hooks HostingDiagnosticListener before the
        // pipeline), so this lands on the server span and makes Tempo searchable
        // by the same id the customer was given. Null when the SDK is disabled
        // or when this runs outside a host, hence the null-conditional.
        Activity.Current?.SetTag("nomaos.correlation_id", correlationId);

        // One PushProperty covers the whole request: NomaosSerilog configures
        // .Enrich.FromLogContext(), so every event written deeper in the
        // pipeline — including GlobalExceptionMiddleware's — carries it, without
        // that middleware having to know this class exists (it lives in
        // Infrastructure, which cannot reference the API project).
        using (LogContext.PushProperty(LogContextProperty, correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The id for the current request, or <c>null</c> when the middleware has
    /// not run (background jobs, health checks executed outside the pipeline).
    /// </summary>
    public static string? GetCorrelationId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;
    }

    /// <summary>
    /// Accepts a client id only if it is well formed; otherwise generates one.
    /// Never throws and never truncates-and-keeps: a caller that sends 200
    /// characters has a bug, and silently shortening it would produce two ids
    /// for one request across two services.
    /// </summary>
    internal static string AcquireOrGenerate(string? inbound)
    {
        var candidate = inbound?.Trim();

        return IsWellFormed(candidate) ? candidate! : Generate();
    }

    /// <summary>
    /// 1..128 characters of <c>[A-Za-z0-9._-]</c>. That allow-list is the whole
    /// defense: no CR/LF (header/log splitting), no space or <c>=</c> (breaks
    /// Loki's structured-field parsing), no quotes or braces (breaks the JSON
    /// line), no unicode (survives the round trip through headers, Grafana
    /// links and copy-paste).
    /// </summary>
    internal static bool IsWellFormed(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxAcceptedLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            var allowed =
                (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character is '-' or '_' or '.';

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 32 lowercase hex characters, no dashes: shorter in Loki, easier to
    /// dictate, and impossible to mangle by a client that strips braces or
    /// capitalises.
    /// </summary>
    private static string Generate() => Guid.NewGuid().ToString("N");

    private static string? ReadInboundId(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Headers.TryGetValue(HeaderName, out var values) || values.Count == 0)
        {
            return null;
        }

        // A duplicated header is a client bug; the first value is the one we
        // echo back so the response agrees with whatever the caller logged.
        return values[0];
    }
}

/// <summary>
/// Convenience accessor for controllers, filters and exception handlers, which
/// hold an <see cref="HttpContext"/> rather than the middleware itself.
/// </summary>
public static class CorrelationIdExtensions
{
    public static string? GetCorrelationId(this HttpContext context) =>
        CorrelationIdMiddleware.GetCorrelationId(context);
}
