namespace PolicyModule.Application;

public static class Constants
{
    public static class StateMachineIds
    {
        public const string GeneralPolicies =
            "general-policies-state-machine";

        public const string ProjectPolicies =
            "project-policies-state-machine";

        public const string RepositoryToProjectMap =
            "repository-to-project-map-state-machine";
    }
}
