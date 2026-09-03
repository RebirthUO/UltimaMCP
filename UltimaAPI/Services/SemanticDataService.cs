using System.Drawing;
using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Cliloc;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class SemanticDataService
{
    private static readonly IReadOnlyDictionary<int, string> VerdataFileNames = new Dictionary<int, string>
    {
        [0] = "map0.mul",
        [1] = "staidx0.mul",
        [2] = "statics0.mul",
        [3] = "artidx.mul",
        [4] = "art.mul",
        [5] = "anim.idx",
        [6] = "anim.mul",
        [7] = "soundidx.mul",
        [8] = "sound.mul",
        [9] = "texidx.mul",
        [10] = "texmaps.mul",
        [11] = "gumpidx.mul",
        [12] = "gumpart.mul",
        [13] = "multi.idx",
        [14] = "multi.mul",
        [15] = "skills.idx",
        [16] = "skills.mul",
        [30] = "tiledata.mul",
        [31] = "animdata.mul"
    };

    private readonly UltimaSdkGateway _sdk;
    private readonly int _maxSearchResults;
    private readonly Dictionary<string, ClilocFile> _clilocFiles = new(StringComparer.OrdinalIgnoreCase);

    public SemanticDataService(UltimaSdkGateway sdk, IOptions<UltimaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sdk);
        ArgumentNullException.ThrowIfNull(options);

        _sdk = sdk;
        _maxSearchResults = options.Value.Limits.MaxSearchResults;
    }

    public MultiData GetMulti(int id, int offset, int? limit)
    {
        _sdk.RequireCapability("multis");
        RequestBounds.RequireInRange(id, 0, 0x1FFF, nameof(id));
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        int take = RequestBounds.NormalizeLimit(limit, 250, 2_000);

        return _sdk.Read(() =>
        {
            MultiComponentList components = Multis.GetComponents(id);
            MultiComponentList.MultiTileEntry[] entries = components.SortedTiles ?? [];
            MultiComponentData[] models = entries.Skip(offset).Take(take).Select(entry =>
            {
                ItemTileData? itemData = null;
                if (_sdk.IsCapabilityAvailable("tileData") && entry.m_ItemID < TileData.ItemTable.Length)
                {
                    itemData = TileDataMapper.ToModel(entry.m_ItemID, TileData.ItemTable[entry.m_ItemID]);
                }

                return new MultiComponentData(
                    entry.m_ItemID,
                    TileDataMapper.ToHexId(entry.m_ItemID),
                    entry.m_OffsetX,
                    entry.m_OffsetY,
                    entry.m_OffsetZ,
                    TileDataMapper.ToFlags(entry.m_Flags),
                    itemData);
            }).ToArray();

            return new MultiData(
                id,
                TileDataMapper.ToHexId(id),
                components.Width,
                components.Height,
                components.maxHeight,
                components.Surface,
                components.Center.X,
                components.Center.Y,
                entries.Length,
                offset,
                take,
                models,
                components.Width > 0 && components.Height > 0 && _sdk.IsCapabilityAvailable("art")
                    ? $"/api/multis/{id}.png"
                    : null);
        });
    }

    public MediaAsset GetMultiPng(int id, int maximumHeight)
    {
        _sdk.RequireCapability("multis");
        _sdk.RequireCapability("art");
        RequestBounds.RequireInRange(id, 0, 0x1FFF, nameof(id));
        RequestBounds.RequireInRange(maximumHeight, 1, 1_000, nameof(maximumHeight));

        return _sdk.Read(() =>
        {
            Bitmap? bitmap = Multis.GetComponents(id).GetImage(maximumHeight);
            if (bitmap is null)
            {
                throw new KeyNotFoundException($"Multi {id} has no renderable components.");
            }

            using (bitmap)
            {
                return MediaEncoder.ToPng(bitmap, $"multi-{id:X4}.png");
            }
        });
    }

    public IReadOnlyList<SkillData> GetSkills()
    {
        _sdk.RequireCapability("skills");

        return _sdk.Read(() => Skills.SkillEntries.Select(skill =>
        {
            int? groupId = null;
            string? groupName = null;
            if (_sdk.IsCapabilityAvailable("skillGroups") && skill.Index < SkillGroups.SkillList.Count)
            {
                int candidate = SkillGroups.SkillList[skill.Index];
                if (candidate >= 0 && candidate < SkillGroups.List.Count)
                {
                    groupId = candidate;
                    groupName = SkillGroups.List[candidate].Name;
                }
            }

            return new SkillData(skill.Index, skill.Name, skill.IsAction, skill.Extra, groupId, groupName);
        }).ToArray());
    }

    public IReadOnlyList<SkillGroupData> GetSkillGroups()
    {
        _sdk.RequireCapability("skillGroups");

        return _sdk.Read(() => SkillGroups.List.Select((group, id) =>
        {
            int[] skillIds = SkillGroups.SkillList
                .Select((groupId, skillId) => (groupId, skillId))
                .Where(value => value.groupId == id)
                .Select(static value => value.skillId)
                .ToArray();
            return new SkillGroupData(id, group.Name ?? string.Empty, skillIds);
        }).ToArray());
    }

    public LocalizedStringData GetLocalizedString(string? language, int number)
    {
        string normalizedLanguage = NormalizeLanguage(language);
        RequestBounds.RequireInRange(number, 0, int.MaxValue, nameof(number));

        return _sdk.Read(() =>
        {
            ClilocFile cliloc = GetClilocFile(normalizedLanguage);
            ClilocEntry? entry = cliloc.TryGetEntry(number);
            if (entry is null)
            {
                throw new KeyNotFoundException($"Localized string {number} was not found in {normalizedLanguage}.");
            }

            return ToLocalizedString(normalizedLanguage, entry);
        });
    }

    public PageResult<LocalizedStringData> SearchLocalizedStrings(string? language, string? query, int offset, int? limit)
    {
        string normalizedLanguage = NormalizeLanguage(language);
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 50, _maxSearchResults);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));

        return _sdk.Read(() =>
        {
            ClilocEntry[] matches = GetClilocFile(normalizedLanguage).Entries
                .Where(entry => entry.Text.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            LocalizedStringData[] items = matches.Skip(offset).Take(take)
                .Select(entry => ToLocalizedString(normalizedLanguage, entry))
                .ToArray();
            return new PageResult<LocalizedStringData>(offset, take, matches.Length, items);
        });
    }

    public PageResult<SpeechData> SearchSpeech(string? query, int offset, int? limit)
    {
        string term = RequestBounds.RequireSearchTerm(query, nameof(query));
        int take = RequestBounds.NormalizeLimit(limit, 50, _maxSearchResults);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        _sdk.RequireCapability("speech");

        return _sdk.Read(() =>
        {
            SpeechEntry[] matches = SpeechList.Entries
                .Where(entry => entry.KeyWord.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            SpeechData[] items = matches.Skip(offset).Take(take)
                .Select(static entry => new SpeechData(entry.ID, entry.Order, entry.KeyWord))
                .ToArray();
            return new PageResult<SpeechData>(offset, take, matches.Length, items);
        });
    }

    public PageResult<VerdataPatchData> GetVerdataPatches(int offset, int? limit)
    {
        int take = RequestBounds.NormalizeLimit(limit, 100, 500);
        RequestBounds.RequireInRange(offset, 0, int.MaxValue, nameof(offset));
        _sdk.RequireCapability("verdata");

        return _sdk.Read(() =>
        {
            Entry5D[] patches = Verdata.Patches;
            VerdataPatchData[] items = patches.Skip(offset).Take(take).Select(static patch => new VerdataPatchData(
                patch.file,
                VerdataFileNames.GetValueOrDefault(patch.file, "unknown"),
                patch.index,
                patch.lookup,
                patch.length,
                patch.extra)).ToArray();
            return new PageResult<VerdataPatchData>(offset, take, patches.Length, items);
        });
    }

    public MultiMapInfo GetMultiMapInfo()
    {
        _sdk.RequireCapability("multiMap");

        return _sdk.Read(() =>
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                throw new PlatformNotSupportedException("Multi-map image processing requires Windows 7 or later.");
            }

            using Bitmap? bitmap = MultiMap.GetMultiMap();
            if (bitmap is null)
            {
                throw new KeyNotFoundException("The multi-map image was not found.");
            }

            return new MultiMapInfo("multimap", null, bitmap.Width, bitmap.Height, "/api/multimap.png");
        });
    }

    public MediaAsset GetMultiMapPng()
    {
        _sdk.RequireCapability("multiMap");

        return _sdk.Read(() =>
        {
            using Bitmap? bitmap = MultiMap.GetMultiMap();
            if (bitmap is null)
            {
                throw new KeyNotFoundException("The multi-map image was not found.");
            }

            return MediaEncoder.ToPng(bitmap, "multimap.png");
        });
    }

    public MediaAsset GetFacetPng(int facetId)
    {
        RequestBounds.RequireInRange(facetId, 0, 5, nameof(facetId));
        string fileName = $"facet0{facetId}.mul";
        if (!_sdk.IsFileAvailable(fileName))
        {
            throw new UltimaSdkUnavailableException($"Facet image file '{fileName}' is unavailable.");
        }

        return _sdk.Read(() =>
        {
            using Bitmap? bitmap = MultiMap.GetFacetImage(facetId);
            if (bitmap is null)
            {
                throw new KeyNotFoundException($"Facet image {facetId} was not found.");
            }

            return MediaEncoder.ToPng(bitmap, $"facet-{facetId}.png");
        });
    }

    private ClilocFile GetClilocFile(string language)
    {
        if (_clilocFiles.TryGetValue(language, out ClilocFile? existing))
        {
            return existing;
        }

        string? dataPath = _sdk.Diagnostics.DataPath;
        if (dataPath is null)
        {
            throw new UltimaSdkUnavailableException("The Ultima data path is unavailable.");
        }

        string path = Path.Combine(dataPath, $"cliloc.{language}");
        if (!File.Exists(path))
        {
            throw new UltimaSdkUnavailableException($"Localized string file 'cliloc.{language}' is unavailable.");
        }

        ClilocFile cliloc = ClilocFile.Load(language, path);
        _clilocFiles.Add(language, cliloc);
        return cliloc;
    }

    private static string NormalizeLanguage(string? language)
    {
        string value = string.IsNullOrWhiteSpace(language) ? "enu" : language.Trim().ToLowerInvariant();
        if (value.Length is < 3 or > 8 || value.Any(static character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("Language must contain 3 to 8 ASCII letters or digits.", nameof(language));
        }

        return value;
    }

    private static LocalizedStringData ToLocalizedString(string language, ClilocEntry entry)
        => new(language, entry.Number, entry.Text, entry.Flag.ToString());
}
