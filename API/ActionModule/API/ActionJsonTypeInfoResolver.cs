using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ActionModule.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ActionModule.API;

public sealed class ActionJsonTypeInfoResolver
    : DefaultJsonTypeInfoResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActionJsonTypeInfoResolver(
        IHttpContextAccessor httpContextAccessor
    )
    {
        _httpContextAccessor = httpContextAccessor;
        Modifiers.Add(ConfigureActionCreation);
    }

    private void ConfigureActionCreation(
        JsonTypeInfo typeInfo
    )
    {
        if (
            !typeof(IAction).IsAssignableFrom(typeInfo.Type)
            || typeInfo.Type.IsAbstract
        )
            return;

        typeInfo.CreateObject = () =>
            ResolveAction(typeInfo.Type);
    }

    private object ResolveAction(Type actionType)
    {
        var requestServices = _httpContextAccessor
            .HttpContext?
            .RequestServices;

        if (requestServices is null)
            throw new JsonException(
                $"Action '{actionType.Name}' can only be deserialized "
                + "during an active HTTP request."
            );

        return requestServices.GetRequiredService(actionType);
    }
}
