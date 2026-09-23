using ActionModule.Shared;
using ActionModule.Shared.Models;
using PolicyModule.Application.Commands;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
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
        "policy_agent_family_policy_remove"
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mutation_binds_injected_context_even_when_hidden_from_schema(bool supplyMemoryId)
    {
        var command = new UpdateGeneralPolicyCommand(null!)
        {
            PolicyId = Guid.Empty,
            Title = string.Empty, Description = string.Empty
        };
        using var services = new ServiceCollection()
            .AddSingleton(command)
            .AddSingleton<IExecutorProvider>(new UnavailableExecutorProvider())
            .BuildServiceProvider();
        var policyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var memoryId = Guid.NewGuid();
        var arguments = new AIFunctionArguments
        {
            Services = services,
            ["policyId"] = JsonSerializer.SerializeToElement(policyId),
            ["title"] = "Policy",
            ["description"] = "Description",
            ["sessionId"] = JsonSerializer.SerializeToElement(sessionId)
        };
        if (supplyMemoryId)
            arguments["memoryAggregateId"] = JsonSerializer.SerializeToElement(memoryId);
        var function = PolicyMcpFunctions.Create().Single(
            function => function.Name == "policy_general_update"
        );

        await Assert.ThrowsAsync<ExecutorUnavailableException>(
            () => function.InvokeAsync(arguments).AsTask()
        );

        Assert.Equal(policyId, command.PolicyId);
        Assert.Equal("Policy", command.Title);
        Assert.Equal("Description", command.Description);
        Assert.Equal(sessionId, command.SessionId);
        Assert.Equal(supplyMemoryId ? memoryId : Guid.Empty, command.MemoryAggregateId);
    }

    private sealed class ExecutorUnavailableException : Exception;

    private sealed class UnavailableExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => throw new ExecutorUnavailableException();
    }

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
    public void Repository_policies_are_not_reachable_over_mcp()
    {
        var functionNames = PolicyMcpFunctions.Create()
            .Select(function => function.Name)
            .ToList();

        Assert.DoesNotContain("policy_get_by_repository", functionNames);
        Assert.All(
            functionNames,
            name =>
                Assert.False(
                    name.Contains("by_repository", StringComparison.Ordinal)
                )
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
