using ActionModule;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SkillsModule.API.Mapping;
using SkillsModule.API.Requests;
using SkillsModule.Application.Attachments;
using SkillsModule.Application.Commands;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Models;
using SkillsModule.Application.Queries;

namespace SkillsModule.API.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(
    StateMachineHandler stateMachineHandler,
    IEventStore eventStore,
    IExecutorProvider executorProvider,
    IAttachmentContentStorage attachmentContentStorage
) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SkillCreatedCommandResult>> Add(
        [FromBody] CreateSkillRequest request
    )
    {
        var command = new AddSkillCommand(stateMachineHandler)
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Tags = [.. request.Tags]
        };
        var executor = await executorProvider.GetExecutor();
        var result = (SkillCreatedCommandResult)await command.Execute(
            executor
        );

        return CreatedAtAction(
            nameof(Get),
            new { skillId = result.SkillId },
            result
        );
    }

    [HttpPost("{skillId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<IReadOnlyCollection<AttachmentDto>>
    > AddAttachments(
        Guid skillId,
        [FromForm] AddSkillAttachmentsRequest request
    )
    {
        var attachments = (
            await request.Files.MapToAttachments()
        ).ToArray();
        var executor = await executorProvider.GetExecutor();

        foreach (var (attachment, bytes) in attachments)
        {
            var command = new AddSkillAttachmentCommand(
                stateMachineHandler,
                attachmentContentStorage
            )
            {
                SkillId = skillId,
                Attachment = attachment,
                Bytes = bytes
            };

            await command.Execute(executor);
        }

        return Ok(
            attachments
                .Select(
                    attachment =>
                        AttachmentDto.FromModel(
                            attachment.attachment
                        )
                )
                .ToArray()
        );
    }

    [HttpPost("{skillId:guid}/references")]
    public async Task<ActionResult<SkillCommandResult>> AddReference(
        Guid skillId,
        [FromBody] AddSkillReferenceRequest request
    )
    {
        var command = new AddSkillReferenceCommand(
            stateMachineHandler
        )
        {
            SkillId = skillId,
            RelativePath = request.RelativePath,
            Content = request.Content
        };
        var executor = await executorProvider.GetExecutor();
        var result = (SkillCommandResult)await command.Execute(
            executor
        );

        return Ok(result);
    }

    [HttpGet("{skillId:guid}")]
    public async Task<ActionResult<SkillDto>> Get(
        Guid skillId,
        [FromQuery] uint? orderNumber = null
    )
    {
        var query = new GetSkillQuery(
            stateMachineHandler,
            eventStore
        )
        {
            SkillId = skillId,
            OrderNumber = orderNumber ?? 0
        };
        var executor = await executorProvider.GetExecutor();
        var skill = await query.Execute(executor);

        return skill is null
            ? NotFound()
            : Ok(skill);
    }
}
