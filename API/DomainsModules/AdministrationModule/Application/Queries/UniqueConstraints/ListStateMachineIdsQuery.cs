using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core.Interfaces;

namespace AdministrationModule.Application.Queries.UniqueConstraints;

public sealed class ListStateMachineIdsQuery(
    IStateMachineDefinitionProvider definitionProvider
) : Query<List<string>>
{
    protected override Task<List<string>> ExecuteInternal(Executor executor) =>
        Task.FromResult(
            definitionProvider
                .GetAll()
                .Select(definition => definition.Id)
                .Order(StringComparer.Ordinal)
                .ToList()
        );
}
