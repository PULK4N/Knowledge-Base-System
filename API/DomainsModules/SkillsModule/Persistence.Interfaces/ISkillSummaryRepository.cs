namespace SkillsModule.Persistence.Interfaces;

public interface ISkillSummaryRepository
{
    Task<List<SkillSummary>> List();

    Task<SkillSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );
}

public sealed record SkillSummary(
    Guid SkillId,
    string Name
);

public sealed record SkillSummarySearchResult(
    List<SkillSummary> Items,
    int TotalCount
);
