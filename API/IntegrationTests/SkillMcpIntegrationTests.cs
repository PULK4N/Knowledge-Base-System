using Xunit;

namespace McpSkillSystem.IntegrationTests;

public sealed class SkillMcpIntegrationTests : McpIntegrationTest
{
    [Fact]
    public async Task Skill_can_be_created_read_updated_and_deleted()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var skillName = $"integration-skill-{suffix}";
        const string referencePath = "references/architecture.md";

        var created = await CallTool<SkillCreatedResult>(
            "skill_add",
            ("name", skillName),
            ("description", "Integration test skill"),
            ("content", "# Usage\nUse the real MCP and PostgreSQL stack."),
            ("tags", new List<string> { "integration", "mcp" }),
            (
                "references",
                new Dictionary<string, SkillReference>
                {
                    [referencePath] = new(
                        "# Architecture\nInitial reference.",
                        true
                    )
                }
            )
        );

        Assert.Equal("OK", created.Status);
        Assert.NotEqual(Guid.Empty, created.SkillId);

        var listed = await CallTool<List<SkillSummary>>("skill_list");
        Assert.Contains(
            listed,
            skill =>
                skill.SkillId == created.SkillId
                && skill.Name == skillName
        );

        var loaded = await CallTool<SkillDetails>(
            "skill_get",
            ("skillId", created.SkillId),
            ("orderNumber", 0u)
        );
        Assert.Equal(skillName, loaded.Name);
        Assert.Equal(
            "Initial reference.",
            loaded.References[referencePath].Content.Split('\n').Last()
        );
        Assert.True(
            loaded.References[referencePath].LoadAutomatically
        );

        var updated = await CallTool<CommandResult>(
            "skill_reference_update",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("content", "# Architecture\nUpdated reference."),
            ("loadAutomatically", false)
        );
        Assert.Equal("OK", updated.Status);

        loaded = await CallTool<SkillDetails>(
            "skill_get",
            ("skillId", created.SkillId),
            ("orderNumber", 0u)
        );
        Assert.Contains(
            "Updated reference.",
            loaded.References[referencePath].Content
        );
        Assert.False(
            loaded.References[referencePath].LoadAutomatically
        );

        var deleted = await CallTool<CommandResult>(
            "skill_delete",
            ("skillId", created.SkillId)
        );
        Assert.Equal("OK", deleted.Status);

        listed = await CallTool<List<SkillSummary>>("skill_list");
        Assert.DoesNotContain(
            listed,
            skill => skill.SkillId == created.SkillId
        );
    }

    private sealed record SkillCreatedResult(
        string Status,
        Guid SkillId
    );

    private sealed record CommandResult(string Status);

    private sealed record SkillSummary(Guid SkillId, string Name);

    private sealed record SkillReference(
        string Content,
        bool LoadAutomatically
    );

    private sealed record SkillDetails
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public Dictionary<string, SkillReference> References { get; init; } = [];
    }
}
