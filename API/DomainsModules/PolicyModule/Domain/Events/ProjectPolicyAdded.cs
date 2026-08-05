using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectPolicyAdded : IEvent;

public readonly record struct ProjectPolicyAddedV1(Policy Policy) : IProjectPolicyAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (ProjectPoliciesStateData)stateData;
        generalPoliciesStateData.Policies.Add(Policy.PolicyId, Policy);

        return generalPoliciesStateData;
    }
}
