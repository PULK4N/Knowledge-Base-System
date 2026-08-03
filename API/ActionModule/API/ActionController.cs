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

    protected Task<Executor> GetExecutor() =>
        executorProvider.GetExecutor();
}
