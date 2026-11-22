using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ActionModule.API;

/// <summary>
/// Answers 403 when an action's <c>IsAuthorized</c> refuses the executor, instead of a 500.
/// </summary>
public sealed class UnauthorizedActionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UnauthorizedAccessException exception)
            return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = exception.Message
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
        context.ExceptionHandled = true;
    }
}
