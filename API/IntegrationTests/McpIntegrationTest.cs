using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;
using Xunit.Sdk;

namespace McpSkillSystem.IntegrationTests;

public abstract class McpIntegrationTest : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        PropertyNameCaseInsensitive = true
    };

    private McpClient? _client;

    public async Task InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable(
            "MCP_INTEGRATION_MCP_URL"
        ) ?? "http://localhost:5232/mcp";
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Name = "mcp-knowledge-base-integration-tests",
                Endpoint = new Uri(endpoint, UriKind.Absolute),
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = TimeSpan.FromSeconds(30)
            }
        );

        _client = await McpClient.CreateAsync(transport);
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }

    protected async Task<T> CallTool<T>(
        string toolName,
        params (string Name, object? Value)[] arguments
    )
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromMinutes(2)
        );
        var result = await Client.CallToolAsync(
            toolName,
            arguments.ToDictionary(
                argument => argument.Name,
                argument => argument.Value,
                StringComparer.Ordinal
            ),
            cancellationToken: timeout.Token
        );
        var text = string.Join(
            "\n",
            result.Content
                .OfType<TextContentBlock>()
                .Select(content => content.Text)
        );

        if (result.IsError is true)
            throw new XunitException(
                $"MCP tool '{toolName}' failed: {text}"
            );

        if (typeof(T) == typeof(string))
            return (T)(object)text;

        return JsonSerializer.Deserialize<T>(text, JsonOptions)
            ?? throw new XunitException(
                $"MCP tool '{toolName}' returned no JSON result."
            );
    }

    private McpClient Client =>
        _client ?? throw new InvalidOperationException(
            "The MCP test client has not been initialized."
        );
}
