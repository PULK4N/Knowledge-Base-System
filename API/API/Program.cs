using ActionModule.Shared;
using EventSourcing.Core;
using EventSourcing.Core.Providers;
using EventSourcing.Optimizations;
using MemoryModule.API.Controllers;
using MemoryModule.Application.Commands;
using MemoryModule.Domain;
using PostgreSqlModule;
using SkillsModule.API.Controllers;
using SkillsModule.Application.Commands;
using SkillsModule.Domain;

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

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(SkillsController).Assembly)
    .AddApplicationPart(typeof(MemoryController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.RegisterPostgreSqlModule(builder.Configuration);
builder.Services.RegisterEventSourcingOptmizations(builder.Configuration);
builder.Services.RegisterEventSourcingCore(
    typeof(SkillStateData).Assembly,
    typeof(MemoryStateData).Assembly
);
builder.Services.RegisterActions(
    typeof(AddSkillCommand).Assembly,
    typeof(RecordCodexPromptHookCommand).Assembly
);
builder.Services.AddScoped<IExecutorProvider, TemporaryExecutorProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
