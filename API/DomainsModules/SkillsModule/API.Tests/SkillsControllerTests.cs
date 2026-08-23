using System.ComponentModel.DataAnnotations;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using SkillsModule.API.Controllers;
using SkillsModule.API.Requests;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Contracts;
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
    public void List_request_uses_shared_defaults()
    {
        var request = new SearchSkillsRequest();

        Assert.Equal(Pagination.DefaultPage, request.Page);
        Assert.Equal(Pagination.DefaultPageSize, request.PageSize);
        Assert.Equal(SkillSearchSortField.Name, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }

    [Fact]
    public void List_request_rejects_invalid_enums_filters_and_deep_offsets()
    {
        var invalid = new SearchSkillsRequest
        {
            Tag = new string('x', EntityQueryLimits.MaximumSearchLength + 1),
            SortBy = (SkillSearchSortField)999,
            SortDirection = (SortDirection)999
        };
        var invalidResults = new List<ValidationResult>();

        var requestIsValid = Validator.TryValidateObject(
            invalid,
            new ValidationContext(invalid),
            invalidResults,
            validateAllProperties: true
        );

        Assert.False(requestIsValid);
        Assert.Contains(
            invalidResults,
            result => result.MemberNames.Contains(nameof(invalid.Tag))
        );
        Assert.Contains(
            invalidResults,
            result => result.MemberNames.Contains(nameof(invalid.SortBy))
        );
        Assert.Contains(
            invalidResults,
            result => result.MemberNames.Contains(
                nameof(invalid.SortDirection)
            )
        );

        var deepOffset = new SearchSkillsRequest
        {
            Page = Pagination.MaximumPage,
            PageSize = 2
        };
        var offsetResults = new List<ValidationResult>();

        var offsetIsValid = Validator.TryValidateObject(
            deepOffset,
            new ValidationContext(deepOffset),
            offsetResults,
            validateAllProperties: true
        );

        Assert.False(offsetIsValid);
        Assert.Contains(
            offsetResults,
            result => result.MemberNames.Contains(
                nameof(deepOffset.PageSize)
            )
        );
    }

    [Fact]
    public async Task List_maps_the_request_to_the_scoped_query()
    {
        var repository = new CapturingSkillListRepository();
        var query = new SearchSkillsQuery(repository);
        var controller = new SkillsController(new StubExecutorProvider());
        var request = new SearchSkillsRequest
        {
            Page = 2,
            PageSize = 5,
            Search = "query",
            Tag = "dotnet",
            HasReferences = true,
            HasAttachments = false,
            SortBy = SkillSearchSortField.AttachmentCount,
            SortDirection = SortDirection.Descending
        };

        var response = await controller.List(query, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.IsType<PagedResult<SkillListItemDto>>(ok.Value);
        var mapped = Assert.IsType<
            EntityQuery<SkillSearchFilters, SkillSearchSortField>
        >(repository.LastRequest);
        Assert.Equal(new PageRequest(2, 5), mapped.Page);
        Assert.Equal("query", mapped.Search);
        Assert.Equal(
            new SkillSearchFilters("dotnet", true, false),
            mapped.Filters
        );
        Assert.Equal(
            SkillSearchSortField.AttachmentCount,
            mapped.Sort.Field
        );
        Assert.Equal(SortDirection.Descending, mapped.Sort.Direction);
    }

    [Fact]
    public async Task Search_maps_query_and_uses_default_result_count()
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
            "event sourced modules"
        );

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var matches = Assert.IsType<List<SkillSearchMatchDto>>(ok.Value);
        var match = Assert.Single(matches);
        Assert.Equal(skillId.Value, match.SkillId);
        Assert.Equal("event sourced modules", skillSearch.LastQuery);
        Assert.Equal(5, skillSearch.LastOptions!.ResultCount);
        Assert.Equal(50, skillSearch.LastOptions.CandidateCount);
    }

    private sealed class StubExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class CapturingSkillListRepository
        : ISkillListRepository
    {
        public object? LastRequest { get; private set; }

        public Task<PagedResult<SkillListItem>> Search(
            EntityQuery<SkillSearchFilters, SkillSearchSortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastRequest = request;
            return Task.FromResult(
                new PagedResult<SkillListItem>(
                    [],
                    request.Page.Number,
                    request.Page.Size,
                    0
                )
            );
        }
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
