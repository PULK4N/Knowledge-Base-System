namespace MemoryModule.API.Requests;

public sealed record SetPromptModelRequest
{
    public required string Model { get; init; }
}
