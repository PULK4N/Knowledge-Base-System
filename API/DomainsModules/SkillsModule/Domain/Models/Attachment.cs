namespace SkillsModule.Domain.Models;

public sealed record Attachment
{
    public required FileId Id { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required string FileType { get; init; }
    public required string Extension { get; init; }
}
