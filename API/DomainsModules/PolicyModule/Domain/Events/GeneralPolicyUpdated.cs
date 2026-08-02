using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IGeneralPolicyUpdated : IEvent;

public class GeneralPolicyUpdatedV1(Policy Policy) : IGeneralPolicyUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Policies.Add(Policy.PolicyId, Policy);

        return generalPoliciesStateData;
    }
}
