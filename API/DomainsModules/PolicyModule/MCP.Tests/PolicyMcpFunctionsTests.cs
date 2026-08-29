using ModelContextProtocol.Server;

namespace PolicyModule.MCP.Tests;

public sealed class PolicyMcpFunctionsTests
{
    private static readonly string[] ExpectedFunctionNames =
    [
        "policy_general_list",
        "policy_general_add",
        "policy_general_update",
        "policy_general_remove",
        "policy_topic_list",
        "policy_topic_policy_list",
        "policy_topic_create",
        "policy_topic_update",
        "policy_topic_remove",
        "policy_topic_policy_add",
        "policy_topic_policy_update",
        "policy_topic_policy_remove",
        "policy_project_list",
        "policy_project_get_by_name",
        "policy_project_policy_list",
        "policy_project_create",
        "policy_project_update",
        "policy_project_delete",
        "policy_project_repository_add",
        "policy_project_policy_add",
        "policy_project_policy_update",
        "policy_project_policy_remove",
        "policy_project_topic_add",
        "policy_project_topic_remove",
        "policy_agent_family_list",
        "policy_agent_family_policy_list",
        "policy_agent_family_create",
        "policy_agent_family_update",
        "policy_agent_family_remove",
        "policy_agent_family_policy_add",
        "policy_agent_family_policy_update",
        "policy_agent_family_policy_remove",
        "policy_get_by_repository"
    ];

    [Fact]
    public void Create_exposes_every_policy_action_as_an_mcp_compatible_function()
    {
        var functions = PolicyMcpFunctions.Create();

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
    public void Project_list_requires_no_arguments_and_get_by_name_requires_name()
    {
        var functions = PolicyMcpFunctions.Create();
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
    public void Topic_list_does_not_require_arguments()
    {
        var function = PolicyMcpFunctions.Create().Single(
            function => function.Name == "policy_topic_list"
        );
        var properties = function.JsonSchema
            .GetProperty("properties");

        Assert.Empty(properties.EnumerateObject());
    }

    [Fact]
    public void Get_by_repository_exposes_the_repository_path_and_agent_family()
    {
        var function = PolicyMcpFunctions.Create().Single(
            function =>
                function.Name == "policy_get_by_repository"
        );

        Assert.Equal(
            ["repositoryPath", "agentFamily"],
            function.JsonSchema
                .GetProperty("properties")
                .EnumerateObject()
                .Select(property => property.Name)
                .ToList()
        );
        Assert.Equal(
            ["repositoryPath"],
            function.JsonSchema
                .GetProperty("required")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToList()
        );
        Assert.Contains(
            "stop reasoning",
            function.Description,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void Agent_family_list_does_not_require_arguments()
    {
        var function = PolicyMcpFunctions.Create().Single(
            function => function.Name == "policy_agent_family_list"
        );

        Assert.Empty(
            function.JsonSchema
                .GetProperty("properties")
                .EnumerateObject()
        );
    }
}
