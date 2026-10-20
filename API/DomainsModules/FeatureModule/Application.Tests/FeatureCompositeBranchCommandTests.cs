using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeatureCompositeBranchCommandTests
{
    [Theory]
    [InlineData("status", null, "feature/old")]
    [InlineData("summary", null, "feature/old")]
    [InlineData("status", " feature/testing ", "feature/testing")]
    [InlineData("summary", " feature/testing ", "feature/testing")]
    [InlineData("set", " feature/testing ", "feature/testing")]
    [InlineData("status", " ", null)]
    [InlineData("summary", "", null)]
    [InlineData("set", "", null)]
    public async Task Commands_set_replace_clear_or_preserve_the_composite_branch(
        string kind, string? branchName, string? expected)
    {
        var featureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var store = new FeatureCommandEventStore(featureId);
        FeatureCommand command = kind switch
        {
            "status" => new UpdateFeatureStatusCommand(store.CreateHandler())
            {
                FeatureId = featureId.Value, Status = "Testing", CompositeBranchName = branchName
            },
            "summary" => new UpdateFeatureSummaryCommand(store.CreateHandler())
            {
                FeatureId = featureId.Value, Summary = "Overview", CompositeBranchName = branchName
            },
            _ => new SetFeatureCompositeBranchCommand(store.CreateHandler())
            {
                FeatureId = featureId.Value, BranchName = branchName!
            }
        };
        command.UseUserOrigin();

        await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });

        Assert.Equal(1, store.WriteCount);
        Assert.Equal(kind == "set" || branchName is null ? 1 : 2, store.Written.Count);
        var state = new FeatureStateData(featureId)
        {
            Branch = new FeatureBranch { Name = "feature/main" }, CompositeBranchName = "feature/old"
        };
        foreach (var payload in store.Written)
            payload.EventData.Apply(state, payload.EventExecutionInfo);
        var dto = FeatureDto.FromStateData(state);
        Assert.Equal(expected, dto.CompositeBranchName);
        Assert.Equal("feature/main", dto.Branch!.Name);
    }
}
