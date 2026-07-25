namespace SkillsModule.Domain.Models;

public sealed class SkillDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<SkillReference> References { get; set; } = [];
}
