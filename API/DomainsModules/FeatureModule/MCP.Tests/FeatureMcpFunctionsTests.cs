using ModelContextProtocol.Server;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureMcpFunctionsTests
{
    private static readonly List<string> ExpectedFunctionNames =
    [
        "feature_get",
        "feature_add",
        "feature_remove",
        "feature_status_update",
        "feature_skill_add",
        "feature_skill_remove",
        "feature_record_add",
        "feature_record_update",
        "feature_record_remove",
        "feature_plan_add",
        "feature_plan_current_update",
        "feature_plan_current_change",
        "feature_plan_remove"
    ];

    [Fact]
    public void Create_ExposesEveryFeatureActionAsMcpTool()
    {
        var functions = FeatureMcpFunctions.Create();

        Assert.Equal(
            ExpectedFunctionNames,
            functions.Select(function => function.Name)
        );

        foreach (var function in functions)
        {
            var properties = function.JsonSchema.GetProperty(
                "properties"
            );
            Assert.False(properties.TryGetProperty("services", out _));

            var tool = McpServerTool.Create(function);
            Assert.Equal(function.Name, tool.ProtocolTool.Name);
            Assert.Equal(
                function.JsonSchema,
                tool.ProtocolTool.InputSchema
            );
        }
    }

    [Theory]
    [InlineData("feature_plan_add")]
    [InlineData("feature_plan_current_update")]
    public void PlanWrite_ExposesOptionalContentType(
        string functionName
    )
    {
        var function = FeatureMcpFunctions.Create().Single(
            item => item.Name == functionName
        );
        var schema = function.JsonSchema;
        var contentType = schema
            .GetProperty("properties")
            .GetProperty("contentType");
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Equal("Markdown", contentType.GetProperty("default").GetString());
        Assert.Equal(
            ["Markdown", "Html"],
            contentType
                .GetProperty("enum")
                .EnumerateArray()
                .Select(element => element.GetString())
        );
        Assert.DoesNotContain("contentType", required);
    }

    [Fact]
    public void Get_RequiresFeatureIdButNotOrderNumber()
    {
        var function = FeatureMcpFunctions.Create().Single(
            item => item.Name == "feature_get"
        );
        var required = function.JsonSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("featureId", required);
        Assert.DoesNotContain("orderNumber", required);
    }
}
