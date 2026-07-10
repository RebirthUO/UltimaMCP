namespace UltimaAPI.Models;

internal sealed record SdkFileStatus(string Name, bool Available, string? Path);

internal sealed record SdkCapability(
    string Name,
    bool Available,
    IReadOnlyList<string> RequiredFiles,
    IReadOnlyList<string> MissingFiles);

internal sealed record SdkDiagnostics(
    bool Initialized,
    bool Degraded,
    string? DataPath,
    int AvailableFileCount,
    int KnownFileCount,
    IReadOnlyList<SdkCapability> Capabilities,
    IReadOnlyList<SdkFileStatus> Files,
    string? Error);
