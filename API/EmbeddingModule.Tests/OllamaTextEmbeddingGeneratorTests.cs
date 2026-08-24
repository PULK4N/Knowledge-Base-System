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

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestBody = await request.Content!.ReadAsStringAsync(
                cancellationToken
            );

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"embeddings":[[0.1,0.2]]}""",
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }
    }
}
