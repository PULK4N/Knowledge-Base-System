using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Domain;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
{
    private static readonly Guid SessionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    );

    [Theory]
    [InlineData("general", false, false)]
    [InlineData("topic", false, false)]
    [InlineData("agent-family", false, false)]
    [InlineData("general", false, true)]
    [InlineData("general", true, false)]
    public async Task Policy_mutations_record_origin_and_write_memory_in_the_same_batch(
        string scope, bool userOrigin, bool suppliedMemoryId
    )
    {
        var store = new CapturingEventStoreWithOutbox();
        if (!userOrigin)
            await SeedSession(store);
        var handler = CreateHandler(store);
        PolicyCommand command = scope switch
        {
            "general" => new AddGeneralPolicyCommand(handler) { Title = "Policy", Description = "Description" },
            "topic" => new CreateTopicCommand(handler) { TopicName = "topic", Description = "Description" },
            _ => new CreateAgentFamilyCommand(handler) { AgentFamilyName = "codex", Description = "Description" }
        };
        command.SessionId = SessionId;
        if (suppliedMemoryId)
            command.MemoryAggregateId = MemoryId.Value;
        if (userOrigin)
            command.UseUserOrigin();

        await command.Execute(Executor);

        Assert.Equal(1, store.WriteCount);
        var policyState = Assert.Single(store.LastWritten.Values,
            state => state.StateData is GeneralPoliciesStateData);
        var payload = Assert.Single(policyState.LastExecutedPayloads);
        var history = Assert.Single(((GeneralPoliciesStateData)policyState.StateData).MemoryHistory);
        var expectedMemoryId = userOrigin
            ? AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User)
            : MemoryId;
        Assert.Equal(expectedMemoryId, history.AggregateId);
        Assert.Equal(payload.EventExecutionInfo.EventName, history.EventName);
        Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Timestamp);
        Assert.Equal(userOrigin ? Guid.Empty : SessionId,
            (Guid)payload.EventData.GetType().GetProperty("SessionId")!.GetValue(payload.EventData)!);
        if (userOrigin)
        {
            Assert.DoesNotContain(store.LastWritten.Values, state => state.StateData is MemoryStateData);
        }
        else
        {
            var relation = Assert.IsType<MemoryRelationAddedV1>(Assert.Single(
                store.LastWritten[MemoryId].LastExecutedPayloads
            ).EventData);
            Assert.Equal(payload.EventExecutionInfo.AggregateId, relation.AggregateId);
            Assert.Equal(history.EventName, relation.Relation);
        }
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
    [InlineData("dddddddd-dddd-dddd-dddd-dddddddddddd", "00000000-0000-0000-0000-000000000000")]
    [InlineData("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "dddddddd-dddd-dddd-dddd-dddddddddddd")]
    public async Task Missing_or_unrelated_memory_prevents_all_policy_writes(string sessionId, string memoryId)
    {
        var store = new CapturingEventStoreWithOutbox();
        await SeedSession(store);
        var command = new AddGeneralPolicyCommand(CreateHandler(store))
        {
            Title = "Policy", Description = "Description",
            SessionId = Guid.Parse(sessionId), MemoryAggregateId = Guid.Parse(memoryId)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.Execute(Executor));

        Assert.Equal(0, store.WriteCount);
    }

    private static Task SeedSession(CapturingEventStoreWithOutbox store)
    {
        var payload = EventPayload.Create(
            Executor.Id, MemoryModule.Domain.MemoryAggregateIds.SessionAggregateMap,
            MemoryModule.Application.Constants.StateMachineIds.SessionAggregateMap,
            new SessionAggregateMapAddedV1(new ThreadId(SessionId), MemoryId)
        );
        payload.EventExecutionInfo.OrderNumber = 1;
        var stateInfo = StateInfo.Create(
            new object(),
            payload.EventExecutionInfo.StateMachineId,
            payload.EventExecutionInfo.AggregateId
        );
        stateInfo.LastExecutedPayloads = [payload];
        return store.Write(new List<StateInfo> { stateInfo });
    }
}
