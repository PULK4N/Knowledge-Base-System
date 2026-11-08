namespace ProjectModule.API.Requests;

public sealed record AddTopicRelationToProjectRequest
{
    public required Guid ProjectId { get; init; }

    public required string TopicName { get; init; }
}
