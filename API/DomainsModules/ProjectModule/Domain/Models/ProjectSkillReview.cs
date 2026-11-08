using ProjectModule.Shared;
namespace ProjectModule.Domain.Models;

/// <summary>How a linked skill takes part in the project's code review.</summary>
public sealed record ProjectSkillReview(
    bool UsedForReview,
    string ReviewNotes,
    ReviewLevel? ReviewLevel
);
