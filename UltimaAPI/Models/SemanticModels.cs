namespace UltimaAPI.Models;

internal sealed record MultiComponentData(
    int ItemId,
    string HexItemId,
    int X,
    int Y,
    int Z,
    TileFlags Flags,
    ItemTileData? TileData);

internal sealed record MultiData(
    int Id,
    string HexId,
    int Width,
    int Height,
    int MaximumHeight,
    int Surface,
    int CenterX,
    int CenterY,
    int TotalComponents,
    int Offset,
    int Limit,
    IReadOnlyList<MultiComponentData> Components,
    string? MediaUrl);

internal sealed record SkillData(int Id, string Name, bool IsAction, int Extra, int? GroupId, string? GroupName);

internal sealed record SkillGroupData(int Id, string Name, IReadOnlyList<int> SkillIds);

internal sealed record LocalizedStringData(string Language, int Number, string Text, string Flag);

internal sealed record SpeechData(int Id, int Order, string Keyword);

internal sealed record VerdataPatchData(int FileId, string FileName, int Index, int Lookup, int Length, int Extra);

internal sealed record MultiMapInfo(string Kind, int? FacetId, int Width, int Height, string MediaUrl);
