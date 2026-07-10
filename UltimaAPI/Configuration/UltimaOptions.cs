namespace UltimaAPI.Configuration;

internal sealed class UltimaOptions
{
    public const string SectionName = "Ultima";

    public string? DataPath { get; init; }

    public bool CacheData { get; init; } = true;

    public bool UseMapDiffs { get; init; } = true;

    public bool EnableWritableUopShadow { get; init; } = true;

    public string? UopCachePath { get; init; }

    public UltimaLimits Limits { get; init; } = new();
}

internal sealed class UltimaLimits
{
    public int MaxSearchResults { get; init; } = 100;

    public int MaxMapRegionSize { get; init; } = 64;

    public int MaxMapRenderBlocks { get; init; } = 32;

    public int MaxAnimationFrames { get; init; } = 32;

    public int MaxTextLength { get; init; } = 512;
}
