namespace PolicyModule.API.Requests;

public sealed record AddGeneralPolicyRequest
{
    public required string Title { get; init; }

    public required string Description { get; init; }
}

public sealed record UpdateGeneralPolicyRequest
{
    public required Guid PolicyId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}

public sealed record RemoveGeneralPolicyRequest
{
    public required Guid PolicyId { get; init; }
}

public sealed record CreateAgentFamilyRequest
{
    public required string AgentFamilyName { get; init; }

    public required string Description { get; init; }
}

public sealed record UpdateAgentFamilyRequest
{
    public required string AgentFamilyName { get; init; }

    public required string Description { get; init; }
}

public sealed record RemoveAgentFamilyRequest
{
    public required string AgentFamilyName { get; init; }
}

public sealed record AddAgentFamilyPolicyRequest
{
    public required string AgentFamilyName { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}

public sealed record UpdateAgentFamilyPolicyRequest
{
    public required string AgentFamilyName { get; init; }

    public required Guid PolicyId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}

public sealed record RemoveAgentFamilyPolicyRequest
{
    public required string AgentFamilyName { get; init; }

    public required Guid PolicyId { get; init; }
}

public sealed record CreateTopicRequest
{
    public required string TopicName { get; init; }

    public required string Description { get; init; }
}

public sealed record UpdateTopicRequest
{
    public required string TopicName { get; init; }

    public required string Description { get; init; }
}

public sealed record RemoveTopicRequest
{
    public required string TopicName { get; init; }
}

public sealed record AddTopicPolicyRequest
{
    public required string TopicName { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}

public sealed record RemoveTopicPolicyRequest
{
    public required string TopicName { get; init; }

    public required Guid PolicyId { get; init; }
}

public sealed record UpdateTopicPolicyRequest
{
    public required string TopicName { get; init; }

    public required Guid PolicyId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }
}
