namespace PolicyModule.Persistence.Models;

public sealed class GeneralPolicyText
{
    public int Id { get; set; }
    public Guid AggregateId { get; set; }
    public string Text { get; set; } = string.Empty;
}
