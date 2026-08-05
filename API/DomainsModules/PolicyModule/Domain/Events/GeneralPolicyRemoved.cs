using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IGeneralPolicyRemoved : IEvent;

public readonly record struct GeneralPolicyRemovedV1(
    PolicyId PolicyId
) : IGeneralPolicyRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.Policies.Remove(PolicyId);

        return generalPolicies;
    }
}
