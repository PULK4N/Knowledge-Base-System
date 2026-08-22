using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;

namespace PolicyModule.Persistence;

public sealed class GeneralPolicyTextProjector(
    PolicyTextRepository repository
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var generalPolicies = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<GeneralPoliciesStateData>()
            .Single();

        await repository.ReplaceGeneral(
            generalPolicies.Id,
            PolicyTextCompiler.CompileGeneral(
                generalPolicies.Policies.Values
            )
        );
    }
}
