using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Persistence;

public interface IProjectionReplayRepository
{
    Task<List<EventPayload>> GetLastEvents(
        string stateMachineId
    );
}
