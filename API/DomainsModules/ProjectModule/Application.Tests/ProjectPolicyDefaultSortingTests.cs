using ProjectModule.Application.Commands;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;

namespace ProjectModule.Application.Tests;

public sealed partial class ProjectCommandTests
{
    [Fact]
    public async Task Rest_project_policies_sort_before_paging_and_mcp_keeps_insertion_order()
    {
        var store = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(store);
        var projectId = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Sorting", ProjectDescription = "Sorting",
                RepositoryPaths = ["/workspace/sorting"]
            })).ProjectId;
        var first = await AddProjectPolicy(handler, projectId, "Alpha");
        var second = await AddProjectPolicy(handler, projectId, "Zulu");
        var query = new SearchProjectPoliciesQuery(CreateCalculator(), store)
        {
            ProjectId = projectId, PageSize = 1
        };
        Assert.Equal(second, Assert.Single((await query.Execute(Executor))!.Items).PolicyId);
        query.Page = 2;
        Assert.Equal(first, Assert.Single((await query.Execute(Executor))!.Items).PolicyId);

        await ExecuteAsUser(new UpdateProjectPolicyCommand(handler)
        {
            ProjectId = projectId, PolicyId = first, Title = "Alpha updated", Description = "Description"
        });
        query.Page = 1;
        Assert.Equal(first, Assert.Single((await query.Execute(Executor))!.Items).PolicyId);
        var mcpList = await new ListProjectPoliciesQuery(CreateCalculator(), store)
        {
            ProjectId = projectId
        }.Execute(Executor);
        Assert.Equal(new List<Guid> { first, second }, mcpList!.Select(policy => policy.PolicyId).ToList());
    }
}
