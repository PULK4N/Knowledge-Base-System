using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// Runs dispatch cycles until shutdown, one DI scope per cycle so each cycle
/// gets its own DbContext and the claimed rows stay tracked across claim,
/// publish and complete.
/// </summary>
public sealed class OutboxPublisherWorker(
    IServiceScopeFactory _scopes,
    IOptions<OutboxProcessingOptions> _options,
    ILogger<OutboxPublisherWorker> _logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _options.Value;
        var retryDelay = settings.RetryDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var step = scope.ServiceProvider.GetRequiredService<OutboxDispatchStep>();

                var claimedRows = await step.Execute(stoppingToken);

                retryDelay = settings.RetryDelay;

                if (claimedRows)
                    continue;

                await Task.Delay(settings.IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "An outbox dispatch cycle failed.");

                await Delay(retryDelay, stoppingToken);
                retryDelay = Next(retryDelay, settings.MaxRetryDelay);
            }
        }
    }

    private static TimeSpan Next(TimeSpan current, TimeSpan maximum)
    {
        var doubled = current + current;
        return doubled > maximum ? maximum : doubled;
    }

    private static async Task Delay(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown; the loop condition ends the worker.
        }
    }
}
