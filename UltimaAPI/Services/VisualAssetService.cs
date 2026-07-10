using System.Drawing;
using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class VisualAssetService
{
    private readonly UltimaSdkGateway _sdk;
    private readonly int _maxMapRenderBlocks;
    private readonly int _maxTextLength;

    public VisualAssetService(UltimaSdkGateway sdk, IOptions<UltimaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sdk);
        ArgumentNullException.ThrowIfNull(options);

        _sdk = sdk;
        _maxMapRenderBlocks = options.Value.Limits.MaxMapRenderBlocks;
        _maxTextLength = options.Value.Limits.MaxTextLength;
    }

    public VisualAssetInfo GetLandArtInfo(int id)
    {
        _sdk.RequireCapability("art");
        RequestBounds.RequireInRange(id, 0, 0x3FFF, nameof(id));

        return _sdk.Read(() => CreateInfo("land-art", id, Art.GetLand(id, out bool patched), patched, $"/api/art/land/{id}.png"));
    }

    public MediaAsset GetLandArtPng(int id)
        => GetPng("art", id, 0, 0x3FFF, Art.GetLand, $"land-{id:X4}.png");

    public VisualAssetInfo GetStaticArtInfo(int id)
    {
        _sdk.RequireCapability("art");
        RequestBounds.RequireInRange(id, 0, Art.GetMaxItemID(), nameof(id));

        return _sdk.Read(() => CreateInfo("static-art", id, Art.GetStatic(id, out bool patched), patched, $"/api/art/static/{id}.png"));
    }

    public MediaAsset GetStaticArtPng(int id)
        => GetPng("art", id, 0, Art.GetMaxItemID(), static index => Art.GetStatic(index), $"static-{id:X4}.png");

    public VisualAssetInfo GetGumpInfo(int id)
    {
        _sdk.RequireCapability("gumps");
        RequestBounds.RequireInRange(id, 0, Gumps.GetCount() - 1, nameof(id));

        return _sdk.Read(() => CreateInfo("gump", id, Gumps.GetGump(id, out bool patched), patched, $"/api/gumps/{id}.png"));
    }

    public MediaAsset GetGumpPng(int id)
        => GetPng("gumps", id, 0, Gumps.GetCount() - 1, Gumps.GetGump, $"gump-{id:X4}.png");

    public VisualAssetInfo GetTextureInfo(int id)
    {
        _sdk.RequireCapability("textures");
        int maximum = Math.Min(Textures.GetIdxLength(), 0x4000) - 1;
        RequestBounds.RequireInRange(id, 0, maximum, nameof(id));

        return _sdk.Read(() => CreateInfo("texture", id, Textures.GetTexture(id, out bool patched), patched, $"/api/textures/{id}.png"));
    }

    public MediaAsset GetTexturePng(int id)
        => GetPng("textures", id, 0, Math.Min(Textures.GetIdxLength(), 0x4000) - 1, Textures.GetTexture, $"texture-{id:X4}.png");

    public VisualAssetInfo GetLightInfo(int id)
    {
        _sdk.RequireCapability("lights");
        RequestBounds.RequireInRange(id, 0, Math.Min(Light.GetCount(), 100) - 1, nameof(id));

        return _sdk.Read(() => CreateInfo("light", id, Light.GetLight(id), false, $"/api/lights/{id}.png"));
    }

    public MediaAsset GetLightPng(int id)
        => GetPng("lights", id, 0, Math.Min(Light.GetCount(), 100) - 1, Light.GetLight, $"light-{id:D3}.png");

    public HueData GetHue(int id)
    {
        _sdk.RequireCapability("hues");
        RequestBounds.RequireInRange(id, 0, Hues.List.Length - 1, nameof(id));

        return _sdk.Read(() =>
        {
            Hue hue = Hues.List[id];
            RgbColor[] colors = hue.Colors.Select(ToRgb).ToArray();
            return new HueData(id, hue.Name ?? string.Empty, (ushort)hue.TableStart, (ushort)hue.TableEnd, colors);
        });
    }

    public PageResult<HueData> SearchHues(string? query, int offset, int? limit)
    {
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 50, 100);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        _sdk.RequireCapability("hues");

        return _sdk.Read(() =>
        {
            int[] matches = Enumerable.Range(0, Hues.List.Length)
                .Where(id => (Hues.List[id].Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            HueData[] items = matches.Skip(offset).Take(take).Select(GetHueWithoutLock).ToArray();
            return new PageResult<HueData>(offset, take, matches.Length, items);
        });
    }

    public RadarColorData GetRadarColor(string kind, int id)
    {
        _sdk.RequireCapability("radarColors");
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        return _sdk.Read(() =>
        {
            short value = kind.ToLowerInvariant() switch
            {
                "land" => RadarCol.GetLandColor(RequestBounds.RequireInRange(id, 0, 0x3FFF, nameof(id))),
                "item" => RadarCol.GetItemColor(RequestBounds.RequireInRange(id, 0, 0xFFFF, nameof(id))),
                _ => throw new ArgumentException("Kind must be 'land' or 'item'.", nameof(kind))
            };

            return new RadarColorData(kind.ToLowerInvariant(), id, (ushort)value, ToRgb(value));
        });
    }

    public MediaAsset RenderMapPng(int mapId, int blockX, int blockY, int widthInBlocks, int heightInBlocks, bool includeStatics)
    {
        Map map = GetMap(mapId);
        RequestBounds.RequireInRange(widthInBlocks, 1, _maxMapRenderBlocks, nameof(widthInBlocks));
        RequestBounds.RequireInRange(heightInBlocks, 1, _maxMapRenderBlocks, nameof(heightInBlocks));
        RequestBounds.RequireInRange(blockX, 0, map.Width / 8 - 1, nameof(blockX));
        RequestBounds.RequireInRange(blockY, 0, map.Height / 8 - 1, nameof(blockY));
        if (widthInBlocks > map.Width / 8 - blockX || heightInBlocks > map.Height / 8 - blockY)
        {
            throw new ArgumentOutOfRangeException(nameof(widthInBlocks), "The requested render extends beyond the map bounds.");
        }

        EnsureMapAvailable(map);

        return _sdk.Read(() =>
        {
            using Bitmap bitmap = map.GetImage(blockX, blockY, widthInBlocks, heightInBlocks, includeStatics);
            return MediaEncoder.ToPng(bitmap, $"map-{mapId}-{blockX}-{blockY}.png");
        });
    }

    public IReadOnlyList<FontInfo> GetFonts()
    {
        List<FontInfo> fonts = [];

        if (_sdk.IsCapabilityAvailable("asciiFonts"))
        {
            fonts.AddRange(_sdk.Read(() => ASCIIText.Fonts.Select((font, id) =>
                new FontInfo("ascii", id, font?.Height ?? 0, font?.Characters.Length)).ToArray()));
        }

        if (_sdk.IsCapabilityAvailable("unicodeFonts"))
        {
            fonts.AddRange(_sdk.Read(() => UnicodeFonts.Fonts.Select((font, id) =>
                new FontInfo("unicode", id, font?.GetHeight("Ag") ?? 0, font?.Chars.Length)).ToArray()));
        }

        return fonts;
    }

    public MediaAsset RenderAsciiTextPng(int fontId, string? text)
    {
        _sdk.RequireCapability("asciiFonts");
        string value = ValidateText(text);
        RequestBounds.RequireInRange(fontId, 0, 9, nameof(fontId));

        return _sdk.Read(() =>
        {
            if (ASCIIText.Fonts[fontId] is null)
            {
                throw new KeyNotFoundException($"ASCII font {fontId} is not available.");
            }

            using Bitmap bitmap = ASCIIText.DrawText(fontId, value);
            return MediaEncoder.ToPng(bitmap, $"ascii-font-{fontId}.png");
        });
    }

    private MediaAsset GetPng(string capability, int id, int minimum, int maximum, Func<int, Bitmap> getBitmap, string fileName)
    {
        _sdk.RequireCapability(capability);
        RequestBounds.RequireInRange(id, minimum, maximum, nameof(id));

        return _sdk.Read(() =>
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                throw new PlatformNotSupportedException("Image processing requires Windows 7 or later.");
            }

            Bitmap? bitmap = getBitmap(id);
            if (bitmap is null)
            {
                throw new KeyNotFoundException($"{capability} asset {id} was not found.");
            }

            return MediaEncoder.ToPng(bitmap, fileName);
        });
    }

    private static VisualAssetInfo CreateInfo(string domain, int id, Bitmap? bitmap, bool patched, string mediaUrl)
    {
        if (bitmap is null)
        {
            throw new KeyNotFoundException($"{domain} asset {id} was not found.");
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException("Image processing requires Windows 7 or later.");
        }

        return new VisualAssetInfo(domain, id, TileDataMapper.ToHexId(id), bitmap.Width, bitmap.Height, patched, mediaUrl);
    }

    private HueData GetHueWithoutLock(int id)
    {
        Hue hue = Hues.List[id];
        return new HueData(
            id,
            hue.Name ?? string.Empty,
            (ushort)hue.TableStart,
            (ushort)hue.TableEnd,
            hue.Colors.Select(ToRgb).ToArray());
    }

    private string ValidateText(string? text)
    {
        string value = RequestBounds.RequireSearchTerm(text, nameof(text));
        if (value.Length > _maxTextLength)
        {
            throw new ArgumentOutOfRangeException(nameof(text), $"Text cannot exceed {_maxTextLength} characters.");
        }

        return value;
    }

    private void EnsureMapAvailable(Map map)
    {
        if (!_sdk.IsFileAvailable($"map{map.FileIndex}.mul") && !_sdk.IsFileAvailable($"map{map.FileIndex}legacymul.uop"))
        {
            throw new UltimaSdkUnavailableException($"Map data for file index {map.FileIndex} is unavailable.");
        }

        _sdk.RequireCapability("art");
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

    private static RgbColor ToRgb(short value)
    {
        Color color = Hues.HueToColor(value);
        return new RgbColor(color.R, color.G, color.B, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }
}
