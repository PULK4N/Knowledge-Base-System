namespace MemoryModule.Application.Queries;

internal sealed class ApproximateTokenBudget(int maximumTokens)
{
    private const int CharactersPerToken = 4;
    private int _remainingCharacters = maximumTokens * CharactersPerToken;
    private int _usedCharacters;

    public bool IsTruncated { get; private set; }

    public int ApproximateTokenCount =>
        (_usedCharacters + CharactersPerToken - 1)
        / CharactersPerToken;

    public string? Take(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (_remainingCharacters == 0)
        {
            IsTruncated = true;
            return null;
        }

        if (text.Length <= _remainingCharacters)
        {
            _remainingCharacters -= text.Length;
            _usedCharacters += text.Length;
            return text;
        }

        var suffix = _remainingCharacters > 1 ? "…" : string.Empty;
        var result = string.Concat(
            text.AsSpan(
                0,
                _remainingCharacters - suffix.Length
            ),
            suffix
        );

        _usedCharacters += _remainingCharacters;
        _remainingCharacters = 0;
        IsTruncated = true;

        return result;
    }
}
