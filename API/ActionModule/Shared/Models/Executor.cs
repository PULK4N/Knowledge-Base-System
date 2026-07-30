using EventSourcing.Shared.Models;

namespace ActionModule.Shared.Models;

public sealed class Executor
{
    public required EventExecutor Id { get; set; }
}
