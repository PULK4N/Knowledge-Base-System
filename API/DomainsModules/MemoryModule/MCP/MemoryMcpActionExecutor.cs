using ActionModule.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryModule.MCP;

internal static class MemoryMcpActionExecutor
{
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
