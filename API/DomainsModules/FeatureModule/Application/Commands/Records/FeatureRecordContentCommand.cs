using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Shared content of record add and update. Type is free text, such as
/// "conversation" or "research", and is stored in lower case.
/// </summary>
public abstract class FeatureRecordContentCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Type { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeatureRecordSourceType? SourceType { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    protected string NormalizedType => FeatureRecordTypes.Normalize(Type);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Type)
            && NormalizedType.Length <= FeatureRecordTypes.MaximumLength
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && (SourceType is null || Enum.IsDefined(SourceType.Value))
        );
}
