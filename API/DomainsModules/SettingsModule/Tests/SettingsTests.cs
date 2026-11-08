using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Core.Models;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SettingsModule.Application.Commands;
using SettingsModule.Application.Queries;
using SettingsModule.Domain;
using SettingsModule.Domain.Events;
using SettingsModule.Persistence;
using SettingsModule.Persistence.Models;
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
using SettingsModule.Application.Persistence;

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
    public async Task Projection_ReplacesSettingsAndIgnoresReplayedOlderVersions()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TestContext(new DbContextOptionsBuilder<TestContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var repository = new CodeReviewSettingsRepository(db);
        SettingsStateData State(uint version, int batchSize) => new(SettingsId)
        { IsInitialized = true, OrderNumber = version, CodeReviewBatchSize = batchSize, CodeReviewTaskMessage = "Review" };

        await repository.Write([State(1, 5)]);
        await repository.Write([State(3, 2)]);
        await repository.Write([State(2, 9), State(3, 2)]);
        var result = await new GetCodeReviewSettingsQuery(repository).Execute(Executor);
        Assert.Equal(2, result!.BatchSize);
        Assert.Equal("Review", result.TaskMessage);
        Assert.Single(await db.CodeReviewSettings.ToListAsync());
    }

    [Fact]
    public async Task HttpApi_ReadsProjectionAndUpdatesThroughStateMachine()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestContext>().UseSqlite(connection).Options;
        await using var db = new TestContext(options);
        await db.Database.EnsureCreatedAsync();
        await new CodeReviewSettingsRepository(db).Write([new(SettingsId)
        { IsInitialized = true, OrderNumber = 1, CodeReviewBatchSize = 5, CodeReviewTaskMessage = "Initial task" }]);
        var store = new Store(true);
        var definitions = new YamlStateMachineDefinitionProvider(Path.Combine(AppContext.BaseDirectory, "StateMachines"));
        var calculator = new StateCalculator(new OrderNumberHelper(), new StateProvider(),
            new EventValidatorProvider(definitions), definitions);
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddControllers().AddApplicationPart(typeof(CodeReviewSettingsController).Assembly);
            services.AddScoped(_ => new TestContext(options));
            services.AddScoped<ISettingsModuleDbContext>(provider => provider.GetRequiredService<TestContext>());
            services.AddScoped<ICodeReviewSettingsReader, CodeReviewSettingsRepository>();
            services.AddScoped<GetCodeReviewSettingsQuery>();
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
        var state = (SettingsStateData)calculator.Calculate(store.History.Concat(store.Written).ToList(), []).StateData;
        await new CodeReviewSettingsRepository(db).Write([state]);
        Assert.Equal(updated, await client.GetFromJsonAsync<CodeReviewSettingsDto>("/api/settings/code-review"));
        var invalid = await client.PostAsJsonAsync("/api/settings/code-review", new CodeReviewSettingsDto(0, " "));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Single(store.Written);
    }

    [Fact]
    public async Task ProjectionQuery_ReturnsNullBeforeInitialization()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TestContext(new DbContextOptionsBuilder<TestContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        Assert.Null(await new GetCodeReviewSettingsQuery(new CodeReviewSettingsRepository(db)).Execute(Executor));
    }

    private sealed class StateProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) => new SettingsStateData(aggregateId);
    }

    private sealed class TestExecutorProvider : IExecutorProvider
    {
        public Task<Executor> GetExecutor() => Task.FromResult(Executor);
    }

    private sealed class Store(bool initialized) : IEventStoreWithOutbox
    {
        public List<EventPayload> History { get; } = initialized ? [Initial()] : [];
        public List<EventPayload> Written { get; private set; } = [];
        private static EventPayload Initial()
        {
            var payload = EventPayload.Create(Executor.Id, SettingsId, "settings-state-machine", new SettingsInitializedV1(5, "Initial task"));
            payload.EventExecutionInfo.OrderNumber = 1;
            return payload;
        }
        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> ids) =>
            Task.FromResult(ids.ToDictionary(id => id, id => History.ToList()));
        public Task Write(Dictionary<AggregateId, StateInfo> states)
        {
            Written = states.Values.SelectMany(state => state.LastExecutedPayloads).ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options), ISettingsModuleDbContext
    {
        public DbSet<CodeReviewSettingsEntry> CodeReviewSettings => Set<CodeReviewSettingsEntry>();
        protected override void OnModelCreating(ModelBuilder builder) =>
            builder.Entity<CodeReviewSettingsEntry>(entry =>
            {
                entry.HasKey(value => value.AggregateId);
                entry.Property(value => value.OrderNumber).HasConversion<long>().IsConcurrencyToken();
            });
    }
}
