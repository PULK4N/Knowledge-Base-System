using ProjectModule.Application.Commands;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;

namespace ProjectModule.Application.Tests;

public sealed partial class ProjectCommandTests
{
    [Fact]
    public async Task GetProjectPolicyHistory_ReturnsOnlyTheChangesOfThatPolicy()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "History project",
                ProjectDescription = "Policy history test project.",
                RepositoryPaths = ["/workspace/history"]
            })
        ).ProjectId;

        var policyId = await AddProjectPolicy(handler, projectId, "Tracked");
        await AddProjectPolicy(handler, projectId, "Other");
        await ExecuteAsUser(new UpdateProjectPolicyCommand(handler)
        {
            ProjectId = projectId, PolicyId = policyId,
            Title = "Tracked updated", Description = "Description"
        });

        var query = new GetProjectPolicyHistoryQuery(CreateCalculator(), eventStore)
        {
            ProjectId = projectId,
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

    private static async Task<Guid> AddProjectPolicy(
        EventSourcing.Core.StateMachineHandler handler,
        Guid projectId,
        string title
    ) =>
        Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddProjectPolicyCommand(handler)
            {
                ProjectId = projectId, Title = title, Description = "Description"
            })
        ).PolicyId;
}
