namespace FeatureModule.Persistence.Models;

public sealed class FeatureSearchEntry
{
    public int Id { get; set; }
    public required Guid FeatureAggregateId { get; set; }
    public required Guid ProjectId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Summary { get; set; }
    public required string SearchText { get; set; }
    public required string Status { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? CurrentPlanId { get; set; }
    public int PlanCount { get; set; }
    public int RecordCount { get; set; }
    public long ProjectedOrderNumber { get; set; }
}
