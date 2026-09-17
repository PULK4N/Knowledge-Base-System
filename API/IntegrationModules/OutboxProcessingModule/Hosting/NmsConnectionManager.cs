using Apache.NMS;
using Microsoft.Extensions.Logging;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// Owns the publisher's long-lived broker connection. An NMS connection is a
/// heavyweight, thread-safe object meant to be shared for the life of the
/// process; sessions and producers are the cheap per-cycle objects. Short
/// outages are bridged inside the connection by the failover transport. Only
/// when the client reports the connection as failed for good, or a caller saw
/// an error it cannot attribute, is the object dropped so the next
/// <see cref="Get"/> builds a fresh one.
/// </summary>
public sealed class NmsConnectionManager(
    BrokerConnectionFactory _factory,
    ILogger<NmsConnectionManager> _logger
) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private volatile bool _faulted;

    public async Task<IConnection> Get(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_faulted)
                Close();

            if (_connection is not null)
                return _connection;

            var connection = _factory.Create().CreateConnection();

            try
            {
                connection.Start();
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            // Attached after Start so a startup failure surfaces as the thrown
            // exception alone and never as a callback racing this method.
            connection.ExceptionListener += OnConnectionFailed;
            connection.ConnectionInterruptedListener += OnConnectionInterrupted;
            connection.ConnectionResumedListener += OnConnectionResumed;

            _faulted = false;
            _connection = connection;
            return connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Drops the current connection so the next cycle reconnects. Safe to call
    /// for any error: a healthy connection is merely rebuilt once.
    /// </summary>
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

    private void OnConnectionInterrupted() =>
        _logger.LogWarning("The broker connection dropped; the client is reconnecting.");

    private void OnConnectionResumed() =>
        _logger.LogInformation("The broker connection is back.");

    /// <summary>
    /// The client raises this only when it will not recover on its own, for
    /// example after exhausting its reconnect attempts. Keeping the object
    /// would make every later cycle fail on a dead connection. The close is
    /// deferred to the next <see cref="Get"/>: this runs on the client's own
    /// thread, and closing the client from inside its callback could deadlock.
    /// </summary>
    private void OnConnectionFailed(Exception error)
    {
        _logger.LogError(error, "The broker connection failed and will be rebuilt.");
        _faulted = true;
    }

    private void Close()
    {
        if (_connection is null)
            return;

        try
        {
            _connection.ExceptionListener -= OnConnectionFailed;
            _connection.ConnectionInterruptedListener -= OnConnectionInterrupted;
            _connection.ConnectionResumedListener -= OnConnectionResumed;
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
            _faulted = false;
        }
    }
}
