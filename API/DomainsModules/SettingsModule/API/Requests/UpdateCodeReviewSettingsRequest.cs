using System.ComponentModel.DataAnnotations;

namespace SettingsModule.API.Requests;

public sealed record UpdateCodeReviewSettingsRequest
{
    [Range(1, int.MaxValue)]
    public int BatchSize { get; init; }

    [Required]
    public string TaskMessage { get; init; } = string.Empty;
}
