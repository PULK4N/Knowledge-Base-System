using EventSourcing.Core.Interfaces;
using MemoryMcp.Domain;
using MemoryMcp.Domain.Policies;
using MemoryMcp.Domain.Skills;

namespace MemoryMcp.Infrastructure;

public sealed class MemoryStateDataProvider : IStateDataProvider
{
    public Task<object> GetStateDataByStateMachine(string stateMachineId) =>
        Task.FromResult<object>(
            stateMachineId switch
            {
                MemoryConstants.SkillsStateMachine => new SkillState(),
                MemoryConstants.PoliciesStateMachine => new PolicyState(),
                _
                    => throw new InvalidOperationException(
                        $"Unknown memory state machine '{stateMachineId}'."
                    )
            }
        );
}
