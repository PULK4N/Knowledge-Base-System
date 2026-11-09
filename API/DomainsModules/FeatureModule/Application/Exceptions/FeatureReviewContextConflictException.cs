namespace FeatureModule.Application.Exceptions;

/// <summary>
/// The KB branch data does not identify exactly one review context for a branch.
/// The message tells the user what to fix in the KB; the API returns it as a conflict.
/// </summary>
public sealed class FeatureReviewContextConflictException(string message) : Exception(message);
