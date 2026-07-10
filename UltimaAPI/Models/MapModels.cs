namespace UltimaAPI.Models;

internal sealed record MapDescriptor(
    int Id,
    string Name,
    int FileIndex,
    int Width,
    int Height,
    bool LandAvailable,
    bool StaticsAvailable);

internal sealed record MapLandTile(
    int Id,
    string HexId,
    int Z,
    string? Name,
    int? TextureId,
    TileFlags? Flags);

internal sealed record MapStaticTile(
    int Id,
    string HexId,
    int Z,
    int Hue,
    ItemTileData? TileData);

internal sealed record MapCoordinate(
    int MapId,
    int X,
    int Y,
    MapLandTile Land,
    IReadOnlyList<MapStaticTile> Statics);

internal sealed record MapRegion(
    int MapId,
    int X,
    int Y,
    int Width,
    int Height,
    IReadOnlyList<MapCoordinate> Coordinates);
