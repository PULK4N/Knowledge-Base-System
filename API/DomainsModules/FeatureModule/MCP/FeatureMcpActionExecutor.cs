using ActionModule.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureModule.MCP;

internal static class FeatureMcpActionExecutor
{
    internal static async Task<TResult> Execute<TResult>(
        IServiceProvider services,
        IAction<TResult> action
    )
    {
        var executor = await services
            .GetRequiredService<IExecutorProvider>()
            .GetExecutor();

        return await action.Execute(executor);
    }

    internal static async Task<TResult> ExecuteCommand<
        TCommand,
        TResult
    >(
        IServiceProvider services,
        System.Action<TCommand> configure
    ) where TCommand : IAction<object>
    {
        var command = services.GetRequiredService<TCommand>();
        configure(command);
        return (TResult)await Execute(services, command);
    }

    internal static async Task<TResult> ExecuteQuery<TQuery, TResult>(
        IServiceProvider services,
        System.Action<TQuery> configure
    ) where TQuery : IAction<TResult>
    {
        var query = services.GetRequiredService<TQuery>();
        configure(query);
        return await Execute(services, query);
    }
}
