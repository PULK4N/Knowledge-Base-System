using Apache.NMS;
using Apache.NMS.AMQP;
using Microsoft.Extensions.Options;

namespace OutboxProcessingModule.IntegrationTests;

/// <summary>
/// Talks to the Artemis instance from docker-compose. Every test here needs a
/// running broker, so the whole suite skips when there is none instead of
/// failing a build that has no Docker.
/// </summary>
public static class ArtemisBroker
{
    public const string Uri = "amqp://localhost:5673";
    public const string UserName = "artemis";
    public const string Password = "artemis";

    public const string ProjectionsQueue =
        "knowledge-base.skills-state-machine.projections";
    public const string HooksQueue =
        "knowledge-base.skills-state-machine.hooks";
    public const string UnprovisionedQueue =
        "knowledge-base.no-such-state-machine.projections";

    private static readonly Lazy<bool> Reachable = new(Probe);

    public static bool IsReachable => Reachable.Value;

    public static IConnection Connect()
    {
        var connection = new NmsConnectionFactory(UserName, Password, Uri)
            .CreateConnection();
        connection.Start();
        return connection;
    }

    public static IOptions<OutboxProcessingOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(
            new OutboxProcessingOptions
            {
                BrokerUri = Uri,
                UserName = UserName,
                Password = Password,
                BatchSize = 10
            });

    /// <summary>Removes anything a previous test left behind.</summary>
    public static void Drain(params string[] queueNames)
    {
        using var connection = Connect();
        using var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);

        foreach (var queueName in queueNames)
        {
            using var consumer = session.CreateConsumer(session.GetQueue(queueName));
            while (consumer.Receive(TimeSpan.FromMilliseconds(200)) is not null) { }
        }
    }

    public static List<string> ReceiveAll(string queueName, int expected)
    {
        using var connection = Connect();
        using var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        using var consumer = session.CreateConsumer(session.GetQueue(queueName));

        var bodies = new List<string>();

        // One extra receive proves nothing else arrived.
        for (var attempt = 0; attempt <= expected; attempt++)
        {
            if (consumer.Receive(TimeSpan.FromSeconds(2)) is not ITextMessage message)
                break;

            bodies.Add(message.Text);
        }

        return bodies;
    }

    private static bool Probe()
    {
        try
        {
            using var connection = Connect();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public sealed class BrokerFactAttribute : FactAttribute
{
    public BrokerFactAttribute()
    {
        if (!ArtemisBroker.IsReachable)
            Skip = $"Artemis is not reachable at {ArtemisBroker.Uri}. "
                + "Start it with: docker-compose up -d artemis";
    }
}
