using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;

namespace PolicyModule.Persistence;

public sealed class TopicPolicyTextProjector(
    PolicyTextRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var generalPolicies = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<GeneralPoliciesStateData>()
            .Single();
        var policyTexts = generalPolicies.Topics.Values.ToDictionary(
            topic => topic.TopicName.Name,
            topic => PolicyTextCompiler.CompileTopic(
                topic.TopicName.Name,
                topic.Policies.Values
            )
        );

        return repository.ReplaceTopics(policyTexts);
    }
}
