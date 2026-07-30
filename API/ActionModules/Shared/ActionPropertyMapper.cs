using System.Collections.Concurrent;
using System.Reflection;

namespace ActionModule;

public static class ActionPropertyMapper
{
    private static readonly ConcurrentDictionary<
        Type,
        List<PropertyInfo>
    > Mappings = new();

    public static void Map(object body, object action)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(action);

        if (body.GetType() != action.GetType())
        {
            throw new InvalidOperationException(
                $"Body type '{body.GetType().Name}' must match "
                + $"action type '{action.GetType().Name}'."
            );
        }

        var mappings = Mappings.GetOrAdd(
            action.GetType(),
            static actionType => CreateMappings(actionType)
        );

        foreach (var property in mappings)
        {
            property.SetValue(
                action,
                property.GetValue(body)
            );
        }
    }

    private static List<PropertyInfo> CreateMappings(
        Type actionType
    )
    {
        return actionType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                property =>
                    property.GetMethod is { IsPublic: true }
                    && property.SetMethod is { IsPublic: true }
                    && property.GetIndexParameters().Length == 0
            )
            .ToList();
    }
}
