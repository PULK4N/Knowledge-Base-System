using System.Reflection;
using ActionModule.Shared;

namespace Microsoft.Extensions.DependencyInjection;

public static class ActionRegistration
{
    public static IServiceCollection RegisterActions(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        var actionTypes = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IAction).IsAssignableFrom(type))
            .Where(type => type is { IsClass: true, IsAbstract: false });

        foreach (var actionType in actionTypes)
            services.AddScoped(actionType);

        return services;
    }
}
