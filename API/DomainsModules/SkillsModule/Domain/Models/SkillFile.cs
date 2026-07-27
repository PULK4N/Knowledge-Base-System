namespace SkillsModule.Domain.Models;

public sealed record SkillFile(
    string ContentType,
    long Length,
    string Sha256
);
