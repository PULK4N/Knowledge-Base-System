using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Contracts;
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
        var repository = new StubSkillListRepository(
            [
                new SkillListItem(
                    skillId,
                    "event-sourcing",
                    "Build event-sourced modules",
                    ["dotnet"],
                    2,
                    1
                )
            ]
        );
        var query = new SearchSkillsQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = "event",
            Tag = "dotnet",
            HasReferences = true,
            HasAttachments = false,
            SortBy = SkillSearchSortField.ReferenceCount,
            SortDirection = SortDirection.Descending
        };

        var result = await query.Execute(
            new Executor { Id = EventExecutor.New() }
        );

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(6, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(skillId, item.SkillId);
        Assert.Equal("event-sourcing", item.Name);
        Assert.Equal("Build event-sourced modules", item.Description);
        Assert.Equal(["dotnet"], item.Tags);
        Assert.Equal(2, item.ReferenceCount);
        Assert.Equal(1, item.AttachmentCount);
        var request = Assert.IsType<
            EntityQuery<SkillSearchFilters, SkillSearchSortField>
        >(repository.LastSearchRequest);
        Assert.Equal(new PageRequest(2, 5), request.Page);
        Assert.Equal("event", request.Search);
        Assert.Equal(
            new SkillSearchFilters("dotnet", true, false),
            request.Filters
        );
        Assert.Equal(
            SkillSearchSortField.ReferenceCount,
            request.Sort.Field
        );
        Assert.Equal(SortDirection.Descending, request.Sort.Direction);
    }

    [Fact]
    public async Task Get_by_name_returns_the_matching_summary()
    {
        var skillId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var repository = new StubSkillSummaryRepository(
            [new SkillSummary(skillId, "event-sourcing")]
        );
        var query = new GetSkillByNameQuery(repository)
        {
            Name = "  EVENT-SOURCING  "
        };

        var result = await query.Execute(
            new Executor
            {
                Id = EventExecutor.FromDatabaseGuid(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                )
            }
        );

        Assert.Equal(
            new SkillSummaryDto(skillId, "event-sourcing"),
            result
        );
        Assert.Equal("  EVENT-SOURCING  ", repository.LastNameRequest);
    }

    private sealed class StubSkillSummaryRepository(
        List<SkillSummary> skills
    ) : ISkillSummaryRepository
    {
        public (int Page, int PageSize, string? Search)? LastSearchRequest { get; private set; }
        public string? LastNameRequest { get; private set; }

        public Task<List<SkillSummary>> List() =>
            Task.FromResult(skills);

        public Task<SkillSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        )
        {
            LastNameRequest = name;
            return Task.FromResult(
                skills.SingleOrDefault(
                    skill => string.Equals(
                        skill.Name,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );
        }

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

    private sealed class StubSkillListRepository(
        List<SkillListItem> skills
    ) : ISkillListRepository
    {
        public object? LastSearchRequest { get; private set; }

        public Task<PagedResult<SkillListItem>> Search(
            EntityQuery<SkillSearchFilters, SkillSearchSortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = request;
            return Task.FromResult(
                new PagedResult<SkillListItem>(
                    skills,
                    request.Page.Number,
                    request.Page.Size,
                    6
                )
            );
        }
    }
}
