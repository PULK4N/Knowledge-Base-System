using System.Text.Json;
using ActionModule.API;
using ActionModule.Shared;
using MemoryModule.API.Mapping;
using MemoryModule.API.Requests;
using MemoryModule.Application.Commands;
using MemoryModule.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MemoryModule.API.Controllers;

[ApiController]
[Route("api/memory")]
public sealed class MemoryController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    [HttpPost("codex/prompt-hooks")]
    public async Task<ActionResult<MemoryCommandResult>> RecordPromptHook(
        [FromBody] JsonElement payload,
        [FromServices] RecordCodexPromptHookCommand command
    )
    {
        payload.MapTo(command);

        return Ok(await Execute(command));
    }

    [HttpPost("codex/migrations")]
    public async Task<ActionResult<MemoryCommandResult>> Migrate(
        [FromBody] CodexMemoryMigrationRequest body,
        [FromServices] MigrateCodexMemoryCommand command
    )
    {
        body.MapTo(command);

        return Ok(await Execute(command));
    }
}
