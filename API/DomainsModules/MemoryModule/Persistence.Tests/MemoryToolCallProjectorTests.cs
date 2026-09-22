using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemoryToolCallProjectorTests
{
    private static readonly AggregateId MemoryId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
    private static readonly PromptId FirstPromptId =
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001"));
    private static readonly PromptId SecondPromptId =
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000002"));

    [Fact]
    public async Task Update_writes_each_tool_call_of_a_prompt_in_order()
    {
        var repository = new FakeMemoryToolCallRepository();
        var projector = new MemoryToolCallProjector(repository);

        await projector.Update([CreateStateInfo(CreateMemory())]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Equal(
            [
                ("Read", "toolu_01"),
                ("Edit", "toolu_02"),
                ("Bash", "toolu_03")
            ],
            repository.ToolCalls
                .Select(toolCall => (toolCall.ToolName, toolCall.ToolUseId))
                .ToList()
        );
        Assert.Equal(
            [FirstPromptId, FirstPromptId, SecondPromptId],
            repository.ToolCalls.Select(toolCall => toolCall.PromptId).ToList()
        );
        Assert.Equal(
            [0, 1, 0],
            repository.ToolCalls
                .Select(toolCall => toolCall.ToolCallIndex)
                .ToList()
        );
        Assert.Equal(DateTime.UnixEpoch, repository.ToolCalls[0].Timestamp);
        Assert.Equal(
            ["Read the entry point", string.Empty, string.Empty],
            repository.ToolCalls
                .Select(toolCall => toolCall.Description)
                .ToList()
        );
        Assert.Equal(
            new ThreadId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            repository.ToolCalls[0].ThreadId
        );
    }

    [Fact]
    public async Task Update_keeps_only_the_tool_input_and_tool_response()
    {
        var repository = new FakeMemoryToolCallRepository();
        var projector = new MemoryToolCallProjector(repository);

        await projector.Update([CreateStateInfo(CreateMemory())]);

        using var payload = JsonDocument.Parse(
            repository.ToolCalls[0].PayloadJson
        );
        Assert.Equal(
            ["tool_input", "tool_response"],
            payload.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .ToList()
        );
        Assert.Equal(
            "Program.cs",
            payload.RootElement
                .GetProperty("tool_input")
                .GetProperty("file_path")
                .GetString()
        );
        Assert.Equal(
            "file contents",
            payload.RootElement
                .GetProperty("tool_response")
                .GetProperty("content")
                .GetString()
        );
    }

    [Fact]
    public async Task Update_writes_an_empty_payload_without_input_or_response()
    {
        var repository = new FakeMemoryToolCallRepository();
        var projector = new MemoryToolCallProjector(repository);
        var memory = new MemoryStateData(MemoryId);
        memory.ChatPrompts.Add(
            FirstPromptId,
            new ChatPrompt
            {
                PromptId = FirstPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch,
                ToolCalls =
                [
                    CreateToolCall(
                        "Read",
                        "toolu_01",
                        """{"session_id":"019f","hook_event_name":"PostToolUse"}"""
                    )
                ]
            }
        );

        await projector.Update([CreateStateInfo(memory)]);

        Assert.Equal("{}", repository.ToolCalls[0].PayloadJson);
    }

    [Fact]
    public async Task Update_removes_the_tool_calls_of_a_deleted_memory()
    {
        var repository = new FakeMemoryToolCallRepository();
        var projector = new MemoryToolCallProjector(repository);
        var memory = CreateMemory();
        memory.IsDeleted = true;

        await projector.Update([CreateStateInfo(memory)]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Empty(repository.ToolCalls);
    }

    [Fact]
    public async Task Update_writes_no_tool_calls_for_a_prompt_without_any()
    {
        var repository = new FakeMemoryToolCallRepository();
        var projector = new MemoryToolCallProjector(repository);
        var memory = new MemoryStateData(MemoryId);
        memory.ChatPrompts.Add(
            FirstPromptId,
            new ChatPrompt
            {
                PromptId = FirstPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch,
                PromptHookRecords =
                [
                    new PromptHookRecord
                    {
                        HookEventName = "UserPromptSubmit",
                        Payload = JsonDocument
                            .Parse("""{"prompt":"Hello"}""")
                            .RootElement.Clone()
                    }
                ]
            }
        );

        await projector.Update([CreateStateInfo(memory)]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Empty(repository.ToolCalls);
    }

    private static MemoryStateData CreateMemory()
    {
        var memory = new MemoryStateData(MemoryId)
        {
            ThreadId = new ThreadId(
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            )
        };
        memory.ChatPrompts.Add(
            FirstPromptId,
            new ChatPrompt
            {
                PromptId = FirstPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch,
                ToolCalls =
                [
                    CreateToolCall(
                        "Read",
                        "toolu_01",
                        """
                        {
                            "session_id": "019f",
                            "tool_input": {
                                "file_path": "Program.cs",
                                "description": "Read the entry point"
                            },
                            "tool_response": { "content": "file contents" },
                            "duration_ms": 12
                        }
                        """
                    ),
                    CreateToolCall(
                        "Edit",
                        "toolu_02",
                        """{"tool_input":{"file_path":"Program.cs"}}"""
                    )
                ]
            }
        );
        memory.ChatPrompts.Add(
            SecondPromptId,
            new ChatPrompt
            {
                PromptId = SecondPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch.AddMinutes(2),
                ToolCalls =
                [
                    CreateToolCall(
                        "Bash",
                        "toolu_03",
                        """{"tool_input":{"command":"dotnet build"}}"""
                    )
                ]
            }
        );

        return memory;
    }

    private static ToolCallRecord CreateToolCall(
        string toolName,
        string toolUseId,
        string payloadJson
    ) =>
        new()
        {
            ToolName = toolName,
            ToolUseId = toolUseId,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone()
        };

    private static StateInfo CreateStateInfo(MemoryStateData memory) =>
        StateInfo.Create(memory, "memory-state-machine", memory.Id);

    private sealed class FakeMemoryToolCallRepository
        : IMemoryToolCallRepository
    {
        public IReadOnlyList<AggregateId> AggregateIds { get; private set; } = [];
        public IReadOnlyList<MemoryToolCall> ToolCalls { get; private set; } = [];

        public Task<List<MemoryToolCall>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryToolCall> toolCalls,
            CancellationToken cancellationToken = default
        )
        {
            AggregateIds = memoryAggregateIds.ToList();
            ToolCalls = toolCalls.ToList();
            return Task.CompletedTask;
        }
    }
}
