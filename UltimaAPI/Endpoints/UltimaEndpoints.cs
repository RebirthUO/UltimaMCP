using UltimaAPI.Models;
using UltimaAPI.Services;

namespace UltimaAPI.Endpoints;

internal static class UltimaEndpoints
{
    public static IEndpointRouteBuilder MapUltimaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder api = endpoints.MapGroup("/api");

        MapDiagnostics(api);
        MapTileData(api);
        MapMaps(api);
        MapVisualAssets(api);
        MapAudioAndAnimations(api);
        MapSemanticData(api);

        return endpoints;
    }

    private static void MapDiagnostics(RouteGroupBuilder api)
    {
        RouteGroupBuilder sdk = api.MapGroup("/sdk").WithTags("SDK diagnostics");

        sdk.MapGet("/health", (UltimaSdkGateway gateway) => Results.Ok(new
        {
            status = "healthy",
            mcpProtocol = "streamable-http",
            mcpEndpoint = "/mcp",
            sdkInitialized = gateway.Diagnostics.Initialized,
            sdkDegraded = gateway.Diagnostics.Degraded
        })).WithName("GetHealth");

        sdk.MapGet("/diagnostics", (UltimaSdkGateway gateway) => Results.Ok(gateway.Diagnostics))
            .WithName("GetSdkDiagnostics");

        sdk.MapGet("/files", (UltimaSdkGateway gateway) => Results.Ok(gateway.Diagnostics.Files))
            .WithName("GetSdkFiles");
    }

    private static void MapTileData(RouteGroupBuilder api)
    {
        RouteGroupBuilder tiles = api.MapGroup("/tiles").WithTags("Tile data");

        tiles.MapGet("/summary", (TileDataService service) => Results.Ok(service.GetSummary()))
            .WithName("GetTileDataSummary");
        tiles.MapGet("/land/{id:int}", (int id, TileDataService service) => Results.Ok(service.GetLand(id)))
            .WithName("GetLandTileData");
        tiles.MapGet("/items/{id:int}", (int id, TileDataService service) => Results.Ok(service.GetItem(id)))
            .WithName("GetItemTileData");
        tiles.MapGet("/land/search", (string query, int offset, int? limit, TileDataService service) =>
                Results.Ok(service.SearchLand(query, offset, limit)))
            .WithName("SearchLandTileData");
        tiles.MapGet("/items/search", (string query, int offset, int? limit, TileDataService service) =>
                Results.Ok(service.SearchItems(query, offset, limit)))
            .WithName("SearchItemTileData");
    }

    private static void MapMaps(RouteGroupBuilder api)
    {
        RouteGroupBuilder maps = api.MapGroup("/maps").WithTags("Maps");

        maps.MapGet("/", (MapService service) => Results.Ok(service.GetMaps()))
            .WithName("GetMaps");
        maps.MapGet("/{mapId:int}/coordinates/{x:int}/{y:int}",
                (int mapId, int x, int y, bool includeStatics, MapService service) =>
                    Results.Ok(service.GetCoordinate(mapId, x, y, includeStatics)))
            .WithName("GetMapCoordinate");
        maps.MapGet("/{mapId:int}/regions/{x:int}/{y:int}",
                (int mapId, int x, int y, int width, int height, bool includeStatics, MapService service) =>
                    Results.Ok(service.GetRegion(mapId, x, y, width, height, includeStatics)))
            .WithName("GetMapRegion");
        maps.MapGet("/{mapId:int}/render.png",
                (int mapId, int blockX, int blockY, int width, int height, bool includeStatics, VisualAssetService service) =>
                    ToFile(service.RenderMapPng(mapId, blockX, blockY, width, height, includeStatics)))
            .WithName("RenderMapPng");
    }

    private static void MapVisualAssets(RouteGroupBuilder api)
    {
        RouteGroupBuilder art = api.MapGroup("/art").WithTags("Art");
        art.MapGet("/land/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetLandArtInfo(id)))
            .WithName("GetLandArtInfo");
        art.MapGet("/land/{id:int}.png", (int id, VisualAssetService service) => ToFile(service.GetLandArtPng(id)))
            .WithName("GetLandArtPng");
        art.MapGet("/static/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetStaticArtInfo(id)))
            .WithName("GetStaticArtInfo");
        art.MapGet("/static/{id:int}.png", (int id, VisualAssetService service) => ToFile(service.GetStaticArtPng(id)))
            .WithName("GetStaticArtPng");

        RouteGroupBuilder gumps = api.MapGroup("/gumps").WithTags("Gumps");
        gumps.MapGet("/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetGumpInfo(id)))
            .WithName("GetGumpInfo");
        gumps.MapGet("/{id:int}.png", (int id, VisualAssetService service) => ToFile(service.GetGumpPng(id)))
            .WithName("GetGumpPng");

        RouteGroupBuilder textures = api.MapGroup("/textures").WithTags("Textures");
        textures.MapGet("/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetTextureInfo(id)))
            .WithName("GetTextureInfo");
        textures.MapGet("/{id:int}.png", (int id, VisualAssetService service) => ToFile(service.GetTexturePng(id)))
            .WithName("GetTexturePng");

        RouteGroupBuilder lights = api.MapGroup("/lights").WithTags("Lights");
        lights.MapGet("/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetLightInfo(id)))
            .WithName("GetLightInfo");
        lights.MapGet("/{id:int}.png", (int id, VisualAssetService service) => ToFile(service.GetLightPng(id)))
            .WithName("GetLightPng");

        RouteGroupBuilder hues = api.MapGroup("/hues").WithTags("Hues and radar colors");
        hues.MapGet("/{id:int}", (int id, VisualAssetService service) => Results.Ok(service.GetHue(id)))
            .WithName("GetHue");
        hues.MapGet("/search", (string query, int offset, int? limit, VisualAssetService service) =>
                Results.Ok(service.SearchHues(query, offset, limit)))
            .WithName("SearchHues");
        hues.MapGet("/radar/{kind}/{id:int}", (string kind, int id, VisualAssetService service) =>
                Results.Ok(service.GetRadarColor(kind, id)))
            .WithName("GetRadarColor");

        RouteGroupBuilder fonts = api.MapGroup("/fonts").WithTags("Fonts");
        fonts.MapGet("/", (VisualAssetService service) => Results.Ok(service.GetFonts()))
            .WithName("GetFonts");
        fonts.MapGet("/ascii/{fontId:int}/render.png", (int fontId, string text, VisualAssetService service) =>
                ToFile(service.RenderAsciiTextPng(fontId, text)))
            .WithName("RenderAsciiFontPng");
    }

    private static void MapAudioAndAnimations(RouteGroupBuilder api)
    {
        RouteGroupBuilder sounds = api.MapGroup("/sounds").WithTags("Sounds");
        sounds.MapGet("/{id:int}", (int id, AudioAnimationService service) => Results.Ok(service.GetSoundInfo(id)))
            .WithName("GetSoundInfo");
        sounds.MapGet("/{id:int}.wav", (int id, AudioAnimationService service) => ToFile(service.GetSoundWav(id)))
            .WithName("GetSoundWav");
        sounds.MapGet("/search", (string query, int offset, int? limit, AudioAnimationService service) =>
                Results.Ok(service.SearchSounds(query, offset, limit)))
            .WithName("SearchSounds");

        RouteGroupBuilder animations = api.MapGroup("/animations").WithTags("Animations");
        animations.MapGet("/sets", (AudioAnimationService service) => Results.Ok(service.GetAnimationSets()))
            .WithName("GetAnimationSets");
        animations.MapGet("/{fileType:int}/{body:int}/{action:int}/{direction:int}",
                (int fileType, int body, int action, int direction, AudioAnimationService service) =>
                    Results.Ok(service.GetAnimationInfo(fileType, body, action, direction)))
            .WithName("GetAnimationInfo");
        animations.MapGet("/{fileType:int}/{body:int}/{action:int}/{direction:int}/frames/{frameIndex:int}.png",
                (int fileType, int body, int action, int direction, int frameIndex, AudioAnimationService service) =>
                    ToFile(service.GetAnimationFramePng(fileType, body, action, direction, frameIndex)))
            .WithName("GetAnimationFramePng");
        animations.MapGet("/data/{id:int}", (int id, AudioAnimationService service) =>
                Results.Ok(service.GetAnimationData(id)))
            .WithName("GetAnimationData");
    }

    private static void MapSemanticData(RouteGroupBuilder api)
    {
        RouteGroupBuilder multis = api.MapGroup("/multis").WithTags("Multis");
        multis.MapGet("/{id:int}", (int id, int offset, int? limit, SemanticDataService service) =>
                Results.Ok(service.GetMulti(id, offset, limit)))
            .WithName("GetMulti");
        multis.MapGet("/{id:int}.png", (int id, int maximumHeight, SemanticDataService service) =>
                ToFile(service.GetMultiPng(id, maximumHeight)))
            .WithName("GetMultiPng");

        RouteGroupBuilder skills = api.MapGroup("/skills").WithTags("Skills");
        skills.MapGet("/", (SemanticDataService service) => Results.Ok(service.GetSkills()))
            .WithName("GetSkills");
        skills.MapGet("/groups", (SemanticDataService service) => Results.Ok(service.GetSkillGroups()))
            .WithName("GetSkillGroups");

        RouteGroupBuilder strings = api.MapGroup("/strings").WithTags("Localized strings");
        strings.MapGet("/{language}/{number:int}", (string language, int number, SemanticDataService service) =>
                Results.Ok(service.GetLocalizedString(language, number)))
            .WithName("GetLocalizedString");
        strings.MapGet("/{language}/search", (string language, string query, int offset, int? limit, SemanticDataService service) =>
                Results.Ok(service.SearchLocalizedStrings(language, query, offset, limit)))
            .WithName("SearchLocalizedStrings");

        api.MapGet("/speech/search", (string query, int offset, int? limit, SemanticDataService service) =>
                Results.Ok(service.SearchSpeech(query, offset, limit)))
            .WithTags("Speech").WithName("SearchSpeech");
        api.MapGet("/verdata", (int offset, int? limit, SemanticDataService service) =>
                Results.Ok(service.GetVerdataPatches(offset, limit)))
            .WithTags("Verdata").WithName("GetVerdataPatches");
        api.MapGet("/multimap", (SemanticDataService service) => Results.Ok(service.GetMultiMapInfo()))
            .WithTags("Map images").WithName("GetMultiMapInfo");
        api.MapGet("/multimap.png", (SemanticDataService service) => ToFile(service.GetMultiMapPng()))
            .WithTags("Map images").WithName("GetMultiMapPng");
        api.MapGet("/facets/{facetId:int}.png", (int facetId, SemanticDataService service) =>
                ToFile(service.GetFacetPng(facetId)))
            .WithTags("Map images").WithName("GetFacetPng");
    }

    private static IResult ToFile(MediaAsset media)
        => Results.File(media.Content, media.ContentType, media.FileName, enableRangeProcessing: false);
}
