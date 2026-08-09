using MemoryModule.Application.Commands;
using MemoryModule.Application.Models;
using MemoryModule.Domain.Models;
using Microsoft.Extensions.AI;

namespace MemoryModule.MCP;

public static class MemoryMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            AIFunctionFactory.Create(
            (Func<IServiceProvider, Guid, string, Task<MemoryCommandResult>>)AddSummary,
            new AIFunctionFactoryOptions
            {
                Name = "memory_summary_add",
                Description =
                    "Adds or replaces the summary for an existing chat memory identified by its Codex thread ID."
            }
        )
        ];

    private static Task<MemoryCommandResult> AddSummary(
        IServiceProvider services,
        Guid threadId,
        string summary
    ) =>
        MemoryMcpActionExecutor.ExecuteCommand<AddChatSummaryCommand, MemoryCommandResult>(
            services,
            command =>
            {
                command.ThreadId = new ThreadId(threadId);
                command.Summary = summary;
            }
        );
}
