using EventSourcing.Shared.Interfaces;
using MemoryModule.Persistence.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryModule.Persistence;

public static class InjectionSetup
{
    public static IServiceCollection RegisterMemoryModulePersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var baseUrl = configuration[
            $"{MemoryEmbeddingOptions.SectionName}:BaseUrl"
        ] ?? "http://localhost:11435";
        var model = configuration[
            $"{MemoryEmbeddingOptions.SectionName}:Model"
        ] ?? MemoryEmbeddingOptions.DefaultModel;
        var configuredDimensions = configuration[
            $"{MemoryEmbeddingOptions.SectionName}:Dimensions"
        ];
        var dimensions = configuredDimensions is null
            ? MemoryEmbeddingOptions.DefaultDimensions
            : int.Parse(configuredDimensions);
        var options = new MemoryEmbeddingOptions
        {
            BaseUrl = new Uri(baseUrl, UriKind.Absolute),
            Model = model,
            Dimensions = dimensions
        };

        services.AddSingleton(options);
        services.AddHttpClient<IMemoryEmbeddingGenerator, OllamaMemoryEmbeddingGenerator>(
            client =>
            {
                client.BaseAddress = options.BaseUrl;
                client.Timeout = TimeSpan.FromMinutes(10);
            }
        );
        services.AddScoped<IMemorySearch, MemorySearch>();
        services.AddScoped<IProjector, MemorySearchProjector>();

        return services;
    }
}
