using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Tests;

public sealed class FeatureDtoTests
{
    [Theory]
    [InlineData("cccccccc-cccc-cccc-cccc-cccccccccccc", false)]
    [InlineData(null, true)]
    public void FromStateData_MapsMemoryHistory(
        string? memoryId,
        bool isUserOriginated
    )
    {
        var memoryGuid = memoryId is null
            ? SharedModule.Constants.MemoryAggregateIds.User
            : Guid.Parse(memoryId);
        var timestamp = new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc);
        var state = new FeatureStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        );
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                "FeatureAddedV2",
                timestamp,
                AggregateId.FromDatabaseGuid(memoryGuid)
            )
        );

        var history = Assert.Single(FeatureDto.FromStateData(state).MemoryHistory);

        Assert.Equal(
            new FeatureMemoryHistoryDto(
                "FeatureAddedV2",
                timestamp,
                memoryGuid,
                isUserOriginated
            ),
            history
        );
    }
}
