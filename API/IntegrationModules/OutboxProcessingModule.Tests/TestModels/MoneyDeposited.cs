using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace OutboxProcessingModule.Tests.TestModels;

public sealed class MoneyDeposited : IEvent
{
    public float Amount { get; set; }

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.Money += Amount;
        return account;
    }
}
