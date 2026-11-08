using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using PolicyModule.Domain.Validators;

namespace PolicyModule.Domain.Tests;

public sealed class PolicyValidatorTests
{
    private static readonly PolicyId PolicyId =
        PolicyModule.Shared.PolicyId.FromDatabaseGuid(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        );
    private static readonly TopicName TopicName = new("cloud");
    private static readonly AgentFamilyName AgentFamilyName =
        new("claude");

    [Fact]
    public void GeneralPolicyUpdate_RequiresExistingPolicy()
    {
        var state = CreateGeneralState();
        var payload = CreatePayload(
            new GeneralPolicyUpdatedV1(CreatePolicy())
        );
        var validator = new GeneralPolicyMustExistValidator();

        Assert.False(validator.Validate(state, payload).Succeded);

        state.Policies.Add(PolicyId, CreatePolicy());

        Assert.True(validator.Validate(state, payload).Succeded);
    }

    [Fact]
    public void TopicAndTopicPolicyUpdates_RequireExistingState()
    {
        var state = CreateGeneralState();
        var topicPayload = CreatePayload(
            new TopicUpdatedV1(TopicName, "Updated")
        );
        var policyPayload = CreatePayload(
            new TopicPolicyUpdatedV1(
                TopicName,
                CreatePolicy()
            )
        );

        Assert.False(
            new TopicMustExistValidator()
                .Validate(state, topicPayload)
                .Succeded
        );

        var topic = new Topic
        {
            TopicName = TopicName,
            Description = "Cloud policies"
        };
        state.Topics.Add(TopicName, topic);

        Assert.True(
            new TopicMustExistValidator()
                .Validate(state, topicPayload)
                .Succeded
        );
        Assert.False(
            new TopicPolicyMustExistValidator()
                .Validate(state, policyPayload)
                .Succeded
        );

        topic.Policies.Add(PolicyId, CreatePolicy());

        Assert.True(
            new TopicPolicyMustExistValidator()
                .Validate(state, policyPayload)
                .Succeded
        );
    }

    [Fact]
    public void AgentFamilyAndAgentFamilyPolicyUpdates_RequireExistingState()
    {
        var state = CreateGeneralState();
        var agentFamilyPayload = CreatePayload(
            new AgentFamilyUpdatedV1(AgentFamilyName, "Updated")
        );
        var policyPayload = CreatePayload(
            new AgentFamilyPolicyUpdatedV1(
                AgentFamilyName,
                CreatePolicy()
            )
        );

        Assert.False(
            new AgentFamilyMustExistValidator()
                .Validate(state, agentFamilyPayload)
                .Succeded
        );

        var agentFamily = new AgentFamily
        {
            AgentFamilyName = AgentFamilyName,
            Description = "Claude policies"
        };
        state.AgentFamilies.Add(AgentFamilyName, agentFamily);

        Assert.True(
            new AgentFamilyMustExistValidator()
                .Validate(state, agentFamilyPayload)
                .Succeded
        );
        Assert.False(
            new AgentFamilyPolicyMustExistValidator()
                .Validate(state, policyPayload)
                .Succeded
        );

        agentFamily.Policies.Add(PolicyId, CreatePolicy());

        Assert.True(
            new AgentFamilyPolicyMustExistValidator()
                .Validate(state, policyPayload)
                .Succeded
        );
    }

    [Fact]
    public void AgentFamilyCreation_RequiresAnUnusedName()
    {
        var state = CreateGeneralState();
        var payload = CreatePayload(
            new AgentFamilyCreatedV1(AgentFamilyName, "Claude policies")
        );
        var validator = new AgentFamilyMustNotExistValidator();

        Assert.True(validator.Validate(state, payload).Succeded);

        state.AgentFamilies.Add(
            AgentFamilyName,
            new AgentFamily
            {
                AgentFamilyName = AgentFamilyName,
                Description = "Claude policies"
            }
        );

        Assert.False(validator.Validate(state, payload).Succeded);
    }

    [Theory]
    [InlineData("claude", "claude")]
    [InlineData("Claude", "claude")]
    [InlineData("  CODEX  ", "codex")]
    public void AgentFamilyNames_AreNormalized(
        string name,
        string expected
    ) =>
        Assert.Equal(
            expected,
            AgentFamilyName.Normalized(name).Name
        );

    private static GeneralPoliciesStateData CreateGeneralState() =>
        new(AggregateId.FromDatabaseGuid(Guid.Empty));

    private static Policy CreatePolicy() =>
        new()
        {
            PolicyId = PolicyId,
            Title = "Policy",
            Description = "Description"
        };

    private static EventPayload CreatePayload(IEvent eventData) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            AggregateId.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            "policy-test-state-machine",
            eventData
        );
}
