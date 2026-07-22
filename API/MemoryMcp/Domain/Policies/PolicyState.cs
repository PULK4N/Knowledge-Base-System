using EventSourcing.Shared.Models;

namespace MemoryMcp.Domain.Policies;

public sealed class PolicyState : ISharedStateData
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string Scope { get; set; } = "global";
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
    public List<string> Tags { get; set; } = [ ];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public uint Version { get; set; }
}
