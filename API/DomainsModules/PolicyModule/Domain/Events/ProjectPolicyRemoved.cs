using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectPolicyRemoved : IEvent;

public class ProjectPolicyRemovedV1(Policy Policy) : IProjectPolicyRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (ProjectPoliciesStateData)stateData;
        generalPoliciesStateData.Policies.Remove(Policy.PolicyId);

        return generalPoliciesStateData;
    }
}
