using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;

namespace PolicyModule.Persistence;

public sealed class AgentFamilyPolicyTextProjector(
    PolicyTextRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var generalPolicies = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<GeneralPoliciesStateData>()
            .Single();
        var policyTexts = generalPolicies.AgentFamilies.Values.ToDictionary(
            agentFamily => agentFamily.AgentFamilyName.Name,
            agentFamily => PolicyTextCompiler.CompileAgentFamily(
                agentFamily.AgentFamilyName.Name,
                agentFamily.Policies.Values
            )
        );

        return repository.ReplaceAgentFamilies(policyTexts);
    }
}
