using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Tests;

public sealed class ListSkillsQueryTests
{
    [Fact]
    public async Task Execute_ReturnsSkillIdsAndNamesFromProjection()
    {
        var firstId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var secondId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var query = new ListSkillsQuery(
            new StubSkillSummaryRepository(
                [
                    new SkillSummary(firstId, "first"),
                    new SkillSummary(secondId, "second")
                ]
            )
        );

        var skills = await query.Execute(
            new Executor
            {
                Id = EventExecutor.New()
            }
        );

        Assert.Equal(
            [
                new SkillSummaryDto(firstId, "first"),
                new SkillSummaryDto(secondId, "second")
            ],
            skills
        );
    }

    [Fact]
    public async Task Search_returns_pagination_metadata_and_mapped_summaries()
    {
        var skillId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var repository = new StubSkillSummaryRepository(
            [new SkillSummary(skillId, "event-sourcing")]
        );
        var query = new SearchSkillsQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = "event"
        };

        var result = await query.Execute(
            new Executor { Id = EventExecutor.New() }
        );

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(
            new SkillSummaryDto(skillId, "event-sourcing"),
            Assert.Single(result.Items)
        );
        Assert.Equal((2, 5, "event"), repository.LastSearchRequest);
    }

    private sealed class StubSkillSummaryRepository(
        List<SkillSummary> skills
    ) : ISkillSummaryRepository
    {
        public (int Page, int PageSize, string? Search)? LastSearchRequest { get; private set; }

        public Task<List<SkillSummary>> List() =>
            Task.FromResult(skills);

        public Task<SkillSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = (page, pageSize, search);
            return Task.FromResult(
                new SkillSummarySearchResult(skills, 6)
            );
        }
    }
}
