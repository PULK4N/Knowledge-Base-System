using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Core.Models;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using SettingsModule.Application.Commands;
using SettingsModule.Application.Queries;
using SettingsModule.Domain;
using SettingsModule.Domain.Events;
using SharedModule.Constants;
using Xunit;
using System.Net;
using System.Net.Http.Json;
using ActionModule.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using SettingsModule.API.Controllers;
using SettingsModule.Application.DTOs;

namespace SettingsModule.Tests;

public sealed class SettingsTests
{
    private static readonly AggregateId SettingsId = AggregateId.FromDatabaseGuid(StateDataAggregateIds.Settings);
    private static readonly Executor Executor = new() { Id = EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) };

    static SettingsTests() => new ServiceCollection().RegisterEventSourcingCore(typeof(SettingsStateData).Assembly);

    [Theory]
    [InlineData(5, "Review carefully", true, true)]
    [InlineData(0, "Review", true, false)]
    [InlineData(-1, "Review", true, false)]
    [InlineData(5, "   ", true, false)]
    [InlineData(5, "Review", false, false)]
    public async Task Update_UsesRealYamlAndWritesOnlyValidInitializedSettings(
        int batchSize, string taskMessage, bool initialized, bool succeeds)
    {
        var store = new Store(initialized);
        var definitions = new YamlStateMachineDefinitionProvider(Path.Combine(AppContext.BaseDirectory, "StateMachines"));
        var calculator = new StateCalculator(new OrderNumberHelper(), new StateProvider(),
            new EventValidatorProvider(definitions), definitions);
        var command = new UpdateCodeReviewSettingsCommand(new StateMachineHandler(calculator, store))
        { BatchSize = batchSize, TaskMessage = taskMessage };

        if (succeeds)
        {
            var result = await command.Execute(Executor);
            Assert.Equal(batchSize, result.BatchSize);
            var payload = Assert.Single(store.Written);
            Assert.IsType<CodeReviewSettingsUpdatedV1>(payload.EventData);
            Assert.Equal(Executor.Id, payload.EventExecutionInfo.EventExecutor);
            Assert.Equal(SettingsId, payload.EventExecutionInfo.AggregateId);
            var state = (SettingsStateData)calculator.Calculate(store.History.Concat(store.Written).ToList(), []).StateData;
            Assert.Equal(taskMessage, state.CodeReviewTaskMessage);
            Assert.Equal(2U, state.OrderNumber);
        }
        else
        {
            await Assert.ThrowsAnyAsync<Exception>(() => command.Execute(Executor));
            Assert.Empty(store.Written);
        }
    }

    [Fact]
    public async Task Query_ReplaysEventsAndReturnsLatestSettings()
    {
        var store = new Store(true);
        store.Written.Add(Payload(new CodeReviewSettingsUpdatedV1(2, "Latest task"), 2));

        var result = await new GetCodeReviewSettingsQuery(store, Calculator()).Execute(Executor);

        Assert.Equal(new CodeReviewSettingsDto(2, "Latest task"), result);
    }

    [Fact]
    public async Task Query_ReturnsNullBeforeInitialization() =>
        Assert.Null(await new GetCodeReviewSettingsQuery(new Store(false), Calculator()).Execute(Executor));

    [Fact]
    public async Task HttpApi_ReadsEventsAndUpdatesThroughStateMachine()
    {
        var store = new Store(true);
        var calculator = Calculator();
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddControllers().AddApplicationPart(typeof(CodeReviewSettingsController).Assembly);
            services.AddScoped(_ => new GetCodeReviewSettingsQuery(store, calculator));
            services.AddScoped<UpdateCodeReviewSettingsCommand>();
            services.AddScoped(_ => new StateMachineHandler(calculator, store));
            services.AddSingleton<IExecutorProvider, TestExecutorProvider>();
        }).Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapControllers())));
        using var client = server.CreateClient();
        Assert.Equal(5, (await client.GetFromJsonAsync<CodeReviewSettingsDto>("/api/settings/code-review"))!.BatchSize);
        var updated = new CodeReviewSettingsDto(2, "Feature instructions and skills");
        var response = await client.PostAsJsonAsync("/api/settings/code-review", updated);
        response.EnsureSuccessStatusCode();
        Assert.Equal(updated, await response.Content.ReadFromJsonAsync<CodeReviewSettingsDto>());
        Assert.Single(store.Written);
        Assert.Equal(updated, await client.GetFromJsonAsync<CodeReviewSettingsDto>("/api/settings/code-review"));
        var invalid = await client.PostAsJsonAsync("/api/settings/code-review", new CodeReviewSettingsDto(0, " "));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Single(store.Written);
    }

    private static StateCalculator Calculator()
    {
        var definitions = new YamlStateMachineDefinitionProvider(Path.Combine(AppContext.BaseDirectory, "StateMachines"));
        return new StateCalculator(new OrderNumberHelper(), new StateProvider(),
            new EventValidatorProvider(definitions), definitions);
    }

    private static EventPayload Payload(IEvent eventData, uint orderNumber)
    {
        var payload = EventPayload.Create(Executor.Id, SettingsId, "settings-state-machine", eventData);
        payload.EventExecutionInfo.OrderNumber = orderNumber;
        return payload;
    }

    private sealed class StateProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) => new SettingsStateData(aggregateId);
    }

    private sealed class TestExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class Store(bool initialized) : IEventStoreWithOutbox, IEventStore
    {
        public List<EventPayload> History { get; } =
            initialized ? [Payload(new SettingsInitializedV1(5, "Initial task"), 1)] : [];
        public List<EventPayload> Written { get; } = [];
        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> ids) =>
            Task.FromResult(ids.ToDictionary(id => id, id => History.Concat(Written).ToList()));
        public Task Write(Dictionary<AggregateId, StateInfo> states)
        {
            Written.AddRange(states.Values.SelectMany(state => state.LastExecutedPayloads));
            return Task.CompletedTask;
        }
        public Task Write(List<StateInfo> states) => throw new NotSupportedException();
    }
}
