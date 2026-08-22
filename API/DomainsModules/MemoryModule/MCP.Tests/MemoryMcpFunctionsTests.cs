using ModelContextProtocol.Server;

namespace MemoryModule.MCP.Tests;

public sealed class MemoryMcpFunctionsTests
{
    [Fact]
    public void Create_exposes_every_memory_action_as_an_mcp_compatible_function()
    {
        var functions = MemoryMcpFunctions.Create();

        Assert.Equal(
            new List<string>
            {
                "memory_search",
                "memory_summary_add"
            },
            functions.Select(function => function.Name)
        );

        foreach (var function in functions)
        {
            var properties = function.JsonSchema.GetProperty("properties");

            Assert.False(properties.TryGetProperty("services", out _));

            var tool = McpServerTool.Create(function);

            Assert.Equal(function.Name, tool.ProtocolTool.Name);
            Assert.Equal(function.JsonSchema, tool.ProtocolTool.InputSchema);
        }
    }

    [Fact]
    public void Search_requires_query_and_exposes_optional_token_budget()
    {
        var function = MemoryMcpFunctions.Create().Single(
            function => function.Name == "memory_search"
        );
        var properties = function.JsonSchema.GetProperty("properties");
        var required = function.JsonSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.True(properties.TryGetProperty("query", out _));
        Assert.True(
            properties.TryGetProperty("maxTokens", out var maxTokens)
        );
        Assert.Equal(2000, maxTokens.GetProperty("default").GetInt32());
        Assert.Contains("query", required);
        Assert.DoesNotContain("maxTokens", required);
    }
}
