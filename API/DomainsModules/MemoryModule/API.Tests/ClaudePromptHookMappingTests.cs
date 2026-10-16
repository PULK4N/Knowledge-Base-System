using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.API.Mapping;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;
using UUIDNext;

namespace MemoryModule.API.Tests;

public sealed class ClaudePromptHookMappingTests
{
    private static readonly Guid SessionId = Guid.Parse(
        "019fb72e-e0c3-7452-b32b-5bbf65433c98"
    );

    private static readonly Guid TurnId = Guid.Parse(
        "019fb72e-e3c3-7093-a89d-050d309ca4ac"
    );

    private static readonly JsonElement Hook = JsonSerializer.SerializeToElement(
        new
        {
            session_id = SessionId,
            turn_id = TurnId,
            hook_event_name = "UserPromptSubmit",
            cwd = "/home/nikola/Documents/github/Knowledge-base-system",
            prompt = "Look at [Image #1]"
        }
    );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapTo_MapsIdentityAndKeepsOnlyTheHookAsPayload(bool wrapped)
    {
        var body = wrapped
            ? JsonSerializer.SerializeToElement(new { hook = Hook, attachments = Array.Empty<object>() })
            : Hook;
        var command = CreateCommand();

        body.MapTo(command);

        Assert.Equal(new ThreadId(SessionId), command.ThreadId);
        Assert.Equal(new PromptId(TurnId), command.PromptId);
        Assert.Equal("UserPromptSubmit", command.HookEventName);
        Assert.Equal(Hook.GetRawText(), command.Payload.GetRawText());
        Assert.Empty(command.Attachments);
    }

    [Fact]
    public void MapTo_WrappedBody_MapsAttachmentsWithNewFileIds()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var body = JsonSerializer.SerializeToElement(
            new
            {
                hook = Hook,
                attachments = new[]
                {
                    new
                    {
                        name = "image-1.png",
                        media_type = "image/png",
                        content = Convert.ToBase64String([1, 2, 3]),
                        placeholder = "[Image #1]"
                    }
                }
            }
        );
        var command = CreateCommand();

        body.MapTo(command);

        var upload = Assert.Single(command.Attachments);
        Assert.NotEqual(Guid.Empty, upload.Attachment.Id.Value);
        Assert.Equal(
            new MemoryAttachment
            {
                Id = upload.Attachment.Id,
                Name = "image-1.png",
                Size = 3,
                FileType = "image/png",
                Extension = "png",
                Placeholder = "[Image #1]"
            },
            upload.Attachment
        );
        Assert.Equal([1, 2, 3], upload.Content);
    }

    private static RecordClaudePromptHookCommand CreateCommand() =>
        new(null!, null!)
        {
            ThreadId = default,
            PromptId = default,
            HookEventName = string.Empty,
            Payload = default
        };
}
