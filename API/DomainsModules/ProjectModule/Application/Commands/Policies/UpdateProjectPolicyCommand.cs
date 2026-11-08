using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class UpdateProjectPolicyCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required Guid PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && PolicyId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectEvent(
            executor,
            new ProjectPolicyUpdatedV2(
                CreatePolicy(
                    PolicyModule.Shared.PolicyId.FromDatabaseGuid(
                        PolicyId
                    ),
                    Title,
                    Description
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
