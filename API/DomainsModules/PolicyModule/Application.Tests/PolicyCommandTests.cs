using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using PolicyModule.Persistence.Interfaces;
using Shared.Interfaces;
using SharedModule.Constants;
using UUIDNext;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
{
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    public PolicyCommandTests()
    {
        DatabaseFriendlyGuidGenerator
            .SetDefaultGuidGenerationDatabase(Database.SqlServer);
        RegisterPolicyTypesOnce();
    }

    [Fact]
    public void ProjectPolicyEvents_UseSharedProjections()
    {
        var definition = CreateDefinitionProvider().Get(
            PolicyModule.Application.Constants.StateMachineIds.ProjectPolicies
        );

        Assert.Equal(
            [
                "ProjectPolicyTextProjector",
                "ProjectTopicProjector",
                "PolicyProjectSummaryProjector"
            ],
            definition.Projections
        );
        Assert.Equal(
            [nameof(ProjectCreatedV1), nameof(ProjectCreatedV2)],
            definition.InitializationEvents
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.Projections)
        );

        var repositoryMapDefinition = CreateDefinitionProvider().Get(
            PolicyModule.Application.Constants.StateMachineIds.RepositoryToProjectMap
        );
        Assert.Equal(
            [nameof(RepositoryToProjectMapAddedV1)],
            repositoryMapDefinition.InitializationEvents
        );
    }

    [Fact]
    public void GeneralPolicyEvents_UseSharedProjections()
    {
        var definition = CreateDefinitionProvider().Get(
            PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies
        );

        Assert.Equal(
            [
                "GeneralPolicyTextProjector",
                "TopicPolicyTextProjector",
                "AgentFamilyPolicyTextProjector"
            ],
            definition.Projections
        );
        Assert.Equal(
            [
                nameof(GeneralPolicyAddedV1), nameof(GeneralPolicyAddedV2),
                nameof(TopicCreatedV1), nameof(TopicCreatedV2),
                nameof(AgentFamilyCreatedV1), nameof(AgentFamilyCreatedV2)
            ],
            definition.InitializationEvents
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.Projections)
        );
    }

    [Fact]
    public async Task GeneralPolicyCommands_UseFixedGlobalStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var addResult = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddGeneralPolicyCommand(handler)
            {
                Title = "Always run focused tests",
                Description = "Verify only the affected behavior."
            })
        );

        await ExecuteAsUser(new UpdateGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId,
            Title = "Always run the smallest focused tests",
            Description = "Verify the affected behavior first."
        });
        await ExecuteAsUser(new RemoveGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload =>
            {
                var added = Assert.IsType<GeneralPolicyAddedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    addResult.PolicyId,
                    added.Policy.PolicyId.Value
                );
                Assert.Equal(
                    PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies,
                    payload.EventExecutionInfo.StateMachineId
                );
            },
            payload =>
            {
                var updated = Assert.IsType<GeneralPolicyUpdatedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    "Always run the smallest focused tests",
                    updated.Policy.Title
                );
            },
            payload =>
                Assert.IsType<GeneralPolicyRemovedV2>(
                    payload.EventData
                )
        );
        Assert.Empty(
            Assert.IsType<GeneralPoliciesStateData>(
                eventStore.LastWritten[globalAggregateId].StateData
            ).Policies
        );
    }

    [Fact]
    public async Task TopicCommands_CreateAddAndRemovePolicyOnGlobalStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string topicName = "cloud";

        await ExecuteAsUser(new CreateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Policies for cloud usage."
        });
        await ExecuteAsUser(new UpdateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Updated cloud policies."
        });
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddTopicPolicyCommand(handler)
            {
                TopicName = topicName,
                Title = "Use managed identities",
                Description = "Do not store service credentials."
            })
        );
        await ExecuteAsUser(new UpdateTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId,
            Title = "Use workload identities",
            Description = "Do not store service credentials."
        });
        await ExecuteAsUser(new RemoveTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId
        });
        await ExecuteAsUser(new RemoveTopicCommand(handler)
        {
            TopicName = topicName
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload => Assert.IsType<TopicCreatedV2>(payload.EventData),
            payload => Assert.IsType<TopicUpdatedV2>(payload.EventData),
            payload => Assert.IsType<TopicPolicyAddedV2>(payload.EventData),
            payload =>
                Assert.IsType<TopicPolicyUpdatedV2>(
                    payload.EventData
                ),
            payload =>
            {
                var removed = Assert.IsType<TopicPolicyRemovedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            },
            payload => Assert.IsType<TopicRemovedV2>(payload.EventData)
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.Topics);
    }

    [Theory]
    [InlineData("claude", "claude")]
    [InlineData("Codex", "codex")]
    public async Task AgentFamilyCommands_CreateAddAndRemovePolicyOnGlobalStream(
        string requestedName,
        string storedName
    )
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);

        await ExecuteAsUser(new CreateAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName,
            Description = "Policies for this agent family."
        });
        await ExecuteAsUser(new UpdateAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName,
            Description = "Updated agent family policies."
        });
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddAgentFamilyPolicyCommand(handler)
            {
                AgentFamilyName = requestedName,
                Title = "Prefer the dedicated file tools",
                Description = "Read and edit through the provided tools."
            })
        );
        await ExecuteAsUser(new UpdateAgentFamilyPolicyCommand(handler)
        {
            AgentFamilyName = requestedName,
            PolicyId = addedPolicy.PolicyId,
            Title = "Prefer the dedicated file tools",
            Description = "Read and edit through the dedicated tools."
        });
        await ExecuteAsUser(new RemoveAgentFamilyPolicyCommand(handler)
        {
            AgentFamilyName = requestedName,
            PolicyId = addedPolicy.PolicyId
        });
        await ExecuteAsUser(new RemoveAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload =>
            {
                var created = Assert.IsType<AgentFamilyCreatedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    storedName,
                    created.AgentFamilyName.Name
                );
            },
            payload =>
                Assert.IsType<AgentFamilyUpdatedV2>(payload.EventData),
            payload =>
                Assert.IsType<AgentFamilyPolicyAddedV2>(
                    payload.EventData
                ),
            payload =>
                Assert.IsType<AgentFamilyPolicyUpdatedV2>(
                    payload.EventData
                ),
            payload =>
            {
                var removed = Assert.IsType<AgentFamilyPolicyRemovedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            },
            payload =>
                Assert.IsType<AgentFamilyRemovedV2>(payload.EventData)
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.AgentFamilies);
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    [InlineData("in-house-agent")]
    public async Task GetPoliciesByRepository_ForwardsTheRequestedAgentFamily(
        string agentFamily
    )
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/agent-family-project";
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Agent family project",
                ProjectDescription = "Agent family query test project.",
                RepositoryPaths = [repositoryPath]
            })
        );
        var projectAggregateId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );
        var policyTextRepository = new StubPolicyTextRepository();
        policyTextRepository.AgentFamilies.Add(agentFamily);
        policyTextRepository.PolicyTexts[projectAggregateId] =
            "# General policies\n\n## General policy\nApplies everywhere.";

        var result = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository,
            new StubPolicyProjectSummaryRepository()
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = agentFamily
        }.Execute(Executor);

        Assert.Equal(GetPoliciesByRepositoryResult.OkStatus, result.Status);
        Assert.Equal(
            projectAggregateId,
            policyTextRepository.LastProjectId
        );
        Assert.Equal(agentFamily, policyTextRepository.LastAgentFamily);
    }

    [Fact]
    public async Task GetPoliciesByRepository_ReportsAnAgentFamilyThatDoesNotExist()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/unknown-family-project";
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Unknown family project",
                ProjectDescription = "Agent family is not created yet.",
                RepositoryPaths = [repositoryPath]
            })
        );
        var policyTextRepository = new StubPolicyTextRepository();
        policyTextRepository.PolicyTexts[
            AggregateId.FromDatabaseGuid(project.ProjectId)
        ] = "# General policies";

        var result = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository,
            new StubPolicyProjectSummaryRepository()
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = "not-created-yet"
        }.Execute(Executor);

        Assert.Equal(
            GetPoliciesByRepositoryResult.AgentFamilyNotFoundStatus,
            result.Status
        );
        Assert.Null(result.Policies);
        Assert.False(result.RequiresUserInput);
        Assert.Contains("not-created-yet", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPoliciesByRepository_RefusesToRunWithoutAnAgentFamily(
        string agentFamily
    )
    {
        var query = new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            new CapturingEventStoreWithOutbox(),
            new StubPolicyTextRepository(),
            new StubPolicyProjectSummaryRepository()
        )
        {
            RepositoryPath = "/workspace/agent-family-project",
            AgentFamily = agentFamily
        };

        Assert.False(await query.CanExecute(Executor));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => query.Execute(Executor)
        );
    }

    [Fact]
    public async Task ProjectCommands_CreateMappingsAndReuseGeneratedProjectStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var repositoryPaths = new List<string>
        {
            "/workspace/main",
            "/workspace/secondary-checkout"
        };
        var created = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "MCP Knowledge Base",
                ProjectDescription = "Event-sourced Codex tooling.",
                RepositoryPaths = repositoryPaths
            })
        );
        var projectId = AggregateId.FromDatabaseGuid(created.ProjectId);

        await ExecuteAsUser(new UpdateProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            ProjectName = "MCP Skill and Policy System",
            ProjectDescription = "Updated event-sourced tooling."
        });

        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddProjectPolicyCommand(handler)
            {
                ProjectId = created.ProjectId,
                Title = "Keep policies separate from skills",
                Description = "Policies are injected into every chat."
            })
        );
        await ExecuteAsUser(new UpdateProjectPolicyCommand(handler)
        {
            ProjectId = created.ProjectId,
            PolicyId = addedPolicy.PolicyId,
            Title = "Keep policies distinct from skills",
            Description = "Policies are injected into every chat."
        });
        await ExecuteAsUser(new RemoveProjectPolicyCommand(handler)
        {
            ProjectId = created.ProjectId,
            PolicyId = addedPolicy.PolicyId
        });
        await ExecuteAsUser(new AddTopicRelationToProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            TopicName = "cloud"
        });
        await ExecuteAsUser(new RemoveTopicRelationFromProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            TopicName = "cloud"
        });
        await ExecuteAsUser(new DeleteProjectCommand(handler)
        {
            ProjectId = created.ProjectId
        });

        Assert.NotEqual(Guid.Empty, created.ProjectId);
        var projectEvents = eventStore.GetStoredEvents(projectId);
        Assert.Collection(
            projectEvents,
            payload =>
            {
                Assert.IsType<ProjectCreatedV2>(payload.EventData);
                Assert.Equal(
                    "MCP KNOWLEDGE BASE",
                    Assert.Single(
                        payload.UniqueEventConstraintsToAdd
                    ).ValueToHash
                );
            },
            payload => Assert.IsType<ProjectUpdatedV2>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyAddedV2>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyUpdatedV2>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyRemovedV2>(payload.EventData),
            payload => Assert.IsType<TopicRelationAddedToProjectV2>(payload.EventData),
            payload => Assert.IsType<TopicRelationRemovedFromProjectV2>(payload.EventData),
            payload => Assert.IsType<ProjectDeletedV2>(payload.EventData)
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        var mapEvents = eventStore.GetStoredEvents(mapAggregateId);
        Assert.Collection(
            mapEvents,
            payload => AssertMappingAdded(payload, projectId),
            payload => AssertMappingAdded(payload, projectId),
            payload => AssertMappingRemoved(payload, projectId),
            payload => AssertMappingRemoved(payload, projectId)
        );
        var projectState = Assert.IsType<ProjectPoliciesStateData>(
            eventStore.LastWritten[projectId].StateData
        );
        Assert.Equal(
            "MCP Skill and Policy System",
            projectState.ProjectName
        );
        Assert.Equal(repositoryPaths, projectState.RepositoryPaths);
        Assert.Empty(projectState.Policies);
        Assert.Empty(projectState.RelatedTopics);
        Assert.True(projectState.IsDeleted);
        Assert.Empty(
            Assert.IsType<RepositoryToProjectMapStateData>(
                eventStore.LastWritten[mapAggregateId].StateData
            ).RepositoryToProjectMap
        );
    }

    [Fact]
    public async Task ProjectUpdate_AfterDeletion_IsRejectedWithoutWrite()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Deleted project",
                ProjectDescription = "Validation test.",
                RepositoryPaths = []
            })
        );
        var projectId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );

        await ExecuteAsUser(new DeleteProjectCommand(handler)
        {
            ProjectId = project.ProjectId
        });
        var eventCount = eventStore.GetStoredEvents(projectId).Count;

        var exception = await Assert.ThrowsAsync<EventValidationException>(
            () =>
                ExecuteAsUser(new UpdateProjectCommand(handler)
                {
                    ProjectId = project.ProjectId,
                    ProjectName = "Invalid update",
                    ProjectDescription = "Must not be persisted."
                })
        );

        Assert.Contains("project is deleted", exception.Message);
        Assert.Equal(
            eventCount,
            eventStore.GetStoredEvents(projectId).Count
        );
    }

    [Fact]
    public async Task RepositoryPath_CanBeReusedAfterProjectDeletion()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/reusable";
        var firstProject = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "First project",
                ProjectDescription = "Will be deleted.",
                RepositoryPaths = [repositoryPath]
            })
        );

        await ExecuteAsUser(new DeleteProjectCommand(handler)
        {
            ProjectId = firstProject.ProjectId
        });

        var secondProject = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Second project",
                ProjectDescription = "Reuses the released path.",
                RepositoryPaths = [repositoryPath]
            })
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        var mapState = Assert.IsType<RepositoryToProjectMapStateData>(
            eventStore.LastWritten[mapAggregateId].StateData
        );

        Assert.Equal(
            AggregateId.FromDatabaseGuid(secondProject.ProjectId),
            mapState.RepositoryToProjectMap[repositoryPath]
        );
        Assert.Collection(
            eventStore.GetStoredEvents(mapAggregateId),
            payload => Assert.IsType<RepositoryToProjectMapAddedV1>(
                payload.EventData
            ),
            payload => Assert.IsType<RepositoryToProjectMapRemovedV1>(
                payload.EventData
            ),
            payload => Assert.IsType<RepositoryToProjectMapAddedV1>(
                payload.EventData
            )
        );
    }

    [Fact]
    public async Task AddRepositoryToProject_UpdatesProjectAndGlobalMapTogether()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/new-repository";
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Repository target",
                ProjectDescription = "Starts without a repository.",
                RepositoryPaths = []
            })
        );
        var projectAggregateId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );

        var result = await ExecuteAsUser(new AddRepositoryToProjectCommand(handler)
        {
            ProjectId = project.ProjectId,
            RepositoryPath = repositoryPath
        });

        Assert.Same(PolicyCommandResult.Ok, result);
        Assert.Collection(
            eventStore.GetStoredEvents(projectAggregateId),
            payload => Assert.IsType<ProjectCreatedV2>(payload.EventData),
            payload => Assert.IsType<RepositoryAddedToProjectV2>(
                payload.EventData
            )
        );
        Assert.Equal(
            [repositoryPath],
            Assert.IsType<ProjectPoliciesStateData>(
                eventStore.LastWritten[projectAggregateId].StateData
            ).RepositoryPaths
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        Assert.Equal(
            projectAggregateId,
            Assert.IsType<RepositoryToProjectMapStateData>(
                eventStore.LastWritten[mapAggregateId].StateData
            ).RepositoryToProjectMap[repositoryPath]
        );
    }

    [Fact]
    public async Task GetPoliciesByRepository_ReturnsGeneralProjectAndTopicPolicies()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/policy-project";

        await ExecuteAsUser(new AddGeneralPolicyCommand(handler)
        {
            Title = "General policy",
            Description = "Applies to every project."
        });
        await ExecuteAsUser(new CreateTopicCommand(handler)
        {
            TopicName = "cloud",
            Description = "Cloud policies."
        });
        await ExecuteAsUser(new CreateTopicCommand(handler)
        {
            TopicName = "dotnet",
            Description = ".NET development policies."
        });
        await ExecuteAsUser(new AddTopicPolicyCommand(handler)
        {
            TopicName = "cloud",
            Title = "Cloud policy",
            Description = "Applies to cloud projects."
        });
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = "Policy project",
                ProjectDescription = "Query test project.",
                RepositoryPaths = [repositoryPath]
            })
        );
        await ExecuteAsUser(new AddProjectPolicyCommand(handler)
        {
            ProjectId = project.ProjectId,
            Title = "Project policy",
            Description = "Applies only to this project."
        });
        await ExecuteAsUser(new AddTopicRelationToProjectCommand(handler)
        {
            ProjectId = project.ProjectId,
            TopicName = "cloud"
        });
        var policyTextRepository = new StubPolicyTextRepository();
        var projectAggregateId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );
        policyTextRepository.PolicyTexts[projectAggregateId] =
            "# General policies\n\n"
            + "## General policy\nApplies to every project.\n\n"
            + "# Project \"Policy project\" policies\n\n"
            + "## Project policy\nApplies only to this project.\n\n"
            + "# Topic \"cloud\" policies\n\n"
            + "## Cloud policy\nApplies to cloud projects.";

        var result = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository,
            new StubPolicyProjectSummaryRepository()
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = "claude"
        }.Execute(Executor);

        Assert.Equal(
            "# General policies\n\n"
                + "## General policy\nApplies to every project.\n\n"
                + "# Project \"Policy project\" policies\n\n"
                + "## Project policy\nApplies only to this project.\n\n"
                + "# Topic \"cloud\" policies\n\n"
                + "## Cloud policy\nApplies to cloud projects.",
            result.Policies
        );
        Assert.Equal(GetPoliciesByRepositoryResult.OkStatus, result.Status);
        Assert.False(result.RequiresUserInput);
        Assert.Equal(
            projectAggregateId,
            policyTextRepository.LastProjectId
        );
        Assert.Equal(
            "General policy",
            Assert.Single(
                await new ListGeneralPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                ).Execute(Executor)
            ).Title
        );
        Assert.Equal(
            "Cloud policy",
            Assert.Single(
                await new ListTopicPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                )
                {
                    TopicName = "cloud"
                }.Execute(Executor)
            )?.Title
        );
        var topics =
            await new ListPolicyTopicsQuery(
                CreateCalculator(),
                eventStore
            ).Execute(Executor);
        Assert.Collection(
            topics,
            topic =>
            {
                Assert.Equal("cloud", topic.TopicName);
                Assert.Equal("Cloud policies.", topic.Description);
                Assert.Equal(1, topic.PolicyCount);
            },
            topic =>
            {
                Assert.Equal("dotnet", topic.TopicName);
                Assert.Equal(
                    ".NET development policies.",
                    topic.Description
                );
                Assert.Equal(0, topic.PolicyCount);
            }
        );
        Assert.Equal(
            "Project policy",
            Assert.Single(
                await new ListProjectPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                )
                {
                    ProjectId = project.ProjectId
                }.Execute(Executor)
            )?.Title
        );
        var projectDetails = Assert.IsType<PolicyProjectDetailsDto>(
            await new GetPolicyProjectQuery(
                CreateCalculator(),
                eventStore
            )
            {
                ProjectId = project.ProjectId
            }.Execute(Executor)
        );
        Assert.Equal("Policy project", projectDetails.ProjectName);
        Assert.Equal(
            "Query test project.",
            projectDetails.ProjectDescription
        );
        Assert.Equal([repositoryPath], projectDetails.RepositoryPaths);
        Assert.Equal(["cloud"], projectDetails.TopicNames);

        await ExecuteAsUser(new RemoveTopicCommand(handler)
        {
            TopicName = "cloud"
        });
        policyTextRepository.PolicyTexts[projectAggregateId] =
            "# Project \"Policy project\" policies\n\n"
            + "## Project policy\nApplies only to this project.\n\n"
            + "# General policies\n\n"
            + "## General policy\nApplies to every project.";

        var updatedResult = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository,
            new StubPolicyProjectSummaryRepository()
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = "claude"
        }.Execute(Executor);
        Assert.Equal(
            "# Project \"Policy project\" policies\n\n"
                + "## Project policy\nApplies only to this project.\n\n"
                + "# General policies\n\n"
                + "## General policy\nApplies to every project.",
            updatedResult.Policies
        );

        await ExecuteAsUser(new DeleteProjectCommand(handler)
        {
            ProjectId = project.ProjectId
        });
        Assert.Null(
            await new GetPolicyProjectQuery(
                CreateCalculator(),
                eventStore
            )
            {
                ProjectId = project.ProjectId
            }.Execute(Executor)
        );

        var projectSummaryRepository =
            new StubPolicyProjectSummaryRepository();
        projectSummaryRepository.Projects.Add(
            new PolicyProjectSummary(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                "Available project",
                ["/workspace/available"]
            )
        );
        var missing = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository,
            projectSummaryRepository
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = "claude"
        }.Execute(Executor);

        Assert.Equal(
            GetPoliciesByRepositoryResult.RepositoryMappingRequiredStatus,
            missing.Status
        );
        Assert.True(missing.RequiresUserInput);
        Assert.Null(missing.Policies);
        Assert.Contains("Stop and ask the user", missing.Message);
        var option = Assert.Single(missing.Projects);
        Assert.Equal("Available project", option.ProjectName);
        Assert.Equal(["/workspace/available"], option.RepositoryPaths);
    }

    private static Task<object> ExecuteAsUser(PolicyCommand command)
    {
        command.UseUserOrigin();
        return command.Execute(Executor);
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(CreateCalculator(), eventStore);

    private static void AssertMappingAdded(
        EventPayload payload,
        AggregateId projectId
    )
    {
        var mapping = Assert.IsType<RepositoryToProjectMapAddedV1>(
            payload.EventData
        );
        Assert.Equal(projectId, mapping.ProjectAggregateId);
    }

    private static void AssertMappingRemoved(
        EventPayload payload,
        AggregateId projectId
    )
    {
        var mapping = Assert.IsType<RepositoryToProjectMapRemovedV1>(
            payload.EventData
        );
        Assert.Equal(projectId, mapping.ProjectAggregateId);
    }

    private static StateCalculator CreateCalculator() =>
        CreateCalculator(CreateDefinitionProvider());

    private static StateCalculator CreateCalculator(
        YamlStateMachineDefinitionProvider definitionProvider
    ) =>
        new(
            new OrderNumberHelper(),
            new PolicyStateDataProvider(),
            new EventValidatorProvider(definitionProvider),
            new StateMachineUniqueEventConstraintProvider(
                definitionProvider
            ),
            definitionProvider
        );

    private static YamlStateMachineDefinitionProvider
        CreateDefinitionProvider() =>
            new(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "StateMachines"
                )
            );

    private static void RegisterPolicyTypesOnce()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            new ServiceCollection().RegisterEventSourcingCore(
                typeof(GeneralPoliciesStateData).Assembly,
                typeof(MemoryModule.Domain.MemoryStateData).Assembly
            );
            _typesRegistered = true;
        }
    }

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox, IEventStore
    {
        private readonly Dictionary<AggregateId, List<EventPayload>> eventsByAggregate = [];

        public int WriteCount { get; private set; }

        public Dictionary<AggregateId, StateInfo> LastWritten { get; private set; } = [];

        public List<EventPayload> GetStoredEvents(
            AggregateId aggregateId
        ) =>
            eventsByAggregate.TryGetValue(
                aggregateId,
                out var events
            )
                ? events.ToList()
                : [];

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            WriteCount++;
            LastWritten = stateInfos;

            foreach (var (aggregateId, stateInfo) in stateInfos)
            {
                if (!eventsByAggregate.TryGetValue(
                        aggregateId,
                        out var events
                    ))
                {
                    events = [];
                    eventsByAggregate.Add(aggregateId, events);
                }

                events.AddRange(stateInfo.LastExecutedPayloads);
            }

            return Task.CompletedTask;
        }

        public Task Write(List<EventPayload> payloads)
        {
            foreach (
                var payloadGroup in payloads.GroupBy(
                    payload =>
                        payload.EventExecutionInfo.AggregateId
                )
            )
            {
                if (!eventsByAggregate.TryGetValue(
                        payloadGroup.Key,
                        out var events
                    ))
                {
                    events = [];
                    eventsByAggregate.Add(
                        payloadGroup.Key,
                        events
                    );
                }

                events.AddRange(payloadGroup);
            }

            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    GetStoredEvents
                )
            );
    }

    private sealed class PolicyStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(
                stateMachineId switch
                {
                    PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies =>
                        new GeneralPoliciesStateData(AggregateId.New()),
                    PolicyModule.Application.Constants.StateMachineIds.ProjectPolicies =>
                        new ProjectPoliciesStateData(aggregateId),
                    PolicyModule.Application.Constants.StateMachineIds.RepositoryToProjectMap =>
                        new RepositoryToProjectMapStateData(aggregateId),
                    MemoryModule.Application.Constants.StateMachineIds.Memory =>
                        new MemoryModule.Domain.MemoryStateData(aggregateId),
                    MemoryModule.Application.Constants.StateMachineIds.SessionAggregateMap =>
                        new MemoryModule.Domain.SessionAggregateMapStateData(aggregateId),
                    _ => throw new InvalidOperationException(
                        $"Unknown state machine '{stateMachineId}'."
                    )
                }
            );
    }

    private sealed class StubPolicyTextRepository
        : IPolicyTextRepository
    {
        public Dictionary<AggregateId, string> PolicyTexts { get; } = [];
        public AggregateId? LastProjectId { get; private set; }

        public string? LastAgentFamily { get; private set; }

        public HashSet<string> AgentFamilies { get; } =
            new(StringComparer.OrdinalIgnoreCase) { "claude" };

        public Task<bool> AgentFamilyExists(string agentFamilyName) =>
            Task.FromResult(AgentFamilies.Contains(agentFamilyName));

        public Task<string?> Get(
            AggregateId projectAggregateId,
            string? agentFamilyName
        )
        {
            LastProjectId = projectAggregateId;
            LastAgentFamily = agentFamilyName;

            return Task.FromResult(
                PolicyTexts.TryGetValue(
                    projectAggregateId,
                    out var text
                )
                    ? text
                    : null
            );
        }

    }

    private sealed class StubPolicyProjectSummaryRepository
        : IPolicyProjectSummaryRepository
    {
        public List<PolicyProjectSummary> Projects { get; } = [];

        public Task<List<PolicyProjectSummary>> List() =>
            Task.FromResult(Projects);

        public Task<PolicyProjectSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                Projects.SingleOrDefault(
                    project => string.Equals(
                        project.ProjectName,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

        public Task<PolicyProjectSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new PolicyProjectSummarySearchResult(
                    Projects,
                    Projects.Count
                )
            );
    }

}
