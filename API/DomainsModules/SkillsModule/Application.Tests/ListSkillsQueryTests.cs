using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Tests;

public sealed class ListSkillsQueryTests
{
    [Fact]
    public async Task Execute_ReturnsSkillIdsAndNamesFromProjection()
    {
        var firstId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var secondId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var query = new ListSkillsQuery(
            new StubSkillSummaryRepository(
                [
                    new SkillSummary(firstId, "first"),
                    new SkillSummary(secondId, "second")
                ]
            )
        );

        var skills = await query.Execute(
            new Executor
            {
                Id = EventExecutor.New()
            }
        );

        Assert.Equal(
            [
                new SkillSummaryDto(firstId, "first"),
                new SkillSummaryDto(secondId, "second")
            ],
            skills
        );
    }

    private sealed class StubSkillSummaryRepository(
        List<SkillSummary> skills
    ) : ISkillSummaryRepository
    {
        public Task<List<SkillSummary>> List() =>
            Task.FromResult(skills);
    }
}
