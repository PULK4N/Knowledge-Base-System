using EventSourcing.Core;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using IdentityModule.Domain.Events;
using IdentityModule.Domain.Validators;

namespace IdentityModule.Domain.Tests;

public sealed class AccountValidatorTests
{
    private static readonly AggregateId AccountId =
        AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly AdministratorCreatedV1 Administrator = new("admin");
    private static readonly AgentCreatedV1 Agent = new("codex", "Codex", false, ["memory.write"]);

    public static TheoryData<IEvent[], IEvent, string?> Cases => new()
    {
        { [], Administrator, null },
        { [Administrator], Administrator, nameof(AccountMustNotExistValidator) },
        { [], new AdministratorCreatedV1("  "), nameof(UserNameRequiredValidator) },

        { [Administrator], new AdministratorPasswordChangedV1("new-hash"), null },
        { [Agent], new AdministratorPasswordChangedV1("new-hash"), nameof(AccountMustNotBeAgentValidator) },

        { [], Agent, null },
        { [Agent], Agent, nameof(AccountMustNotExistValidator) },
        { [], Agent with { UserName = "" }, nameof(UserNameRequiredValidator) },
        { [], Agent with { Permissions = ["memory.write", "everything"] }, nameof(KnownPermissionsValidator) },

        { [Agent], new AgentPermissionsSetV1(true, ["skills.read"]), null },
        { [Administrator], new AgentPermissionsSetV1(false, ["skills.read"]), nameof(AccountMustBeAgentValidator) },
        { [Agent], new AgentPermissionsSetV1(false, ["skills.admin"]), nameof(KnownPermissionsValidator) }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Event_IsAcceptedOrRejectedByTheValidatorsInTheRealYaml(
        IEvent[] history, IEvent eventData, string? failingValidator)
    {
        var store = new InMemoryEventStore(IdentityStateMachine.Payloads(AccountId, history));
        var handler = new StateMachineHandler(IdentityStateMachine.Calculator(), store);
        var execute = () => handler.ExecuteEvents(
            EventPayload.Create(IdentityStateMachine.Executor, AccountId, IdentityStateMachine.Id, eventData));

        if (failingValidator is null)
        {
            await execute();
            Assert.Equal(eventData, Assert.Single(store.Written).EventData);
            return;
        }

        var exception = await Assert.ThrowsAsync<EventValidationException>(execute);
        Assert.Contains(exception.ValidationResults, result => result.ValidatorName == failingValidator);
        Assert.Empty(store.Written);
    }
}
