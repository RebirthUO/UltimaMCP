using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class MapService
{
    private static readonly string[] MapNames = ["Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno", "Ter Mur"];

    private readonly UltimaSdkGateway _sdk;
    private readonly int _maxRegionSize;

    public MapService(UltimaSdkGateway sdk, IOptions<UltimaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sdk);
        ArgumentNullException.ThrowIfNull(options);

        _sdk = sdk;
        _maxRegionSize = options.Value.Limits.MaxMapRegionSize;
    }

    public IReadOnlyList<MapDescriptor> GetMaps()
        => _sdk.Read(() => Enumerable.Range(0, MapNames.Length).Select(CreateDescriptor).ToArray());

    public MapCoordinate GetCoordinate(int mapId, int x, int y, bool includeStatics = true)
    {
        Map map = GetMap(mapId);
        ValidateCoordinate(map, x, y);
        EnsureLandAvailable(map);

        return _sdk.Read(() => ReadCoordinate(mapId, map, x, y, includeStatics));
    }

    public MapRegion GetRegion(int mapId, int x, int y, int width, int height, bool includeStatics = true)
    {
        Map map = GetMap(mapId);
        RequestBounds.RequireInRange(width, 1, _maxRegionSize, nameof(width));
        RequestBounds.RequireInRange(height, 1, _maxRegionSize, nameof(height));
        ValidateCoordinate(map, x, y);

        if (width > map.Width - x || height > map.Height - y)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The requested region extends beyond the map bounds.");
        }

        EnsureLandAvailable(map);

        return _sdk.Read(() =>
        {
            List<MapCoordinate> coordinates = new(width * height);
            for (int currentY = y; currentY < y + height; currentY++)
            {
                for (int currentX = x; currentX < x + width; currentX++)
                {
                    coordinates.Add(ReadCoordinate(mapId, map, currentX, currentY, includeStatics));
                }
            }

            return new MapRegion(mapId, x, y, width, height, coordinates);
        });
    }

    private MapCoordinate ReadCoordinate(int mapId, Map map, int x, int y, bool includeStatics)
    {
        Tile land = map.Tiles.GetLandTile(x, y);
        LandData? landData = TryGetLandData(land.ID);
        HuedTile[] statics = includeStatics && HasStatics(map)
            ? map.Tiles.GetStaticTiles(x, y)
            : [];

        MapLandTile landModel = new(
            land.ID,
            TileDataMapper.ToHexId(land.ID),
            land.Z,
            landData?.Name,
            landData?.TextureID,
            landData is LandData data ? TileDataMapper.ToFlags(data.Flags) : null);
        MapStaticTile[] staticModels = statics.Select(tile =>
        {
            ItemTileData? itemData = TryGetItemData(tile.ID);
            return new MapStaticTile(
                tile.ID,
                TileDataMapper.ToHexId(tile.ID),
                tile.Z,
                tile.Hue,
                itemData);
        }).ToArray();

        return new MapCoordinate(mapId, x, y, landModel, staticModels);
    }

    private MapDescriptor CreateDescriptor(int mapId)
    {
        Map map = GetMap(mapId);
        return new MapDescriptor(
            mapId,
            MapNames[mapId],
            map.FileIndex,
            map.Width,
            map.Height,
            HasLand(map),
            HasStatics(map));
    }

    private LandData? TryGetLandData(int id)
    {
        if (!_sdk.IsCapabilityAvailable("tileData") || id < 0 || id >= TileData.LandTable.Length)
        {
            return null;
        }

        return TileData.LandTable[id];
    }

    private ItemTileData? TryGetItemData(int id)
    {
        if (!_sdk.IsCapabilityAvailable("tileData") || id < 0 || id >= TileData.ItemTable.Length)
        {
            return null;
        }

        return TileDataMapper.ToModel(id, TileData.ItemTable[id]);
    }

    private void EnsureLandAvailable(Map map)
    {
        if (!HasLand(map))
        {
            throw new UltimaSdkUnavailableException($"Map data for file index {map.FileIndex} is unavailable.");
        }

        _sdk.RequireCapability("art");
    }

    private bool HasLand(Map map)
        => _sdk.IsFileAvailable($"map{map.FileIndex}.mul") || _sdk.IsFileAvailable($"map{map.FileIndex}legacymul.uop");

    private bool HasStatics(Map map)
        => _sdk.IsFileAvailable($"statics{map.FileIndex}.mul") && _sdk.IsFileAvailable($"staidx{map.FileIndex}.mul");

    private static void ValidateCoordinate(Map map, int x, int y)
    {
        RequestBounds.RequireInRange(x, 0, map.Width - 1, nameof(x));
        RequestBounds.RequireInRange(y, 0, map.Height - 1, nameof(y));
    }

    private static Map GetMap(int mapId) => mapId switch
    {
        0 => Map.Felucca,
        1 => Map.Trammel,
        2 => Map.Ilshenar,
        3 => Map.Malas,
        4 => Map.Tokuno,
        5 => Map.TerMur,
        _ => throw new ArgumentOutOfRangeException(nameof(mapId), mapId, "Map ID must be between 0 and 5.")
    };
}
