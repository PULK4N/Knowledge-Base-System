using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Services;
using FeatureModule.Domain;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Queries;

public sealed class ResolveFeatureReviewContextQuery(
    IFeatureReviewCandidateReader candidates, IEventStore eventStore, StateCalculator calculator)
    : Query<FeatureReviewContextDto?>
{
    public Guid ProjectId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty && !string.IsNullOrWhiteSpace(BranchName));

    protected override async Task<FeatureReviewContextDto?> ExecuteInternal(Executor executor)
    {
        var ids = (await candidates.ListFeatureIds(ProjectId)).Select(AggregateId.FromDatabaseGuid).ToList();
        if (ids.Count == 0)
            return null;
        var streams = await eventStore.GetEvents(ids);
        var matches = new List<FeatureReviewContextDto>();
        foreach (var history in streams.Values.Where(events => events.Count > 0))
        {
            var state = (FeatureStateData)calculator.Calculate(
                history.OrderBy(value => value.EventExecutionInfo.OrderNumber).ToList(), []).StateData;
            var context = FeatureReviewContextResolver.Resolve(state, ProjectId, BranchName);
            if (context is not null)
                matches.Add(context);
        }
        if (matches.Count > 1)
            throw new InvalidOperationException($"Branch '{BranchName}' belongs to several features in this project.");
        return matches.SingleOrDefault();
    }
}
