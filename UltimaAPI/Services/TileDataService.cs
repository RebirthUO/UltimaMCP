using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class TileDataService
{
    private readonly UltimaSdkGateway _sdk;
    private readonly int _maxSearchResults;

    public TileDataService(UltimaSdkGateway sdk, IOptions<UltimaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sdk);
        ArgumentNullException.ThrowIfNull(options);

        _sdk = sdk;
        _maxSearchResults = options.Value.Limits.MaxSearchResults;
    }

    public TileDataSummary GetSummary()
    {
        _sdk.RequireCapability("tileData");

        return _sdk.Read(() => new TileDataSummary(TileData.LandTable.Length, TileData.ItemTable.Length));
    }

    public LandTileData GetLand(int id)
    {
        _sdk.RequireCapability("tileData");

        return _sdk.Read(() =>
        {
            RequestBounds.RequireInRange(id, 0, TileData.LandTable.Length - 1, nameof(id));
            return TileDataMapper.ToModel(id, TileData.LandTable[id]);
        });
    }

    public ItemTileData GetItem(int id)
    {
        _sdk.RequireCapability("tileData");

        return _sdk.Read(() =>
        {
            RequestBounds.RequireInRange(id, 0, TileData.ItemTable.Length - 1, nameof(id));
            return TileDataMapper.ToModel(id, TileData.ItemTable[id]);
        });
    }

    public PageResult<LandTileData> SearchLand(string? query, int offset, int? limit)
    {
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 50, _maxSearchResults);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        _sdk.RequireCapability("tileData");

        return _sdk.Read(() =>
        {
            List<LandTileData> matches = [];
            int total = 0;

            for (int id = 0; id < TileData.LandTable.Length; id++)
            {
                LandData data = TileData.LandTable[id];
                if (!(data.Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (total >= offset && matches.Count < take)
                {
                    matches.Add(TileDataMapper.ToModel(id, data));
                }

                total++;
            }

            return new PageResult<LandTileData>(offset, take, total, matches);
        });
    }

    public PageResult<ItemTileData> SearchItems(string? query, int offset, int? limit)
    {
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 50, _maxSearchResults);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        _sdk.RequireCapability("tileData");

        return _sdk.Read(() =>
        {
            List<ItemTileData> matches = [];
            int total = 0;

            for (int id = 0; id < TileData.ItemTable.Length; id++)
            {
                ItemData data = TileData.ItemTable[id];
                if (!(data.Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (total >= offset && matches.Count < take)
                {
                    matches.Add(TileDataMapper.ToModel(id, data));
                }

                total++;
            }

            return new PageResult<ItemTileData>(offset, take, total, matches);
        });
    }
}
