using System.Text.Json.Serialization;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.DTOs;

public sealed record MemoryRelatedEntityDto(
    [property: JsonRequired] MemoryEntityType Type,
    [property: JsonRequired] Guid Id
)
{
    public MemoryRelatedEntity ToDomain() => new(Type, new MemoryEntityId(Id));
}
