using Apache.NMS;
using Apache.NMS.AMQP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// Owns the long-lived broker connection. A caller that saw a broker failure
/// calls <see cref="Reset"/> so the next cycle builds a new connection instead
/// of reusing a broken one. Backoff between attempts belongs to the caller.
/// </summary>
public sealed class NmsConnectionManager(
    IOptions<OutboxProcessingOptions> _options,
    ILogger<NmsConnectionManager> _logger
) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> Get(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_connection is not null)
                return _connection;

            var settings = _options.Value;
            var factory = new NmsConnectionFactory(
                settings.UserName, settings.Password, settings.BrokerUri);
            factory.PrefetchPolicy.QueuePrefetch = settings.QueuePrefetch;

            var connection = factory.CreateConnection();
            connection.ExceptionListener += OnConnectionFailed;
            connection.Start();

            _connection = connection;
            return connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Reset()
    {
        _gate.Wait();

        try
        {
            Close();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        Close();
        _gate.Dispose();
    }

    private void OnConnectionFailed(Exception error) =>
        _logger.LogWarning(error, "The broker connection reported a failure.");

    private void Close()
    {
        if (_connection is null)
            return;

        try
        {
            _connection.ExceptionListener -= OnConnectionFailed;
            _connection.Close();
            _connection.Dispose();
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Closing the broker connection failed.");
        }
        finally
        {
            _connection = null;
        }
    }
}
