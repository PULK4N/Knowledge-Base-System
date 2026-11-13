namespace FeatureModule.Application.Models;

public sealed record CreatedSpecificationItem(Guid Id, string Label);

public sealed record FeatureSpecificationItemsCreatedCommandResult(
    string Status,
    List<CreatedSpecificationItem> Items
)
{
    public static FeatureSpecificationItemsCreatedCommandResult Ok(List<CreatedSpecificationItem> items) =>
        new("OK", items);
}
