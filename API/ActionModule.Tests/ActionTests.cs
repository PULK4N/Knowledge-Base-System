namespace ActionModule.Tests;

public sealed class ActionTests
{
    [Fact]
    public async Task Execute_WhenAuthorizedAndExecutable_ReturnsInternalResult()
    {
        var action = new TestAction(isAuthorized: true, canExecute: true, "executed");

        var result = await action.Execute();

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
            action.Execute
        );

        Assert.Contains(nameof(TestAction), exception.Message);
        Assert.Equal(["IsAuthorized"], action.Calls);
    }

    [Fact]
    public async Task Execute_WhenCannotExecute_StopsBeforeInternalExecution()
    {
        var action = new TestAction(isAuthorized: true, canExecute: false, "not returned");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            action.Execute
        );

        Assert.Contains(nameof(TestAction), exception.Message);
        Assert.Equal(["IsAuthorized", "CanExecute"], action.Calls);
    }

    private sealed class TestAction(
        bool isAuthorized,
        bool canExecute,
        string result
    ) : Action<string>
    {
        public List<string> Calls { get; } = [];

        public override Task<bool> IsAuthorized()
        {
            Calls.Add(nameof(IsAuthorized));
            return Task.FromResult(isAuthorized);
        }

        public override Task<bool> CanExecute()
        {
            Calls.Add(nameof(CanExecute));
            return Task.FromResult(canExecute);
        }

        protected override Task<string> ExecuteInternal()
        {
            Calls.Add(nameof(ExecuteInternal));
            return Task.FromResult(result);
        }
    }
}
