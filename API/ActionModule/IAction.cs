namespace ActionModule;

public interface IAction
{
    Task<bool> IsAuthorized();
    Task<bool> CanExecute();
}

public interface IAction<TResult> : IAction
{
    Task<TResult> Execute();
}
