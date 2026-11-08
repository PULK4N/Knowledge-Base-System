using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ProjectModule.MCP.Tests;

public sealed class ProjectMcpFunctionsTests
{
    private static readonly string[] ExpectedFunctionNames =
    [
        "policy_project_list",
        "policy_project_get_by_name",
        "policy_project_policy_list",
        "project_create",
        "policy_project_update",
        "policy_project_delete",
        "policy_project_repository_add",
        "policy_project_policy_add",
        "policy_project_policy_update",
        "policy_project_policy_remove",
        "policy_project_topic_add",
        "policy_project_topic_remove"
    ];

    [Fact]
    public void Create_exposes_every_project_action_as_an_mcp_compatible_function()
    {
        var functions = ProjectMcpFunctions.Create();

        Assert.Equal(
            ExpectedFunctionNames,
            functions.Select(function => function.Name)
        );

        foreach (var function in functions)
        {
            var properties = function.JsonSchema
                .GetProperty("properties");

            Assert.False(properties.TryGetProperty("services", out _));
            Assert.False(properties.TryGetProperty("sessionId", out _));
            Assert.False(properties.TryGetProperty("memoryAggregateId", out _));

            var tool = McpServerTool.Create(function);

            Assert.Equal(function.Name, tool.ProtocolTool.Name);
            Assert.Equal(
                function.JsonSchema,
                tool.ProtocolTool.InputSchema
            );
        }
    }
    [Fact]
    public void Project_list_requires_no_arguments_and_get_by_name_requires_name()
    {
        var functions = ProjectMcpFunctions.Create();
        var listProperties = functions
            .Single(function => function.Name == "policy_project_list")
            .JsonSchema
            .GetProperty("properties");
        var getByName = functions.Single(
            function => function.Name == "policy_project_get_by_name"
        );

        Assert.Empty(listProperties.EnumerateObject());
        Assert.Equal(
            ["name"],
            getByName.JsonSchema
                .GetProperty("required")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToList()
        );
    }


    [Fact]
    public void Repository_policies_are_not_reachable_over_mcp()
    {
        Assert.DoesNotContain(
            ProjectMcpFunctions.Create(),
            function => function.Name.Contains("by_repository", StringComparison.Ordinal)
        );
    }
}
