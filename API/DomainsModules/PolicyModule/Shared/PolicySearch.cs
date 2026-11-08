using ActionModule.Shared.Models;

namespace PolicyModule.Shared;

public static class PolicySearch
{
    public static PagedResult<PolicyDto> Create(
        IEnumerable<Policy> policies,
        int page,
        int pageSize,
        string? search
    )
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();
        if (normalizedSearch is not null)
        {
            policies = policies.Where(
                policy =>
                    policy.Title.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || policy.Description.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        var ordered = policies
            .OrderByDescending(
                policy =>
                    normalizedSearch is not null
                    && policy.Title.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase
                    )
            )
            .ThenByDescending(
                policy => policy.UpdatedAt
            )
            .ThenBy(
                policy => policy.Title,
                StringComparer.OrdinalIgnoreCase
            )
            .ThenBy(policy => policy.Title, StringComparer.Ordinal)
            .ThenBy(policy => policy.PolicyId.Value)
            .ToList();

        return new PagedResult<PolicyDto>(
            ordered
                .Skip(Pagination.Offset(page, pageSize))
                .Take(pageSize)
                .Select(PolicyDto.FromModel)
                .ToList(),
            page,
            pageSize,
            ordered.Count
        );
    }
}
