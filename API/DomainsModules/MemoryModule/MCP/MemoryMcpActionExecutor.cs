using ActionModule.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryModule.MCP;

internal static class MemoryMcpActionExecutor
{
    public static async Task<TResult> ExecuteQuery<
        TQuery,
        TResult
    >(
        IServiceProvider services,
        System.Action<TQuery> configure
    ) where TQuery : IAction<TResult>
    {
        var query = services.GetRequiredService<TQuery>();
        configure(query);

        var executor = await services
            .GetRequiredService<IExecutorProvider>()
            .GetExecutor();

        return await query.Execute(executor);
    }

    public static async Task<TResult> ExecuteCommand<
        TCommand,
        TResult
    >(
        IServiceProvider services,
        System.Action<TCommand> configure
    ) where TCommand : IAction<TResult>
    {
        var command = services.GetRequiredService<TCommand>();
        configure(command);

        var executor = await services
            .GetRequiredService<IExecutorProvider>()
            .GetExecutor();

        return await command.Execute(executor);
    }
}
