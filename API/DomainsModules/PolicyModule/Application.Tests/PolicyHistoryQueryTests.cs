using EventSourcing.Shared.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Queries;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
{
    [Theory]
    [InlineData(PolicyScope.General)]
    [InlineData(PolicyScope.Topic)]
    [InlineData(PolicyScope.AgentFamily)]
    public async Task GetPolicyHistory_ReturnsOnlyTheChangesOfThatPolicy(
        PolicyScope scope
    )
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        string? scopeName = null;
        if (scope == PolicyScope.Topic)
        {
            scopeName = "cloud";
            await ExecuteAsUser(new CreateTopicCommand(handler)
            {
                TopicName = scopeName,
                Description = "Cloud policies."
            });
        }
        if (scope == PolicyScope.AgentFamily)
        {
            scopeName = "claude";
            await ExecuteAsUser(new CreateAgentFamilyCommand(handler)
            {
                AgentFamilyName = scopeName,
                Description = "Claude policies."
            });
        }

        var policyId = await AddPolicy(handler, scope, scopeName, "Tracked");
        await AddPolicy(handler, scope, scopeName, "Other");
        await ExecuteAsUser(UpdatePolicy(handler, scope, scopeName, policyId));

        var query = new GetPolicyHistoryQuery(CreateCalculator(), eventStore)
        {
            Scope = scope,
            ScopeName = scopeName,
            PolicyId = policyId
        };
        var policy = Assert.IsType<PolicyHistoryDto>(await query.Execute(Executor));

        Assert.Equal("Tracked updated", policy.Title);
        Assert.Equal(2, policy.MemoryHistory.Count);
        Assert.All(policy.MemoryHistory, record => Assert.True(record.IsUserOriginated));
        Assert.EndsWith("V2", policy.MemoryHistory.First().EventName);
        Assert.Contains("Added", policy.MemoryHistory.First().EventName);
        Assert.Contains("Updated", policy.MemoryHistory.Last().EventName);

        query.PolicyId = Guid.NewGuid();
        Assert.Null(await query.Execute(Executor));
    }

    private static async Task<Guid> AddPolicy(
        EventSourcing.Core.StateMachineHandler handler,
        PolicyScope scope,
        string? scopeName,
        string title
    )
    {
        PolicyCommand command = scope switch
        {
            PolicyScope.Topic => new AddTopicPolicyCommand(handler)
            {
                TopicName = scopeName!, Title = title, Description = "Description"
            },
            PolicyScope.AgentFamily => new AddAgentFamilyPolicyCommand(handler)
            {
                AgentFamilyName = scopeName!, Title = title, Description = "Description"
            },
            _ => new AddGeneralPolicyCommand(handler)
            {
                Title = title, Description = "Description"
            }
        };

        return Assert.IsType<PolicyAddedCommandResult>(await ExecuteAsUser(command)).PolicyId;
    }

    private static PolicyCommand UpdatePolicy(
        EventSourcing.Core.StateMachineHandler handler,
        PolicyScope scope,
        string? scopeName,
        Guid policyId
    ) =>
        scope switch
        {
            PolicyScope.Topic => new UpdateTopicPolicyCommand(handler)
            {
                TopicName = scopeName!, PolicyId = policyId,
                Title = "Tracked updated", Description = "Description"
            },
            PolicyScope.AgentFamily => new UpdateAgentFamilyPolicyCommand(handler)
            {
                AgentFamilyName = scopeName!, PolicyId = policyId,
                Title = "Tracked updated", Description = "Description"
            },
            _ => new UpdateGeneralPolicyCommand(handler)
            {
                PolicyId = policyId, Title = "Tracked updated", Description = "Description"
            }
        };
}
