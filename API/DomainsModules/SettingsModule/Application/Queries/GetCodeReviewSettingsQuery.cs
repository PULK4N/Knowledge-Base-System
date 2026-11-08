using ActionModule.Shared;
using ActionModule.Shared.Models;
using SettingsModule.Application.DTOs;
using SettingsModule.Application.Persistence;

namespace SettingsModule.Application.Queries;

public sealed class GetCodeReviewSettingsQuery(ICodeReviewSettingsReader reader)
    : Query<CodeReviewSettingsDto?>
{
    protected override async Task<CodeReviewSettingsDto?> ExecuteInternal(Executor executor)
    {
        var value = await reader.Get();
        return value is null ? null : new(value.BatchSize, value.TaskMessage);
    }
}
