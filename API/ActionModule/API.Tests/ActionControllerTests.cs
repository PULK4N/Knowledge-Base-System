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
    public async Task ExecuteWithBody_MapsSameTypePropertiesAndExecutes()
    {
        var executorProvider = new StubExecutorProvider();
        var controller = new TestActionController(executorProvider);
        var action = new TestAction();
        var body = new TestAction
        {
            Name = "payment",
            Count = 3
        };

        var result = await controller.ExecuteWithBody(action, body);

        Assert.Equal("payment:3", result);
        Assert.Equal("payment", action.Name);
        Assert.Equal(3, action.Count);
        Assert.Same(Executor, action.ExecutedBy);
        Assert.Equal(1, executorProvider.CallCount);
    }

    [Fact]
    public async Task ExecuteWithoutBody_ExecutesWithoutMapping()
    {
        var executorProvider = new StubExecutorProvider();
        var controller = new TestActionController(executorProvider);
        var action = new TestAction
        {
            Name = "existing",
            Count = 2
        };

        var result = await controller.ExecuteWithoutBody(action);

        Assert.Equal("existing:2", result);
        Assert.Same(Executor, action.ExecutedBy);
        Assert.Equal(1, executorProvider.CallCount);
    }

    [Fact]
    public async Task ExecuteWithBody_ThrowsForDifferentTypes()
    {
        var executorProvider = new StubExecutorProvider();
        var controller = new TestActionController(executorProvider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                controller.ExecuteWithBody(
                    new TestAction(),
                    new DifferentAction()
                )
        );

        Assert.Contains("DifferentAction", exception.Message);
        Assert.Contains("TestAction", exception.Message);
        Assert.Equal(0, executorProvider.CallCount);
    }

    [Fact]
    public async Task ExecuteWithBody_SkipsReadOnlyProperties()
    {
        var executorProvider = new StubExecutorProvider();
        var controller = new TestActionController(executorProvider);

        var result = await controller.ExecuteWithBody(
            new ReadOnlyAction(),
            new ReadOnlyAction()
        );

        Assert.Equal("read-only", result);
        Assert.Equal(1, executorProvider.CallCount);
    }

    private sealed class TestActionController(
        IExecutorProvider executorProvider
    ) : ActionController(executorProvider)
    {
        public Task<TResult> ExecuteWithBody<TResult>(
            IAction<TResult> action,
            IAction<TResult> body
        ) =>
            Execute(action, body);

        public Task<TResult> ExecuteWithoutBody<TResult>(
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

    private sealed class DifferentAction : IAction<string>
    {
        public Task<bool> IsAuthorized(Executor executor) =>
            Task.FromResult(true);

        public Task<bool> CanExecute(Executor executor) =>
            Task.FromResult(true);

        public Task<string> Execute(Executor executor) =>
            Task.FromResult("");
    }

    private sealed class ReadOnlyAction : IAction<string>
    {
        public string Name => "read-only";

        public Task<bool> IsAuthorized(Executor executor) =>
            Task.FromResult(true);

        public Task<bool> CanExecute(Executor executor) =>
            Task.FromResult(true);

        public Task<string> Execute(Executor executor) =>
            Task.FromResult(Name);
    }

}
