using EventSourcing.Persistence;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Interfaces;
using PolicyModule.Persistence.Models;
using SkillsModule.Application.Attachments;
using SkillsModule.Persistence;
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
        var projectorTypes = scope.ServiceProvider
            .GetServices<IProjector>()
            .Select(projector => projector.GetType())
            .ToList();
        Assert.Contains(typeof(GeneralPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(ProjectPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(TopicPolicyTextProjector), projectorTypes);
        Assert.Contains(typeof(ProjectTopicProjector), projectorTypes);
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
        AssertIntPrimaryKey<TopicPolicyText>(context);
        AssertIntPrimaryKey<ProjectPolicyTopic>(context);
        AssertUniqueIndex<GeneralPolicyText>(context, 1);
        AssertUniqueIndex<ProjectPolicyText>(context, 1);
        AssertUniqueIndex<TopicPolicyText>(context, 1);
        AssertUniqueIndex<ProjectPolicyTopic>(context, 2);
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
