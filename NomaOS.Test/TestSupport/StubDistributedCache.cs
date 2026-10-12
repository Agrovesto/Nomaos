using Microsoft.Extensions.Caching.Distributed;

namespace NomaOS.Test.TestSupport;

/// <summary>
/// In-memory <see cref="IDistributedCache"/> that can be told to fail, so the
/// Redis readiness check can be exercised in both directions without a Redis
/// server (or Testcontainers) anywhere near the test run.
/// </summary>
public sealed class StubDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public Exception? FailWith { get; set; }

    public List<string> Reads { get; } = new();

    public byte[]? Get(string key)
    {
        Reads.Add(key);
        if (FailWith is not null)
        {
            throw FailWith;
        }

        return _store.TryGetValue(key, out var value) ? value : null;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Get(key));
    }

    public void Refresh(string key)
    {
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }
}
