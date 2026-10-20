using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP.Tests;

public sealed class FeaturePlanBranchTreeFormatterTests
{
    [Fact]
    public void Format_RendersNestedBranchesLikeTree()
    {
        var branch = new FeaturePlanBranchDto(
            "feature/plans",
            "Plan storage rework",
            [
                new("feature/plans-api", "Events\nand MCP", [new("feature/plans-api-tests", "", [], [])], []),
                new("feature/plans-ui", "Angular pages", [], [])
            ],
            []
        );

        Assert.Equal(
            """
            feature/plans - Plan storage rework
            ├── feature/plans-api - Events and MCP
            │   └── feature/plans-api-tests
            └── feature/plans-ui - Angular pages
            """,
            FeaturePlanBranchTreeFormatter.Format(branch)
        );
    }

    [Fact]
    public void Format_ReturnsNullWithoutBranch() =>
        Assert.Null(FeaturePlanBranchTreeFormatter.Format(null));
}
