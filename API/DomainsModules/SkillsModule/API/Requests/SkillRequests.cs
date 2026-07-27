using Microsoft.AspNetCore.Http;

namespace SkillsModule.API.Requests;

public sealed record CreateSkillRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];
}

public sealed record AddSkillAttachmentsRequest
{
    public List<IFormFile> Files { get; init; } = [];
}

public sealed record AddSkillReferenceRequest
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
}
