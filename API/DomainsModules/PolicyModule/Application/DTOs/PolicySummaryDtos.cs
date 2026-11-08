using PolicyModule.Domain.Models;

namespace PolicyModule.Application.DTOs;

public sealed record PolicyTopicSummaryDto(
    string TopicName,
    string Description,
    int PolicyCount
)
{
    public static PolicyTopicSummaryDto FromModel(Topic topic) =>
        new(
            topic.TopicName.Name,
            topic.Description,
            topic.Policies.Count
        );
}

public sealed record PolicyAgentFamilySummaryDto(
    string AgentFamilyName,
    string Description,
    int PolicyCount
)
{
    public static PolicyAgentFamilySummaryDto FromModel(
        AgentFamily agentFamily
    ) =>
        new(
            agentFamily.AgentFamilyName.Name,
            agentFamily.Description,
            agentFamily.Policies.Count
        );
}
