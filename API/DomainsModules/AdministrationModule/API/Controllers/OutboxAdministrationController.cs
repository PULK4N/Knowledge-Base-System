using ActionModule.API;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.API.Requests;
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
        [FromQuery] SearchOutboxPayloadsRequest request
    )
    {
        query.Page = request.Page;
        query.PageSize = request.PageSize;
        query.Search = request.Search;
        query.OnlyIncomplete = request.OnlyIncomplete;
        query.State = request.State;
        query.AggregateId = request.AggregateId;
        query.SortBy = request.SortBy;
        query.SortDirection = request.SortDirection;

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
