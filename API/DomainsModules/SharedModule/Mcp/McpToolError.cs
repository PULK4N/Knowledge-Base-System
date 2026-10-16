using System.Reflection;
using System.Text.Json;
using EventSourcing.Shared.Exceptions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using SharedModule.Exceptions;

namespace SharedModule.Mcp;

/// <summary>
/// Describes a failed tool call in words a model can act on: what kind of
/// failure it was, the reason, the arguments that were sent, and what to do
/// next.
/// </summary>
internal sealed record McpToolError(McpToolErrorKind Kind, string Reason)
{
    /// <summary>
    /// Long arguments, such as skill content, are cut so the error stays
    /// readable; the model still has its own copy of what it sent.
    /// </summary>
    internal const int MaximumArgumentsLength = 2000;

    public static McpToolError From(Exception exception)
    {
        var cause = Unwrap(exception);

        return new McpToolError(Classify(cause), cause.Message);
    }

    public CallToolResult ToResult(
        string toolName,
        IDictionary<string, JsonElement>? arguments
    ) =>
        new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = string.Join(
                        "\n",
                        Summary(toolName),
                        $"Reason: {Reason}",
                        $"Arguments received: {FormatArguments(arguments)}",
                        $"What to do: {Guidance(toolName)}"
                    )
                }
            ]
        };

    // Argument binding, action CanExecute checks, event validators, and
    // missing entities all mean the request itself must change.
    private static McpToolErrorKind Classify(Exception exception) =>
        exception switch
        {
            UnauthorizedAccessException => McpToolErrorKind.NotAuthorized,
            ArgumentException
                or JsonException
                or FormatException
                or InvalidOperationException
                or NotFoundException
                or EventValidationException
                or McpException => McpToolErrorKind.InvalidArguments,
            _ => McpToolErrorKind.UnexpectedFailure
        };

    private static Exception Unwrap(Exception exception) =>
        exception switch
        {
            TargetInvocationException { InnerException: { } inner } =>
                Unwrap(inner),
            AggregateException { InnerExceptions.Count: 1 } aggregate =>
                Unwrap(aggregate.InnerExceptions[0]),
            _ => exception
        };

    private string Summary(string toolName) =>
        Kind switch
        {
            McpToolErrorKind.InvalidArguments =>
                $"The call to '{toolName}' failed because the arguments were not accepted.",
            McpToolErrorKind.NotAuthorized =>
                $"The call to '{toolName}' failed because it is not authorized.",
            _ =>
                $"The call to '{toolName}' failed because of an unexpected server error."
        };

    private string Guidance(string toolName) =>
        Kind switch
        {
            McpToolErrorKind.InvalidArguments =>
                "Compare these arguments with the tool's input schema and parameter descriptions. "
                + "Fix missing, empty, wrongly typed, or out-of-range values, for example an empty list "
                + "where at least one item is required, or an ID that does not exist. "
                + $"Then call '{toolName}' again with different arguments. "
                + "Do not stop the task because of this error.",
            McpToolErrorKind.NotAuthorized =>
                "Do not retry with the same request. Continue the task without this call "
                + "and tell the user that it was not authorized.",
            _ =>
                "This may not be caused by your arguments. Check them once and call "
                + $"'{toolName}' again. If it fails again with the same error, continue the "
                + "task without this tool and tell the user that it is failing."
        };

    private static string FormatArguments(
        IDictionary<string, JsonElement>? arguments
    )
    {
        if (arguments is null || arguments.Count == 0)
            return "none";

        var json = JsonSerializer.Serialize(arguments);

        return json.Length <= MaximumArgumentsLength
            ? json
            : $"{json[..MaximumArgumentsLength]}... (cut at {MaximumArgumentsLength} characters)";
    }
}
