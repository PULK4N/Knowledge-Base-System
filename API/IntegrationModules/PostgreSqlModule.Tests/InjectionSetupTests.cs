using System.Collections.Immutable;
using EmbeddingModule;
using ActionModule.Persistence;
using AdministrationModule.Application.Persistence;
using AdministrationModule.Persistence;
using AttachmentsModule.Application;
using AttachmentsModule.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ActionModule.Shared.Models;
using FeatureModule.Contracts;
using FeatureModule.Persistence.Models;
using FeatureModule.Persistence;
using FeatureModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemoryModule.Persistence;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;
using SkillsModule.Contracts;
using SkillsModule.Persistence;
using SkillsModule.Persistence.Interfaces;
using SkillsModule.Persistence.Models;
using SharedModule.Persistence;
using Xunit;

namespace PostgreSqlModule.Tests;

public sealed class InjectionSetupTests
{
    [Fact]
    public void EmbeddingCacheMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddEmbeddingCache();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
    }

    [Fact]
    public void EntityRelationsMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddEntityRelations();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
    }

    [Fact]
    public void FeatureSearchMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddFeatureSearchProjection();

        Assert.Empty(
            migration.UpOperations.OfType<SqlOperation>()
        );
    }

    [Fact]
    public void FeatureRecordSearchMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddFeatureResearchSearchProjection();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
    }

    [Fact]
    public void KnowledgeSearchMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddKnowledgeSearchProjection();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
    }

    [Fact]
    public void SkillListMigration_is_schema_only()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddSkillListProjection();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
    }

    [Fact]
    public void MemoryToolCallFullTextMigration_adds_text_columns_before_their_generated_vectors()
    {
        var migration = new PostgreSqlModule.Migrations.EventSourcing
            .AddMemoryToolCallFullTextSearch();

        var addedColumns = migration.UpOperations
            .OfType<AddColumnOperation>()
            .Select(operation => operation.Name)
            .ToList();

        Assert.Empty(
            migration.UpOperations.Where(
                operation => operation is SqlOperation
                    or InsertDataOperation
            )
        );
        Assert.True(
            addedColumns.IndexOf("InputText")
                < addedColumns.IndexOf("InputSearchVector")
        );
        Assert.True(
            addedColumns.IndexOf("OutputText")
                < addedColumns.IndexOf("OutputSearchVector")
        );
    }

    [Fact]
    public void RegisterPostgreSqlModuleRegistersContextsAndStorage()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [
                        $"ConnectionStrings:{PostgreSqlModuleDefaults.ConnectionStringName}"
                    ] = PostgreSqlModuleDefaults.LocalDevelopmentConnectionString
                }
            )
            .Build();
        var services = new ServiceCollection();

        services.RegisterPostgreSqlModule(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var eventSourcingContext =
            scope.ServiceProvider.GetRequiredService<EventSourcingDbContext>();
        var attachmentsContext =
            scope.ServiceProvider.GetRequiredService<AttachmentsModuleDbContext>();
        var policyContext =
            scope.ServiceProvider.GetRequiredService<IPolicyModuleDbContext>();
        var skillProjectionContext =
            scope.ServiceProvider.GetRequiredService<ISkillsModuleDbContext>();
        var entityRelationContext =
            scope.ServiceProvider.GetRequiredService<IEntityRelationDbContext>();

        Assert.IsType<PostgreSqlEventSourcingDbContext>(
            eventSourcingContext
        );
        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            eventSourcingContext.Database.ProviderName
        );
        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            attachmentsContext.Database.ProviderName
        );
        Assert.Same(
            eventSourcingContext,
            policyContext
        );
        Assert.Same(
            eventSourcingContext,
            skillProjectionContext
        );
        Assert.Same(eventSourcingContext, entityRelationContext);
        Assert.IsType<EntityRelationRepository>(
            scope.ServiceProvider.GetRequiredService<
                IEntityRelationRepository
            >()
        );
        Assert.NotNull(
            scope.ServiceProvider.GetRequiredService<IEventStore>()
        );
        Assert.IsType<ProjectionReplayRepository>(
            scope.ServiceProvider.GetRequiredService<
                IProjectionReplayRepository
            >()
        );
        Assert.IsType<CachedProjectionEmbeddingGenerator>(
            scope.ServiceProvider.GetRequiredService<
                IProjectionEmbeddingGenerator
            >()
        );
        Assert.IsType<PostgreSqlProjectionEmbeddingCache>(
            scope.ServiceProvider.GetRequiredService<
                IProjectionEmbeddingCache
            >()
        );
        Assert.IsType<AttachmentContentStorage>(
            scope.ServiceProvider.GetRequiredService<
                IAttachmentContentStorage
            >()
        );
        Assert.IsType<PolicyTextRepository>(
            scope.ServiceProvider.GetRequiredService<
                IPolicyTextRepository
            >()
        );
        Assert.IsType<SkillSummaryRepository>(
            scope.ServiceProvider.GetRequiredService<
                ISkillSummaryRepository
            >()
        );
        Assert.IsType<SkillListRepository>(
            scope.ServiceProvider.GetRequiredService<
                ISkillListRepository
            >()
        );
        Assert.IsType<FeatureSearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                IFeatureSearchRepository
            >()
        );
        Assert.IsType<PostgreSqlFeatureRecordSearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                IFeatureRecordSearchRepository
            >()
        );
        Assert.IsType<FeatureRecordSearch>(
            scope.ServiceProvider.GetRequiredService<
                IFeatureRecordSearch
            >()
        );
        Assert.IsType<PostgreSqlMemorySearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                IMemorySearchRepository
            >()
        );
        Assert.IsType<MemorySearch>(
            scope.ServiceProvider.GetRequiredService<IMemorySearch>()
        );
        Assert.IsType<PostgreSqlMemorySummaryRepository>(
            scope.ServiceProvider.GetRequiredService<
                IMemorySummaryRepository
            >()
        );
        Assert.IsType<PostgreSqlMemoryConversationRepository>(
            scope.ServiceProvider.GetRequiredService<
                IMemoryConversationRepository
            >()
        );
        Assert.IsType<PostgreSqlSkillSearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                ISkillSearchRepository
            >()
        );
        Assert.IsType<SkillSearch>(
            scope.ServiceProvider.GetRequiredService<ISkillSearch>()
        );
        Assert.IsType<PostgreSqlKnowledgeSearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                IKnowledgeSearchRepository
            >()
        );
        Assert.IsType<KnowledgeSearch>(
            scope.ServiceProvider.GetRequiredService<IKnowledgeSearch>()
        );
        Assert.IsType<PostgreSqlMemorySearchProjectionWriter>(
            scope.ServiceProvider.GetRequiredService<
                IMemorySearchProjectionWriter
            >()
        );
        Assert.IsType<PostgreSqlSkillSearchProjectionWriter>(
            scope.ServiceProvider.GetRequiredService<
                ISkillSearchProjectionWriter
            >()
        );
        Assert.IsType<PostgreSqlFeatureSearchProjectionWriter>(
            scope.ServiceProvider.GetRequiredService<
                IFeatureSearchProjectionWriter
            >()
        );
        var projectorTypes = scope.ServiceProvider
            .GetServices<IProjector>()
            .Select(projector => projector.GetType())
            .ToList();
        Assert.Contains(typeof(GeneralPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(ProjectPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(TopicPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(ProjectTopicProjector), projectorTypes);
        Assert.Contains(
            typeof(PolicyProjectSummaryProjector),
            projectorTypes
        );
        Assert.Contains(typeof(SkillSummaryProjector), projectorTypes);
        Assert.Contains(typeof(SkillListProjector), projectorTypes);
        Assert.Contains(typeof(MemorySearchProjector), projectorTypes);
        Assert.Contains(typeof(MemorySummaryProjector), projectorTypes);
        Assert.Contains(typeof(MemoryConversationProjector), projectorTypes);
        Assert.Contains(typeof(MemoryToolCallProjector), projectorTypes);
        Assert.Contains(typeof(MemoryEntityRelationProjector), projectorTypes);
        Assert.Contains(typeof(SkillSearchProjector), projectorTypes);
        Assert.Contains(typeof(FeatureSearchProjector), projectorTypes);
        Assert.Contains(
            typeof(FeatureRecordSearchProjector),
            projectorTypes
        );
    }

    [Fact]
    public void EventSourcingContextMapsOutboxConcurrencyToPostgreSqlXmin()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );

        var entityType = context.Model.FindEntityType(
            typeof(SerializedPayloadMessage)
        );
        var xmin = entityType?.FindProperty("xmin");

        Assert.NotNull(entityType);
        Assert.Null(
            entityType!.FindProperty(
                nameof(SerializedPayloadMessage.Version)
            )
        );
        Assert.NotNull(xmin);
        Assert.Equal(typeof(uint), xmin!.ClrType);
        Assert.True(xmin.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, xmin.ValueGenerated);
        Assert.Equal("xmin", xmin.GetColumnName());
        AssertIntPrimaryKey<GeneralPolicyText>(context);
        AssertIntPrimaryKey<ProjectPolicyText>(context);
        AssertIntPrimaryKey<PolicyProjectSummaryEntry>(context);
        AssertIntPrimaryKey<TopicPolicyText>(context);
        AssertIntPrimaryKey<ProjectPolicyTopic>(context);
        AssertIntPrimaryKey<SkillSummaryEntry>(context);
        AssertIntPrimaryKey<SkillListEntry>(context);
        AssertIntPrimaryKey<SkillListTagEntry>(context);
        AssertIntPrimaryKey<FeatureSummaryEntry>(context);
        AssertIntPrimaryKey<FeatureSearchEntry>(context);
        var entityRelation = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(EntityRelation));
        Assert.NotNull(entityRelation);
        var entityRelationId = Assert.Single(
            entityRelation!.FindPrimaryKey()!.Properties
        );
        Assert.Equal(typeof(long), entityRelationId.ClrType);
        Assert.Equal(ValueGenerated.OnAdd, entityRelationId.ValueGenerated);
        Assert.Equal(
            20,
            entityRelation
                .FindProperty(nameof(EntityRelation.RelationType))!
                .GetMaxLength()
        );
        Assert.Equal(
            "text",
            entityRelation
                .FindProperty(nameof(EntityRelation.RelatedEntitySummary))!
                .GetColumnType()
        );
        Assert.Contains(
            entityRelation.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(EntityRelation.EntityId),
                            nameof(EntityRelation.RelatedEntityId)
                        ]
                    )
        );
        AssertUniqueIndex<GeneralPolicyText>(context, 1);
        AssertUniqueIndex<ProjectPolicyText>(context, 1);
        AssertUniqueIndex<TopicPolicyText>(context, 1);
        AssertUniqueIndex<ProjectPolicyTopic>(context, 2);
        Assert.Equal(
            2,
            context.Model
                .FindEntityType(typeof(PolicyProjectSummaryEntry))!
                .GetIndexes()
                .Count(index => index.IsUnique)
        );
        AssertUniqueIndex<FeatureSearchEntry>(context, 1);
        AssertUniqueIndex<SkillListEntry>(context, 1);
        var skillList = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(SkillListEntry));
        Assert.NotNull(skillList);
        Assert.All(
            skillList!.GetIndexes().Where(index => !index.IsUnique),
            index => Assert.Equal(
                "\"IsDeleted\" = FALSE",
                index.GetFilter()
            )
        );
        Assert.Contains(
            skillList.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(SkillListEntry.NormalizedName),
                        nameof(SkillListEntry.Name),
                        nameof(SkillListEntry.SkillAggregateId)
                    ]
                )
        );
        Assert.Contains(
            skillList.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(SkillListEntry.ReferenceCount),
                        nameof(SkillListEntry.SkillAggregateId)
                    ]
                )
        );
        Assert.Contains(
            skillList.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(SkillListEntry.AttachmentCount),
                        nameof(SkillListEntry.SkillAggregateId)
                    ]
                )
        );
        Assert.Contains(
            skillList.GetIndexes(),
            index =>
                index.Properties.SingleOrDefault()?.Name
                    == nameof(SkillListEntry.SearchText)
                && string.Equals(
                    index.GetMethod(),
                    "gin",
                    StringComparison.OrdinalIgnoreCase
                )
                && index.GetOperators() is ["gin_trgm_ops"]
        );
        var skillListTag = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(SkillListTagEntry));
        Assert.NotNull(skillListTag);
        Assert.Contains(
            skillListTag!.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(SkillListTagEntry.SkillListEntryId),
                            nameof(SkillListTagEntry.NormalizedTag)
                        ]
                    )
        );
        Assert.Contains(
            skillListTag.GetIndexes(),
            index => !index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(SkillListTagEntry.NormalizedTag),
                            nameof(SkillListTagEntry.SkillListEntryId)
                        ]
                    )
        );
        var featureSearch = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(FeatureSearchEntry));
        Assert.NotNull(featureSearch);
        Assert.All(
            featureSearch!.GetIndexes().Where(index => !index.IsUnique),
            index => Assert.Equal(
                "\"IsDeleted\" = FALSE",
                index.GetFilter()
            )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual(
                    [
                        nameof(FeatureSearchEntry.ProjectId),
                        nameof(FeatureSearchEntry.NormalizedName),
                        nameof(FeatureSearchEntry.Name),
                        nameof(FeatureSearchEntry.FeatureAggregateId)
                    ]
                )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(FeatureSearchEntry.NormalizedName),
                            nameof(FeatureSearchEntry.Name),
                            nameof(FeatureSearchEntry.FeatureAggregateId)
                        ]
                    )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(FeatureSearchEntry.ProjectId),
                            nameof(FeatureSearchEntry.PlanCount),
                            nameof(FeatureSearchEntry.FeatureAggregateId)
                        ]
                    )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(FeatureSearchEntry.ProjectId),
                            nameof(FeatureSearchEntry.RecordCount),
                            nameof(FeatureSearchEntry.FeatureAggregateId)
                        ]
                    )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(FeatureSearchEntry.PlanCount),
                            nameof(FeatureSearchEntry.FeatureAggregateId)
                        ]
                    )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(FeatureSearchEntry.RecordCount),
                            nameof(FeatureSearchEntry.FeatureAggregateId)
                        ]
                    )
        );
        Assert.Contains(
            featureSearch.GetIndexes(),
            index =>
                index.Properties.SingleOrDefault()?.Name
                    == nameof(FeatureSearchEntry.SearchText)
                && string.Equals(
                    index.GetMethod(),
                    "gin",
                    StringComparison.OrdinalIgnoreCase
                )
                && index.GetOperators() is ["gin_trgm_ops"]
        );
        Assert.Equal(
            2,
            context.Model
                .FindEntityType(typeof(FeatureSummaryEntry))!
                .GetIndexes()
                .Count(index => index.IsUnique)
        );
        Assert.Equal(
            2,
            context.Model
                .FindEntityType(typeof(SkillSummaryEntry))!
                .GetIndexes()
                .Count(index => index.IsUnique)
        );

        var memorySummary = context.Model.FindEntityType(
            typeof(MemorySummaryEntry)
        );
        Assert.NotNull(memorySummary);
        Assert.Equal(
            nameof(MemorySummaryEntry.MemoryAggregateId),
            Assert.Single(memorySummary!.FindPrimaryKey()!.Properties).Name
        );
        Assert.Contains(
            memorySummary.GetIndexes(),
            index => index.Properties.Single().Name
                == nameof(MemorySummaryEntry.ThreadId)
        );
        Assert.Contains(
            memorySummary.GetIndexes(),
            index => index.Properties.Single().Name
                == nameof(MemorySummaryEntry.LastActivityTimestamp)
        );

        var memorySearch = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(
                typeof(MemorySearchEntry)
            );
        Assert.NotNull(memorySearch);
        Assert.Equal(
            "vector(1024)",
            memorySearch!
                .FindProperty(nameof(MemorySearchEntry.Embedding))!
                .GetColumnType()
        );
        Assert.Equal(
            "tsvector",
            memorySearch
                .FindProperty(nameof(MemorySearchEntry.SearchVector))!
                .GetColumnType()
        );
        Assert.Contains(
            memorySearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "gin",
                StringComparison.OrdinalIgnoreCase
            )
        );

        var skillSearch = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(SkillSearchEntry));
        Assert.NotNull(skillSearch);
        Assert.Equal(
            "vector(1024)",
            skillSearch!
                .FindProperty(nameof(SkillSearchEntry.Embedding))!
                .GetColumnType()
        );
        Assert.Equal(
            "tsvector",
            skillSearch
                .FindProperty(nameof(SkillSearchEntry.SearchVector))!
                .GetColumnType()
        );
        Assert.Contains(
            skillSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "gin",
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.Contains(
            skillSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "hnsw",
                StringComparison.OrdinalIgnoreCase
            )
        );
        var featureRecordSearch = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(FeatureRecordSearchEntry));
        Assert.NotNull(featureRecordSearch);
        Assert.Equal(
            "vector(1024)",
            featureRecordSearch!
                .FindProperty(nameof(FeatureRecordSearchEntry.Embedding))!
                .GetColumnType()
        );
        Assert.Equal(
            "tsvector",
            featureRecordSearch
                .FindProperty(
                    nameof(FeatureRecordSearchEntry.SearchVector)
                )!
                .GetColumnType()
        );
        Assert.Contains(
            featureRecordSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "gin",
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.Contains(
            featureRecordSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "hnsw",
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.Contains(
            memorySearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "hnsw",
                StringComparison.OrdinalIgnoreCase
            )
        );

        var knowledgeSearch = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(KnowledgeSearchEntry));
        Assert.NotNull(knowledgeSearch);
        Assert.Equal(
            "vector(1024)",
            knowledgeSearch!
                .FindProperty(nameof(KnowledgeSearchEntry.Embedding))!
                .GetColumnType()
        );
        Assert.Equal(
            "jsonb",
            knowledgeSearch
                .FindProperty(nameof(KnowledgeSearchEntry.MetadataJson))!
                .GetColumnType()
        );
        Assert.Equal(
            "timestamp with time zone",
            knowledgeSearch
                .FindProperty(nameof(KnowledgeSearchEntry.Timestamp))!
                .GetColumnType()
        );
        Assert.Contains(
            knowledgeSearch.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                        [
                            nameof(KnowledgeSearchEntry.OwnerType),
                            nameof(KnowledgeSearchEntry.OwnerAggregateId),
                            nameof(KnowledgeSearchEntry.SourceType),
                            nameof(KnowledgeSearchEntry.SourceKey),
                            nameof(KnowledgeSearchEntry.ChunkIndex)
                        ]
                    )
        );
        Assert.Contains(
            knowledgeSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "gin",
                StringComparison.OrdinalIgnoreCase
            )
        );
        Assert.Contains(
            knowledgeSearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "hnsw",
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

    [Fact]
    public void KnowledgeSearchQueries_translate_to_bounded_full_text_and_cosine_search()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlKnowledgeSearchRepository(context);

        var textSql = repository.CreateTextQuery(["event", "sourcing"], 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("plainto_tsquery", textSql);
        Assert.Contains("@@", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains("LIMIT", textSql);
        Assert.Contains("<=>", vectorSql);
        Assert.Contains("LIMIT", vectorSql);
    }

    [Fact]
    public void KnowledgeSourceQueries_group_the_ranked_window_by_owner()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlKnowledgeSearchRepository(context);

        var textSql = repository.CreateTextSourceQuery(
            ["event", "sourcing"],
            5,
            200
        ).ToQueryString();
        var vectorSql = repository.CreateVectorSourceQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            5,
            200
        ).ToQueryString();

        Assert.Contains(
            "DISTINCT ON (\"OwnerType\", \"OwnerAggregateId\")",
            textSql
        );
        Assert.Contains("plainto_tsquery", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains(
            "DISTINCT ON (\"OwnerType\", \"OwnerAggregateId\")",
            vectorSql
        );
        Assert.Contains("<=>", vectorSql);
        Assert.Contains("::vector", vectorSql);
    }

    [Fact]
    public void MemoryToolCallQueries_filter_by_chat_prompt_and_separate_input_output_text()
    {
        using var context = CreateEventSourcingContext();
        var filters = new MemoryToolCallFilters(
            "Bash",
            AggregateId.FromDatabaseGuid(Guid.NewGuid()),
            new ThreadId(Guid.NewGuid()),
            new PromptId(Guid.NewGuid()),
            ["git", "status"],
            ["fatal"]
        );

        var sql = CreateMemoryToolCallPageQuery(
            context,
            filters,
            MemoryToolCallSortField.Relevance
        ).ToQueryString();

        Assert.Contains("\"InputSearchVector\" @@ plainto_tsquery", sql);
        Assert.Contains("\"OutputSearchVector\" @@ plainto_tsquery", sql);
        Assert.Contains("ts_rank_cd(m.\"InputSearchVector\"", sql);
        Assert.Contains("ts_rank_cd(m.\"OutputSearchVector\"", sql);
        Assert.Contains("\"MemoryAggregateId\" = ", sql);
        Assert.Contains("\"ThreadId\" = ", sql);
        Assert.Contains("\"PromptId\" = ", sql);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void MemoryToolCallRelevance_ranks_only_the_searched_sides(
        bool hasInputKeywords,
        bool hasOutputKeywords
    )
    {
        using var context = CreateEventSourcingContext();
        var filters = new MemoryToolCallFilters(
            InputKeywords: hasInputKeywords ? ["git"] : [],
            OutputKeywords: hasOutputKeywords ? ["fatal"] : []
        );

        var sql = CreateMemoryToolCallPageQuery(
            context,
            filters,
            MemoryToolCallSortField.Relevance
        ).ToQueryString();
        var orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];

        Assert.Equal(
            hasInputKeywords,
            orderBy.Contains("\"InputSearchVector\"")
        );
        Assert.Equal(
            hasOutputKeywords,
            orderBy.Contains("\"OutputSearchVector\"")
        );
        Assert.Equal(
            !hasInputKeywords && !hasOutputKeywords,
            orderBy.StartsWith(
                "ORDER BY m.\"Timestamp\" DESC",
                StringComparison.Ordinal
            )
        );
    }

    [Fact]
    public void MemorySummaryQueries_group_models_and_filter_sessions_by_model()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlMemorySummaryRepository(context);

        var modelsSql = repository.CreateModelsQuery().ToQueryString();
        var filterSql = PostgreSqlMemorySummaryRepository
            .FilterByModel(
                context.Set<MemorySummaryEntry>(),
                "claude-opus-5-5"
            )
            .ToQueryString();

        Assert.Contains("unnest(", modelsSql);
        Assert.Contains("GROUP BY", modelsSql);
        Assert.Contains("ORDER BY", modelsSql);
        Assert.Contains("\"Models\"", filterSql);
        Assert.Contains("claude-opus-5-5", filterSql);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    public void MemorySearchQueries_filter_every_search_by_time_and_model(
        bool hasFrom,
        bool hasTo,
        bool hasModel
    )
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlMemorySearchRepository(context);
        var filter = new MemorySearchFilter(
            new MemorySearchTimeRange(
                hasFrom ? DateTime.UnixEpoch : null,
                hasTo ? DateTime.UnixEpoch.AddDays(1) : null
            ),
            hasModel ? "claude-opus-5-5" : null
        );
        var embedding = Enumerable.Repeat(0.1f, 1024).ToImmutableArray();

        var queries = new List<string>
        {
            repository.CreateTextQuery(["event"], 50, filter)
                .ToQueryString(),
            repository.CreateVectorQuery(embedding, 50, filter)
                .ToQueryString(),
            repository.CreateTextSourceQuery(["event"], 5, 200, filter)
                .ToQueryString(),
            repository.CreateVectorSourceQuery(embedding, 5, 200, filter)
                .ToQueryString()
        };

        Assert.All(
            queries,
            sql =>
            {
                Assert.Equal(
                    hasFrom,
                    sql.Contains("\"PromptStartTimestamp\" >= @")
                );
                Assert.Equal(
                    hasTo,
                    sql.Contains("\"PromptStartTimestamp\" <= @")
                );
                Assert.Equal(hasModel, sql.Contains("\"Model\" = @"));
            }
        );
    }

    [Fact]
    public void MemorySearchQueries_translate_to_full_text_and_cosine_search()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlMemorySearchRepository(context);

        var textSourceSql = repository.CreateTextSourceQuery(
            ["event", "sourcing"],
            5,
            200
        ).ToQueryString();
        var vectorSourceSql = repository.CreateVectorSourceQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            5,
            200
        ).ToQueryString();

        Assert.Contains(
            "DISTINCT ON (\"MemoryAggregateId\")",
            textSourceSql
        );
        Assert.Contains(
            "DISTINCT ON (\"MemoryAggregateId\")",
            vectorSourceSql
        );

        var textSql = repository.CreateTextQuery(["event", "sourcing"], 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("plainto_tsquery", textSql);
        Assert.Contains("@@", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains("<=>", vectorSql);
    }

    [Fact]
    public void SkillSearchQueries_translate_to_full_text_and_cosine_search()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlSkillSearchRepository(context);

        var textSourceSql = repository.CreateTextSourceQuery(
            ["event", "sourcing"],
            5,
            200
        ).ToQueryString();
        var vectorSourceSql = repository.CreateVectorSourceQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            5,
            200
        ).ToQueryString();

        Assert.Contains(
            "DISTINCT ON (\"SkillAggregateId\")",
            textSourceSql
        );
        Assert.Contains(
            "DISTINCT ON (\"SkillAggregateId\")",
            vectorSourceSql
        );

        var textSql = repository.CreateTextQuery(["event", "sourcing"], 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("plainto_tsquery", textSql);
        Assert.Contains("@@", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains("<=>", vectorSql);
    }

    [Fact]
    public void FeatureRecordSearchQueries_translate_to_full_text_and_cosine_search()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var repository = new PostgreSqlFeatureRecordSearchRepository(
            context
        );

        var textSourceSql = repository.CreateTextSourceQuery(
            ["event", "sourcing"],
            5,
            200
        ).ToQueryString();
        var vectorSourceSql = repository.CreateVectorSourceQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            5,
            200
        ).ToQueryString();

        Assert.Contains(
            "DISTINCT ON (\"FeatureAggregateId\")",
            textSourceSql
        );
        Assert.Contains(
            "DISTINCT ON (\"FeatureAggregateId\")",
            vectorSourceSql
        );

        var textSql = repository.CreateTextQuery(["event", "sourcing"], 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("plainto_tsquery", textSql);
        Assert.Contains("@@", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains("<=>", vectorSql);
    }

    [Fact]
    public void FeatureSearchQuery_translates_entirely_to_postgresql()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var request = new EntityQuery<
            FeatureSearchFilters,
            FeatureSearchSortField
        >(
            new PageRequest(2, 25),
            " trace ",
            new FeatureSearchFilters(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            new SortRequest<FeatureSearchSortField>(
                FeatureSearchSortField.Name,
                SortDirection.Ascending
            )
        );

        var sql = new FeatureSearchRepository(context)
            .CreatePageQuery(request)
            .ToQueryString();

        Assert.Contains("FeatureSearchEntries", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }

    [Fact]
    public void SkillListQuery_translates_entirely_to_postgresql()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );
        using var context = new PostgreSqlEventSourcingDbContext(
            options.Options
        );
        var request = new EntityQuery<
            SkillSearchFilters,
            SkillSearchSortField
        >(
            new PageRequest(2, 25),
            " trace ",
            new SkillSearchFilters("dotnet", true, false),
            new SortRequest<SkillSearchSortField>(
                SkillSearchSortField.ReferenceCount,
                SortDirection.Descending
            )
        );

        var sql = new SkillListRepository(context)
            .CreatePageQuery(request)
            .ToQueryString();

        Assert.Contains("SkillListEntries", sql);
        Assert.Contains("SkillListTags", sql);
        Assert.Contains("EXISTS", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("ReferenceCount", sql);
        Assert.Contains("AttachmentCount", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }

    private static void AssertIntPrimaryKey<TEntity>(DbContext context)
    {
        var primaryKey = context.Model
            .FindEntityType(typeof(TEntity))!
            .FindPrimaryKey();

        var property = Assert.Single(primaryKey!.Properties);
        Assert.Equal(
            typeof(int),
            property.ClrType
        );
        Assert.Equal(ValueGenerated.OnAdd, property.ValueGenerated);
    }

    private static void AssertUniqueIndex<TEntity>(
        DbContext context,
        int propertyCount
    )
    {
        Assert.Contains(
            context.Model.FindEntityType(typeof(TEntity))!.GetIndexes(),
            index =>
                index.IsUnique
                && index.Properties.Count == propertyCount
        );
    }

    private static PostgreSqlEventSourcingDbContext CreateEventSourcingContext()
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();
        PostgreSqlDbContextOptions.Configure(
            options,
            PostgreSqlModuleDefaults.LocalDevelopmentConnectionString,
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );

        return new PostgreSqlEventSourcingDbContext(options.Options);
    }

    private static IQueryable<MemoryToolCall> CreateMemoryToolCallPageQuery(
        PostgreSqlEventSourcingDbContext context,
        MemoryToolCallFilters filters,
        MemoryToolCallSortField sortBy
    ) =>
        EntityQueryExecutor.BuildPageQuery(
            context.Set<MemoryToolCallEntry>(),
            new EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField>(
                new PageRequest(1, 10),
                null,
                filters,
                new SortRequest<MemoryToolCallSortField>(
                    sortBy,
                    SortDirection.Descending
                )
            ),
            new MemoryToolCallQueryProfile(filters)
        );
}
