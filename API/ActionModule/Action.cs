namespace ActionModule;

public abstract class Action<TResult> : IAction<TResult>
{
    public abstract Task<bool> IsAuthorized();

    public abstract Task<bool> CanExecute();

    public async Task<TResult> Execute()
    {
        if (!await IsAuthorized())
            throw new UnauthorizedAccessException(
                $"Action '{GetType().Name}' is not authorized."
            );

        if (!await CanExecute())
            throw new InvalidOperationException(
                $"Action '{GetType().Name}' cannot be executed."
            );

        return await ExecuteInternal();
    }

    protected abstract Task<TResult> ExecuteInternal();
}

public abstract class Command<TResult> : Action<TResult> { }

public abstract class Query<TResult> : Action<TResult> { }
