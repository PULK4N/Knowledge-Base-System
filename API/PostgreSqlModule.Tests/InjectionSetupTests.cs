using System.Collections.Immutable;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemoryModule.Persistence;
using MemoryModule.Persistence.Interfaces;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;
using SkillsModule.Application.Attachments;
using SkillsModule.Persistence;
using SkillsModule.Persistence.Interfaces;
using SkillsModule.Persistence.Models;
using Xunit;

namespace PostgreSqlModule.Tests;

public sealed class InjectionSetupTests
{
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
        var skillsContext =
            scope.ServiceProvider.GetRequiredService<SkillsModuleDbContext>();
        var policyContext =
            scope.ServiceProvider.GetRequiredService<IPolicyModuleDbContext>();
        var skillProjectionContext =
            scope.ServiceProvider.GetRequiredService<ISkillsModuleDbContext>();

        Assert.IsType<PostgreSqlEventSourcingDbContext>(
            eventSourcingContext
        );
        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            eventSourcingContext.Database.ProviderName
        );
        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            skillsContext.Database.ProviderName
        );
        Assert.Same(
            eventSourcingContext,
            policyContext
        );
        Assert.Same(
            eventSourcingContext,
            skillProjectionContext
        );
        Assert.NotNull(
            scope.ServiceProvider.GetRequiredService<IEventStore>()
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
        Assert.IsType<PostgreSqlMemorySearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                IMemorySearchRepository
            >()
        );
        Assert.IsType<MemorySearch>(
            scope.ServiceProvider.GetRequiredService<IMemorySearch>()
        );
        Assert.IsType<PostgreSqlSkillSearchRepository>(
            scope.ServiceProvider.GetRequiredService<
                ISkillSearchRepository
            >()
        );
        Assert.IsType<SkillSearch>(
            scope.ServiceProvider.GetRequiredService<ISkillSearch>()
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
        Assert.Contains(typeof(MemorySearchProjector), projectorTypes);
        Assert.Contains(typeof(SkillSearchProjector), projectorTypes);
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
        Assert.Equal(
            2,
            context.Model
                .FindEntityType(typeof(SkillSummaryEntry))!
                .GetIndexes()
                .Count(index => index.IsUnique)
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
        Assert.Contains(
            memorySearch.GetIndexes(),
            index => string.Equals(
                index.GetMethod(),
                "hnsw",
                StringComparison.OrdinalIgnoreCase
            )
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

        var textSql = repository.CreateTextQuery("event sourcing", 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("websearch_to_tsquery", textSql);
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

        var textSql = repository.CreateTextQuery("event sourcing", 50)
            .ToQueryString();
        var vectorSql = repository.CreateVectorQuery(
            Enumerable.Repeat(0.1f, 1024).ToImmutableArray(),
            50
        ).ToQueryString();

        Assert.Contains("websearch_to_tsquery", textSql);
        Assert.Contains("@@", textSql);
        Assert.Contains("ts_rank_cd", textSql);
        Assert.Contains("<=>", vectorSql);
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
}
