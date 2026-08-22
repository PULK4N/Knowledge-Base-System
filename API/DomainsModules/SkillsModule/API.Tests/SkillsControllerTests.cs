using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using SkillsModule.API.Controllers;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.API.Tests;

public sealed class SkillsControllerTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task Search_maps_query_parameters_and_returns_matches()
    {
        var skillId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var skillSearch = new FakeSkillSearch(
            [
                new SkillSearchResult(
                    new SkillSearchCandidate(
                        skillId,
                        "event-sourcing",
                        "SKILL.md",
                        0,
                        "Use event-sourced modules."
                    ),
                    0.032,
                    2,
                    1
                )
            ]
        );
        var searchQuery = new SearchSkillContentQuery(skillSearch)
        {
            SearchText = string.Empty
        };
        var controller = new SkillsController(
            new StubExecutorProvider()
        );

        var result = await controller.Search(
            searchQuery,
            "event sourced modules",
            3
        );

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var matches = Assert.IsType<List<SkillSearchMatchDto>>(ok.Value);
        var match = Assert.Single(matches);
        Assert.Equal(skillId.Value, match.SkillId);
        Assert.Equal("event sourced modules", skillSearch.LastQuery);
        Assert.Equal(3, skillSearch.LastOptions!.ResultCount);
        Assert.Equal(50, skillSearch.LastOptions.CandidateCount);
    }

    private sealed class StubExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class FakeSkillSearch(
        IReadOnlyList<SkillSearchResult> results
    ) : ISkillSearch
    {
        public string? LastQuery { get; private set; }
        public HybridSkillSearchOptions? LastOptions { get; private set; }

        public Task<IReadOnlyList<SkillSearchResult>> Search(
            string query,
            HybridSkillSearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            LastOptions = options;

            return Task.FromResult(results);
        }
    }
}
