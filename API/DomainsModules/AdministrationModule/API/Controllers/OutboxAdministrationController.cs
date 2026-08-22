using System.ComponentModel.DataAnnotations;
using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AdministrationModule.API.Controllers;

[ApiController]
[Route("api/administration/outbox")]
public sealed class OutboxAdministrationController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OutboxPayloadDto>>> List(
        [FromServices] ListOutboxPayloadsQuery query,
        [FromQuery, Range(Pagination.DefaultPage, Pagination.MaximumPage)]
            int page = Pagination.DefaultPage,
        [FromQuery, Range(1, Pagination.MaximumPageSize)]
            int pageSize = Pagination.DefaultPageSize,
        [FromQuery] bool onlyIncomplete = false
    )
    {
        query.Page = page;
        query.PageSize = pageSize;
        query.OnlyIncomplete = onlyIncomplete;

        return Ok(await Execute(query));
    }

    [HttpPost("{outboxPayloadId:long}/requeue")]
    public async Task<ActionResult<OutboxPayloadDto>> Requeue(
        long outboxPayloadId,
        [FromServices] RequeueOutboxPayloadCommand command
    )
    {
        command.OutboxPayloadId = outboxPayloadId;
        var result = await Execute(command);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}
