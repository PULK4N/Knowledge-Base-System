namespace OutboxProcessingModule;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "OutboxProcessing";

    public string BrokerUri { get; set; } = "amqp://localhost:5672";
    public string UserName { get; set; } = "artemis";
    public string Password { get; set; } = "artemis";

    /// <summary>Rows claimed per publisher cycle.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Wait before the next cycle when a claim finds nothing.</summary>
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>First wait after a failed cycle. Doubles up to MaxRetryDelay.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Budget for returning rows to New after a cycle was cancelled.</summary>
    public TimeSpan CleanupTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
