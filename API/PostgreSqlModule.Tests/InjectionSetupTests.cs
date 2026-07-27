using EventSourcing.Persistence;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillsModule.Application.Attachments;
using SkillsModule.Persistence;
using Xunit;

namespace PostgreSqlModule.Tests;

public sealed class InjectionSetupTests
{
    [Fact]
    public void RegisterPostgreSqlModuleRegistersBothContextsAndStorage()
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
        Assert.NotNull(
            scope.ServiceProvider.GetRequiredService<IEventStore>()
        );
        Assert.IsType<AttachmentContentStorage>(
            scope.ServiceProvider.GetRequiredService<
                IAttachmentContentStorage
            >()
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
    }
}
