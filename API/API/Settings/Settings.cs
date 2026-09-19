namespace Api.Settings;

public sealed class Settings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public Theme Theme { get; set; }
}
