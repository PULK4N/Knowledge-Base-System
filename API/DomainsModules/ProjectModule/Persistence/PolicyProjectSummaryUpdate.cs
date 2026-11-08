using ProjectModule.Domain;

namespace ProjectModule.Persistence;

public sealed record PolicyProjectSummaryUpdate(
    ProjectStateData State,
    DateTime UpdatedAt
);
