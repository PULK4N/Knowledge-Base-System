namespace PolicyModule.Persistence.Models;

public sealed class AgentFamilyPolicyText
{
    public int Id { get; set; }
    public string AgentFamilyName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
