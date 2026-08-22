using System.Text.Json;
using AdministrationModule.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdministrationModule.Persistence.Tests;

public sealed class OutboxAdministrationRepositoryTests
{
    private static readonly object RegistrationLock = new();
    private static bool _eventRegistered;

    [Fact]
    public async Task Search_and_requeue_manage_outbox_delivery_metadata()
    {
        RegisterEventOnce();
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new EventSourcingDbContext(options);
        var storedMessage = SerializedPayloadMessage.FromPayload(
            CreatePayload()
        );
        dbContext.Set<SerializedPayloadMessage>().Add(storedMessage);
        var entityEntry = dbContext.Entry(storedMessage);
        var stateProperty = entityEntry.Properties.Single(
            property =>
                property.Metadata.Name.Contains(
                    "State",
                    StringComparison.OrdinalIgnoreCase
                )
                || property.Metadata.Name.Contains(
                    "Status",
                    StringComparison.OrdinalIgnoreCase
                )
        );
        const string incompleteState = "Error";
        stateProperty.CurrentValue = Enum.Parse(
            stateProperty.Metadata.ClrType,
            incompleteState
        );
        entityEntry.Properties.Single(
            property =>
                IsIntegral(property.Metadata.ClrType)
                && !property.Metadata.IsPrimaryKey()
                && !property.Metadata.IsConcurrencyToken
        ).CurrentValue = 4;
        entityEntry.Properties.Single(
            property => property.Metadata.Name.Contains(
                "Error",
                StringComparison.OrdinalIgnoreCase
            )
        ).CurrentValue = "Projection failed.";
        var versionProperty = entityEntry.Property("Version");
        versionProperty.CurrentValue =
            versionProperty.Metadata.ClrType == typeof(byte[])
                ? Array.Empty<byte>()
                : Activator.CreateInstance(
                    versionProperty.Metadata.ClrType
                );

        var sentMessage = SerializedPayloadMessage.FromPayload(
            CreatePayload()
        );
        dbContext.Set<SerializedPayloadMessage>().Add(sentMessage);
        var sentEntry = dbContext.Entry(sentMessage);
        var sentStateProperty = sentEntry.Properties.Single(
            property =>
                property.Metadata.Name.Contains(
                    "State",
                    StringComparison.OrdinalIgnoreCase
                )
                || property.Metadata.Name.Contains(
                    "Status",
                    StringComparison.OrdinalIgnoreCase
                )
        );
        sentStateProperty.CurrentValue = Enum.Parse(
            sentStateProperty.Metadata.ClrType,
            "Sent"
        );
        var sentVersionProperty = sentEntry.Property("Version");
        sentVersionProperty.CurrentValue =
            sentVersionProperty.Metadata.ClrType == typeof(byte[])
                ? Array.Empty<byte>()
                : Activator.CreateInstance(
                    sentVersionProperty.Metadata.ClrType
                );
        await dbContext.SaveChangesAsync();
        var repository = new OutboxAdministrationRepository(dbContext);

        var search = await repository.Search(1, 10, true);
        var failed = Assert.Single(search.Items);

        Assert.Equal(1, search.TotalCount);
        Assert.Equal(incompleteState, failed.State);
        Assert.Equal(4, failed.RetryCount);
        Assert.Equal("Projection failed.", failed.ErrorMessage);
        Assert.Equal(
            nameof(AdministrationOutboxTestEvent),
            failed.EventName
        );
        using var executionInfoJson = JsonDocument.Parse(
            failed.ExecutionInfoJson
        );
        using var eventDataJson = JsonDocument.Parse(failed.EventDataJson);
        Assert.Equal(
            JsonValueKind.Object,
            executionInfoJson.RootElement.ValueKind
        );
        Assert.Equal(
            JsonValueKind.Object,
            eventDataJson.RootElement.ValueKind
        );
        Assert.Contains(
            "Administration outbox test",
            failed.EventDataJson
        );

        var requeued = Assert.IsType<
            AdministrationModule.Application.Persistence.OutboxPayloadEntry
        >(await repository.Requeue(failed.Id));

        Assert.Equal("New", requeued.State);
        Assert.Equal(0, requeued.RetryCount);
        Assert.Equal("Projection failed.", requeued.ErrorMessage);
    }

    private static EventPayload CreatePayload() =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            ),
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            "administration-test-state-machine",
            new AdministrationOutboxTestEvent()
        );

    private static void RegisterEventOnce()
    {
        lock (RegistrationLock)
        {
            if (_eventRegistered)
                return;

            EventTypeContainer.AddEventType(
                typeof(AdministrationOutboxTestEvent)
            );
            _eventRegistered = true;
        }
    }

    private static bool IsIntegral(Type type)
    {
        var valueType = Nullable.GetUnderlyingType(type) ?? type;

        return valueType == typeof(byte)
            || valueType == typeof(sbyte)
            || valueType == typeof(short)
            || valueType == typeof(ushort)
            || valueType == typeof(int)
            || valueType == typeof(uint)
            || valueType == typeof(long)
            || valueType == typeof(ulong);
    }

    private sealed class AdministrationOutboxTestEvent : IEvent
    {
        public string Name { get; } = "Administration outbox test";

        public object Apply(
            object stateData,
            EventExecutionInfo eventExecutionInfo
        ) =>
            stateData;
    }
}
