using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Application.Queries;

public enum PolicyScope
{
    General,
    Topic,
    AgentFamily,
    Project
}

/// <summary>
/// Returns one active policy with the changes recorded for it. History is kept
/// per aggregate, so the records of the owning aggregate are filtered by policy.
/// </summary>
public sealed class GetPolicyHistoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PolicyHistoryDto?>(stateCalculator, eventStore)
{
    public required PolicyScope Scope { get; set; }

    /// <summary>Topic or agent family name; unused for general and project policies.</summary>
    public string? ScopeName { get; set; }

    public Guid ProjectId { get; set; }

    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            PolicyId != Guid.Empty
            && Scope switch
            {
                PolicyScope.Topic or PolicyScope.AgentFamily =>
                    !string.IsNullOrWhiteSpace(ScopeName),
                PolicyScope.Project => ProjectId != Guid.Empty,
                _ => true
            }
        );

    protected override async Task<PolicyHistoryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var policyId = Domain.Models.PolicyId.FromDatabaseGuid(PolicyId);
        if (Scope == PolicyScope.Project)
        {
            var projectId = AggregateId.FromDatabaseGuid(ProjectId);
            var project = await Replay<ProjectPoliciesStateData>(
                await GetEvents([projectId]),
                projectId
            );

            return project is null || project.IsDeleted
                ? null
                : Find(project.Policies, policyId, project.MemoryHistory);
        }

        var generalId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var general = await Replay<GeneralPoliciesStateData>(
            await GetEvents([generalId]),
            generalId
        );
        if (general is null)
            return null;

        var policies = Scope switch
        {
            PolicyScope.Topic => general.Topics.GetValueOrDefault(
                new TopicName(ScopeName!)
            )?.Policies,
            PolicyScope.AgentFamily => general.AgentFamilies.GetValueOrDefault(
                AgentFamilyName.Normalized(ScopeName!)
            )?.Policies,
            _ => general.Policies
        };

        return policies is null
            ? null
            : Find(policies, policyId, general.MemoryHistory);
    }

    private static PolicyHistoryDto? Find(
        Dictionary<PolicyId, Policy> policies,
        PolicyId policyId,
        List<MemoryHistoryRecord> history
    ) =>
        policies.TryGetValue(policyId, out var policy)
            ? PolicyHistoryDto.FromModel(policy, history)
            : null;
}
