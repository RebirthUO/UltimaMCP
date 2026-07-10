namespace UltimaAPI.Models;

internal sealed record MediaAsset(byte[] Content, string ContentType, string FileName);

internal sealed record VisualAssetInfo(
    string Domain,
    int Id,
    string HexId,
    int Width,
    int Height,
    bool Patched,
    string MediaUrl);

internal sealed record RgbColor(int Red, int Green, int Blue, string Hex);

internal sealed record HueData(
    int Id,
    string Name,
    int TableStart,
    int TableEnd,
    IReadOnlyList<RgbColor> Colors);

internal sealed record RadarColorData(string Kind, int Id, int RawValue, RgbColor Color);

internal sealed record FontInfo(string Kind, int Id, int Height, int? CharacterCount);
