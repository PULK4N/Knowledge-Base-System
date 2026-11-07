namespace PolicyModule.Interfaces;

public sealed record SessionPolicyText(
    string GeneralText,
    string AgentFamilyText,
    List<string> TopicTexts
);
