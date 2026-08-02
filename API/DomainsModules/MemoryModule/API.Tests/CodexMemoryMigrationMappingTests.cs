using System.Text.Json;
using MemoryModule.API.Mapping;
using MemoryModule.API.Requests;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Tests;

public sealed class CodexMemoryMigrationMappingTests
{
    [Fact]
    public void RequestDeserializationAndMapping_PreservesMigrationFields()
    {
        var sessionId = Guid.Parse(
            "019fb72e-e0c3-7452-b32b-5bbf65433c98"
        );
        var body = JsonSerializer.Deserialize<CodexMemoryMigrationRequest>(
            $$"""
            {
              "session_id": "{{sessionId}}",
              "raw_memory": "Raw stage-one memory",
              "rollout_summary": "Thread rollout summary",
              "source": "codex-stage1-output"
            }
            """
        )!;
        var command = new MigrateCodexMemoryCommand(null!)
        {
            ThreadId = default,
            RawMemory = string.Empty,
            RolloutSummary = string.Empty,
            Source = string.Empty
        };

        body.MapTo(command);

        Assert.Equal(new ThreadId(sessionId), command.ThreadId);
        Assert.Equal("Raw stage-one memory", command.RawMemory);
        Assert.Equal("Thread rollout summary", command.RolloutSummary);
        Assert.Equal("codex-stage1-output", command.Source);
    }
}
