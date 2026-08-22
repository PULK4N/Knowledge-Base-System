using ActionModule.API;
using ActionModule.Shared;
using AdministrationModule.API.Controllers;
using AdministrationModule.Application.Commands;
using EventSourcing.Core;
using EventSourcing.Core.Providers;
using EventSourcing.Optimizations;
using FeatureModule.API.Controllers;
using FeatureModule.Application.Commands;
using FeatureModule.Domain;
using FeatureModule.MCP;
using MemoryModule.API.Controllers;
using MemoryModule.Application.Commands;
using MemoryModule.Domain;
using MemoryModule.MCP;
using PolicyModule.API.Controllers;
using PolicyModule.Application.Commands;
using PolicyModule.Domain;
using PolicyModule.MCP;
using PostgreSqlModule;
using SkillsModule.API.Controllers;
using SkillsModule.Application.Commands;
using SkillsModule.Domain;
using SkillsModule.MCP;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(
    options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    }
);

builder.Configuration[YamlStateMachineDefinitionProvider.ConfigurationPath] =
    Path.Combine(AppContext.BaseDirectory, "StateMachines");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ActionJsonTypeInfoResolver>();
builder.Services
    .AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>()
    .Configure<ActionJsonTypeInfoResolver>(
        (options, resolver) =>
            options.JsonSerializerOptions.TypeInfoResolver = resolver
    );

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AdministrationController).Assembly)
    .AddApplicationPart(typeof(FeaturesController).Assembly)
    .AddApplicationPart(typeof(SkillsController).Assembly)
    .AddApplicationPart(typeof(MemoryController).Assembly)
    .AddApplicationPart(typeof(PoliciesController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.RegisterPostgreSqlModule(builder.Configuration);
builder.Services.RegisterEventSourcingOptmizations(builder.Configuration);
builder.Services.RegisterEventSourcingCore(
    typeof(SkillStateData).Assembly,
    typeof(FeatureStateData).Assembly,
    typeof(MemoryStateData).Assembly,
    typeof(GeneralPoliciesStateData).Assembly
);
builder.Services.RegisterActions(
    typeof(QueueProjectionReplayCommand).Assembly,
    typeof(AddFeatureCommand).Assembly,
    typeof(AddSkillCommand).Assembly,
    typeof(RecordCodexPromptHookCommand).Assembly,
    typeof(AddGeneralPolicyCommand).Assembly
);
builder.Services.AddScoped<IExecutorProvider, TemporaryExecutorProvider>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools(
        PolicyMcpFunctions.Create()
            .Concat(FeatureMcpFunctions.Create())
            .Concat(SkillMcpFunctions.Create())
            .Concat(MemoryMcpFunctions.Create())
            .Select(
                function =>
                    ModelContextProtocol.Server.McpServerTool.Create(
                        function
                    )
            )
    );

var app = builder.Build();

await app.Services.ApplyPostgreSqlMigrations();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapMcp("/mcp");

app.Run();

public partial class Program;
