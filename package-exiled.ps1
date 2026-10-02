param(
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = (Resolve-Path -LiteralPath (Split-Path -Parent $MyInvocation.MyCommand.Path)).Path
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$dll = Join-Path $root 'AutoEvent/bin/Release/net48/AutoEvent.dll'
$mer = Join-Path $root 'dependencies/ProjectMER.dll'
$harmony = Join-Path $root 'dependencies/Harmony-Fat.2.3.6.0/net48/0Harmony.dll'
$exiled = Join-Path $root 'dependencies/Exiled-9.14.2.tar.gz'

foreach ($path in @($dll, $mer, $harmony, $exiled)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Отсутствует файл для пакета: $path" }
}

function Add-ZipFile([IO.Compression.ZipArchive] $archive, [string] $source, [string] $name) {
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive, $source, $name.Replace('\', '/'),
        [IO.Compression.CompressionLevel]::Optimal) | Out-Null
}

function Add-ZipTree([IO.Compression.ZipArchive] $archive, [string] $directory) {
    $base = Join-Path $root $directory
    Get-ChildItem -LiteralPath $base -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        if ($relative -notmatch '(^|/)(bin|obj|\.git)/') {
            Add-ZipFile $archive $_.FullName $relative
        }
    }
}

function New-Package([string] $destination, [scriptblock] $fill) {
    $temporary = Join-Path $output ([IO.Path]::GetRandomFileName() + '.zip')
    try {
        $archive = [IO.Compression.ZipFile]::Open($temporary, [IO.Compression.ZipArchiveMode]::Create)
        try { & $fill $archive } finally { $archive.Dispose() }
        Move-Item -LiteralPath $temporary -Destination $destination -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    }
}

New-Package (Join-Path $output 'AutoEvent-EXILED-9.14.2-install.zip') {
    param($archive)
    Add-ZipFile $archive $dll 'AutoEvent.dll'
    Add-ZipFile $archive $mer 'ProjectMER.dll'
    Add-ZipFile $archive $harmony '0Harmony.dll'
    Add-ZipFile $archive $exiled 'Exiled-9.14.2.tar.gz'
    Add-ZipFile $archive (Join-Path $root 'Docs/Installation.md') 'INSTALL.md'
    Add-ZipFile $archive (Join-Path $root 'DEPENDENCIES.md') 'DEPENDENCIES.md'
    Add-ZipFile $archive (Join-Path $root 'LICENSE') 'LICENSE'
    foreach ($directory in @('Music', 'Schematics')) { Add-ZipTree $archive $directory }
}

New-Package (Join-Path $output 'AutoEvent-EXILED-9.14.2-source.zip') {
    param($archive)
    foreach ($directory in @('AutoEvent', 'Docs', 'Music', 'Schematics', 'Tests')) { Add-ZipTree $archive $directory }
    foreach ($name in @('.gitignore', 'LICENSE', 'README.md', 'DEPENDENCIES.md', 'build-exiled.ps1', 'prepare-dependencies.ps1', 'package-exiled.ps1')) {
        Add-ZipFile $archive (Join-Path $root $name) $name
    }
    Add-ZipFile $archive $mer 'dependencies/ProjectMER.dll'
    Get-ChildItem -LiteralPath (Join-Path $root 'dependencies/AudioPlayerApi-1.1.2/dependencies') -File | ForEach-Object {
        Add-ZipFile $archive $_.FullName ('dependencies/AudioPlayerApi-1.1.2/dependencies/' + $_.Name)
    }
}

Copy-Item -LiteralPath $dll -Destination (Join-Path $output 'AutoEvent.dll') -Force
Copy-Item -LiteralPath (Join-Path $root 'Docs/Installation.md') -Destination (Join-Path $output 'INSTALL-AutoEvent-EXILED.md') -Force
Write-Host "Пакеты AutoEvent созданы в $output"
