using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Agrovesto.Backend.Infrastructure.Logic.Services;

public sealed class OutboxOptions
{
    public string Source { get; set; } = "monolith";
    public string? TopicArn { get; set; }
}

public sealed class OutboxWriter(
    DataContext context,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider) : IOutboxWriter
{
    public void Enqueue(string eventType, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            Source = options.Value.Source,
            CreatedOn = timeProvider.GetUtcNow().UtcDateTime,
            IsProcessed = false
        });
    }
}
