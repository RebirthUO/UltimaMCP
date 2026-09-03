[CmdletBinding()]
param(
    [Alias('c')]
    [string]$Configuration = 'Debug',

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet was not found on PATH. Install the .NET SDK and retry.'
    exit 1
}

$DllPath = Join-Path $RepoRoot 'Ultima.dll'
if (-not (Test-Path -LiteralPath $DllPath)) {
    Write-Error "Ultima.dll is missing at repo root ($DllPath). UltimaAPI references ..\Ultima.dll."
    exit 1
}

$Project = Join-Path $RepoRoot 'UltimaAPI\UltimaAPI.csproj'
& dotnet build $Project -c $Configuration @RemainingArgs
exit $LASTEXITCODE
