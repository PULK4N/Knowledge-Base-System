using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace MemoryMcp.Domain.Skills;

public sealed record SkillSaved(string Name, string Description, string Content, string[] Tags)
    : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (SkillState)stateData;
        if (state.Id != Guid.Empty)
            throw new InvalidOperationException($"Skill '{info.AggregateId}' already exists.");

        state.Id = info.AggregateId;
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags =  [ .. Tags ];
        state.CreatedAtUtc = info.Timestamp;
        state.UpdatedAtUtc = info.Timestamp;
        state.Version = 1;
        return state;
    }
}

public record SkillUpdated(
    string Name,
    string Description,
    string Content,
    string[] Tags,
    string PreviousName
) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (SkillState)stateData;
        EnsureActive(state, info.AggregateId);
        state.Name = Name;
        state.Description = Description;
        state.Content = Content;
        state.Tags =  [ .. Tags ];
        state.UpdatedAtUtc = info.Timestamp;
        state.Version++;
        return state;
    }

    private static void EnsureActive(SkillState state, Guid id)
    {
        if (state.Id == Guid.Empty || state.IsDeleted)
            throw new InvalidOperationException($"Active skill '{id}' was not found.");
    }
}

public sealed record SkillDeleted(string Reason) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo info)
    {
        var state = (SkillState)stateData;
        if (state.Id == Guid.Empty || state.IsDeleted)
            throw new InvalidOperationException(
                $"Active skill '{info.AggregateId}' was not found."
            );

        state.IsDeleted = true;
        state.UpdatedAtUtc = info.Timestamp;
        state.Version++;
        return state;
    }
}
