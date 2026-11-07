namespace ProjectModule.Persistence.Interfaces;

public sealed record ProjectPolicyTextEntry(
    string Text,
    List<string> TopicNames
);
