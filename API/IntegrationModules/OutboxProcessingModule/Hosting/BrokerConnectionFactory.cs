using Apache.NMS;
using Apache.NMS.AMQP;
using Microsoft.Extensions.Options;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// The one place that turns options into an NMS connection factory, so the
/// publisher and every consumer connect the same way. Wraps the broker address
/// in the failover transport: reconnecting after a dropped socket is the
/// client library's job, not something each caller re-implements.
/// </summary>
public sealed class BrokerConnectionFactory(IOptions<OutboxProcessingOptions> _options)
{
    public IConnectionFactory Create()
    {
        var settings = _options.Value;

        var factory = new NmsConnectionFactory(
            settings.UserName, settings.Password, BuildUri(settings));
        factory.PrefetchPolicy.QueuePrefetch = settings.QueuePrefetch;

        return factory;
    }

    /// <summary>
    /// failover:(broker?transport.*)?failover.*&amp;nms.*
    /// Transport options belong to the inner URI because they apply per socket;
    /// failover and nms options belong outside because they apply to the
    /// connection for its whole life, across reconnects.
    /// </summary>
    public static string BuildUri(OutboxProcessingOptions settings)
    {
        var reconnect = settings.Reconnect;

        var transportOptions = string.Join("&",
        [
            $"transport.tcpKeepAliveTime={Milliseconds(reconnect.TcpKeepAliveTime)}",
            $"transport.tcpKeepAliveInterval={Milliseconds(reconnect.TcpKeepAliveInterval)}"
        ]);

        var connectionOptions = string.Join("&",
        [
            $"failover.startupMaxReconnectAttempts={reconnect.StartupMaxAttempts}",
            $"failover.maxReconnectAttempts={reconnect.MaxAttempts}",
            "failover.initialReconnectDelay=0",
            $"failover.reconnectDelay={Milliseconds(reconnect.InitialDelay)}",
            "failover.useReconnectBackOff=true",
            $"failover.maxReconnectDelay={Milliseconds(reconnect.MaxDelay)}",
            $"nms.requestTimeout={Milliseconds(reconnect.RequestTimeout)}",
            $"nms.sendTimeout={Milliseconds(reconnect.RequestTimeout)}"
        ]);

        var separator = settings.BrokerUri.Contains('?') ? "&" : "?";

        return $"failover:({settings.BrokerUri}{separator}{transportOptions})?{connectionOptions}";
    }

    private static long Milliseconds(TimeSpan value) =>
        (long)value.TotalMilliseconds;
}
