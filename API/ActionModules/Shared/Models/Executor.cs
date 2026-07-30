using EventSourcing.Shared.Models;

namespace ActionModule.Models;

public sealed class Executor
{
    public required EventExecutor Id { get; set; }
}
