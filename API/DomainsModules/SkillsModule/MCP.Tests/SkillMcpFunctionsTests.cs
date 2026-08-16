using ModelContextProtocol.Server;

namespace SkillsModule.MCP.Tests;

public sealed class SkillMcpFunctionsTests
{
    private static readonly string[] ExpectedFunctionNames =
    [
        "skill_list",
        "skill_get",
        "skill_add",
        "skill_update",
        "skill_delete",
        "skill_reference_add",
        "skill_reference_update",
        "skill_reference_delete",
        "skill_attachment_add",
        "skill_attachment_delete"
    ];

    [Fact]
    public void List_does_not_require_arguments()
    {
        var function = SkillMcpFunctions.Create().Single(
            function => function.Name == "skill_list"
        );
        var properties = function.JsonSchema
            .GetProperty("properties");

        Assert.Empty(properties.EnumerateObject());
    }

    [Fact]
    public void Create_exposes_every_skill_action_as_an_mcp_compatible_function()
    {
        var functions = SkillMcpFunctions.Create();

        Assert.Equal(
            ExpectedFunctionNames,
            functions.Select(function => function.Name)
        );

        foreach (var function in functions)
        {
            var properties = function.JsonSchema
                .GetProperty("properties");

            Assert.False(properties.TryGetProperty("services", out _));

            var tool = McpServerTool.Create(function);

            Assert.Equal(function.Name, tool.ProtocolTool.Name);
            Assert.Equal(
                function.JsonSchema,
                tool.ProtocolTool.InputSchema
            );
        }
    }

    [Fact]
    public void Add_exposes_optional_tags_and_text_references()
    {
        var function = SkillMcpFunctions.Create().Single(
            function => function.Name == "skill_add"
        );
        var schema = function.JsonSchema;
        var properties = schema.GetProperty("properties");
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.True(properties.TryGetProperty("tags", out _));
        Assert.True(properties.TryGetProperty("references", out _));
        Assert.DoesNotContain("tags", required);
        Assert.DoesNotContain("references", required);
    }

    [Theory]
    [InlineData("skill_reference_add")]
    [InlineData("skill_reference_update")]
    public void Reference_write_exposes_optional_automatic_loading(
        string functionName
    )
    {
        var function = SkillMcpFunctions.Create().Single(
            function => function.Name == functionName
        );
        var schema = function.JsonSchema;
        var properties = schema.GetProperty("properties");
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.True(
            properties.TryGetProperty("loadAutomatically", out _)
        );
        Assert.DoesNotContain("loadAutomatically", required);
    }

    [Fact]
    public void Get_exposes_order_number_as_optional()
    {
        var function = SkillMcpFunctions.Create().Single(
            function => function.Name == "skill_get"
        );
        var schema = function.JsonSchema;
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("skillId", required);
        Assert.DoesNotContain("orderNumber", required);
    }
}
