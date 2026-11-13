namespace FeatureModule.Application.Models;

public sealed record MovedSpecificationItem(Guid Id, string PreviousLabel, string Label);

public sealed record FeatureSpecificationItemsMovedCommandResult(
    string Status,
    List<MovedSpecificationItem> Items
)
{
    public static FeatureSpecificationItemsMovedCommandResult Ok(List<MovedSpecificationItem> items) =>
        new("OK", items);
}
