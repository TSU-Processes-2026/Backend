namespace Infrastructure.Files.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; init; } = "App_Data/uploads";
}
