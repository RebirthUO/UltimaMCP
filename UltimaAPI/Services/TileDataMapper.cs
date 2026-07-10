using Ultima;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal static class TileDataMapper
{
    public static LandTileData ToModel(int id, LandData data)
        => new(
            id,
            ToHexId(id),
            data.Name ?? string.Empty,
            data.TextureID,
            ToFlags(data.Flags),
            data.Unk1);

    public static ItemTileData ToModel(int id, ItemData data)
        => new(
            id,
            ToHexId(id),
            data.Name ?? string.Empty,
            data.Animation,
            data.Weight,
            data.Quality,
            data.Quantity,
            data.Value,
            data.Hue,
            data.StackingOffset,
            data.Height,
            data.CalcHeight,
            data.MiscData,
            ToFlags(data.Flags),
            data.Unk1,
            data.Unk2,
            data.Unk3);

    public static TileFlags ToFlags(TileFlag flags)
    {
        string[] names = Enum.GetValues<TileFlag>()
            .Where(flag => flag != TileFlag.None && flags.HasFlag(flag))
            .Select(static flag => flag.ToString())
            .ToArray();
        ulong value = (ulong)flags;

        return new TileFlags(value, $"0x{value:X8}", names);
    }

    public static string ToHexId(int id) => $"0x{id:X4}";
}
