param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [string]$OutputRoot = "",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    $scriptRoot = Split-Path -Parent $PSCommandPath
    return (Resolve-Path (Join-Path $scriptRoot "..")).Path
}

function Resolve-Version {
    param([string]$RequestedVersion)

    if (-not [string]::IsNullOrWhiteSpace($RequestedVersion)) {
        return $RequestedVersion
    }

    $gitVersion = (& git describe --tags --always --dirty 2>$null)
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitVersion)) {
        return $gitVersion.Trim()
    }

    return "local"
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    return ($Value -replace '[^\w\.\-]+', '-').Trim("-")
}

function Clear-Directory {
    param(
        [string]$Path,
        [string]$RepoRoot
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)

    if (-not $fullPath.StartsWith($fullRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear a directory outside the repository: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath | Out-Null
}

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Publish-Birds {
    param(
        [string]$Project,
        [string]$Configuration,
        [string]$Runtime,
        [string]$Output,
        [bool]$SingleFile
    )

    $singleFileValue = if ($SingleFile) { "true" } else { "false" }
    $arguments = @(
        "publish",
        $Project,
        "-c", $Configuration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-o", $Output,
        "-p:PublishSingleFile=$singleFileValue",
        "-p:PublishTrimmed=false",
        "-p:DebugType=None",
        "-p:DebugSymbols=false"
    )

    if ($SingleFile) {
        $arguments += @(
            "-p:EnableCompressionInSingleFile=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true"
        )
    }

    Invoke-DotNet -Arguments $arguments
}

$repoRoot = Resolve-RepoRoot
$versionLabel = ConvertTo-SafeFileName (Resolve-Version $Version)
$project = Join-Path $repoRoot "Birds.App\Birds.App.csproj"
$tests = Join-Path $repoRoot "Birds.Tests\Birds.Tests.csproj"

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts"
}

$releaseRoot = Join-Path $OutputRoot "release"
$publishRoot = Join-Path $OutputRoot "publish"
$folderPublish = Join-Path $publishRoot "folder"
$singlePublish = Join-Path $publishRoot "single"

Clear-Directory -Path $releaseRoot -RepoRoot $repoRoot
Clear-Directory -Path $publishRoot -RepoRoot $repoRoot

Invoke-DotNet -Arguments @("restore", $project)

if (-not $SkipTests) {
    Invoke-DotNet -Arguments @("test", $tests, "-c", $Configuration, "--verbosity", "minimal")
}

Publish-Birds -Project $project -Configuration $Configuration -Runtime $Runtime -Output $folderPublish -SingleFile $false
Publish-Birds -Project $project -Configuration $Configuration -Runtime $Runtime -Output $singlePublish -SingleFile $true

$folderZip = Join-Path $releaseRoot "Birds-$versionLabel-$Runtime-folder.zip"
$singleZip = Join-Path $releaseRoot "Birds-$versionLabel-$Runtime-single.zip"
$singleExe = Join-Path $singlePublish "Birds.App.exe"

Compress-Archive -Path (Join-Path $folderPublish "*") -DestinationPath $folderZip -Force

if (-not (Test-Path -LiteralPath $singleExe)) {
    throw "Single-file publish did not produce $singleExe"
}

$singlePackageRoot = Join-Path $publishRoot "single-package"
Clear-Directory -Path $singlePackageRoot -RepoRoot $repoRoot
Copy-Item -LiteralPath $singleExe -Destination $singlePackageRoot
Compress-Archive -Path (Join-Path $singlePackageRoot "*") -DestinationPath $singleZip -Force

Write-Host "Created release artifacts:"
Write-Host "  $folderZip"
Write-Host "  $singleZip"
