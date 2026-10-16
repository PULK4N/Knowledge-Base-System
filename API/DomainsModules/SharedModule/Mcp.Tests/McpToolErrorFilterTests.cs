using System.ComponentModel;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SharedModule.Exceptions;
using Xunit;

namespace SharedModule.Mcp.Tests;

public sealed class McpToolErrorFilterTests
{
    private const string ArgumentsNotAccepted =
        "failed because the arguments were not accepted";

    public static TheoryData<string, Dictionary<string, object?>, string, string>
        FailingCalls => new()
        {
            {
                "search",
                new() { ["query"] = "find", ["keywords"] = new List<string>() },
                ArgumentsNotAccepted,
                "\"keywords\":[]"
            },
            {
                "search",
                new() { ["query"] = "find" },
                ArgumentsNotAccepted,
                "keywords"
            },
            {
                "search",
                new() { ["query"] = "find", ["keywords"] = "not a list" },
                ArgumentsNotAccepted,
                "not a list"
            },
            {
                "get",
                new() { ["id"] = "missing" },
                ArgumentsNotAccepted,
                "Item 'missing' was not found."
            },
            {
                "crash",
                new(),
                "failed because of an unexpected server error",
                "Arguments received: none"
            }
        };

    [Theory]
    [MemberData(nameof(FailingCalls))]
    public async Task Failing_call_returns_reason_arguments_and_retry_guidance(
        string toolName,
        Dictionary<string, object?> arguments,
        string expectedSummary,
        string expectedDetail
    )
    {
        var result = await Call(toolName, arguments);
        var text = Text(result);

        Assert.True(result.IsError);
        Assert.Contains($"The call to '{toolName}' {expectedSummary}", text);
        Assert.Contains(expectedDetail, text);
        Assert.Contains($"call '{toolName}' again", text);
    }

    [Fact]
    public async Task Long_arguments_are_cut()
    {
        var result = await Call(
            "search",
            new()
            {
                ["query"] = new string('a', McpToolError.MaximumArgumentsLength * 2),
                ["keywords"] = new List<string>()
            }
        );

        Assert.Contains(
            $"(cut at {McpToolError.MaximumArgumentsLength} characters)",
            Text(result)
        );
    }

    [Fact]
    public async Task Successful_call_is_returned_unchanged()
    {
        var result = await Call(
            "search",
            new() { ["query"] = "find", ["keywords"] = new List<string> { "a", "b" } }
        );

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("find: a b", Text(result));
    }

    private static async Task<CallToolResult> Call(
        string toolName,
        Dictionary<string, object?> arguments
    )
    {
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddMcpServer()
            .WithStreamServerTransport(
                clientToServer.Reader.AsStream(),
                serverToClient.Writer.AsStream()
            )
            .WithTools(CreateTools())
            .WithToolErrorHandling();

        await using var provider = services.BuildServiceProvider();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var server = provider.GetRequiredService<McpServer>();
        var running = server.RunAsync(stop.Token);

        await using (
            var client = await McpClient.CreateAsync(
                new StreamClientTransport(
                    clientToServer.Writer.AsStream(),
                    serverToClient.Reader.AsStream()
                ),
                cancellationToken: stop.Token
            )
        )
        {
            var result = await client.CallToolAsync(
                toolName,
                arguments,
                cancellationToken: stop.Token
            );

            await stop.CancelAsync();
            await clientToServer.Writer.CompleteAsync();
            await Task.WhenAny(running, Task.Delay(TimeSpan.FromSeconds(5)));

            return result;
        }
    }

    private static List<McpServerTool> CreateTools() =>
    [
        McpServerTool.Create(
            (Func<string, List<string>, string>)Search,
            new McpServerToolCreateOptions { Name = "search" }
        ),
        McpServerTool.Create(
            (Func<string, string>)Get,
            new McpServerToolCreateOptions { Name = "get" }
        ),
        McpServerTool.Create(
            (Func<string>)Crash,
            new McpServerToolCreateOptions { Name = "crash" }
        )
    ];

    private static string Search(
        string query,
        [Description("At least one keyword.")] List<string> keywords
    ) =>
        keywords.Count == 0
            ? throw new InvalidOperationException(
                "Action 'SearchQuery' cannot be executed."
            )
            : $"{query}: {string.Join(' ', keywords)}";

    private static string Get(string id) =>
        throw new NotFoundException($"Item '{id}' was not found.");

    private static string Crash() =>
        throw new TimeoutException("The database did not respond.");

    private static string Text(CallToolResult result) =>
        string.Join(
            "\n",
            result.Content.OfType<TextContentBlock>().Select(block => block.Text)
        );
}
