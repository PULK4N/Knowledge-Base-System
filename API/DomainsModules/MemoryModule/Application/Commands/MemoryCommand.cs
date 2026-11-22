using ActionModule.Shared;
using ActionModule.Shared.Models;
using IdentityModule.Shared;
using MemoryModule.Application.Models;

namespace MemoryModule.Application.Commands;

/// <summary>Base of every memory write: chat records, hook records, summaries and prompt usage.</summary>
public abstract class MemoryCommand : Command<MemoryCommandResult>
{
    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(executor.Has(Permissions.MemoryWrite));
}
