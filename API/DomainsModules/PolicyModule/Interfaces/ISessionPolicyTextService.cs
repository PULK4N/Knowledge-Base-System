namespace PolicyModule.Interfaces;

/// <summary>
/// Supplies the policy sections that surround a project's own policies in the
/// text an agent receives at session start.
/// </summary>
public interface ISessionPolicyTextService
{
    Task<bool> AgentFamilyExists(string agentFamilyName);

    /// <summary>
    /// Returns empty texts for sections without policies; topics without
    /// projected text are left out of <see cref="SessionPolicyText.TopicTexts"/>.
    /// </summary>
    Task<SessionPolicyText> Get(
        string? agentFamilyName,
        List<TopicName> topicNamesInOrder
    );
}
