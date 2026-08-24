using ModelContextProtocol.Server;
using Xunit;

namespace KnowledgeSearchModule.MCP.Tests;

public sealed class KnowledgeSearchMcpFunctionsTests
{
    [Fact]
    public void Create_exposes_global_search_with_query_required()
    {
        var function = Assert.Single(KnowledgeSearchMcpFunctions.Create());

        Assert.Equal("knowledge_search", function.Name);
        Assert.Equal(
            ["query"],
            function.JsonSchema
                .GetProperty("required")
                .EnumerateArray()
                .Select(value => value.GetString())
                .ToList()
        );
        Assert.False(
            function.JsonSchema.GetProperty("properties")
                .TryGetProperty("services", out _)
        );
        Assert.Equal(
            function.JsonSchema,
            McpServerTool.Create(function).ProtocolTool.InputSchema
        );
    }
}
