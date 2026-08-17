using Microsoft.AspNetCore.Http;

namespace SkillsModule.API.Requests;

public sealed record AddSkillAttachmentsRequest
{
    public List<IFormFile> Files { get; init; } = [];
}

public sealed record AddSkillReferenceRequest
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
    public bool LoadAutomatically { get; init; }
}

public sealed record UpdateSkillRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public required List<string> Tags { get; init; }
}

public sealed record UpdateSkillReferenceRequest
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
    public required bool LoadAutomatically { get; init; }
}

public sealed record DeleteSkillReferenceRequest
{
    public required string RelativePath { get; init; }
}

public sealed record UpdateSkillReferenceAutoLoadRequest
{
    public required string RelativePath { get; init; }
    public required bool LoadAutomatically { get; init; }
}
