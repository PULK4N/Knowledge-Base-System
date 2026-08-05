using SkillsModule.Application.DTOs;

namespace SkillsModule.MCP;

public sealed record SkillAttachmentAddedMcpResult(
    string Status,
    AttachmentDto Attachment
);
