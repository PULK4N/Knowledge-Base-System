using MemoryModule.API.Requests;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Mapping;

public static class CodexMemoryMigrationMappingExtensions
{
    public static void MapTo(
        this CodexMemoryMigrationRequest body,
        MigrateCodexMemoryCommand command
    )
    {
        command.ThreadId = new ThreadId(body.SessionId);
        command.RawMemory = body.RawMemory;
        command.RolloutSummary = body.RolloutSummary;
        command.Source = body.Source;
    }
}
