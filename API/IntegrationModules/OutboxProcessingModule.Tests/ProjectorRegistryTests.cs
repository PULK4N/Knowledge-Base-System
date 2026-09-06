using EventSourcing.Shared.Interfaces;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Tests;

public sealed class ProjectorRegistryTests
{
    [Fact]
    public void GetRequiredReturnsTheProjectorRegisteredUnderItsClassName()
    {
        var projector = new RecordingProjector();
        var registry = new ProjectorRegistry([ projector ]);

        Assert.Same(projector, registry.GetRequired(nameof(RecordingProjector)));
    }

    [Fact]
    public void GetRequiredThrowsForAnUnregisteredName()
    {
        var registry = new ProjectorRegistry([ ]);

        Assert.Throws<InvalidOperationException>(
            () => registry.GetRequired(nameof(RecordingProjector)));
    }

    [Fact]
    public void ConstructionThrowsWhenTwoProjectorsShareAName()
    {
        List<IProjector> projectors = [ new RecordingProjector(), new RecordingProjector() ];

        Assert.Throws<InvalidOperationException>(() => new ProjectorRegistry(projectors));
    }
}
