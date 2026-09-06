using EventSourcing.Persistence.Models;

namespace OutboxProcessingModule.Application;

public interface IOutboxQueueResolver
{
    List<string> ResolveQueues(SerializedPayloadMessage row);

    List<OutboxDispatch> Resolve(List<SerializedPayloadMessage> rows);
}
