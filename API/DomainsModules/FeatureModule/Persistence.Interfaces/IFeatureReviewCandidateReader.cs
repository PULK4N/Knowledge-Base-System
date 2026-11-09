namespace FeatureModule.Persistence.Interfaces;

public interface IFeatureReviewCandidateReader
{
    Task<List<Guid>> ListFeatureIds(Guid projectId);
}
