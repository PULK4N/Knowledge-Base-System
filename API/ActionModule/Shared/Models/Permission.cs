namespace ActionModule.Shared.Models;

/// <summary>
/// A named permission such as <c>skills.read</c>. Events and requests store only the name, so a new
/// permission or extra data on this type never changes their shape.
/// </summary>
public readonly record struct Permission(string Name)
{
    public override string ToString() => Name;
}
