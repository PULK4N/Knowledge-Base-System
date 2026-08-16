using Xunit;

namespace McpSkillSystem.IntegrationTests;

public sealed class SkillMcpIntegrationTests : McpIntegrationTest
{
    [Fact]
    public async Task Skill_reference_can_be_read_by_id_and_path()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        const string referencePath = "references/manual.md";
        var created = await CallTool<SkillCreatedResult>(
            "skill_add",
            ("name", $"reference-read-{suffix}"),
            ("description", "Reference read integration test"),
            ("content", "# Usage\nLoad references selectively."),
            ("tags", new List<string>()),
            (
                "references",
                new Dictionary<string, SkillReference>
                {
                    [referencePath] = new(
                        "# Manual reference\nRead this by path.",
                        false
                    )
                }
            )
        );

        var skill = await CallTool<SkillDetails>(
            "skill_get",
            ("skillId", created.SkillId),
            ("orderNumber", 0u)
        );
        Assert.Empty(skill.References);
        Assert.Contains(referencePath, skill.OtherReferences);

        var reference = await CallTool<SkillReference>(
            "skill_reference_get",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("orderNumber", 0u)
        );
        Assert.Equal(
            "# Manual reference\nRead this by path.",
            reference.Content
        );
        Assert.False(reference.LoadAutomatically);

        var deleted = await CallTool<CommandResult>(
            "skill_delete",
            ("skillId", created.SkillId)
        );
        Assert.Equal("OK", deleted.Status);
    }

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
        var loadedReference = await CallTool<SkillReference>(
            "skill_reference_get",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("orderNumber", 0u)
        );
        Assert.Contains("Initial reference.", loadedReference.Content);
        Assert.True(loadedReference.LoadAutomatically);

        var updated = await CallTool<CommandResult>(
            "skill_reference_update",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("content", "# Architecture\nUpdated reference."),
            ("loadAutomatically", true)
        );
        Assert.Equal("OK", updated.Status);

        var autoLoadUpdated = await CallTool<CommandResult>(
            "skill_reference_auto_load_update",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("loadAutomatically", false)
        );
        Assert.Equal("OK", autoLoadUpdated.Status);

        loaded = await CallTool<SkillDetails>(
            "skill_get",
            ("skillId", created.SkillId),
            ("orderNumber", 0u)
        );
        Assert.False(loaded.References.ContainsKey(referencePath));
        Assert.Contains(referencePath, loaded.OtherReferences);

        loadedReference = await CallTool<SkillReference>(
            "skill_reference_get",
            ("skillId", created.SkillId),
            ("relativePath", referencePath),
            ("orderNumber", 0u)
        );
        Assert.Contains("Updated reference.", loadedReference.Content);
        Assert.False(loadedReference.LoadAutomatically);

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
        public List<string> OtherReferences { get; init; } = [];
    }
}
