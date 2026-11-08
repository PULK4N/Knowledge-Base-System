namespace SkillsModule.Persistence.Interfaces;

public interface ISkillSummaryRepository
{
    Task<List<SkillSummary>> List();

    Task<SkillSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    );

    Task<SkillSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns the active skills among <paramref name="skillIds"/>, ordered by name.</summary>
    Task<List<SkillUseScenarios>> GetUseScenarios(
        List<Guid> skillIds,
        CancellationToken cancellationToken = default
    );
}

public sealed record SkillSummary(
    Guid SkillId,
    string Name
);

public sealed record SkillUseScenarios(
    Guid SkillId,
    string Name,
    List<string> UseScenarios
);

public sealed record SkillSummarySearchResult(
    List<SkillSummary> Items,
    int TotalCount
);
