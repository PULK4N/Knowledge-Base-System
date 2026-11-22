using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Commands;
using MemoryModule.Application.Models;

namespace MemoryModule.Application.Tests;

public sealed class MemoryCommandAuthorizationTests
{
    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, "memory.read", false)]
    [InlineData(false, "memory.write", true)]
    [InlineData(true, null, true)]
    public async Task MemoryCommands_RequireMemoryWrite(bool isAdministrator, string? permission, bool expected)
    {
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()),
            IsAdministrator = isAdministrator,
            Permissions = permission is null ? [] : [new Permission(permission)]
        };

        Assert.Equal(expected, await new AnyMemoryCommand().IsAuthorized(executor));
    }

    /// <summary>Stands for every memory command: the rule lives in their shared base class.</summary>
    private sealed class AnyMemoryCommand : MemoryCommand
    {
        protected override Task<MemoryCommandResult> ExecuteInternal(Executor executor) =>
            Task.FromResult(MemoryCommandResult.Ok);
    }
}
