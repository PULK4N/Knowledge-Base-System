using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmbeddingModule;

public static class InjectionSetup
{
    public static IServiceCollection RegisterTextEmbeddings(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var baseUrl = configuration[
            $"{EmbeddingOptions.SectionName}:BaseUrl"
        ] ?? "http://localhost:11435";
        var model = configuration[
            $"{EmbeddingOptions.SectionName}:Model"
        ] ?? EmbeddingOptions.DefaultModel;
        var configuredDimensions = configuration[
            $"{EmbeddingOptions.SectionName}:Dimensions"
        ];
        var dimensions = configuredDimensions is null
            ? EmbeddingOptions.DefaultDimensions
            : int.Parse(configuredDimensions);
        var configuredNumGpu = configuration[
            $"{EmbeddingOptions.SectionName}:NumGpu"
        ];
        var numGpu = configuredNumGpu is null
            ? EmbeddingOptions.ForceAllGpuLayers
            : int.Parse(configuredNumGpu);
        var configuredMainGpu = configuration[
            $"{EmbeddingOptions.SectionName}:MainGpu"
        ];
        var mainGpu = configuredMainGpu is null
            ? EmbeddingOptions.DefaultMainGpu
            : int.Parse(configuredMainGpu);
        var configuredNumCtx = configuration[
            $"{EmbeddingOptions.SectionName}:NumCtx"
        ];
        var numCtx = configuredNumCtx is null
            ? EmbeddingOptions.DefaultNumCtx
            : int.Parse(configuredNumCtx);
        var options = new EmbeddingOptions
        {
            BaseUrl = new Uri(baseUrl, UriKind.Absolute),
            Model = model,
            Dimensions = dimensions,
            NumGpu = numGpu,
            MainGpu = mainGpu,
            NumCtx = numCtx
        };

        services.AddSingleton(options);
        services.AddHttpClient<ITextEmbeddingGenerator, OllamaTextEmbeddingGenerator>(
            client =>
            {
                client.BaseAddress = options.BaseUrl;
                client.Timeout = TimeSpan.FromMinutes(10);
            }
        );

        return services;
    }
}
