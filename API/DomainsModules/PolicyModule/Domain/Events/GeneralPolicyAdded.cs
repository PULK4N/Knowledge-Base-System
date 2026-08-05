using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IGeneralPolicyAdded : IEvent;

public readonly record struct GeneralPolicyAddedV1(Policy Policy) : IGeneralPolicyAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Policies.Add(Policy.PolicyId, Policy);

        return generalPoliciesStateData;
    }
}
