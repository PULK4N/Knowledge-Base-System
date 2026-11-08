using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain.Models;
using PolicyModule.Interfaces;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence;

public sealed class PolicyTextRepository(
    IPolicyModuleDbContext dbContext
) : ISessionPolicyTextService
{
    public async Task<SessionPolicyText> Get(
        string? agentFamilyName,
        List<TopicName> topicNamesInOrder
    )
    {
        var generalText = await dbContext.GeneralPolicyTexts
            .AsNoTracking()
            .Select(policyText => policyText.Text)
            .SingleOrDefaultAsync();
        var normalizedAgentFamilyName = string.IsNullOrWhiteSpace(agentFamilyName)
            ? null
            : AgentFamilyName.Normalized(agentFamilyName).Name;
        var agentFamilyText = normalizedAgentFamilyName is null
            ? null
            : await dbContext.AgentFamilyPolicyTexts
                .AsNoTracking()
                .Where(
                    policyText =>
                        policyText.AgentFamilyName == normalizedAgentFamilyName
                )
                .Select(policyText => policyText.Text)
                .SingleOrDefaultAsync();
        var topicNames = topicNamesInOrder
            .Select(topicName => topicName.Name)
            .ToList();
        var topicTexts = await dbContext.TopicPolicyTexts
            .AsNoTracking()
            .Where(policyText => topicNames.Contains(policyText.TopicName))
            .ToDictionaryAsync(
                policyText => policyText.TopicName,
                policyText => policyText.Text
            );

        return new SessionPolicyText(
            generalText ?? string.Empty,
            agentFamilyText ?? string.Empty,
            topicNames
                .Where(topicTexts.ContainsKey)
                .Select(topicName => topicTexts[topicName])
                .ToList()
        );
    }

    public Task<bool> AgentFamilyExists(string agentFamilyName)
    {
        var normalizedAgentFamilyName =
            AgentFamilyName.Normalized(agentFamilyName).Name;

        return dbContext.AgentFamilyPolicyTexts.AnyAsync(
            policyText =>
                policyText.AgentFamilyName == normalizedAgentFamilyName
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

    public async Task ReplaceAgentFamilies(
        IReadOnlyDictionary<string, string> policyTexts
    )
    {
        await dbContext.AgentFamilyPolicyTexts.ExecuteDeleteAsync();

        await dbContext.AgentFamilyPolicyTexts.AddRangeAsync(
            policyTexts.Select(
                policyText =>
                    new AgentFamilyPolicyText
                    {
                        AgentFamilyName = policyText.Key,
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

}
