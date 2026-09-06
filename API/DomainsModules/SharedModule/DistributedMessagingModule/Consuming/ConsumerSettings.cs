namespace SharedModule.DistributedMessaging.Consuming;

public sealed class ConsumerSettings
{
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
