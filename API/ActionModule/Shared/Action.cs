using ActionModule.Shared.Models;

namespace ActionModule.Shared;

public abstract class Action<TResult>() : IAction<TResult>
{
    public virtual Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public virtual Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

    public async Task<TResult> Execute(Executor executor)
    {
        if (!await IsAuthorized(executor))
            throw new UnauthorizedAccessException($"Action '{GetType().Name}' is not authorized.");

        if (!await CanExecute(executor))
            throw new InvalidOperationException($"Action '{GetType().Name}' cannot be executed.");

        var result = await ExecuteInternal(executor);

        return await MapAdditionally(executor, result);
    }

    protected abstract Task<TResult> ExecuteInternal(Executor executor);

    /// <summary>
    /// Enriches the result with data that is not available in state data.
    /// </summary>
    protected virtual Task<TResult> MapAdditionally(
        Executor executor,
        TResult result
    ) => Task.FromResult(result);
}

public abstract class Command<TResult> : Action<TResult> { }

public abstract class Query<TResult> : Action<TResult> { }
