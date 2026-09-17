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

    [Fact]
    public void Summary_exposes_optional_changed_entity_ids_and_excludes_usage()
    {
        var function = MemoryMcpFunctions.Create().Single(
            function => function.Name == "memory_summary_add"
        );
        var properties = function.JsonSchema.GetProperty("properties");
        var required = function.JsonSchema.GetProperty("required")
            .EnumerateArray().Select(element => element.GetString()).ToList();

        Assert.Contains("threadId", required);
        Assert.Contains("summary", required);
        Assert.True(properties.TryGetProperty("relatedEntities", out var property));
        Assert.DoesNotContain("relatedEntities", required);
        Assert.Contains("created or updated", property.GetProperty("description").GetString());
        var entitySchema = property.GetProperty("items");
        var entityRequired = entitySchema.GetProperty("required")
            .EnumerateArray().Select(element => element.GetString()).ToList();
        Assert.Contains("type", entityRequired);
        Assert.Contains("id", entityRequired);
        var entityProperties = entitySchema.GetProperty("properties");
        Assert.True(entityProperties.TryGetProperty("id", out _));
        var types = entityProperties.GetProperty("type").GetProperty("enum")
            .EnumerateArray().Select(element => element.GetString()!).ToList();
        Assert.Equal(new List<string>
        {
            "Feature", "Skill", "FeaturePlan", "FeatureResearchDiscovery",
            "FeatureRecord", "FeatureReviewNote", "SkillAttachment", "Project", "Policy"
        }, types);
        Assert.Contains("Exclude entities only read, consulted, or used", function.Description);
    }
}
