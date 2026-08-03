using System.Text.Json;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActionModule.API.Tests;

public sealed class ActionJsonTypeInfoResolverTests
{
    [Theory]
    [InlineData(typeof(TestCommand))]
    [InlineData(typeof(TestQuery))]
    public void Deserialize_ActionUsesDefaultConstructorArguments(
        Type actionType
    )
    {
        const string json = """
            {
              "name": "payment",
              "amounts": [10, 20]
            }
            """;
        var services = new ServiceCollection();
        services.AddScoped<TestDependency>();
        services.AddScoped<TestCommand>();
        services.AddScoped<TestQuery>();
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider
            }
        };
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new ActionJsonTypeInfoResolver(
                httpContextAccessor
            )
        };

        var action = Assert.IsAssignableFrom<ITestAction>(
            JsonSerializer.Deserialize(json, actionType, options)
        );

        Assert.Equal("payment", action.Name);
        Assert.Equal([10, 20], action.Amounts);
        Assert.True(action.HasDependency);
        Assert.Same(
            scope.ServiceProvider.GetRequiredService(actionType),
            action
        );
    }

    [Fact]
    public void Deserialize_ActionOutsideRequestThrows()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new ActionJsonTypeInfoResolver(
                new HttpContextAccessor()
            )
        };

        var exception = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<TestCommand>("{}", options)
        );

        Assert.Contains("active HTTP request", exception.Message);
    }

    private sealed class TestDependency;

    private interface ITestAction
    {
        string Name { get; }
        List<int> Amounts { get; }
        bool HasDependency { get; }
    }

    private sealed class TestCommand(TestDependency dependency)
        : Command<string>, ITestAction
    {
        public string Name { get; set; } = "";
        public List<int> Amounts { get; set; } = [];
        public bool HasDependency => dependency is not null;

        protected override Task<string> ExecuteInternal(
            Executor executor
        ) =>
            Task.FromResult(Name);
    }

    private sealed class TestQuery(TestDependency dependency)
        : Query<string>, ITestAction
    {
        public string Name { get; set; } = "";
        public List<int> Amounts { get; set; } = [];
        public bool HasDependency => dependency is not null;

        protected override Task<string> ExecuteInternal(
            Executor executor
        ) =>
            Task.FromResult(Name);
    }
}
