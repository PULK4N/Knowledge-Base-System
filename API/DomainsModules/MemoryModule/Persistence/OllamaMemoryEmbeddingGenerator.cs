using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class OllamaMemoryEmbeddingGenerator(
    HttpClient httpClient,
    MemoryEmbeddingOptions options
) : IMemoryEmbeddingGenerator
{
    public async Task<IReadOnlyList<ImmutableArray<float>>> Generate(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (inputs.Count == 0)
            return [];

        using var response = await httpClient.PostAsJsonAsync(
            "api/embed",
            new EmbedRequest(options.Model, inputs),
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(
            cancellationToken: cancellationToken
        ) ?? throw new InvalidOperationException(
            "Ollama returned an empty embedding response."
        );

        if (result.Embeddings.Count != inputs.Count)
        {
            throw new InvalidOperationException(
                $"Ollama returned {result.Embeddings.Count} embeddings for {inputs.Count} inputs."
            );
        }

        return result.Embeddings
            .Select(
                embedding =>
                {
                    if (embedding.Length != options.Dimensions)
                    {
                        throw new InvalidOperationException(
                            $"Embedding model '{options.Model}' returned {embedding.Length} dimensions; expected {options.Dimensions}."
                        );
                    }

                    return embedding.ToImmutableArray();
                }
            )
            .ToList();
    }

    private sealed record EmbedRequest(
        string Model,
        IReadOnlyList<string> Input
    );

    private sealed record EmbedResponse(
        [property: JsonPropertyName("embeddings")]
        IReadOnlyList<float[]> Embeddings
    );
}
