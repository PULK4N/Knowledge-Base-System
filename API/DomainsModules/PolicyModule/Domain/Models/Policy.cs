namespace PolicyModule.Domain.Models;

public class Policy
{
    public PolicyId PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
}
