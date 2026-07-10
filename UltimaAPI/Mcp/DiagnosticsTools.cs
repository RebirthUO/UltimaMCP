using System.ComponentModel;
using ModelContextProtocol.Server;
using UltimaAPI.Models;
using UltimaAPI.Services;

namespace UltimaAPI.Mcp;

[McpServerToolType]
internal sealed class DiagnosticsTools(UltimaSdkGateway sdk)
{
    [McpServerTool]
    [Description("Reports the configured Ultima client path, available files, and which asset domains can be queried. Call this first when an asset tool reports unavailable data.")]
    public SdkDiagnostics GetUltimaSdkDiagnostics() => sdk.Diagnostics;

    [McpServerTool]
    [Description("Lists every known Ultima client file and whether the SDK resolved it in the configured installation.")]
    public IReadOnlyList<SdkFileStatus> ListUltimaFiles() => sdk.Diagnostics.Files;
}
