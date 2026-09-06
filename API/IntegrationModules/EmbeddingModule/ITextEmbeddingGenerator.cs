using System.Collections.Immutable;

namespace EmbeddingModule;

public interface ITextEmbeddingGenerator
{
    Task<IReadOnlyList<ImmutableArray<float>>> Generate(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    );
}
