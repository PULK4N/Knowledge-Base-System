using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EmbeddingModule;

public sealed class OllamaTextEmbeddingGenerator(
    HttpClient httpClient,
    EmbeddingOptions options
) : ITextEmbeddingGenerator
{
    public async Task<IReadOnlyList<ImmutableArray<float>>> Generate(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (inputs.Count == 0)
            return [];

        if (options.BatchSize <= 0)
            throw new InvalidOperationException("Embedding batch size must be positive.");
        if (options.BatchCharacterLimit <= 0)
            throw new InvalidOperationException("Embedding batch character limit must be positive.");

        var embeddings = new List<ImmutableArray<float>>(inputs.Count);
        var batch = new List<string>(options.BatchSize);
        var batchCharacterCount = 0;

        foreach (var input in inputs)
        {
            if (input.Length > options.BatchCharacterLimit)
            {
                throw new ArgumentException(
                    $"One embedding input contains {input.Length} characters, exceeding the configured batch character limit of {options.BatchCharacterLimit}.",
                    nameof(inputs)
                );
            }

            if (batch.Count > 0
                && (batch.Count == options.BatchSize
                    || batchCharacterCount + input.Length
                        > options.BatchCharacterLimit))
            {
                embeddings.AddRange(
                    await GenerateBatch(batch, cancellationToken)
                );
                batch = new List<string>(options.BatchSize);
                batchCharacterCount = 0;
            }

            batch.Add(input);
            batchCharacterCount += input.Length;
        }

        if (batch.Count > 0)
        {
            embeddings.AddRange(
                await GenerateBatch(batch, cancellationToken)
            );
        }

        return embeddings;
    }

    private async Task<List<ImmutableArray<float>>> GenerateBatch(
        List<string> inputs,
        CancellationToken cancellationToken
    )
    {

        using var response = await httpClient.PostAsJsonAsync(
            "api/embed",
            new EmbedRequest(
                options.Model,
                inputs,
                new EmbedRuntimeOptions(
                    options.NumGpu,
                    options.MainGpu,
                    options.NumCtx
                )
            ),
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
        IReadOnlyList<string> Input,
        EmbedRuntimeOptions Options
    );

    private sealed record EmbedRuntimeOptions(
        [property: JsonPropertyName("num_gpu")]
        int NumGpu,
        [property: JsonPropertyName("main_gpu")]
        int MainGpu,
        [property: JsonPropertyName("num_ctx")]
        int NumCtx
    );

    private sealed record EmbedResponse(
        [property: JsonPropertyName("embeddings")]
        IReadOnlyList<float[]> Embeddings
    );
}
