using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence;

public sealed class PolicyTextRepository(
    IPolicyModuleDbContext dbContext
) : IPolicyTextRepository
{
    public async Task<string?> Get(AggregateId projectAggregateId)
    {
        var projectId = projectAggregateId.Value;
        var project = dbContext.ProjectPolicyTexts.Where(
            policyText => policyText.ProjectAggregateId == projectId
        );
        var segments = await project
            .Select(
                policyText =>
                    new
                    {
                        Group = 1,
                        Order = 0,
                        policyText.Text
                    }
            )
            .Concat(
                project.SelectMany(
                    _ => dbContext.GeneralPolicyTexts,
                    (_, policyText) =>
                        new
                        {
                            Group = 0,
                            Order = 0,
                            policyText.Text
                        }
                )
            )
            .Concat(
                from projectPolicyText in project
                join relation in dbContext.ProjectPolicyTopics
                    on projectPolicyText.ProjectAggregateId equals relation.ProjectAggregateId
                join topicPolicyText in dbContext.TopicPolicyTexts
                    on relation.TopicName equals topicPolicyText.TopicName
                select new
                {
                    Group = 2,
                    Order = relation.TopicOrder,
                    topicPolicyText.Text
                }
            )
            .OrderBy(segment => segment.Group)
            .ThenBy(segment => segment.Order)
            .Select(segment => segment.Text)
            .ToListAsync();

        if (segments.Count == 0)
            return null;

        return string.Join(
            "\n\n",
            segments.Where(text => !string.IsNullOrWhiteSpace(text))
        );
    }

    public async Task ReplaceGeneral(
        AggregateId aggregateId,
        string text
    )
    {
        var aggregateGuid = aggregateId.Value;
        await dbContext.GeneralPolicyTexts
            .Where(policyText => policyText.AggregateId == aggregateGuid)
            .ExecuteDeleteAsync();

        dbContext.GeneralPolicyTexts.Add(
            new GeneralPolicyText
            {
                AggregateId = aggregateGuid,
                Text = text
            }
        );

        await dbContext.SaveChangesAsync();
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

    public async Task ReplaceTopics(
        IReadOnlyDictionary<string, string> policyTexts
    )
    {
        await dbContext.TopicPolicyTexts.ExecuteDeleteAsync();

        await dbContext.TopicPolicyTexts.AddRangeAsync(
            policyTexts.Select(
                policyText =>
                    new TopicPolicyText
                    {
                        TopicName = policyText.Key,
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
