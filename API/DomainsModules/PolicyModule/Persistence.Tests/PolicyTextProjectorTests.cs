using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;

namespace PolicyModule.Persistence.Tests;

public sealed class PolicyTextProjectorTests
{
    [Fact]
    public async Task SessionText_ReturnsGeneralFamilyAndTopicsInRequestedOrder()
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        await Project(repository, CreateGeneralPolicies("General policy"));

        var text = await repository.Get(
            "  Codex ",
            [new TopicName("dotnet"), new TopicName("missing"), new TopicName("cloud")]
        );

        Assert.Equal(
            "# General policies\n\n## General policy\nGeneral text.",
            text.GeneralText
        );
        Assert.Equal(
            "# Agent family \"codex\" policies\n\n## Codex policy\nCodex text.",
            text.AgentFamilyText
        );
        Assert.Equal(
            [
                "# Topic \"dotnet\" policies\n\n## Dotnet policy\nDotnet text.",
                "# Topic \"cloud\" policies\n\n## Topic policy\nTopic text."
            ],
            text.TopicTexts
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("gemini")]
    public async Task SessionText_IsEmptyForAMissingFamily(string? agentFamily)
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        await Project(repository, CreateGeneralPolicies("General policy"));

        var text = await repository.Get(agentFamily, []);

        Assert.Equal(string.Empty, text.AgentFamilyText);
        Assert.Empty(text.TopicTexts);
        Assert.False(await repository.AgentFamilyExists("gemini"));
        Assert.True(await repository.AgentFamilyExists(" Claude "));
    }

    [Fact]
    public async Task SessionText_IsEmptyBeforeAnythingIsProjected()
    {
        await using var context = CreateContext();

        var text = await new PolicyTextRepository(context).Get("claude", [new TopicName("cloud")]);

        Assert.Equal(string.Empty, text.GeneralText);
        Assert.Equal(string.Empty, text.AgentFamilyText);
        Assert.Empty(text.TopicTexts);
    }

    [Fact]
    public async Task GeneralProjection_UpdatesOnlyGeneralText()
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        var general = CreateGeneralPolicies("General policy");
        await Project(repository, general);
        var topicText = context.TopicPolicyTexts.Single(text => text.TopicName == "cloud").Text;

        general.Policies.Clear();
        AddPolicy(
            general.Policies,
            "aaaaaaaa-4444-4444-4444-444444444444",
            "Updated general policy",
            "Updated general text."
        );
        await new GeneralPolicyTextProjector(repository).Update(
            [CreateStateInfo(general)]
        );

        Assert.Equal(
            "# General policies\n\n"
                + "## Updated general policy\nUpdated general text.",
            context.GeneralPolicyTexts.Single().Text
        );
        Assert.Equal(
            topicText,
            context.TopicPolicyTexts.Single(text => text.TopicName == "cloud").Text
        );
    }

    private static async Task Project(
        PolicyTextRepository repository,
        GeneralPoliciesStateData general
    )
    {
        var stateInfo = CreateStateInfo(general);
        await new GeneralPolicyTextProjector(repository).Update([stateInfo]);
        await new TopicPolicyTextProjector(repository).Update([stateInfo]);
        await new AgentFamilyPolicyTextProjector(repository).Update([stateInfo]);
    }

    private static TestPolicyDbContext CreateContext()
    {
        var context = new TestPolicyDbContext(
            new DbContextOptionsBuilder<TestPolicyDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private static GeneralPoliciesStateData CreateGeneralPolicies(
        string generalPolicyTitle
    )
    {
        var state = new GeneralPoliciesStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("99999999-9999-9999-9999-999999999999")
            )
        );
        AddPolicy(
            state.Policies,
            "aaaaaaaa-1111-1111-1111-111111111111",
            generalPolicyTitle,
            "General text."
        );
        AddTopic(state, "cloud", "aaaaaaaa-3333-3333-3333-333333333333", "Topic policy", "Topic text.");
        AddTopic(state, "dotnet", "aaaaaaaa-7777-7777-7777-777777777777", "Dotnet policy", "Dotnet text.");

        foreach (var (agentFamilyName, policyId, title, description) in
            new[]
            {
                ("claude", "aaaaaaaa-5555-5555-5555-555555555555",
                    "Claude policy", "Claude text."),
                ("codex", "aaaaaaaa-6666-6666-6666-666666666666",
                    "Codex policy", "Codex text.")
            })
        {
            var agentFamily = new AgentFamily
            {
                AgentFamilyName = new AgentFamilyName(agentFamilyName),
                Description = $"{agentFamilyName} policies."
            };
            AddPolicy(
                agentFamily.Policies,
                policyId,
                title,
                description
            );
            state.AgentFamilies.Add(
                agentFamily.AgentFamilyName,
                agentFamily
            );
        }

        return state;
    }

    private static void AddTopic(
        GeneralPoliciesStateData state,
        string name,
        string policyId,
        string title,
        string description
    )
    {
        var topic = new Topic
        {
            TopicName = new TopicName(name),
            Description = $"{name} policies."
        };
        AddPolicy(topic.Policies, policyId, title, description);
        state.Topics.Add(topic.TopicName, topic);
    }

    private static void AddPolicy(
        Dictionary<PolicyId, Policy> policies,
        string id,
        string title,
        string description
    )
    {
        var policy = new Policy
        {
            PolicyId = PolicyId.FromDatabaseGuid(Guid.Parse(id)),
            Title = title,
            Description = description
        };
        policies.Add(policy.PolicyId, policy);
    }

    private static StateInfo CreateStateInfo(GeneralPoliciesStateData stateData) =>
        StateInfo.Create(stateData, "test-state-machine", stateData.Id);
}
