using System.ComponentModel;
using ModelContextProtocol.Server;
using UltimaAPI.Models;
using UltimaAPI.Services;

namespace UltimaAPI.Mcp;

[McpServerToolType]
internal sealed class SemanticTools(SemanticDataService data)
{
    [McpServerTool]
    [Description("Gets a bounded page of a multi structure's components, offsets, flags, semantic tile data, dimensions, and REST PNG URL.")]
    public MultiData GetMulti(int id, int offset = 0, int limit = 250)
        => data.GetMulti(id, offset, limit);

    [McpServerTool]
    [Description("Lists client skills with action status and skill-group mappings when available.")]
    public IReadOnlyList<SkillData> ListSkills() => data.GetSkills();

    [McpServerTool]
    [Description("Lists client skill groups and the skill IDs assigned to each group.")]
    public IReadOnlyList<SkillGroupData> ListSkillGroups() => data.GetSkillGroups();

    [McpServerTool]
    [Description("Gets one localized cliloc entry by language code and numeric cliloc number.")]
    public LocalizedStringData GetLocalizedString(string language, int number)
        => data.GetLocalizedString(language, number);

    [McpServerTool]
    [Description("Searches localized cliloc text for a language such as enu or deu.")]
    public PageResult<LocalizedStringData> SearchLocalizedStrings(
        string language,
        string query,
        int offset = 0,
        int limit = 50)
        => data.SearchLocalizedStrings(language, query, offset, limit);

    [McpServerTool]
    [Description("Searches speech.mul keywords used by classic client speech commands.")]
    public PageResult<SpeechData> SearchSpeech(string query, int offset = 0, int limit = 50)
        => data.SearchSpeech(query, offset, limit);

    [McpServerTool]
    [Description("Lists bounded verdata.mul patch metadata, including the patched source file and record index.")]
    public PageResult<VerdataPatchData> ListVerdataPatches(int offset = 0, int limit = 100)
        => data.GetVerdataPatches(offset, limit);

    [McpServerTool]
    [Description("Gets multi-map dimensions and its REST PNG URL.")]
    public MultiMapInfo GetMultiMap() => data.GetMultiMapInfo();
}
