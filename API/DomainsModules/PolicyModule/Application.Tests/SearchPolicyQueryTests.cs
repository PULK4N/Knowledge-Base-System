using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Queries;
using PolicyModule.Persistence.Interfaces;

namespace PolicyModule.Application.Tests;

public sealed class SearchPolicyQueryTests
{
    [Fact]
    public async Task Projects_search_maps_repository_results_and_metadata()
    {
        var projectId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var repository = new FakeProjectSummaryRepository(
            new PolicyProjectSummarySearchResult(
                [
                    new PolicyProjectSummary(
                        projectId,
                        "MCP Skill System",
                        ["/workspace/mcp-skill-system"]
                    )
                ],
                8
            )
        );
        var query = new SearchPolicyProjectsQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = "skill"
        };

        var result = await query.Execute(
            new Executor { Id = EventExecutor.New() }
        );

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(8, result.TotalCount);
        var project = Assert.Single(result.Items);
        Assert.Equal(projectId, project.ProjectId);
        Assert.Equal("MCP Skill System", project.ProjectName);
        Assert.Equal(["/workspace/mcp-skill-system"], project.RepositoryPaths);
        Assert.Equal((2, 5, "skill"), repository.LastSearchRequest);
    }

    private sealed class FakeProjectSummaryRepository(
        PolicyProjectSummarySearchResult result
    ) : IPolicyProjectSummaryRepository
    {
        public (int Page, int PageSize, string? Search)? LastSearchRequest { get; private set; }

        public Task<List<PolicyProjectSummary>> List() =>
            Task.FromResult(result.Items);

        public Task<PolicyProjectSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = (page, pageSize, search);
            return Task.FromResult(result);
        }
    }
}
