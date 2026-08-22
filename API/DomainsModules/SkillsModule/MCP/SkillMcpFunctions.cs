using Microsoft.Extensions.AI;
using SkillsModule.Application.Commands;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Models;
using SkillsModule.Application.Queries;
using SkillsModule.Domain.Models;

namespace SkillsModule.MCP;

public static class SkillMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        CreateFunction(
            (Func<IServiceProvider, Task<List<SkillSummaryDto>>>)List,
            "skill_list",
            "Lists active skills by name and ID so a skill can be selected before calling other skill tools."
        ),
        CreateFunction(
            (Func<IServiceProvider, string, int, Task<List<SkillSearchMatchDto>>>)Search,
            "skill_search",
            "Searches active skill content and references using hybrid semantic vector and full-text ranking. Returns the highest-ranked chunk from each unique skill source; use skill_get or skill_reference_get to load the selected source."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<SkillDto?>>)Get,
            "skill_get",
            "Gets a skill by ID. References marked for automatic loading include their content; otherReferences lists the remaining paths without their content. Set orderNumber to zero for the latest state or to an event order number for historical state."
        ),
        CreateFunction(
            (Func<IServiceProvider, string, string, string, List<string>?, Dictionary<string, SkillReference2>?, Task<SkillCreatedCommandResult>>)Add,
            "skill_add",
            "Creates a skill. References map relative file paths to content and whether they should load automatically."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, string, List<string>, Task<SkillCommandResult>>)Update,
            "skill_update",
            "Replaces an existing skill's name, description, content, and tags."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Task<SkillCommandResult>>)Delete,
            "skill_delete",
            "Deletes an existing skill."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, uint, Task<SkillReferenceDto?>>)GetReference,
            "skill_reference_get",
            "Gets one skill reference by skill ID and exact relative path. Set orderNumber to zero for the latest state or to an event order number for historical state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, bool, Task<SkillCommandResult>>)AddReference,
            "skill_reference_add",
            "Adds a text reference at a relative path. loadAutomatically defaults to false."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, bool, Task<SkillCommandResult>>)UpdateReference,
            "skill_reference_update",
            "Updates an existing skill reference's text content and automatic-loading setting."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, bool, Task<SkillCommandResult>>)UpdateReferenceAutoLoad,
            "skill_reference_auto_load_update",
            "Changes whether an existing skill reference loads automatically without changing its content."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, Task<SkillCommandResult>>)DeleteReference,
            "skill_reference_delete",
            "Deletes an existing text reference from a skill."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, byte[], Task<SkillAttachmentAddedMcpResult>>)AddAttachment,
            "skill_attachment_add",
            "Adds a binary attachment to a skill. Content is supplied as base64-encoded bytes."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<SkillCommandResult>>)DeleteAttachment,
            "skill_attachment_delete",
            "Deletes an existing binary attachment from a skill."
        )
    ];

    private static AIFunction CreateFunction(
        Delegate method,
        string name,
        string description
    ) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            }
        );

    private static Task<List<SkillSummaryDto>> List(
        IServiceProvider services
    ) =>
        SkillMcpActionExecutor.ExecuteQuery<
            ListSkillsQuery,
            List<SkillSummaryDto>
        >(services, _ => { });

    private static Task<List<SkillSearchMatchDto>> Search(
        IServiceProvider services,
        string query,
        int resultCount = SearchSkillContentQuery.DefaultResultCount
    ) =>
        SkillMcpActionExecutor.ExecuteQuery<
            SearchSkillContentQuery,
            List<SkillSearchMatchDto>
        >(
            services,
            search =>
            {
                search.SearchText = query;
                search.ResultCount = resultCount;
            }
        );

    private static Task<SkillDto?> Get(
        IServiceProvider services,
        Guid skillId,
        uint orderNumber = 0
    ) =>
        SkillMcpActionExecutor.ExecuteQuery<
            GetSkillQuery,
            SkillDto?
        >(
            services,
            query =>
            {
                query.SkillId = skillId;
                query.OrderNumber = orderNumber;
                query.IncludeAllReferences = false;
            }
        );

    private static Task<SkillCreatedCommandResult> Add(
        IServiceProvider services,
        string name,
        string description,
        string content,
        List<string>? tags = null,
        Dictionary<string, SkillReference2>? references = null
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            AddSkillCommand,
            SkillCreatedCommandResult
        >(
            services,
            command =>
            {
                command.Name = name;
                command.Description = description;
                command.Content = content;
                command.Tags = tags ?? [];
                command.References = references?.ToDictionary(
                    reference => reference.Key,
                    reference => new SkillReference2(
                        reference.Value.Content,
                        reference.Value.LoadAutomatically
                    ),
                    StringComparer.Ordinal
                ) ?? new Dictionary<string, SkillReference2>(
                    StringComparer.Ordinal
                );
            }
        );

    private static Task<SkillCommandResult> Update(
        IServiceProvider services,
        Guid skillId,
        string name,
        string description,
        string content,
        List<string> tags
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            UpdateSkillCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.Name = name;
                command.Description = description;
                command.Content = content;
                command.Tags = tags;
            }
        );

    private static Task<SkillCommandResult> Delete(
        IServiceProvider services,
        Guid skillId
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            DeleteSkillCommand,
            SkillCommandResult
        >(
            services,
            command => command.SkillId = skillId
        );

    private static async Task<SkillReferenceDto?> GetReference(
        IServiceProvider services,
        Guid skillId,
        string relativePath,
        uint orderNumber = 0
    )
    {
        var skill = await SkillMcpActionExecutor.ExecuteQuery<
            GetSkillQuery,
            SkillDto?
        >(
            services,
            query =>
            {
                query.SkillId = skillId;
                query.OrderNumber = orderNumber;
                query.IncludeAllReferences = true;
            }
        );

        return skill is not null
            && skill.References.TryGetValue(
                relativePath,
                out var reference
            )
                ? reference
                : null;
    }

    private static Task<SkillCommandResult> AddReference(
        IServiceProvider services,
        Guid skillId,
        string relativePath,
        string content,
        bool loadAutomatically = false
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            AddSkillReferenceCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.RelativePath = relativePath;
                command.Content = content;
                command.LoadAutomatically = loadAutomatically;
            }
        );

    private static Task<SkillCommandResult> UpdateReference(
        IServiceProvider services,
        Guid skillId,
        string relativePath,
        string content,
        bool loadAutomatically = false
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            UpdateSkillReferenceCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.RelativePath = relativePath;
                command.Content = content;
                command.LoadAutomatically = loadAutomatically;
            }
        );

    private static Task<SkillCommandResult> DeleteReference(
        IServiceProvider services,
        Guid skillId,
        string relativePath
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            DeleteSkillReferenceCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.RelativePath = relativePath;
            }
        );

    private static Task<SkillCommandResult> UpdateReferenceAutoLoad(
        IServiceProvider services,
        Guid skillId,
        string relativePath,
        bool loadAutomatically
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            UpdateSkillReferenceAutoLoadCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.RelativePath = relativePath;
                command.LoadAutomatically = loadAutomatically;
            }
        );

    private static async Task<SkillAttachmentAddedMcpResult> AddAttachment(
        IServiceProvider services,
        Guid skillId,
        string name,
        string fileType,
        byte[] content
    )
    {
        var attachment = new Attachment
        {
            Id = FileId.New(),
            Name = name,
            Size = content.LongLength,
            FileType = fileType,
            Extension = Path
                .GetExtension(name)
                .TrimStart('.')
        };
        var result = await SkillMcpActionExecutor.ExecuteCommand<
            AddSkillAttachmentCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.Attachment = attachment;
                command.Bytes = content;
            }
        );

        return new SkillAttachmentAddedMcpResult(
            result.Status,
            AttachmentDto.FromModel(attachment)
        );
    }

    private static Task<SkillCommandResult> DeleteAttachment(
        IServiceProvider services,
        Guid skillId,
        Guid attachmentId
    ) =>
        SkillMcpActionExecutor.ExecuteCommand<
            DeleteSkillAttachmentCommand,
            SkillCommandResult
        >(
            services,
            command =>
            {
                command.SkillId = skillId;
                command.AttachmentId =
                    FileId.FromDatabaseGuid(attachmentId);
            }
        );
}
