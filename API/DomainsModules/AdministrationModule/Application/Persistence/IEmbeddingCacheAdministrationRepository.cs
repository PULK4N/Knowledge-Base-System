namespace AdministrationModule.Application.Persistence;

public interface IEmbeddingCacheAdministrationRepository
{
    Task<int> Clear(CancellationToken cancellationToken = default);
}
