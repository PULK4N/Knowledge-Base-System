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
    public async Task ProjectSummaryProjection_RefreshesBothRowsOfEachSkillLink()
    {
        await using var context = TestKnowledgeBaseDbContext.Create();
        var repository = new PolicyProjectSummaryRepository(context);
        var projector = new PolicyProjectSummaryProjector(repository);
        var project = CreateProject();
        var keptSkillId = Guid.Parse("21212121-0000-0000-0000-000000000001");
        var removedSkillId = Guid.Parse("21212121-0000-0000-0000-000000000002");
        project.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(keptSkillId));
        project.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(removedSkillId));

        await projector.Update([CreateStateInfo(project)]);

        Assert.Equal(4, context.EntityRelations.Count());
        Assert.Equal(
            [keptSkillId, removedSkillId],
            (await repository.GetSkillIds(ProjectId.Value)).Order().ToList()
        );
        Assert.Equal(
            "Policy project",
            context.EntityRelations
                .Single(
                    relation => relation.EntityId == keptSkillId
                        && relation.RelationType == ProjectEntityRelationTypes.Project
                )
                .RelatedEntitySummary
        );

        project.RelatedSkillIds.RemoveAt(1);
        await projector.Update([CreateStateInfo(project)]);

        Assert.Equal(2, context.EntityRelations.Count());
        Assert.Equal([keptSkillId], await repository.GetSkillIds(ProjectId.Value));

        project.IsDeleted = true;
        await projector.Update([CreateStateInfo(project)]);

        Assert.Empty(context.EntityRelations);
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

    [Fact]
    public async Task ProjectSummaryProjection_sorts_rest_pages_by_last_event_time_and_keeps_mcp_name_order()
    {
        await using var context = TestKnowledgeBaseDbContext.Create();
        var repository = new PolicyProjectSummaryRepository(context);
        var projector = new PolicyProjectSummaryProjector(repository);
        var states = new List<StateInfo>();
        foreach (var (name, minute) in new List<(string, int)> { ("Alpha", 1), ("Zulu", 2), ("Beta", 2) })
        {
            var project = new ProjectStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()))
            {
                ProjectName = name, RepositoryPaths = ["/workspace/sorting"]
            };
            var stateInfo = CreateStateInfo(project);
            stateInfo.LastUpdateTimestamp = DateTime.UnixEpoch.AddMinutes(minute);
            states.Add(stateInfo);
        }
        await projector.Update(states);
        await projector.Update(states);

        var page = await repository.Search(1, 2, "sorting");
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(new List<string> { "Beta", "Zulu" }, page.Items.Select(item => item.ProjectName).ToList());
        Assert.Equal("Alpha", Assert.Single((await repository.Search(2, 2, "sorting")).Items).ProjectName);
        Assert.Equal(new List<string> { "Alpha", "Beta", "Zulu" },
            (await repository.List()).Select(item => item.ProjectName).ToList());
        Assert.All(context.PolicyProjectSummaries.AsEnumerable(), entry =>
            Assert.Equal(entry.ProjectName == "Alpha" ? 1 : 2,
                (entry.UpdatedAt - DateTime.UnixEpoch).TotalMinutes));

        states[0].LastUpdateTimestamp = DateTime.UnixEpoch.AddMinutes(3);
        await projector.Update([states[0]]);
        Assert.Equal("Alpha", Assert.Single((await repository.Search(1, 1, null)).Items).ProjectName);
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
