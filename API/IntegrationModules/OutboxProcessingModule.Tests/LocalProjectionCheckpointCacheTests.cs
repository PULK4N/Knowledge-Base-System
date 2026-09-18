using EventSourcing.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Moq;
using OutboxProcessingModule.Application;
using SharedModule.DistributedMessaging.Projections;

namespace OutboxProcessingModule.Tests;

public sealed class LocalProjectionCheckpointCacheTests
{
    private readonly AggregateId _aggregateId = AggregateId.FromDatabaseGuid(Guid.NewGuid());

    [Fact]
    public async Task CheckpointExpiresAfterFourMinutesAndReadsDoNotExtendItsLifetime()
    {
        var clock = new TestClock();
        using var memory = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var cache = new LocalProjectionCheckpointCache(memory);
        var checkpoint = await cache.Get("memory", _aggregateId, ["Summary"]);
        await cache.Record(checkpoint, 10);
        clock.UtcNow += TimeSpan.FromMinutes(3);
        Assert.Equal(10u, (await cache.Get("memory", _aggregateId, ["Summary"])).OrderNumber);
        clock.UtcNow += TimeSpan.FromMinutes(1);
        Assert.Null((await cache.Get("memory", _aggregateId, ["Summary"])).OrderNumber);
    }

    [Fact]
    public async Task KeysIsolateAggregatesStateMachinesAndProjectorSetsButIgnoreProjectorOrder()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new LocalProjectionCheckpointCache(memory);
        var checkpoint = await cache.Get("memory", _aggregateId, ["Summary", "Search"]);
        await cache.Record(checkpoint, 10);
        await cache.Record(checkpoint, 8);

        Assert.Equal(10u, (await cache.Get("memory", _aggregateId, ["Search", "Summary"])).OrderNumber);
        Assert.Null((await cache.Get("other", _aggregateId, ["Summary", "Search"])).OrderNumber);
        Assert.Null((await cache.Get("memory", AggregateId.FromDatabaseGuid(Guid.NewGuid()), ["Summary", "Search"])).OrderNumber);
        Assert.Null((await cache.Get("memory", _aggregateId, ["Summary"])).OrderNumber);
    }

    [Fact]
    public async Task InvalidationCannotBeUndoneByAnInflightDelivery()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new LocalProjectionCheckpointCache(memory);
        var beforeReplay = await cache.Get("memory", _aggregateId, ["Summary"]);

        await cache.Invalidate("memory");
        await cache.Record(beforeReplay, 10);

        var afterReplay = await cache.Get("memory", _aggregateId, ["Summary"]);
        Assert.NotEqual(beforeReplay.Key, afterReplay.Key);
        Assert.Null(afterReplay.OrderNumber);
        await cache.Record(afterReplay, 10);
        Assert.Equal(10u, (await cache.Get("memory", _aggregateId, ["Summary"])).OrderNumber);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegistrationUsesALocalSingletonAndPreservesAnExplicitProvider(bool replace)
    {
        var services = new ServiceCollection();
        var alternative = Mock.Of<IProjectionCheckpointCache>();
        if (replace)
            services.AddSingleton(alternative);
        services.RegisterOutboxProcessing(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var cache = first.ServiceProvider.GetRequiredService<IProjectionCheckpointCache>();

        Assert.Same(cache, second.ServiceProvider.GetRequiredService<IProjectionCheckpointCache>());
        if (replace)
            Assert.Same(alternative, cache);
        else
            Assert.IsType<LocalProjectionCheckpointCache>(cache);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }
}
