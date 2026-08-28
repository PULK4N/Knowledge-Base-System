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
                        "MCP Knowledge Base",
                        ["/workspace/mcp-knowledge-base"]
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
        Assert.Equal("MCP Knowledge Base", project.ProjectName);
        Assert.Equal(["/workspace/mcp-knowledge-base"], project.RepositoryPaths);
        Assert.Equal((2, 5, "skill"), repository.LastSearchRequest);
    }

    [Fact]
    public async Task Project_list_and_get_by_name_map_summary_results()
    {
        var projectId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var repository = new FakeProjectSummaryRepository(
            new PolicyProjectSummarySearchResult(
                [
                    new PolicyProjectSummary(
                        projectId,
                        "KnowledgeBaseSystem",
                        ["/workspace/knowledge-base"]
                    )
                ],
                1
            )
        );
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

        var projects = await new ListPolicyProjectsQuery(repository)
            .Execute(executor);
        var project = await new GetPolicyProjectByNameQuery(repository)
        {
            Name = "  KNOWLEDGEBASESYSTEM "
        }.Execute(executor);

        AssertSummary(Assert.Single(projects));
        AssertSummary(Assert.IsType<PolicyProjectSummaryDto>(project));

        void AssertSummary(PolicyProjectSummaryDto summary)
        {
            Assert.Equal(projectId, summary.ProjectId);
            Assert.Equal("KnowledgeBaseSystem", summary.ProjectName);
            Assert.Equal(
                ["/workspace/knowledge-base"],
                summary.RepositoryPaths
            );
        }
    }

    private sealed class FakeProjectSummaryRepository(
        PolicyProjectSummarySearchResult result
    ) : IPolicyProjectSummaryRepository
    {
        public (int Page, int PageSize, string? Search)? LastSearchRequest { get; private set; }

        public Task<List<PolicyProjectSummary>> List() =>
            Task.FromResult(result.Items);

        public Task<PolicyProjectSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                result.Items.SingleOrDefault(
                    project => string.Equals(
                        project.ProjectName,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

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
