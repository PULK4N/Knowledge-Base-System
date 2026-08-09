using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using PolicyModule.Domain.Validators;

namespace PolicyModule.Domain.Tests;

public sealed class PolicyValidatorTests
{
    private static readonly AggregateId ProjectId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        );
    private static readonly PolicyId PolicyId =
        PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        );
    private static readonly TopicName TopicName = new("cloud");

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
    public void ProjectMutations_RequireAnActiveProject()
    {
        var state = CreateProjectState();
        var payload = CreatePayload(
            new ProjectUpdatedV1("Updated", "Description")
        );
        var validator = new ProjectMustBeActiveValidator();

        Assert.True(validator.Validate(state, payload).Succeded);

        state.IsDeleted = true;
        var result = validator.Validate(state, payload);

        Assert.False(result.Succeded);
        Assert.Contains("deleted", result.FailureReason);
    }

    [Fact]
    public void ProjectPolicyUpdate_RequiresExistingPolicy()
    {
        var state = CreateProjectState();
        var payload = CreatePayload(
            new ProjectPolicyUpdatedV1(CreatePolicy())
        );
        var validator = new ProjectPolicyMustExistValidator();

        Assert.False(validator.Validate(state, payload).Succeded);

        state.Policies.Add(PolicyId, CreatePolicy());

        Assert.True(validator.Validate(state, payload).Succeded);
    }

    [Fact]
    public void ProjectTopicRelation_RequiresUniqueExistingRelation()
    {
        var state = CreateProjectState();
        var addPayload = CreatePayload(
            new TopicRelationAddedToProjectV1(TopicName)
        );
        var removePayload = CreatePayload(
            new TopicRelationRemovedFromProjectV1(TopicName)
        );
        var mustNotExist = new ProjectTopicMustNotExistValidator();
        var mustExist = new ProjectTopicMustExistValidator();

        Assert.True(mustNotExist.Validate(state, addPayload).Succeded);
        Assert.False(mustExist.Validate(state, removePayload).Succeded);

        state.RelatedTopics.Add(TopicName);

        Assert.False(mustNotExist.Validate(state, addPayload).Succeded);
        Assert.True(mustExist.Validate(state, removePayload).Succeded);
    }

    [Fact]
    public void RepositoryRemoval_RequiresMatchingProjectMapping()
    {
        const string repositoryPath = "/workspace/project";
        var state = new RepositoryToProjectMapStateData(
            AggregateId.FromDatabaseGuid(Guid.Empty)
        );
        var payload = CreatePayload(
            new RepositoryToProjectMapRemovedV1(
                repositoryPath,
                ProjectId
            )
        );
        var validator = new RepositoryMappingMustExistValidator();

        Assert.False(validator.Validate(state, payload).Succeded);

        state.RepositoryToProjectMap.Add(
            repositoryPath,
            ProjectId
        );

        Assert.True(validator.Validate(state, payload).Succeded);
    }

    [Fact]
    public void RepositoryAddition_RequiresARepositoryNotAlreadyOnProject()
    {
        const string repositoryPath = "/workspace/project";
        var state = CreateProjectState();
        var payload = CreatePayload(
            new RepositoryAddedToProjectV1(repositoryPath)
        );
        var validator = new ProjectRepositoryMustNotExistValidator();

        Assert.True(validator.Validate(state, payload).Succeded);

        state.RepositoryPaths.Add(repositoryPath);

        Assert.False(validator.Validate(state, payload).Succeded);
    }

    private static GeneralPoliciesStateData CreateGeneralState() =>
        new(AggregateId.FromDatabaseGuid(Guid.Empty));

    private static ProjectPoliciesStateData CreateProjectState() =>
        new(ProjectId)
        {
            ProjectName = "Project",
            ProjectDescription = "Description"
        };

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
            ProjectId,
            "policy-test-state-machine",
            eventData
        );
}
