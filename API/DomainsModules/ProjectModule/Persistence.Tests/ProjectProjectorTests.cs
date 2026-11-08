using EventSourcing.Shared.Models;
using ProjectModule.Domain;
using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Persistence.Tests;

public sealed class ProjectProjectorTests
{
    private static readonly AggregateId ProjectId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public async Task ProjectText_ReturnsCompiledPoliciesAndTopicsInLinkOrder()
    {
        await using var context = TestKnowledgeBaseDbContext.Create();
        var repository = new ProjectPolicyTextRepository(context);
        var stateInfo = CreateStateInfo(CreateProject());

        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);
        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);

        var text = Assert.IsType<ProjectPolicyTextEntry>(
            await repository.Get(ProjectId)
        );
        Assert.Equal(
            "# Project \"Policy project\" policies\n\n## Project policy\nProject text.",
            text.Text
        );
        Assert.Equal(["dotnet", "cloud"], text.TopicNames);
        Assert.Single(context.ProjectPolicyTexts);
        Assert.Equal(2, context.ProjectPolicyTopics.Count());
    }

    [Fact]
    public async Task DeletedProject_RemovesItsTextAndTopicRelations()
    {
        await using var context = TestKnowledgeBaseDbContext.Create();
        var repository = new ProjectPolicyTextRepository(context);
        var project = CreateProject();
        var stateInfo = CreateStateInfo(project);
        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);

        project.IsDeleted = true;
        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);

        Assert.Empty(context.ProjectPolicyTexts);
        Assert.Empty(context.ProjectPolicyTopics);
        Assert.Null(await repository.Get(ProjectId));
    }

    [Fact]
    public async Task ProjectSummaryProjection_ListsNamesAndRepositoryPaths()
    {
        await using var context = TestKnowledgeBaseDbContext.Create();
        var repository = new PolicyProjectSummaryRepository(context);
        var project = CreateProject();
        var stateInfo = CreateStateInfo(project);
        var projector = new PolicyProjectSummaryProjector(repository);

        await projector.Update([stateInfo]);

        var summary = Assert.Single(await repository.List());
        Assert.Equal(ProjectId.Value, summary.ProjectId);
        Assert.Equal("Policy project", summary.ProjectName);
        Assert.Equal(
            ["/workspace/policy-project"],
            summary.RepositoryPaths
        );
        Assert.Equal(
            ProjectId.Value,
            (await repository.GetByName("  POLICY PROJECT "))?.ProjectId
        );
        Assert.Null(await repository.GetByName("missing"));

        project.ProjectName = "Renamed policy project";
        project.RepositoryPaths.Add("/workspace/secondary");
        await projector.Update([stateInfo]);

        summary = Assert.Single(await repository.List());
        Assert.Equal("Renamed policy project", summary.ProjectName);
        Assert.Equal(
            [
                "/workspace/policy-project",
                "/workspace/secondary"
            ],
            summary.RepositoryPaths
        );

        project.IsDeleted = true;
        await projector.Update([stateInfo]);

        Assert.Empty(await repository.List());
    }

    private static ProjectStateData CreateProject()
    {
        var state = new ProjectStateData(ProjectId)
        {
            ProjectName = "Policy project",
            ProjectDescription = "Projection test project.",
            RepositoryPaths = ["/workspace/policy-project"]
        };
        var policy = new Policy
        {
            PolicyId = PolicyId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222")
            ),
            Title = "Project policy",
            Description = "Project text."
        };
        state.Policies.Add(policy.PolicyId, policy);
        state.RelatedTopics.Add(new TopicName("dotnet"));
        state.RelatedTopics.Add(new TopicName("cloud"));

        return state;
    }

    private static StateInfo CreateStateInfo(ProjectStateData stateData) =>
        StateInfo.Create(stateData, "test-state-machine", stateData.Id);
}
