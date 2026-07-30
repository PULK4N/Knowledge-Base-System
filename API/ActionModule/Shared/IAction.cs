using ActionModule.Shared.Models;

namespace ActionModule.Shared;

public interface IAction
{
    Task<bool> IsAuthorized(Executor executor);
    Task<bool> CanExecute(Executor executor);
}

public interface IAction<TResult> : IAction
{
    Task<TResult> Execute(Executor executor);
}
