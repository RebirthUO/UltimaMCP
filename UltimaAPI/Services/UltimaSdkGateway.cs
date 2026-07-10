using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Ultima;
using UltimaAPI.Configuration;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal sealed class UltimaSdkGateway
{
    private readonly object _syncRoot = new();
    private readonly IReadOnlyDictionary<string, SdkCapability> _capabilities;
    private readonly bool _enableWritableUopShadow;
    private readonly string _uopCachePath;
    private readonly ILogger<UltimaSdkGateway> _logger;

    public UltimaSdkGateway(IOptions<UltimaOptions> options, ILogger<UltimaSdkGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        UltimaOptions settings = options.Value;
        _logger = logger;
        _enableWritableUopShadow = settings.EnableWritableUopShadow;
        _uopCachePath = ResolveUopCachePath(settings.UopCachePath);
        string? dataPath = ResolveDataPath(settings.DataPath);

        if (dataPath is null)
        {
            Diagnostics = CreateUnavailableDiagnostics(settings.DataPath, "No Ultima client directory was configured or discovered.");
            _capabilities = Diagnostics.Capabilities.ToDictionary(static capability => capability.Name, StringComparer.OrdinalIgnoreCase);
            logger.LogWarning("Ultima SDK is unavailable because no client data directory was configured or discovered.");
            return;
        }

        if (!Directory.Exists(dataPath))
        {
            Diagnostics = CreateUnavailableDiagnostics(dataPath, "The configured Ultima client directory does not exist.");
            _capabilities = Diagnostics.Capabilities.ToDictionary(static capability => capability.Name, StringComparer.OrdinalIgnoreCase);
            logger.LogWarning("Ultima SDK data directory does not exist: {DataPath}", dataPath);
            return;
        }

        string fullPath = Path.GetFullPath(dataPath);
        Files.LoadMulPath();
        Files.SetMulPath(fullPath);
        Files.CacheData = settings.CacheData;
        Map.StartUpSetDiff(settings.UseMapDiffs);
        Files.CheckForNewMapSize();

        IReadOnlyList<SdkFileStatus> files = Files.MulPath.Keys
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(CreateFileStatus)
            .ToArray();
        IReadOnlyList<SdkCapability> capabilities = CreateCapabilities(files);

        Diagnostics = new SdkDiagnostics(
            true,
            capabilities.Any(static capability => !capability.Available),
            fullPath,
            files.Count(static file => file.Available),
            files.Count,
            capabilities,
            files,
            null);
        _capabilities = capabilities.ToDictionary(static capability => capability.Name, StringComparer.OrdinalIgnoreCase);

        logger.LogInformation(
            "Ultima SDK mapped to {DataPath}. {AvailableFileCount} of {KnownFileCount} known files are available.",
            fullPath,
            Diagnostics.AvailableFileCount,
            Diagnostics.KnownFileCount);
    }

    public SdkDiagnostics Diagnostics { get; }

    public bool IsFileAvailable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Diagnostics.Files.Any(file =>
            file.Available && string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsCapabilityAvailable(string name)
        => _capabilities.TryGetValue(name, out SdkCapability? capability) && capability.Available;

    public T Read<T>(Func<T> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        EnsureInitialized();

        lock (_syncRoot)
        {
            return read();
        }
    }

    public void Read(Action read)
    {
        ArgumentNullException.ThrowIfNull(read);
        EnsureInitialized();

        lock (_syncRoot)
        {
            read();
        }
    }

    public void RequireCapability(string name)
    {
        if (!_capabilities.TryGetValue(name, out SdkCapability? capability))
        {
            throw new ArgumentException($"Unknown Ultima SDK capability '{name}'.", nameof(name));
        }

        if (!capability.Available)
        {
            string missing = string.Join(", ", capability.MissingFiles);
            throw new UltimaSdkUnavailableException($"Ultima capability '{name}' is unavailable. Missing files: {missing}.");
        }

        PrepareUopForCapability(name);
    }

    private void PrepareUopForCapability(string name)
    {
        string? uopFile = name.ToLowerInvariant() switch
        {
            "tiledata" or "art" or "multis" => "artlegacymul.uop",
            "gumps" => "gumpartlegacymul.uop",
            "sounds" => "soundlegacymul.uop",
            _ => null
        };

        if (uopFile is not null && IsFileAvailable(uopFile))
        {
            EnsureWritableUop(uopFile);
        }
    }

    private void EnsureWritableUop(string fileName)
    {
        lock (_syncRoot)
        {
            string? sourcePath = Files.GetFilePath(fileName);
            if (sourcePath is null || CanOpenReadWrite(sourcePath))
            {
                return;
            }

            if (!_enableWritableUopShadow)
            {
                throw new UltimaSdkUnavailableException(
                    $"The legacy Ultima SDK requires read/write access to '{sourcePath}'. Enable Ultima:EnableWritableUopShadow or use a writable client directory.");
            }

            string installKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Diagnostics.DataPath!)))[..16];
            string cacheDirectory = Path.Combine(_uopCachePath, installKey);
            string shadowPath = Path.Combine(cacheDirectory, fileName.ToLowerInvariant());
            FileInfo source = new(sourcePath);
            FileInfo shadow = new(shadowPath);

            try
            {
                Directory.CreateDirectory(cacheDirectory);
                if (!shadow.Exists || shadow.Length != source.Length || shadow.LastWriteTimeUtc < source.LastWriteTimeUtc)
                {
                    string temporaryPath = $"{shadowPath}.{Guid.NewGuid():N}.tmp";
                    _logger.LogInformation(
                        "Creating writable shadow for legacy UOP access: {SourcePath} -> {ShadowPath}",
                        sourcePath,
                        shadowPath);
                    File.Copy(sourcePath, temporaryPath, true);
                    File.SetLastWriteTimeUtc(temporaryPath, source.LastWriteTimeUtc);
                    File.Move(temporaryPath, shadowPath, true);
                }

                Files.SetMulPath(shadowPath, fileName);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new IOException($"Unable to create writable UOP shadow '{shadowPath}'.", exception);
            }
        }
    }

    private void EnsureInitialized()
    {
        if (!Diagnostics.Initialized)
        {
            throw new UltimaSdkUnavailableException(Diagnostics.Error ?? "The Ultima SDK is not initialized.");
        }
    }

    private static string? ResolveDataPath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        }

        return string.IsNullOrWhiteSpace(Files.Directory) ? null : Files.Directory;
    }

    private static string ResolveUopCachePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath.Trim()));
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UltimaMCP", "UopCache");
    }

    private static bool CanOpenReadWrite(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static SdkFileStatus CreateFileStatus(string name)
    {
        string? path = Files.GetFilePath(name);
        return new SdkFileStatus(name, path is not null, path);
    }

    private static SdkDiagnostics CreateUnavailableDiagnostics(string? dataPath, string error)
    {
        IReadOnlyList<SdkCapability> capabilities = CreateCapabilities([]);
        return new SdkDiagnostics(false, true, dataPath, 0, 0, capabilities, [], error);
    }

    private static IReadOnlyList<SdkCapability> CreateCapabilities(IReadOnlyList<SdkFileStatus> files)
    {
        HashSet<string> available = files
            .Where(static file => file.Available)
            .Select(static file => file.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            CreateCapability("tileData", available, ["tiledata.mul"]),
            CreateCapability("maps", available, ["map0.mul"], ["map0legacymul.uop"]),
            CreateCapability("mapStatics", available, ["statics0.mul", "staidx0.mul"]),
            CreateCapability("art", available, ["art.mul", "artidx.mul"], ["artlegacymul.uop"]),
            CreateCapability("gumps", available, ["gumpart.mul", "gumpidx.mul"], ["gumpartlegacymul.uop"]),
            CreateCapability("hues", available, ["hues.mul"]),
            CreateCapability("textures", available, ["texmaps.mul", "texidx.mul"]),
            CreateCapability("lights", available, ["light.mul", "lightidx.mul"]),
            CreateCapability("sounds", available, ["sound.mul", "soundidx.mul"], ["soundlegacymul.uop"]),
            CreateCapability("animations", available, ["anim.mul", "anim.idx"]),
            CreateCapability("animationData", available, ["animdata.mul"]),
            CreateCapability("multis", available, ["multi.mul", "multi.idx"]),
            CreateCapability("skills", available, ["skills.mul", "skills.idx"]),
            CreateCapability("skillGroups", available, ["skillgrp.mul"]),
            CreateCapability("localizedStrings", available, ["cliloc.enu"]),
            CreateCapability("speech", available, ["speech.mul"]),
            CreateCapability("asciiFonts", available, ["fonts.mul"]),
            CreateCapability("unicodeFonts", available, ["unifont.mul"]),
            CreateCapability("radarColors", available, ["radarcol.mul"]),
            CreateCapability("multiMap", available, ["multimap.rle"]),
            CreateCapability("verdata", available, ["verdata.mul"])
        ];
    }

    private static SdkCapability CreateCapability(
        string name,
        HashSet<string> available,
        params string[][] alternatives)
    {
        string[]? matched = alternatives.FirstOrDefault(alternative => alternative.All(available.Contains));
        string[] closest = alternatives
            .OrderBy(alternative => alternative.Count(file => !available.Contains(file)))
            .First();
        string[] missing = matched is null
            ? closest.Where(file => !available.Contains(file)).ToArray()
            : [];
        string[] requirements = alternatives
            .Select(static alternative => string.Join(" + ", alternative))
            .ToArray();

        return new SdkCapability(name, matched is not null, requirements, missing);
    }
}
