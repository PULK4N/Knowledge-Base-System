using SettingsModule.Application.Models;

namespace SettingsModule.Application.Persistence;

public interface ICodeReviewSettingsReader
{
    Task<CodeReviewSettingsSnapshot?> Get(CancellationToken cancellationToken = default);
}
