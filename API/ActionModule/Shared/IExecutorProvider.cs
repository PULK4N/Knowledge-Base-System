using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;

namespace ActionModule.Shared;

public interface IExecutorProvider
{
    Task<Executor> GetExecutor();
}

public sealed class TemporaryExecutorProvider : IExecutorProvider
{
    public Task<Executor> GetExecutor() =>
        Task.FromResult(
            new Executor
            {
                Id = EventExecutor.New()
            }
        );
}
