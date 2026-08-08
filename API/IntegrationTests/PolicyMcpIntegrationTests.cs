using Xunit;

namespace McpSkillSystem.IntegrationTests;

public sealed class PolicyMcpIntegrationTests : McpIntegrationTest
{
    [Fact]
    public async Task Repository_context_combines_general_project_and_topic_policies()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var topicName = $"integration-topic-{suffix}";
        var repositoryPath = $"/workspace/integration-project-{suffix}";
        var generalTitle = $"General integration policy {suffix}";
        var projectTitle = $"Project integration policy {suffix}";
        var topicTitle = $"Topic integration policy {suffix}";

        var generalPolicy = await CallTool<PolicyCreatedResult>(
            "policy_general_add",
            ("title", generalTitle),
            ("description", "Applies globally during the integration test.")
        );
        Assert.Equal("OK", generalPolicy.Status);

        var topicCreated = await CallTool<CommandResult>(
            "policy_topic_create",
            ("topicName", topicName),
            ("description", "Integration test topic")
        );
        Assert.Equal("OK", topicCreated.Status);

        var topicPolicy = await CallTool<PolicyCreatedResult>(
            "policy_topic_policy_add",
            ("topicName", topicName),
            ("title", topicTitle),
            ("description", "Applies through the related topic.")
        );
        Assert.Equal("OK", topicPolicy.Status);

        var project = await CallTool<ProjectCreatedResult>(
            "policy_project_create",
            ("projectName", $"Integration project {suffix}"),
            ("projectDescription", "Project used by MCP integration tests"),
            ("repositoryPaths", new List<string> { repositoryPath })
        );
        Assert.Equal("OK", project.Status);

        var projectPolicy = await CallTool<PolicyCreatedResult>(
            "policy_project_policy_add",
            ("projectId", project.ProjectId),
            ("title", projectTitle),
            ("description", "Applies directly to the project.")
        );
        Assert.Equal("OK", projectPolicy.Status);

        var topicRelated = await CallTool<CommandResult>(
            "policy_project_topic_add",
            ("projectId", project.ProjectId),
            ("topicName", topicName)
        );
        Assert.Equal("OK", topicRelated.Status);

        var policyContext = await CallTool<string>(
            "policy_get_by_repository",
            ("repositoryPath", repositoryPath)
        );

        Assert.Contains(generalTitle, policyContext);
        Assert.Contains(projectTitle, policyContext);
        Assert.Contains(topicTitle, policyContext);
    }

    private sealed record CommandResult(string Status);

    private sealed record PolicyCreatedResult(
        string Status,
        Guid PolicyId
    );

    private sealed record ProjectCreatedResult(
        string Status,
        Guid ProjectId
    );
}
