using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;

namespace MemoryMcp.Infrastructure;

public sealed class NoOpOutbox : IOutbox
{
    public Task Write(params EventPayload[] payloads) => Task.CompletedTask;

    public Task UpdateCompleted(long id) => Task.CompletedTask;

    public Task UpdateFailed(long id) => Task.CompletedTask;
}
