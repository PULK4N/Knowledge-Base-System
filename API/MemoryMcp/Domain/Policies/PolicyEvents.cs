using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace MemoryMcp.Domain.Policies;

public sealed record PolicySaved(
    string Name,
    string Instruction,
    string Scope,
    int Priority,
    bool Enabled,
    string[] Tags
) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (PolicyState)stateData;
        if (state.Id != Guid.Empty)
            throw new InvalidOperationException($"Policy '{info.AggregateId}' already exists.");

        state.Id = info.AggregateId;
        state.Name = Name;
        state.Instruction = Instruction;
        state.Scope = Scope;
        state.Priority = Priority;
        state.Enabled = Enabled;
        state.Tags =  [ .. Tags ];
        state.CreatedAtUtc = info.Timestamp;
        state.UpdatedAtUtc = info.Timestamp;
        state.Version = 1;
        return state;
    }
}

public sealed record PolicyUpdated(
    string Name,
    string Instruction,
    string Scope,
    int Priority,
    bool Enabled,
    string[] Tags,
    string PreviousName
) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (PolicyState)stateData;
        if (state.Id == Guid.Empty || state.IsDeleted)
            throw new InvalidOperationException(
                $"Active policy '{info.AggregateId}' was not found."
            );

        state.Name = Name;
        state.Instruction = Instruction;
        state.Scope = Scope;
        state.Priority = Priority;
        state.Enabled = Enabled;
        state.Tags =  [ .. Tags ];
        state.UpdatedAtUtc = info.Timestamp;
        state.Version++;
        return state;
    }
}

public sealed record PolicyDeleted(string Reason) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (PolicyState)stateData;
        if (state.Id == Guid.Empty || state.IsDeleted)
            throw new InvalidOperationException(
                $"Active policy '{info.AggregateId}' was not found."
            );

        state.IsDeleted = true;
        state.UpdatedAtUtc = info.Timestamp;
        state.Version++;
        return state;
    }
}
