using AdministrationModule.Persistence;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using FeatureModule.Persistence;
using FeatureModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MemoryModule.Persistence;
using MemoryModule.Persistence.Interfaces;
using PolicyModule.Persistence;
using SkillsModule.Application.Attachments;
using SkillsModule.Persistence;
using SkillsModule.Persistence.Interfaces;
using UUIDNext;

namespace PostgreSqlModule;

public static class InjectionSetup
{
    public static IServiceCollection RegisterPostgreSqlModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString(
            PostgreSqlModuleDefaults.ConnectionStringName
        );

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{PostgreSqlModuleDefaults.ConnectionStringName}' is required."
            );
        }

        services.RegisterEventSourcingPersistence(
            CreateProviderNeutralConfiguration(configuration)
        );
        services.RegisterAdministrationModulePersistence();
        services.RegisterFeatureModulePersistence();
        services.AddScoped<
            IFeatureResearchSearchRepository,
            PostgreSqlFeatureResearchSearchRepository
        >();

        services.AddDbContext<EventSourcingDbContext>(
            options =>
                PostgreSqlDbContextOptions.Configure(
                    options,
                    connectionString,
                    PostgreSqlModuleDefaults
                        .EventSourcingMigrationsHistoryTable
                )
        );

        services.Replace(
            ServiceDescriptor.Scoped<EventSourcingDbContext>(
                serviceProvider =>
                    new PostgreSqlEventSourcingDbContext(
                        serviceProvider.GetRequiredService<
                            DbContextOptions<EventSourcingDbContext>
                        >()
                    )
            )
        );

        services.AddDbContext<SkillsModuleDbContext>(
            options =>
                PostgreSqlDbContextOptions.Configure(
                    options,
                    connectionString,
                    PostgreSqlModuleDefaults.SkillsMigrationsHistoryTable
                )
        );
        services.AddScoped<
            IAttachmentContentStorage,
            AttachmentContentStorage
        >();
        services.RegisterSkillsModulePersistence();
        services.AddScoped<
            ISkillSearchRepository,
            PostgreSqlSkillSearchRepository
        >();
        services.AddScoped<ISkillsModuleDbContext>(
            serviceProvider =>
                (ISkillsModuleDbContext)serviceProvider
                    .GetRequiredService<EventSourcingDbContext>()
        );

        services.RegisterPolicyModulePersistence();
        services.AddScoped<IPolicyModuleDbContext>(
            serviceProvider =>
                (IPolicyModuleDbContext)serviceProvider
                    .GetRequiredService<EventSourcingDbContext>()
        );
        services.AddScoped<IFeatureModuleDbContext>(
            serviceProvider =>
                (IFeatureModuleDbContext)serviceProvider
                    .GetRequiredService<EventSourcingDbContext>()
        );
        services.AddScoped<
            IMemorySearchRepository,
            PostgreSqlMemorySearchRepository
        >();
        services.AddScoped<
            IMemorySummaryRepository,
            PostgreSqlMemorySummaryRepository
        >();
        services.RegisterTextEmbeddings(configuration);
        services.AddScoped<
            IKnowledgeSearchRepository,
            PostgreSqlKnowledgeSearchRepository
        >();
        services.AddScoped<
            IKnowledgeSearchProjectionTransaction,
            PostgreSqlKnowledgeSearchProjectionTransaction
        >();
        services.RegisterMemoryModulePersistence();
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.PostgreSql
        );

        return services;
    }

    private static IConfiguration CreateProviderNeutralConfiguration(
        IConfiguration configuration
    ) =>
        new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["UseSqlServer"] = bool.FalseString
                }
            )
            .Build();
}
