using ActionModule;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;

namespace SkillsModule.API.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(
    StateMachineHandler stateMachineHandler,
    IEventStore eventStore,
    IExecutorProvider executorProvider
) : ControllerBase
{
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
