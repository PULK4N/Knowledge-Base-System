using System.ComponentModel.DataAnnotations;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.API.Controllers;
using FeatureModule.API.Requests;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Queries;
using FeatureModule.Contracts;
using FeatureModule.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FeatureModule.API.Tests;

public sealed class FeaturesControllerTests
{
    [Fact]
    public void Search_request_uses_shared_defaults()
    {
        var request = new SearchFeaturesRequest();

        Assert.Equal(Pagination.DefaultPage, request.Page);
        Assert.Equal(Pagination.DefaultPageSize, request.PageSize);
        Assert.Equal(FeatureSearchSortField.Name, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }

    [Fact]
    public void Search_request_rejects_undefined_enums_and_deep_offsets()
    {
        var undefinedEnums = new SearchFeaturesRequest
        {
            SortBy = (FeatureSearchSortField)999,
            SortDirection = (SortDirection)999
        };
        var enumResults = new List<ValidationResult>();

        var enumsAreValid = Validator.TryValidateObject(
            undefinedEnums,
            new ValidationContext(undefinedEnums),
            enumResults,
            validateAllProperties: true
        );

        Assert.False(enumsAreValid);
        Assert.Contains(
            enumResults,
            result => result.MemberNames.Contains(
                nameof(undefinedEnums.SortBy)
            )
        );
        Assert.Contains(
            enumResults,
            result => result.MemberNames.Contains(
                nameof(undefinedEnums.SortDirection)
            )
        );

        var deepOffset = new SearchFeaturesRequest
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
        var projectId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var repository = new CapturingFeatureSearchRepository();
        var query = new SearchFeaturesQuery(repository);
        var controller = new FeaturesController(
            new FixedExecutorProvider()
        );
        var request = new SearchFeaturesRequest
        {
            Page = 2,
            PageSize = 5,
            Search = "query",
            ProjectId = projectId,
            SortBy = FeatureSearchSortField.RecordCount,
            SortDirection = SortDirection.Descending
        };

        var response = await controller.List(query, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.IsType<PagedResult<FeatureSummaryDto>>(ok.Value);
        var mapped = Assert.IsType<
            EntityQuery<FeatureSearchFilters, FeatureSearchSortField>
        >(repository.LastRequest);
        Assert.Equal(new PageRequest(2, 5), mapped.Page);
        Assert.Equal("query", mapped.Search);
        Assert.Equal(
            new FeatureSearchFilters(projectId),
            mapped.Filters
        );
        Assert.Equal(FeatureSearchSortField.RecordCount, mapped.Sort.Field);
        Assert.Equal(SortDirection.Descending, mapped.Sort.Direction);
    }

    private sealed class CapturingFeatureSearchRepository
        : IFeatureSearchRepository
    {
        public object? LastRequest { get; private set; }

        public Task<PagedResult<FeatureSummary>> Search(
            EntityQuery<FeatureSearchFilters, FeatureSearchSortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastRequest = request;
            return Task.FromResult(
                new PagedResult<FeatureSummary>(
                    [],
                    request.Page.Number,
                    request.Page.Size,
                    0
                )
            );
        }
    }

    private sealed class FixedExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() =>
            Task.FromResult(
                new Executor
                {
                    Id = EventExecutor.FromDatabaseGuid(
                        Guid.Parse(
                            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                        )
                    )
                }
            );
    }
}
