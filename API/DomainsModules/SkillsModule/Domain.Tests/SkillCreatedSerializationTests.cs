using System.Collections.Immutable;
using Newtonsoft.Json;
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

        var json = JsonConvert.SerializeObject(eventData);
        var deserialized = Assert.IsType<SkillCreatedV1>(
            JsonConvert.DeserializeObject<SkillCreatedV1>(json)
        );

        Assert.Equal(eventData.Name, deserialized.Name);
        Assert.Equal(eventData.Tags.ToArray(), deserialized.Tags.ToArray());
        Assert.Equal(
            eventData.References["references/example.md"],
            deserialized.References["references/example.md"]
        );
        Assert.IsAssignableFrom<ISkillCreated>(deserialized);
    }
}
