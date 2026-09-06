namespace OutboxProcessingModule.Application;

/// <summary>
/// Publishing a claimed set failed. A failed commit can also mean the outcome
/// is unknown, so the rows are returned to New and may be published twice.
/// </summary>
public sealed class BatchPublicationException(Exception innerException)
    : Exception("Publishing the claimed outbox rows failed.", innerException);
