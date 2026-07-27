using ActionModule.Models;
using EventSourcing.Shared.Models;
using UUIDNext;

namespace ActionModule.Tests;

public sealed class ActionTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task ExecutorProvider_ReturnsActionExecutorWithGeneratedId()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var provider = new TemporaryExecutorProvider();

        var first = await provider.GetExecutor();
        var second = await provider.GetExecutor();

        Assert.NotEqual(Guid.Empty, first.Id.Value);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Execute_WhenAuthorizedAndExecutable_ReturnsInternalResult()
    {
        var action = new TestAction(isAuthorized: true, canExecute: true, "executed");

        var result = await action.Execute(Executor);

        Assert.Equal("executed", result);
        Assert.Equal(
            ["IsAuthorized", "CanExecute", "ExecuteInternal"],
            action.Calls
        );
    }

    [Fact]
    public async Task Execute_WhenNotAuthorized_StopsBeforeCanExecute()
    {
        var action = new TestAction(isAuthorized: false, canExecute: true, "not returned");

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => action.Execute(Executor)
        );

        Assert.Contains(nameof(TestAction), exception.Message);
        Assert.Equal(["IsAuthorized"], action.Calls);
    }

    [Fact]
    public async Task Execute_WhenCannotExecute_StopsBeforeInternalExecution()
    {
        var action = new TestAction(isAuthorized: true, canExecute: false, "not returned");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => action.Execute(Executor)
        );

        Assert.Contains(nameof(TestAction), exception.Message);
        Assert.Equal(["IsAuthorized", "CanExecute"], action.Calls);
    }

    [Fact]
    public async Task Execute_WhenAdditionalMappingIsOverridden_ReturnsMappedResult()
    {
        var action = new MappedAction("executed");

        var result = await action.Execute(Executor);

        Assert.Equal("executed-mapped", result);
        Assert.Equal(
            ["IsAuthorized", "CanExecute", "ExecuteInternal", "MapAdditionally"],
            action.Calls
        );
    }

    private class TestAction(
        bool isAuthorized,
        bool canExecute,
        string result
    ) : Action<string>
    {
        public List<string> Calls { get; } = [];

        public override Task<bool> IsAuthorized(Executor executor)
        {
            Assert.Equal(Executor, executor);
            Calls.Add(nameof(IsAuthorized));
            return Task.FromResult(isAuthorized);
        }

        public override Task<bool> CanExecute(Executor executor)
        {
            Assert.Equal(Executor, executor);
            Calls.Add(nameof(CanExecute));
            return Task.FromResult(canExecute);
        }

        protected override Task<string> ExecuteInternal(Executor executor)
        {
            Assert.Equal(Executor, executor);
            Calls.Add(nameof(ExecuteInternal));
            return Task.FromResult(result);
        }
    }

    private sealed class MappedAction(string result)
        : TestAction(isAuthorized: true, canExecute: true, result)
    {
        protected override async Task<string> MapAdditionally(
            Executor executor,
            string result
        )
        {
            Assert.Equal(Executor, executor);
            await Task.Yield();
            Calls.Add(nameof(MapAdditionally));

            return $"{result}-mapped";
        }
    }
}
