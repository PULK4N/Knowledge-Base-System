using ProjectModule.Shared;
using ProjectModule.Domain.Models;

namespace ProjectModule.API.Requests;

public sealed record SetProjectReviewLevelModelsRequest
{
    public required Guid ProjectId { get; init; }

    public required Dictionary<ReviewLevel, ReviewModel> LevelModels { get; init; }
}
