using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Persistence;

public sealed class ProjectTopicProjector(
    ProjectPolicyTextRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var projects = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<ProjectStateData>()
            .ToList();
        var topicsByProject = projects
            .Where(project => !project.IsDeleted)
            .ToDictionary(
                project => project.Id,
                project => project.RelatedTopics
                    .Select(topicName => topicName.Name)
                    .ToList()
            );

        return repository.ReplaceProjectTopics(
            projects.Select(project => project.Id).ToList(),
            topicsByProject
        );
    }
}
