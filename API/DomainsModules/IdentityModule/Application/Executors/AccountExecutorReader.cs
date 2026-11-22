using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Domain;

namespace IdentityModule.Application.Executors;

/// <summary>
/// Builds the executor of a logged-in account from the account's current state, so a change to the
/// account applies to the next request.
/// </summary>
public sealed class AccountExecutorReader(StateMachineHandler handler)
{
    /// <returns>The account's executor, or null when no account has that ID.</returns>
    public async Task<Executor?> Read(Guid accountId)
    {
        var stateInfo = await handler.GetByAggregateId(AggregateId.FromDatabaseGuid(accountId));
        if (stateInfo?.StateData is not AccountStateData { IsCreated: true, IsDeleted: false } account)
            return null;

        return new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(accountId),
            IsAdministrator = account.IsAdministrator,
            Permissions = account.Permissions.Select(name => new Permission(name)).ToHashSet()
        };
    }
}
