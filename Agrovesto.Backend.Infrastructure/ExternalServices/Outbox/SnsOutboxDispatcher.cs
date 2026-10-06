using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.Outbox;

public sealed class SnsOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IAmazonSimpleNotificationService sns,
    IOptions<OutboxOptions> options,
    ILogger<SnsOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to dispatch pending outbox messages for {Source}", options.Value.Source);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.TopicArn))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var messages = await context.OutboxMessages
            .Where(message => !message.IsProcessed && message.Source == options.Value.Source)
            .OrderBy(message => message.CreatedOn)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await sns.PublishAsync(new PublishRequest
                {
                    TopicArn = options.Value.TopicArn,
                    Message = message.Payload,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        ["eventType"] = new() { DataType = "String", StringValue = message.EventType },
                        ["eventId"] = new() { DataType = "String", StringValue = message.Id.ToString() },
                        ["source"] = new() { DataType = "String", StringValue = message.Source }
                    }
                }, cancellationToken);

                message.IsProcessed = true;
                message.ProcessedOn = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = exception.Message;
                logger.LogWarning(exception, "Failed to publish outbox event {EventId}", message.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
