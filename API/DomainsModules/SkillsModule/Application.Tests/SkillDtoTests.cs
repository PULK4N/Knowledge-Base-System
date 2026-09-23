using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Domain;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Tests;

public sealed class SkillDtoTests
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
        var state = new SkillStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        );
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                "SkillCreatedV3",
                timestamp,
                AggregateId.FromDatabaseGuid(memoryGuid)
            )
        );

        var history = Assert.Single(SkillDto.FromStateData(state).MemoryHistory);

        Assert.Equal(
            new SkillMemoryHistoryDto(
                "SkillCreatedV3",
                timestamp,
                memoryGuid,
                isUserOriginated
            ),
            history
        );
    }
}
