using EventSourcing.Core;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryMcp.Domain;
using MemoryMcp.Domain.Policies;
using MemoryMcp.Domain.Skills;

namespace MemoryMcp.Services;

public sealed class MemoryService(
    StateMachineHandler stateMachineHandler,
    BaseSqlEventStore eventStore
)
{
    public async Task<SkillDto> SaveSkill(
        string name,
        string description,
        string content,
        string[] tags,
        Guid executorId
    )
    {
        ValidateName(name);
        ValidateRequired(content, nameof(content));

        var aggregateId = Guid.NewGuid();
        var payload = EventPayload.Create(
            executorId,
            aggregateId,
            MemoryConstants.SkillsStateMachine,
            new SkillSaved(name.Trim(), description.Trim(), content.Trim(), NormalizeTags(tags))
        );

        var states = await stateMachineHandler.ExecuteEvents(payload);
        return SkillDto.From((SkillState)states[aggregateId].StateData);
    }

    public async Task<SkillDto> UpdateSkill(
        Guid id,
        string? name,
        string? description,
        string? content,
        string[]? tags,
        Guid executorId
    )
    {
        var current =
            await GetSkillState(id, includeDeleted: false)
            ?? throw new InvalidOperationException($"Active skill '{id}' was not found.");

        var nextName = name?.Trim() ?? current.Name;
        var nextDescription = description?.Trim() ?? current.Description;
        var nextContent = content?.Trim() ?? current.Content;
        var nextTags = tags is null ? [ .. current.Tags ] : NormalizeTags(tags);
        ValidateName(nextName);
        ValidateRequired(nextContent, nameof(content));

        var payload = EventPayload.Create(
            executorId,
            id,
            MemoryConstants.SkillsStateMachine,
            new SkillUpdated(nextName, nextDescription, nextContent, nextTags, current.Name)
        );
        var states = await stateMachineHandler.ExecuteEvents(payload);
        return SkillDto.From((SkillState)states[id].StateData);
    }

    public async Task<DeleteResult> DeleteSkill(Guid id, string reason, Guid executorId)
    {
        _ =
            await GetSkillState(id, includeDeleted: false)
            ?? throw new InvalidOperationException($"Active skill '{id}' was not found.");
        var payload = EventPayload.Create(
            executorId,
            id,
            MemoryConstants.SkillsStateMachine,
            new SkillDeleted(reason.Trim())
        );
        var states = await stateMachineHandler.ExecuteEvents(payload);
        var state = (SkillState)states[id].StateData;
        return new DeleteResult(id, state.IsDeleted, state.Version);
    }

    public async Task<SkillDto?> GetSkill(Guid id, bool includeDeleted = false)
    {
        var state = await GetSkillState(id, includeDeleted);
        return state is null ? null : SkillDto.From(state);
    }

    public async Task<IReadOnlyList<SkillDto>> SearchSkills(
        string query,
        string? tag,
        bool includeDeleted,
        int limit
    )
    {
        ValidateLimit(limit);
        var states = await LoadAll<SkillState>(MemoryConstants.SkillsStateMachine);
        return states
            .Where(state => includeDeleted || !state.IsDeleted)
            .Where(
                state =>
                    string.IsNullOrWhiteSpace(tag)
                    || state.Tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase)
            )
            .Select(state => (State: state, Score: ScoreSkill(state, query)))
            .Where(item => string.IsNullOrWhiteSpace(query) || item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.State.UpdatedAtUtc)
            .Take(limit)
            .Select(item => SkillDto.From(item.State))
            .ToArray();
    }

    public async Task<PolicyDto> SavePolicy(
        string name,
        string instruction,
        string scope,
        int priority,
        bool enabled,
        string[] tags,
        Guid executorId
    )
    {
        ValidateName(name);
        ValidateRequired(instruction, nameof(instruction));
        ValidatePriority(priority);
        var aggregateId = Guid.NewGuid();
        var payload = EventPayload.Create(
            executorId,
            aggregateId,
            MemoryConstants.PoliciesStateMachine,
            new PolicySaved(
                name.Trim(),
                instruction.Trim(),
                NormalizeScope(scope),
                priority,
                enabled,
                NormalizeTags(tags)
            )
        );
        var states = await stateMachineHandler.ExecuteEvents(payload);
        return PolicyDto.From((PolicyState)states[aggregateId].StateData);
    }

    public async Task<PolicyDto> UpdatePolicy(
        Guid id,
        string? name,
        string? instruction,
        string? scope,
        int? priority,
        bool? enabled,
        string[]? tags,
        Guid executorId
    )
    {
        var current =
            await GetPolicyState(id, includeDeleted: false)
            ?? throw new InvalidOperationException($"Active policy '{id}' was not found.");
        var nextName = name?.Trim() ?? current.Name;
        var nextInstruction = instruction?.Trim() ?? current.Instruction;
        var nextScope = scope is null ? current.Scope : NormalizeScope(scope);
        var nextPriority = priority ?? current.Priority;
        var nextTags = tags is null ? [ .. current.Tags ] : NormalizeTags(tags);
        ValidateName(nextName);
        ValidateRequired(nextInstruction, nameof(instruction));
        ValidatePriority(nextPriority);

        var payload = EventPayload.Create(
            executorId,
            id,
            MemoryConstants.PoliciesStateMachine,
            new PolicyUpdated(
                nextName,
                nextInstruction,
                nextScope,
                nextPriority,
                enabled ?? current.Enabled,
                nextTags,
                current.Name
            )
        );
        var states = await stateMachineHandler.ExecuteEvents(payload);
        return PolicyDto.From((PolicyState)states[id].StateData);
    }

    public async Task<DeleteResult> DeletePolicy(Guid id, string reason, Guid executorId)
    {
        _ =
            await GetPolicyState(id, includeDeleted: false)
            ?? throw new InvalidOperationException($"Active policy '{id}' was not found.");
        var payload = EventPayload.Create(
            executorId,
            id,
            MemoryConstants.PoliciesStateMachine,
            new PolicyDeleted(reason.Trim())
        );
        var states = await stateMachineHandler.ExecuteEvents(payload);
        var state = (PolicyState)states[id].StateData;
        return new DeleteResult(id, state.IsDeleted, state.Version);
    }

    public async Task<PolicyDto?> GetPolicy(Guid id, bool includeDeleted = false)
    {
        var state = await GetPolicyState(id, includeDeleted);
        return state is null ? null : PolicyDto.From(state);
    }

    public async Task<IReadOnlyList<PolicyDto>> SearchPolicies(
        string query,
        string? scope,
        string? tag,
        bool includeDisabled,
        bool includeDeleted,
        int limit
    )
    {
        ValidateLimit(limit);
        var states = await LoadAll<PolicyState>(MemoryConstants.PoliciesStateMachine);
        return states
            .Where(state => includeDeleted || !state.IsDeleted)
            .Where(state => includeDisabled || state.Enabled)
            .Where(
                state =>
                    string.IsNullOrWhiteSpace(scope)
                    || string.Equals(state.Scope, scope.Trim(), StringComparison.OrdinalIgnoreCase)
            )
            .Where(
                state =>
                    string.IsNullOrWhiteSpace(tag)
                    || state.Tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase)
            )
            .Select(state => (State: state, Score: ScorePolicy(state, query)))
            .Where(item => string.IsNullOrWhiteSpace(query) || item.Score > 0)
            .OrderByDescending(item => item.State.Priority)
            .ThenByDescending(item => item.Score)
            .ThenByDescending(item => item.State.UpdatedAtUtc)
            .Take(limit)
            .Select(item => PolicyDto.From(item.State))
            .ToArray();
    }

    private async Task<SkillState?> GetSkillState(Guid id, bool includeDeleted)
    {
        var state = await LoadOne<SkillState>(id, MemoryConstants.SkillsStateMachine);
        return state is null || (!includeDeleted && state.IsDeleted) ? null : state;
    }

    private async Task<PolicyState?> GetPolicyState(Guid id, bool includeDeleted)
    {
        var state = await LoadOne<PolicyState>(id, MemoryConstants.PoliciesStateMachine);
        return state is null || (!includeDeleted && state.IsDeleted) ? null : state;
    }

    private async Task<TState?> LoadOne<TState>(Guid id, string stateMachineId)
        where TState : class
    {
        var payloads = await eventStore.GetEvents(
            serialized =>
                serialized.AggregateId == id && serialized.StateMachineId == stateMachineId
        );
        if (payloads.Count == 0)
            return null;

        var state = await stateMachineHandler.Calculate(
            payloads.OrderBy(payload => payload.EventExecutionInfo.OrderNumber).ToList(),
            [ ]
        );
        return (TState)state.StateData;
    }

    private async Task<IReadOnlyList<TState>> LoadAll<TState>(string stateMachineId)
        where TState : class
    {
        var payloads = await eventStore.GetEvents(
            serialized => serialized.StateMachineId == stateMachineId
        );
        var states = new List<TState>();
        foreach (
            var aggregateEvents in payloads.GroupBy(
                payload => payload.EventExecutionInfo.AggregateId
            )
        )
        {
            var state = await stateMachineHandler.Calculate(
                aggregateEvents.OrderBy(payload => payload.EventExecutionInfo.OrderNumber).ToList(),
                [ ]
            );
            states.Add((TState)state.StateData);
        }

        return states;
    }

    private static int ScoreSkill(SkillState state, string query) =>
        Score(
            query,
            (state.Name, 8),
            (state.Description, 4),
            (state.Content, 2),
            (string.Join(' ', state.Tags), 3)
        );

    private static int ScorePolicy(PolicyState state, string query) =>
        Score(
            query,
            (state.Name, 8),
            (state.Instruction, 4),
            (state.Scope, 3),
            (string.Join(' ', state.Tags), 2)
        );

    private static int Score(string query, params (string Value, int Weight)[] fields)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var terms = query.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return fields.Sum(
            field =>
                terms.Count(term => field.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                * field.Weight
        );
    }

    private static string[] NormalizeTags(IEnumerable<string> tags) =>
        tags.Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeScope(string scope) =>
        string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();

    private static void ValidateName(string name)
    {
        ValidateRequired(name, nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required.", parameterName);
    }

    private static void ValidatePriority(int priority)
    {
        if (priority is < -1000 or > 1000)
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                "Priority must be -1000 to 1000."
            );
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1 to 100.");
    }
}
