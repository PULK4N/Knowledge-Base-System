namespace PolicyModule.Persistence.Models;

public sealed class TopicPolicyText
{
    public int Id { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
