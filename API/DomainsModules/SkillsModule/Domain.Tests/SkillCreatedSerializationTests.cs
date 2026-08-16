using System.Collections.Immutable;
using System.Text.Json;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Tests;

public sealed class SkillCreatedSerializationTests
{
    [Fact]
    public void CreatedV1_RoundTripsImmutablePayload()
    {
        var eventData = new SkillCreatedV1(
            "skill-name",
            "Description",
            "Content",
            ImmutableArray.Create("tag-one", "tag-two"),
            ImmutableDictionary<string, SkillReference>
                .Empty
                .WithComparers(StringComparer.Ordinal)
                .Add(
                    "references/example.md",
                    new SkillReference("Reference content")
                )
        );

        var json = JsonSerializer.Serialize(eventData);
        var deserialized = Assert.IsType<SkillCreatedV1>(
            JsonSerializer.Deserialize<SkillCreatedV1>(json)
        );

        Assert.Equal(eventData.Name, deserialized.Name);
        Assert.Equal(eventData.Tags.ToArray(), deserialized.Tags.ToArray());
        Assert.Equal(
            eventData.References["references/example.md"],
            deserialized.References["references/example.md"]
        );
        Assert.IsAssignableFrom<ISkillCreated>(deserialized);
    }

    [Fact]
    public void CreatedV2_RoundTripsAutomaticReferenceLoading()
    {
        var eventData = new SkillCreatedV2(
            "skill-name",
            "Description",
            "Content",
            ImmutableArray<string>.Empty,
            ImmutableDictionary<string, SkillReference2>
                .Empty
                .WithComparers(StringComparer.Ordinal)
                .Add(
                    "references/example.md",
                    new SkillReference2("Reference content", true)
                )
        );

        var json = JsonSerializer.Serialize(eventData);
        var deserialized = Assert.IsType<SkillCreatedV2>(
            JsonSerializer.Deserialize<SkillCreatedV2>(json)
        );

        Assert.True(
            deserialized
                .References["references/example.md"]
                .LoadAutomatically
        );
        Assert.IsAssignableFrom<ISkillCreated>(deserialized);
    }
}
