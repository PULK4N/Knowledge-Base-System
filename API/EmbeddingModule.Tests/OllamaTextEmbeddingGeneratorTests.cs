using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EmbeddingModule.Tests;

public sealed class OllamaTextEmbeddingGeneratorTests
{
    [Fact]
    public async Task Generate_SendsConfiguredRuntimeOptionsToOllama()
    {
        const int numGpu = 999;
        const int mainGpu = 0;
        const int numCtx = 4096;
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var generator = new OllamaTextEmbeddingGenerator(
            httpClient,
            new EmbeddingOptions
            {
                BaseUrl = httpClient.BaseAddress,
                Dimensions = 2,
                NumGpu = numGpu,
                MainGpu = mainGpu,
                NumCtx = numCtx
            }
        );

        await generator.Generate(new List<string> { "embed this" });

        using var request = JsonDocument.Parse(handler.RequestBody);
        var runtimeOptions = request.RootElement.GetProperty("options");
        Assert.Equal(numGpu, runtimeOptions.GetProperty("num_gpu").GetInt32());
        Assert.Equal(
            mainGpu,
            runtimeOptions.GetProperty("main_gpu").GetInt32()
        );
        Assert.Equal(numCtx, runtimeOptions.GetProperty("num_ctx").GetInt32());
    }

    [Fact]
    public async Task Generate_splits_large_input_lists_into_bounded_sequential_batches()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var generator = new OllamaTextEmbeddingGenerator(
            httpClient,
            new EmbeddingOptions
            {
                BaseUrl = httpClient.BaseAddress,
                Dimensions = 2,
                BatchSize = 2,
                BatchCharacterLimit = 5
            }
        );

        var embeddings = await generator.Generate(
            ["aa", "bb", "ccc", "dddd"]
        );

        Assert.Equal(4, embeddings.Count);
        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Equal(
            [2, 1, 1],
            handler.RequestBodies
                .Select(
                    body => JsonDocument.Parse(body).RootElement
                        .GetProperty("input")
                        .GetArrayLength()
                )
                .ToList()
        );
    }

    [Fact]
    public async Task Generate_rejects_an_input_larger_than_the_batch_character_limit()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var generator = new OllamaTextEmbeddingGenerator(
            httpClient,
            new EmbeddingOptions
            {
                BaseUrl = httpClient.BaseAddress,
                Dimensions = 2,
                BatchCharacterLimit = 3
            }
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => generator.Generate(["four"])
        );

        Assert.Empty(handler.RequestBodies);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestBody = await request.Content!.ReadAsStringAsync(
                cancellationToken
            );
            RequestBodies.Add(RequestBody);
            using var body = JsonDocument.Parse(RequestBody);
            var inputCount = body.RootElement.GetProperty("input")
                .GetArrayLength();
            var embeddings = Enumerable.Range(0, inputCount)
                .Select(_ => new[] { 0.1f, 0.2f })
                .ToList();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { embeddings }),
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }
    }
}
