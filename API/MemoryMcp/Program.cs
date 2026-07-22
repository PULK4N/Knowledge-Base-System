using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Interfaces;
using MemoryMcp.Domain.Skills;
using MemoryMcp.Infrastructure;
using MemoryMcp.Services;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterEventSourcingCore(typeof(SkillSaved).Assembly);
builder.Services.RegisterEventSourcingPersistence(builder.Configuration);

// The memory server owns state creation and uses replay-on-read, so it does not
// need the generic module's unfinished production provider or projection outbox.
builder.Services.AddScoped<IStateDataProvider, MemoryStateDataProvider>();
builder.Services.AddScoped<IOutbox, NoOpOutbox>();
builder.Services.AddScoped<MemoryService>();

builder
    .Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp");

await app.RunAsync();

public partial class Program;
