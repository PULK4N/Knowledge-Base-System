using ModelContextProtocol.Server;

namespace MemoryModule.MCP.Tests;

public sealed class MemoryMcpFunctionsTests
{
    [Fact]
    public void Create_exposes_summary_add_as_an_mcp_compatible_function()
    {
        var function = Assert.Single(MemoryMcpFunctions.Create());
        var properties = function.JsonSchema.GetProperty("properties");
        var required = function.JsonSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Equal("memory_summary_add", function.Name);
        Assert.False(properties.TryGetProperty("services", out _));
        Assert.True(properties.TryGetProperty("threadId", out _));
        Assert.True(properties.TryGetProperty("summary", out _));
        Assert.Contains("threadId", required);
        Assert.Contains("summary", required);

        var tool = McpServerTool.Create(function);

        Assert.Equal(function.Name, tool.ProtocolTool.Name);
        Assert.Equal(function.JsonSchema, tool.ProtocolTool.InputSchema);
    }
}
