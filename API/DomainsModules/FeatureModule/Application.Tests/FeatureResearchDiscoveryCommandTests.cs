using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeatureResearchDiscoveryCommandTests
{
    private static readonly Executor Executor = new()
    {
        Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid())
    };

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Projection replay", true)]
    public async Task Add_and_update_require_a_nonblank_title(
        string title,
        bool expected
    )
    {
        var add = new AddFeatureResearchDiscoveryCommand(null!)
        {
            FeatureId = Guid.NewGuid(),
            Title = title,
            Content = "Replay rebuilds the projection.",
            SourceType = FeatureResearchDiscoverySourceType.Code
        };
        var update = new UpdateFeatureResearchDiscoveryCommand(null!)
        {
            FeatureId = Guid.NewGuid(),
            DiscoveryId = Guid.NewGuid(),
            Title = title,
            Content = "Replay rebuilds the projection.",
            SourceType = FeatureResearchDiscoverySourceType.Code
        };

        Assert.Equal(expected, await add.CanExecute(Executor));
        Assert.Equal(expected, await update.CanExecute(Executor));
    }
}
