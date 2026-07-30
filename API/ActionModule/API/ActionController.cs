using ActionModule.Shared;
using ActionModule.Shared.Models;
using Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ActionModule.API;

public abstract class ActionController(
    IExecutorProvider executorProvider
) : ControllerBase
{
    protected async Task<TResult> Execute<TResult>(
        IAction<TResult> action
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        var executor = await GetExecutor();
        return await action.Execute(executor);
    }

    protected Task<TResult> Execute<TResult>(
        IAction<TResult> action,
        IAction<TResult> body
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(body);

        ActionPropertyMapper.Map(body, action);

        return Execute(action);
    }

    protected Task<Executor> GetExecutor() =>
        executorProvider.GetExecutor();
}
