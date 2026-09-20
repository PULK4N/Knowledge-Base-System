using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.Commands;

public sealed class ClearEmbeddingCacheCommand(
    IEmbeddingCacheAdministrationRepository repository
) : Command<int>
{
    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

    protected override Task<int> ExecuteInternal(Executor executor) =>
        repository.Clear();
}
