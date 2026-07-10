using System.ComponentModel;
using ModelContextProtocol.Server;
using UltimaAPI.Models;
using UltimaAPI.Services;

namespace UltimaAPI.Mcp;

[McpServerToolType]
internal sealed class VisualAudioTools(
    VisualAssetService visuals,
    AudioAnimationService media)
{
    [McpServerTool]
    [Description("Gets dimensions, patch state, and the REST PNG URL for land art.")]
    public VisualAssetInfo GetLandArt(int id) => visuals.GetLandArtInfo(id);

    [McpServerTool]
    [Description("Gets dimensions, patch state, and the REST PNG URL for static item art.")]
    public VisualAssetInfo GetStaticArt(int id) => visuals.GetStaticArtInfo(id);

    [McpServerTool]
    [Description("Gets dimensions, patch state, and the REST PNG URL for a gump UI asset.")]
    public VisualAssetInfo GetGump(int id) => visuals.GetGumpInfo(id);

    [McpServerTool]
    [Description("Gets dimensions, patch state, and the REST PNG URL for a terrain texture.")]
    public VisualAssetInfo GetTexture(int id) => visuals.GetTextureInfo(id);

    [McpServerTool]
    [Description("Gets dimensions and the REST PNG URL for a light mask.")]
    public VisualAssetInfo GetLight(int id) => visuals.GetLightInfo(id);

    [McpServerTool]
    [Description("Gets a complete 32-color UO hue palette as RGB values.")]
    public HueData GetHue(int id) => visuals.GetHue(id);

    [McpServerTool]
    [Description("Searches hue names and returns RGB palettes.")]
    public PageResult<HueData> SearchHues(string query, int offset = 0, int limit = 50)
        => visuals.SearchHues(query, offset, limit);

    [McpServerTool]
    [Description("Gets the radar color used for a land or item tile.")]
    public RadarColorData GetRadarColor(string kind, int id) => visuals.GetRadarColor(kind, id);

    [McpServerTool]
    [Description("Lists ASCII and Unicode font slots available from the client files. Rendered ASCII text is available through the REST media endpoint.")]
    public IReadOnlyList<FontInfo> ListFonts() => visuals.GetFonts();

    [McpServerTool]
    [Description("Gets sound name, duration, translation state, and its REST WAV URL.")]
    public SoundInfo GetSound(int id) => media.GetSoundInfo(id);

    [McpServerTool]
    [Description("Searches UO sound names and returns metadata plus REST WAV URLs.")]
    public PageResult<SoundInfo> SearchSounds(string query, int offset = 0, int limit = 25)
        => media.SearchSounds(query, offset, limit);

    [McpServerTool]
    [Description("Lists installed animation MUL sets and body counts.")]
    public IReadOnlyList<AnimationSetInfo> ListAnimationSets() => media.GetAnimationSets();

    [McpServerTool]
    [Description("Gets bounded animation frame metadata and REST PNG URLs for a body/action/direction in a specific anim file set.")]
    public AnimationInfo GetAnimation(int fileType, int body, int action, int direction)
        => media.GetAnimationInfo(fileType, body, action, direction);

    [McpServerTool]
    [Description("Gets animdata.mul timing and frame-sequence metadata for an animated item tile.")]
    public AnimationDataInfo GetItemAnimationData(int id) => media.GetAnimationData(id);
}
