using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using ProjectModule.Persistence.Interfaces;
using ProjectModule.Persistence.Models;

namespace ProjectModule.Persistence;

public sealed class ProjectPolicyTextRepository(
    IProjectModuleDbContext dbContext
) : IProjectPolicyTextRepository
{
    public async Task<ProjectPolicyTextEntry?> Get(
        AggregateId projectAggregateId
    )
    {
        var projectId = projectAggregateId.Value;
        var text = await dbContext.ProjectPolicyTexts
            .AsNoTracking()
            .Where(policyText => policyText.ProjectAggregateId == projectId)
            .Select(policyText => policyText.Text)
            .SingleOrDefaultAsync();

        if (text is null)
            return null;

        var topicNames = await dbContext.ProjectPolicyTopics
            .AsNoTracking()
            .Where(relation => relation.ProjectAggregateId == projectId)
            .OrderBy(relation => relation.TopicOrder)
            .Select(relation => relation.TopicName)
            .ToListAsync();

        return new ProjectPolicyTextEntry(text, topicNames);
    }

    public async Task ReplaceProjects(
        IReadOnlyCollection<AggregateId> projectAggregateIds,
        IReadOnlyDictionary<AggregateId, string> policyTexts
    )
    {
        var projectIds = projectAggregateIds
            .Select(projectId => projectId.Value)
            .ToList();
        await dbContext.ProjectPolicyTexts
            .Where(
                policyText => projectIds.Contains(
                    policyText.ProjectAggregateId
                )
            )
            .ExecuteDeleteAsync();

        await dbContext.ProjectPolicyTexts.AddRangeAsync(
            policyTexts.Select(
                policyText =>
                    new ProjectPolicyText
                    {
                        ProjectAggregateId = policyText.Key.Value,
                        Text = policyText.Value
                    }
            )
        );
        await dbContext.SaveChangesAsync();
    }

    public async Task ReplaceProjectTopics(
        IReadOnlyCollection<AggregateId> projectAggregateIds,
        IReadOnlyDictionary<AggregateId, List<string>> topicsByProject
    )
    {
        var projectIds = projectAggregateIds
            .Select(projectId => projectId.Value)
            .ToList();
        var replacements = topicsByProject
            .SelectMany(
                project => project.Value.Select(
                    (topicName, order) =>
                        new ProjectPolicyTopic
                        {
                            ProjectAggregateId = project.Key.Value,
                            TopicName = topicName,
                            TopicOrder = order
                        }
                )
            )
            .ToList();
        await dbContext.ProjectPolicyTopics
            .Where(
                relation => projectIds.Contains(
                    relation.ProjectAggregateId
                )
            )
            .ExecuteDeleteAsync();

        await dbContext.ProjectPolicyTopics.AddRangeAsync(replacements);
        await dbContext.SaveChangesAsync();
    }
}
