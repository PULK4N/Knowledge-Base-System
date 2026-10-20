using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// Builds the branch graph shown to users and AI agents. Branches of completed
/// plans are already merged, so they are left out and the branches created
/// from them point to where they came from instead: main ← x1 ← x2 becomes
/// main ← x2 once x1 is completed. Nodes are ordered so every branch comes
/// after the branches it came from, starting with the feature branch.
/// </summary>
public static class FeatureBranchGraphBuilder
{
    public const string FeatureBranchNodeKey = "feature";

    public static List<FeatureBranchGraphNodeDto> Build(FeatureStateData state)
    {
        var plans = state.Plans.ToDictionary(plan => plan.Id);
        var edges = FeatureBranchGraph.Build(state.Plans);
        var from = edges.ToDictionary(edge => edge.Branch, edge => edge.From);
        var hidden = edges
            .Select(edge => edge.Branch)
            .Where(key => key.PlanId is { } planId
                && plans[planId].Status == FeaturePlanStatus.Completed)
            .ToHashSet();

        List<FeatureBranchKey> Resolve(
            FeatureBranchKey key,
            HashSet<FeatureBranchKey> visiting
        ) =>
            !hidden.Contains(key)
                ? [key]
                : !visiting.Add(key) || !from.TryGetValue(key, out var sources)
                    ? [FeatureBranchKey.FeatureBranch]
                    : sources.SelectMany(source => Resolve(source, visiting)).Distinct().ToList();

        var visible = edges
            .Where(edge => !hidden.Contains(edge.Branch))
            .Select(edge => (
                edge.Branch,
                From: edge.From
                    .SelectMany(source => Resolve(source, []))
                    .Distinct()
                    .ToList()
            ))
            .ToList();

        var nodes = new List<FeatureBranchGraphNodeDto>
        {
            new(
                FeatureBranchNodeKey,
                state.Branch?.Name,
                state.Branch?.Description ?? string.Empty,
                null,
                null,
                null,
                false,
                []
            )
        };
        var emitted = new HashSet<FeatureBranchKey>
        {
            FeatureBranchKey.FeatureBranch
        };

        while (visible.Count > 0)
        {
            // The first branch whose sources are all shown; a loop cannot be
            // saved, but if one were present the first branch is taken anyway.
            var next = visible.FirstOrDefault(edge => edge.From.All(emitted.Contains));
            if (next == default)
                next = visible[0];

            visible.Remove(next);
            emitted.Add(next.Branch);
            nodes.Add(ToNode(next.Branch, next.From, plans));
        }

        return nodes;
    }

    public static string Key(FeatureBranchKey key) =>
        key.PlanId is { } planId
            ? $"{planId.Value:N}:{key.BranchName}"
            : FeatureBranchNodeKey;

    private static FeatureBranchGraphNodeDto ToNode(
        FeatureBranchKey key,
        List<FeatureBranchKey> from,
        Dictionary<FeaturePlanId, FeaturePlan> plans
    )
    {
        var plan = plans[key.PlanId!.Value];
        var branch = plan.FindBranch(key.BranchName)!;

        return new FeatureBranchGraphNodeDto(
            Key(key),
            branch.Name,
            branch.Description,
            plan.Id.Value,
            plan.Title,
            plan.Status,
            plan.FindBaseBranch(branch.Name) is not null,
            from.Select(Key).ToList()
        );
    }
}
