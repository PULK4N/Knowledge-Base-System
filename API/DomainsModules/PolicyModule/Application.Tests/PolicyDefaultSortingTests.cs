using ActionModule.Shared.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Queries;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
{
    [Theory]
    [InlineData(PolicyScope.General)]
    [InlineData(PolicyScope.Topic)]
    [InlineData(PolicyScope.AgentFamily)]
    public async Task Rest_policies_sort_by_last_update_before_paging_and_mcp_keeps_insertion_order(
        PolicyScope scope
    )
    {
        var store = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(store);
        const string scopeName = "sorting";
        if (scope == PolicyScope.Topic)
            await ExecuteAsUser(new CreateTopicCommand(handler)
            {
                TopicName = scopeName, Description = "Sorting"
            });
        if (scope == PolicyScope.AgentFamily)
            await ExecuteAsUser(new CreateAgentFamilyCommand(handler)
            {
                AgentFamilyName = scopeName, Description = "Sorting"
            });

        var first = await AddPolicy(handler, scope, scopeName, "Alpha");
        var second = await AddPolicy(handler, scope, scopeName, "Zulu");
        Assert.Equal(second, Assert.Single((await Search(1)).Items).PolicyId);
        Assert.Equal(first, Assert.Single((await Search(2)).Items).PolicyId);

        await ExecuteAsUser(UpdatePolicy(handler, scope, scopeName, first));
        Assert.Equal(first, Assert.Single((await Search(1)).Items).PolicyId);

        var policies = scope switch
        {
            PolicyScope.Topic => await new ListTopicPoliciesQuery(CreateCalculator(), store)
            {
                TopicName = scopeName
            }.Execute(Executor),
            PolicyScope.AgentFamily => await new ListAgentFamilyPoliciesQuery(CreateCalculator(), store)
            {
                AgentFamilyName = scopeName
            }.Execute(Executor),
            _ => await new ListGeneralPoliciesQuery(CreateCalculator(), store).Execute(Executor)
        };
        Assert.Equal(new List<Guid> { first, second },
            policies!.Select(policy => policy.PolicyId).ToList());

        async Task<PagedResult<PolicyDto>> Search(int page)
        {
            var result = scope switch
            {
                PolicyScope.Topic => await new SearchTopicPoliciesQuery(CreateCalculator(), store)
                {
                    TopicName = scopeName, Page = page, PageSize = 1, Search = "Description"
                }.Execute(Executor),
                PolicyScope.AgentFamily => await new SearchAgentFamilyPoliciesQuery(CreateCalculator(), store)
                {
                    AgentFamilyName = scopeName, Page = page, PageSize = 1, Search = "Description"
                }.Execute(Executor),
                _ => await new SearchGeneralPoliciesQuery(CreateCalculator(), store)
                {
                    Page = page, PageSize = 1, Search = "Description"
                }.Execute(Executor)
            };
            Assert.Equal(2, result!.TotalCount);
            return result;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rest_containers_move_after_policy_changes_and_mcp_keeps_name_order(bool family)
    {
        var store = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(store);
        foreach (var name in new List<string> { "alpha", "zulu" })
        {
            PolicyCommand command = family
                ? new CreateAgentFamilyCommand(handler) { AgentFamilyName = name, Description = "Sorting" }
                : new CreateTopicCommand(handler) { TopicName = name, Description = "Sorting" };
            await ExecuteAsUser(command);
        }

        Assert.Equal(new List<string> { "zulu", "alpha" }, await Search());
        var scope = family ? PolicyScope.AgentFamily : PolicyScope.Topic;
        await AddPolicy(handler, scope, "alpha", "Policy");
        Assert.Equal(new List<string> { "alpha", "zulu" }, await Search());
        await AddPolicy(handler, scope, "zulu", "Policy");
        Assert.Equal(new List<string> { "zulu", "alpha" }, await Search());

        var mcpNames = family
            ? (await new ListPolicyAgentFamiliesQuery(CreateCalculator(), store).Execute(Executor))
                .Select(item => item.AgentFamilyName).ToList()
            : (await new ListPolicyTopicsQuery(CreateCalculator(), store).Execute(Executor))
                .Select(item => item.TopicName).ToList();
        Assert.Equal(new List<string> { "alpha", "zulu" }, mcpNames);

        async Task<List<string>> Search() => family
            ? (await new SearchPolicyAgentFamiliesQuery(CreateCalculator(), store).Execute(Executor))
                .Items.Select(item => item.AgentFamilyName).ToList()
            : (await new SearchPolicyTopicsQuery(CreateCalculator(), store).Execute(Executor))
                .Items.Select(item => item.TopicName).ToList();
    }

    [Fact]
    public void Equal_update_times_keep_title_and_id_tie_breakers_and_title_search_ranking()
    {
        var first = new Policy { PolicyId = new(Guid.Parse("00000001-0000-0000-0000-000000000000")), Title = "Alpha", Description = "Match", UpdatedAt = DateTime.UnixEpoch };
        var second = first with { PolicyId = new(Guid.Parse("00000002-0000-0000-0000-000000000000")) };
        var titleMatch = first with { PolicyId = new(Guid.NewGuid()), Title = "Match", UpdatedAt = DateTime.UnixEpoch.AddMinutes(-1) };
        var policies = new List<Policy> { second, titleMatch, first };

        Assert.Equal(new List<Guid> { first.PolicyId.Value, second.PolicyId.Value, titleMatch.PolicyId.Value },
            PolicySearch.Create(policies, 1, 25, null).Items.Select(item => item.PolicyId).ToList());
        Assert.Equal(titleMatch.PolicyId.Value,
            Assert.Single(PolicySearch.Create(policies, 1, 1, "Match").Items).PolicyId);
    }
}
