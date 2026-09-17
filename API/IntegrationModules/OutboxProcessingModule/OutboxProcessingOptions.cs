namespace OutboxProcessingModule;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "OutboxProcessing";

    /// <summary>
    /// Plain broker address. The failover wrapper and the reconnect options are
    /// added by <see cref="Hosting.BrokerConnectionFactory"/>, so this stays a
    /// host and port, not a full client URI.
    /// </summary>
    public string BrokerUri { get; set; } = "amqp://localhost:5672";
    public string UserName { get; set; } = "artemis";
    public string Password { get; set; } = "artemis";

    public BrokerReconnectOptions Reconnect { get; set; } = new();

    /// <summary>Rows claimed per publisher cycle.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Wait before the next cycle when a claim finds nothing.</summary>
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>First wait after a failed cycle. Doubles up to MaxRetryDelay.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Messages a consumer may hold. One keeps a failing delivery from taking
    /// the rest of the queue down with it into the dead-letter queue.
    /// </summary>
    public int QueuePrefetch { get; set; } = 1;

    /// <summary>Budget for returning rows to New after a cycle was cancelled.</summary>
    public TimeSpan CleanupTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// How the NMS client behaves when the broker socket drops. These map onto the
/// client's failover transport, which reconnects on its own and re-creates
/// sessions, producers and consumers, so application code never has to notice
/// a short broker outage.
/// </summary>
public sealed class BrokerReconnectOptions
{
    /// <summary>
    /// Attempts for the very first connection before the client gives up and
    /// throws. One keeps startup failures visible in the worker logs and
    /// leaves the retry to the worker loops, which honour shutdown.
    /// </summary>
    public int StartupMaxAttempts { get; set; } = 1;

    /// <summary>
    /// Attempts after an established connection drops. -1 is unlimited: a
    /// broker restart of any length is bridged, and the worker only sees a
    /// delay, not an error.
    /// </summary>
    public int MaxAttempts { get; set; } = -1;

    /// <summary>First wait after a drop. Doubles up to MaxDelay.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Upper bound for a send, a commit or a session or producer open while the
    /// client is offline and reconnecting. Without it those calls block until
    /// the broker returns and a cycle can never fail or be shut down cleanly.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TCP keep-alive probe interval. Detects a peer that vanished without a
    /// FIN (killed container, suspended host, dropped NAT entry) instead of
    /// waiting for the OS retransmit timeout, which can exceed ten minutes.
    /// </summary>
    public TimeSpan TcpKeepAliveTime { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan TcpKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(5);
}
