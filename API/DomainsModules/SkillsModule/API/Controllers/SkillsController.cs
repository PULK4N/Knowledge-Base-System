using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using SkillsModule.API.Mapping;
using SkillsModule.API.Requests;
using SkillsModule.Application.Commands;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Models;
using SkillsModule.Application.Queries;

namespace SkillsModule.API.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SkillSummaryDto>>> List(
        [FromServices] SearchSkillsQuery query,
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null
    )
    {
        query.Page = page;
        query.PageSize = pageSize;
        query.Search = search;

        return Ok(await Execute(query));
    }

    [HttpPost]
    public async Task<ActionResult<SkillCreatedCommandResult>> Add(
        [FromBody] AddSkillCommand command
    )
    {
        var result = (SkillCreatedCommandResult)await Execute(command);

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
        [FromForm] AddSkillAttachmentsRequest request,
        [FromServices] AddSkillAttachmentCommand command
    )
    {
        var attachments = (
            await request.Files.MapToAttachments()
        ).ToList();
        var executor = await GetExecutor();

        foreach (var (attachment, bytes) in attachments)
        {
            command.SkillId = skillId;
            command.Attachment = attachment;
            command.Bytes = bytes;

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
                .ToList()
        );
    }

    [HttpPost("{skillId:guid}/references")]
    public async Task<ActionResult<SkillCommandResult>> AddReference(
        Guid skillId,
        [FromBody] AddSkillReferenceRequest body,
        [FromServices] AddSkillReferenceCommand command
    )
    {
        command.SkillId = skillId;
        command.RelativePath = body.RelativePath;
        command.Content = body.Content;
        command.LoadAutomatically = body.LoadAutomatically;

        var result = (SkillCommandResult)await Execute(command);

        return Ok(result);
    }

    [HttpPost("{skillId:guid}/references/auto-load")]
    public async Task<ActionResult<SkillCommandResult>> UpdateReferenceAutoLoad(
        Guid skillId,
        [FromBody] UpdateSkillReferenceAutoLoadRequest body,
        [FromServices] UpdateSkillReferenceAutoLoadCommand command
    )
    {
        command.SkillId = skillId;
        command.RelativePath = body.RelativePath;
        command.LoadAutomatically = body.LoadAutomatically;

        return Ok((SkillCommandResult)await Execute(command));
    }

    [HttpGet("{skillId:guid}")]
    public async Task<ActionResult<SkillDto>> Get(
        Guid skillId,
        [FromQuery] uint? orderNumber,
        [FromServices] GetSkillQuery query
    )
    {
        query.SkillId = skillId;
        query.OrderNumber = orderNumber ?? 0;

        var skill = await Execute(query);

        return skill is null
            ? NotFound()
            : Ok(skill);
    }
}
