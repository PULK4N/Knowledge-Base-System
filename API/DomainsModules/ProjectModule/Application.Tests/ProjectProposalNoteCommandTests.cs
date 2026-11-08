using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Commands;
using ProjectModule.Application.DTOs;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Tests;

public sealed partial class ProjectCommandTests
{
    [Fact]
    public async Task ProposalNotes_AreAddedUpdatedListedAndRemoved()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Proposal project");

        var added = Assert.IsType<ProposalNoteAddedCommandResult>(
            await ExecuteAsUser(new AddProjectProposalNoteCommand(handler)
            {
                ProjectId = projectId.Value,
                Title = "  Skill visibility  ",
                Content = "Hide skills from other projects."
            })
        );
        await ExecuteAsUser(new UpdateProjectProposalNoteCommand(handler)
        {
            ProjectId = projectId.Value,
            NoteId = added.NoteId,
            Title = "Skill visibility per project",
            Content = "Global or linked projects only."
        });

        var note = Assert.Single(await ProposalNotes(eventStore, projectId));
        Assert.Equal(added.NoteId, note.NoteId);
        Assert.Equal("Skill visibility per project", note.Title);
        Assert.Equal("Global or linked projects only.", note.Content);
        Assert.True(note.UpdatedAt >= note.CreatedAt);

        await ExecuteAsUser(new RemoveProjectProposalNoteCommand(handler)
        {
            ProjectId = projectId.Value,
            NoteId = added.NoteId
        });

        Assert.Empty(await ProposalNotes(eventStore, projectId));
    }

    [Fact]
    public async Task ProposalNote_WithoutTitleOrUnknownId_IsRejectedWithoutWrite()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Rejected proposal project");
        var writeCount = eventStore.WriteCount;

        var missingTitle = await Assert.ThrowsAsync<EventValidationException>(
            () => ExecuteAsUser(new AddProjectProposalNoteCommand(handler)
            {
                ProjectId = projectId.Value,
                Title = "   "
            })
        );
        var unknownNote = await Assert.ThrowsAsync<EventValidationException>(
            () => ExecuteAsUser(new RemoveProjectProposalNoteCommand(handler)
            {
                ProjectId = projectId.Value,
                NoteId = Guid.NewGuid()
            })
        );

        Assert.Contains("needs a title", missingTitle.Message);
        Assert.Contains("does not exist", unknownNote.Message);
        Assert.Equal(writeCount, eventStore.WriteCount);
    }

    private static async Task<List<ProposalNoteDto>> ProposalNotes(
        CapturingEventStoreWithOutbox eventStore,
        AggregateId projectId
    ) =>
        Assert.IsType<PolicyProjectDetailsDto>(
            await new GetPolicyProjectQuery(
                CreateCalculator(),
                eventStore,
                new StubSkillSummaryRepository()
            ) { ProjectId = projectId.Value }.Execute(Executor)
        ).ProposalNotes;
}
