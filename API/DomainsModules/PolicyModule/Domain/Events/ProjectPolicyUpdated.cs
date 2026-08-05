using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectPolicyUpdated : IEvent;

public readonly record struct ProjectPolicyUpdatedV1(
    Policy Policy
) : IProjectPolicyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.Policies[Policy.PolicyId] = Policy;

        return projectPolicies;
    }
}
