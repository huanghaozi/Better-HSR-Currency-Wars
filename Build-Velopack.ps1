[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$ReleaseNotes = ''
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $workspace '.devtools\dotnet\dotnet.exe'
$vpk = Join-Path $workspace '.devtools\velopack\vpk.exe'
$project = Join-Path $PSScriptRoot 'Better HSR-Currency Wars V11.csproj'
$publishDir = Join-Path $PSScriptRoot "artifacts\velopack-publish\$Version"
$releaseDir = Join-Path $PSScriptRoot 'VelopackReleases'

if (-not (Test-Path -LiteralPath $dotnet)) { throw "Portable .NET SDK not found: $dotnet" }
if (-not (Test-Path -LiteralPath $vpk)) { throw "Velopack CLI not found: $vpk" }
if (Test-Path -LiteralPath $publishDir) {
    throw "Publish directory already exists. Remove it only after confirming the target, or use a new version: $publishDir"
}

$env:DOTNET_ROOT = Split-Path -Parent $dotnet
$env:DOTNET_CLI_HOME = Join-Path $workspace '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $workspace '.nuget\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

& $dotnet publish $project -c $Configuration --no-restore -v minimal -o $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$packArgs = @(
    'pack',
    '--packId', 'BetterHSRCurrencyWars',
    '--packVersion', $Version,
    '--packDir', $publishDir,
    '--mainExe', 'Better HSR-Currency Wars V11.exe',
    '--packTitle', 'Better HSR-Currency Wars',
    '--packAuthors', '439awsl-hue',
    '--icon', (Join-Path $PSScriptRoot 'app.ico'),
    '--runtime', 'win-x64',
    '--delta', 'BestSize',
    '--outputDir', $releaseDir
)

if (-not [string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    $notesPath = if ([System.IO.Path]::IsPathRooted($ReleaseNotes)) { $ReleaseNotes } else { Join-Path $PSScriptRoot $ReleaseNotes }
    if (-not (Test-Path -LiteralPath $notesPath)) { throw "Release notes not found: $notesPath" }
    $packArgs += @('--releaseNotes', $notesPath)
}

& $vpk @packArgs
exit $LASTEXITCODE
