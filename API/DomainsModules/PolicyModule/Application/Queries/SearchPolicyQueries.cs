using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Application.Queries;

public sealed class SearchGeneralPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PagedResult<PolicyDto>>(stateCalculator, eventStore)
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<PolicyDto>> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        return PolicySearch.Create(
            state is null
                ? Enumerable.Empty<Policy>()
                : state.Policies.Values,
            Page,
            PageSize,
            Search
        );
    }
}

public sealed class SearchTopicPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PagedResult<PolicyDto>?>(stateCalculator, eventStore)
{
    public required string TopicName { get; set; }
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(TopicName)
            && Pagination.IsValid(Page, PageSize)
        );

    protected override async Task<PagedResult<PolicyDto>?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        if (
            state is null
            || !state.Topics.TryGetValue(
                new TopicName(TopicName),
                out var topic
            )
        )
            return null;

        return PolicySearch.Create(
            topic.Policies.Values,
            Page,
            PageSize,
            Search
        );
    }
}

public sealed class SearchPolicyTopicsQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PagedResult<PolicyTopicSummaryDto>>(stateCalculator, eventStore)
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<PolicyTopicSummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );
        var topics = state?.Topics.Values.AsEnumerable()
            ?? Enumerable.Empty<Topic>();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var normalizedSearch = Search.Trim();
            topics = topics.Where(
                topic =>
                    topic.TopicName.Name.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || topic.Description.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        var ordered = topics
            .OrderBy(
                topic => topic.TopicName.Name,
                StringComparer.OrdinalIgnoreCase
            )
            .ThenBy(topic => topic.TopicName.Name, StringComparer.Ordinal)
            .ToList();

        return new PagedResult<PolicyTopicSummaryDto>(
            ordered
                .Skip(Pagination.Offset(Page, PageSize))
                .Take(PageSize)
                .Select(PolicyTopicSummaryDto.FromModel)
                .ToList(),
            Page,
            PageSize,
            ordered.Count
        );
    }
}

public sealed class SearchAgentFamilyPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PagedResult<PolicyDto>?>(stateCalculator, eventStore)
{
    public required string AgentFamilyName { get; set; }
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(AgentFamilyName)
            && Pagination.IsValid(Page, PageSize)
        );

    protected override async Task<PagedResult<PolicyDto>?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        if (
            state is null
            || !state.AgentFamilies.TryGetValue(
                Domain.Models.AgentFamilyName.Normalized(AgentFamilyName),
                out var agentFamily
            )
        )
            return null;

        return PolicySearch.Create(
            agentFamily.Policies.Values,
            Page,
            PageSize,
            Search
        );
    }
}

public sealed class SearchPolicyAgentFamiliesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PagedResult<PolicyAgentFamilySummaryDto>>(stateCalculator, eventStore)
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<PolicyAgentFamilySummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );
        var agentFamilies = state?.AgentFamilies.Values.AsEnumerable()
            ?? Enumerable.Empty<AgentFamily>();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var normalizedSearch = Search.Trim();
            agentFamilies = agentFamilies.Where(
                agentFamily =>
                    agentFamily.AgentFamilyName.Name.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || agentFamily.Description.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        var ordered = agentFamilies
            .OrderBy(
                agentFamily => agentFamily.AgentFamilyName.Name,
                StringComparer.OrdinalIgnoreCase
            )
            .ThenBy(
                agentFamily => agentFamily.AgentFamilyName.Name,
                StringComparer.Ordinal
            )
            .ToList();

        return new PagedResult<PolicyAgentFamilySummaryDto>(
            ordered
                .Skip(Pagination.Offset(Page, PageSize))
                .Take(PageSize)
                .Select(PolicyAgentFamilySummaryDto.FromModel)
                .ToList(),
            Page,
            PageSize,
            ordered.Count
        );
    }
}
