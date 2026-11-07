namespace ProjectModule.API.Requests;

public sealed record RemoveTopicRelationFromProjectRequest
{
    public required Guid ProjectId { get; init; }

    public required string TopicName { get; init; }
}
