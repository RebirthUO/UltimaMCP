using System.ComponentModel;
using ModelContextProtocol.Server;
using UltimaAPI.Models;
using UltimaAPI.Services;

namespace UltimaAPI.Mcp;

[McpServerToolType]
internal sealed class TileMapTools(TileDataService tiles, MapService maps)
{
    [McpServerTool]
    [Description("Gets complete semantic tiledata.mul metadata for an item tile ID, including numeric fields and expanded tile flags.")]
    public ItemTileData GetItemTile([Description("Item tile ID in decimal, such as 0x0EED expressed as 3821.")] int id)
        => tiles.GetItem(id);

    [McpServerTool]
    [Description("Gets complete semantic tiledata.mul metadata for a land tile ID.")]
    public LandTileData GetLandTile([Description("Land tile ID from 0 through 16383.")] int id)
        => tiles.GetLand(id);

    [McpServerTool]
    [Description("Searches item tile names and returns bounded, paged semantic tiledata results.")]
    public PageResult<ItemTileData> SearchItemTiles(
        [Description("Case-insensitive item name fragment.")] string query,
        [Description("Zero-based matching-result offset.")] int offset = 0,
        [Description("Maximum results to return; server limits apply.")] int limit = 50)
        => tiles.SearchItems(query, offset, limit);

    [McpServerTool]
    [Description("Searches land tile names and returns bounded, paged semantic tiledata results.")]
    public PageResult<LandTileData> SearchLandTiles(string query, int offset = 0, int limit = 50)
        => tiles.SearchLand(query, offset, limit);

    [McpServerTool]
    [Description("Lists UO facets with dimensions, underlying file indexes, and land/static availability.")]
    public IReadOnlyList<MapDescriptor> ListMaps() => maps.GetMaps();

    [McpServerTool]
    [Description("Inspects one UO world coordinate, returning land elevation and semantic static item records.")]
    public MapCoordinate InspectMapCoordinate(
        [Description("Facet ID: 0 Felucca, 1 Trammel, 2 Ilshenar, 3 Malas, 4 Tokuno, 5 Ter Mur.")] int mapId,
        int x,
        int y,
        bool includeStatics = true)
        => maps.GetCoordinate(mapId, x, y, includeStatics);

    [McpServerTool]
    [Description("Reads a bounded rectangular coordinate region for terrain, elevation, wet/impassable flags, and statics.")]
    public MapRegion InspectMapRegion(
        int mapId,
        int x,
        int y,
        int width,
        int height,
        bool includeStatics = true)
        => maps.GetRegion(mapId, x, y, width, height, includeStatics);
}
