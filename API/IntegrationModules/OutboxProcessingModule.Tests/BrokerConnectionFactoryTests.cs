using Apache.NMS.AMQP;
using OutboxProcessingModule.Hosting;

namespace OutboxProcessingModule.Tests;

public sealed class BrokerConnectionFactoryTests
{
    [Theory]
    [InlineData("amqp://artemis:5672", "?")]
    [InlineData("amqp://artemis:5672?amqp.vhost=kb", "&")]
    public void TheBrokerAddressIsWrappedInFailoverWithTransportOptionsInside(
        string brokerUri, string joiner)
    {
        var uri = BrokerConnectionFactory.BuildUri(
            new OutboxProcessingOptions { BrokerUri = brokerUri });

        Assert.StartsWith(
            $"failover:({brokerUri}{joiner}transport.tcpKeepAliveTime=30000"
            + "&transport.tcpKeepAliveInterval=5000)?",
            uri);
    }

    [Fact]
    public void ReconnectSettingsBecomeFailoverAndNmsOptionsOutsideTheParentheses()
    {
        var uri = BrokerConnectionFactory.BuildUri(new OutboxProcessingOptions
        {
            Reconnect = new BrokerReconnectOptions
            {
                StartupMaxAttempts = 3,
                MaxAttempts = 7,
                InitialDelay = TimeSpan.FromMilliseconds(250),
                MaxDelay = TimeSpan.FromSeconds(10),
                RequestTimeout = TimeSpan.FromSeconds(4)
            }
        });

        var outside = uri[(uri.IndexOf(")?", StringComparison.Ordinal) + 2)..];

        Assert.Equal(
            "failover.startupMaxReconnectAttempts=3"
            + "&failover.maxReconnectAttempts=7"
            + "&failover.initialReconnectDelay=0"
            + "&failover.reconnectDelay=250"
            + "&failover.useReconnectBackOff=true"
            + "&failover.maxReconnectDelay=10000"
            + "&nms.requestTimeout=4000"
            + "&nms.sendTimeout=4000",
            outside);
    }

    [Fact]
    public void TheClientAcceptsTheBuiltUriAndAppliesTheConnectionOptions()
    {
        var settings = new OutboxProcessingOptions
        {
            Reconnect = new BrokerReconnectOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(4)
            }
        };

        var factory = new NmsConnectionFactory(BrokerConnectionFactory.BuildUri(settings));

        Assert.Equal("failover", factory.BrokerUri.Scheme);
        Assert.Equal(4000, factory.RequestTimeout);
        Assert.Equal(4000, factory.SendTimeout);
    }
}
