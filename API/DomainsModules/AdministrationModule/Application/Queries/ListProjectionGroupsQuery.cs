using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using EventSourcing.Core.Interfaces;

namespace AdministrationModule.Application.Queries;

public sealed class ListProjectionGroupsQuery(
    IStateMachineDefinitionProvider definitionProvider
) : Query<List<ProjectionGroupDto>>
{
    protected override Task<List<ProjectionGroupDto>> ExecuteInternal(
        Executor executor
    ) =>
        Task.FromResult(
            definitionProvider
                .GetAll()
                .Select(
                    definition =>
                        new ProjectionGroupDto(
                            definition.Id,
                            definition.Projections
                                .Concat(
                                    definition.Events.Values
                                        .SelectMany(
                                            eventDefinition =>
                                                eventDefinition.Projections
                                        )
                                )
                                .Distinct(StringComparer.Ordinal)
                                .Order(StringComparer.Ordinal)
                                .ToList()
                        )
                )
                .Where(group => group.ProjectionNames.Count > 0)
                .OrderBy(
                    group => group.StateMachineId,
                    StringComparer.Ordinal
                )
                .ToList()
        );
}
