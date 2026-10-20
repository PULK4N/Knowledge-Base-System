using System.Text.Json;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureCompositeBranchTests
{
    [Theory]
    [InlineData("feature/all-for-testing")]
    [InlineData(null)]
    public void Setting_or_clearing_the_composite_branch_replays_without_changing_the_main_branch(string? branchName)
    {
        var featureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var memoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var state = new FeatureStateData(featureId)
        {
            Branch = new FeatureBranch { Name = "feature/main", Description = "Main branch" },
            CompositeBranchName = "feature/old-testing"
        };
        var original = new FeatureCompositeBranchSetV1(branchName, Guid.NewGuid(), memoryId);
        var restored = JsonSerializer.Deserialize<FeatureCompositeBranchSetV1>(JsonSerializer.Serialize(original));
        var payload = EventPayload.Create(EventExecutor.FromDatabaseGuid(Guid.NewGuid()), featureId,
            "features-state-machine", restored);

        restored.Apply(state, payload.EventExecutionInfo);

        Assert.Equal(original, restored);
        Assert.Equal(branchName, state.CompositeBranchName);
        Assert.Equal("feature/main", state.Branch.Name);
        Assert.Equal(memoryId, Assert.Single(state.MemoryHistory).AggregateId);
    }
}
