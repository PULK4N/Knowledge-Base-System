using ActionModule.Shared;
using ActionModule.Shared.Models;
using FeatureModule.Application.Commands;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureMcpFunctionsTests
{
    private static readonly List<string> ExpectedFunctionNames =
    [
        "feature_list",
        "feature_get_by_name",
        "feature_get",
        "feature_plan_get",
        "feature_research_discovery_get",
        "feature_research_discovery_search",
        "feature_record_list",
        "feature_review_note_list",
        "feature_add",
        "feature_remove",
        "feature_status_update",
        "feature_summary_update",
        "feature_skill_add",
        "feature_skill_remove",
        "feature_record_add",
        "feature_record_update",
        "feature_record_remove",
        "feature_review_note_add",
        "feature_review_note_update",
        "feature_review_note_remove",
        "feature_research_discovery_add",
        "feature_research_discovery_update",
        "feature_research_discovery_remove",
        "feature_plan_add",
        "feature_plan_current_update",
        "feature_plan_current_change",
        "feature_plan_remove"
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mutation_binds_injected_context_even_when_hidden_from_schema(bool supplyMemoryId)
    {
        var command = new UpdateFeatureStatusCommand(null!)
        {
            FeatureId = Guid.Empty,
            Status = string.Empty
        };
        using var services = new ServiceCollection()
            .AddSingleton(command)
            .AddSingleton<IExecutorProvider>(new UnavailableExecutorProvider())
            .BuildServiceProvider();
        var featureId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var memoryId = Guid.NewGuid();
        var arguments = new AIFunctionArguments
        {
            Services = services,
            ["featureId"] = JsonSerializer.SerializeToElement(featureId),
            ["status"] = "Implementing",
            ["sessionId"] = JsonSerializer.SerializeToElement(sessionId)
        };
        if (supplyMemoryId)
            arguments["memoryAggregateId"] = JsonSerializer.SerializeToElement(memoryId);
        var function = FeatureMcpFunctions.Create().Single(
            function => function.Name == "feature_status_update"
        );

        await Assert.ThrowsAsync<ExecutorUnavailableException>(
            () => function.InvokeAsync(arguments).AsTask()
        );

        Assert.Equal(featureId, command.FeatureId);
        Assert.Equal("Implementing", command.Status);
        Assert.Equal(sessionId, command.SessionId);
        Assert.Equal(supplyMemoryId ? memoryId : Guid.Empty, command.MemoryAggregateId);
    }

    private sealed class ExecutorUnavailableException : Exception;

    private sealed class UnavailableExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => throw new ExecutorUnavailableException();
    }

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
    public void List_requires_no_arguments_and_get_by_name_requires_name()
    {
        var functions = FeatureMcpFunctions.Create();
        var listProperties = functions
            .Single(function => function.Name == "feature_list")
            .JsonSchema
            .GetProperty("properties");
        var getByName = functions.Single(
            function => function.Name == "feature_get_by_name"
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

    [Theory]
    [InlineData("feature_research_discovery_add")]
    [InlineData("feature_research_discovery_update")]
    public void ResearchDiscoveryWrite_ExposesOptionalProvenance(
        string functionName
    )
    {
        var function = FeatureMcpFunctions.Create().Single(
            item => item.Name == functionName
        );
        var schema = function.JsonSchema;
        var properties = schema.GetProperty("properties");
        var sourceType = properties.GetProperty("sourceType");
        var sourceReference = properties.GetProperty("sourceReference");
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Equal("Other", sourceType.GetProperty("default").GetString());
        Assert.Equal(
            ["Other", "Code", "Web", "Mcp"],
            sourceType
                .GetProperty("enum")
                .EnumerateArray()
                .Select(element => element.GetString())
        );
        Assert.Equal(
            string.Empty,
            sourceReference.GetProperty("default").GetString()
        );
        Assert.Contains("featureId", required);
        Assert.Contains("title", required);
        Assert.Contains("content", required);
        Assert.DoesNotContain("sourceType", required);
        Assert.DoesNotContain("sourceReference", required);
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

    [Fact]
    public void PlanGet_RequiresFeatureAndPlanIdsButNotOrderNumber()
    {
        var required = FeatureMcpFunctions.Create()
            .Single(item => item.Name == "feature_plan_get")
            .JsonSchema.GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("featureId", required);
        Assert.Contains("planId", required);
        Assert.DoesNotContain("orderNumber", required);
    }

    [Fact]
    public void ResearchDiscoveryGet_RequiresFeatureAndDiscoveryIds()
    {
        var required = FeatureMcpFunctions.Create()
            .Single(item => item.Name == "feature_research_discovery_get")
            .JsonSchema.GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("featureId", required);
        Assert.Contains("discoveryIds", required);
        Assert.DoesNotContain("orderNumber", required);
    }

    [Fact]
    public void ReviewNoteList_RequiresFeatureIdButNotOrderNumber()
    {
        var required = FeatureMcpFunctions.Create()
            .Single(item => item.Name == "feature_review_note_list")
            .JsonSchema.GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("featureId", required);
        Assert.DoesNotContain("orderNumber", required);
    }

    [Fact]
    public void RecordList_RequiresFeatureIdButNotOrderNumber()
    {
        var required = FeatureMcpFunctions.Create()
            .Single(item => item.Name == "feature_record_list")
            .JsonSchema.GetProperty("required")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Contains("featureId", required);
        Assert.DoesNotContain("orderNumber", required);
    }
}
