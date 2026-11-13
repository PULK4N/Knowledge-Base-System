namespace FeatureModule.Domain.Validators;

/// <summary>Shared checks of the specification validators.</summary>
internal static class SpecificationEventScope
{
    internal const string MissingPlan = "The feature plan does not exist.";

    /// <summary>Null when every ID is distinct and, per mustExist, present or absent; otherwise the error.</summary>
    internal static string? CheckIds<TId>(
        IReadOnlyCollection<TId> ids,
        IEnumerable<TId> existingIds,
        bool mustExist,
        string itemName
    )
    {
        if (ids.Count == 0)
            return $"At least one {itemName} is required.";
        if (ids.Distinct().Count() != ids.Count)
            return $"The same {itemName} is listed more than once.";
        var existing = existingIds.ToHashSet();
        if (mustExist && ids.Any(id => !existing.Contains(id)))
            return $"A {itemName} does not exist.";
        if (!mustExist && ids.Any(existing.Contains))
            return $"A {itemName} already exists.";
        return null;
    }

    /// <summary>Null when every linked ID is distinct per item and exists.</summary>
    internal static string? CheckLinks<TId>(
        IEnumerable<IReadOnlyCollection<TId>> links,
        IEnumerable<TId> existingIds,
        string linkedItemName
    )
    {
        var existing = existingIds.ToHashSet();
        foreach (var itemLinks in links)
        {
            if (itemLinks.Distinct().Count() != itemLinks.Count)
                return $"The same {linkedItemName} is linked more than once.";
            if (itemLinks.Any(id => !existing.Contains(id)))
                return $"A linked {linkedItemName} does not exist in the same specification.";
        }
        return null;
    }
}
