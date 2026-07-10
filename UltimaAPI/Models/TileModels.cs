namespace UltimaAPI.Models;

internal sealed record PageResult<T>(int Offset, int Limit, int Total, IReadOnlyList<T> Items);

internal sealed record TileFlags(ulong Value, string HexValue, IReadOnlyList<string> Names);

internal sealed record LandTileData(
    int Id,
    string HexId,
    string Name,
    int TextureId,
    TileFlags Flags,
    int Unknown1);

internal sealed record ItemTileData(
    int Id,
    string HexId,
    string Name,
    int AnimationId,
    int Weight,
    int Quality,
    int Quantity,
    int Value,
    int Hue,
    int StackingOffset,
    int Height,
    int CalculatedHeight,
    int MiscData,
    TileFlags Flags,
    int Unknown1,
    int Unknown2,
    int Unknown3);

internal sealed record TileDataSummary(int LandTileCount, int ItemTileCount);
