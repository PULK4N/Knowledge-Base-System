using Microsoft.Extensions.DependencyInjection;

namespace SharedModule.Mcp;

public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Turns exceptions thrown by any tool into tool results that explain the
    /// failure and tell the calling model how to retry, instead of the SDK's
    /// generic "An error occurred invoking" message.
    /// </summary>
    public static IMcpServerBuilder WithToolErrorHandling(
        this IMcpServerBuilder builder
    ) =>
        builder.WithRequestFilters(
            filters => filters.AddCallToolFilter(McpToolErrorFilter.Wrap)
        );
}
