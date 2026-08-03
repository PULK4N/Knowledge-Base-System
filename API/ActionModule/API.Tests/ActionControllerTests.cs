using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using Xunit;

namespace ActionModule.API.Tests;

public sealed class ActionControllerTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task Execute_ObtainsExecutorAndRunsAction()
    {
        var executorProvider = new StubExecutorProvider();
        var controller = new TestActionController(executorProvider);
        var action = new TestAction
        {
            Name = "existing",
            Count = 2
        };

        var result = await controller.ExecuteAction(action);

        Assert.Equal("existing:2", result);
        Assert.Same(Executor, action.ExecutedBy);
        Assert.Equal(1, executorProvider.CallCount);
    }

    private sealed class TestActionController(
        IExecutorProvider executorProvider
    ) : ActionController(executorProvider)
    {
        public Task<TResult> ExecuteAction<TResult>(
            IAction<TResult> action
        ) =>
            Execute(action);
    }

    private sealed class StubExecutorProvider : IExecutorProvider
    {
        public int CallCount { get; private set; }

        public Task<Executor> GetExecutor()
        {
            CallCount++;
            return Task.FromResult(Executor);
        }
    }

    private sealed class TestAction : IAction<string>
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public Executor? ExecutedBy;

        public Task<bool> IsAuthorized(Executor executor) =>
            Task.FromResult(true);

        public Task<bool> CanExecute(Executor executor) =>
            Task.FromResult(true);

        public Task<string> Execute(Executor executor)
        {
            ExecutedBy = executor;
            return Task.FromResult($"{Name}:{Count}");
        }
    }

}
