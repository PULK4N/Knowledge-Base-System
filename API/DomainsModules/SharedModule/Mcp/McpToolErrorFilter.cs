using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SharedModule.Mcp;

/// <summary>
/// Call-tool middleware that catches tool failures and returns them as
/// <see cref="CallToolResult.IsError"/> results, so the model sees what went
/// wrong and keeps working instead of stopping on an empty error.
/// </summary>
internal static class McpToolErrorFilter
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Wrap(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next
    ) =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (Exception exception)
                when (ShouldConvert(exception, cancellationToken))
            {
                var error = McpToolError.From(exception);
                Log(context, error, exception);

                return error.ToResult(
                    context.Params?.Name ?? "unknown",
                    context.Params?.Arguments
                );
            }
        };

    // Protocol errors (such as an unknown tool) and client cancellation keep
    // the SDK's own handling.
    private static bool ShouldConvert(
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        exception is not McpProtocolException
        && !(
            exception is OperationCanceledException
            && cancellationToken.IsCancellationRequested
        );

    private static void Log(
        RequestContext<CallToolRequestParams> context,
        McpToolError error,
        Exception exception
    )
    {
        var logger = context.Services
            ?.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(McpToolErrorFilter));

        if (logger is null)
            return;

        var level = error.Kind == McpToolErrorKind.UnexpectedFailure
            ? LogLevel.Error
            : LogLevel.Warning;

        logger.Log(
            level,
            exception,
            "MCP tool {ToolName} failed with {ErrorKind}.",
            context.Params?.Name,
            error.Kind
        );
    }
}
