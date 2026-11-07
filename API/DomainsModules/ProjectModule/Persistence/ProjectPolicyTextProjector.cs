using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Persistence;

public sealed class ProjectPolicyTextProjector(
    ProjectPolicyTextRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var projects = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<ProjectStateData>()
            .ToList();
        var policyTexts = projects
            .Where(project => !project.IsDeleted)
            .ToDictionary(
                project => project.Id,
                project => PolicyTextCompiler.CompileProject(
                    project.ProjectName,
                    project.Policies.Values
                )
            );

        return repository.ReplaceProjects(
            projects.Select(project => project.Id).ToList(),
            policyTexts
        );
    }
}
