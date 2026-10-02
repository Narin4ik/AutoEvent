param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dependencies = Join-Path $root 'dependencies'
$mer = Join-Path $dependencies 'ProjectMER.dll'
$audio = Join-Path $dependencies 'AudioPlayerApi-1.1.2/dependencies/AudioPlayerApi.dll'

function Assert-Hash([string] $path, [string] $expected) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ne $expected) { throw "Неверная контрольная сумма файла $path : $actual" }
    return $true
}

function Get-ReleaseAsset([string] $repository, [string] $tag, [string] $extension, [string] $destination, [string] $dllName) {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/releases/tags/$tag" -Headers @{ 'User-Agent' = 'AutoEvent-build' }
    $assets = @($release.assets | Where-Object { $_.name -like "*$extension" -and $_.name -notlike '*Source*' })
    if ($assets.Count -ne 1) { throw "Ожидался один файл $extension в релизе $repository/$tag, найдено $($assets.Count)." }

    $temporary = Join-Path $env:TEMP ([IO.Path]::GetRandomFileName() + $extension)
    try {
        Invoke-WebRequest -Uri $assets[0].browser_download_url -OutFile $temporary
        if ($extension -eq '.dll') {
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $temporary -Destination $destination -Force
        }
        else {
            $unpacked = Join-Path $env:TEMP ([IO.Path]::GetRandomFileName())
            try {
                Expand-Archive -LiteralPath $temporary -DestinationPath $unpacked
                $matches = @(Get-ChildItem -LiteralPath $unpacked -Filter $dllName -File -Recurse)
                if ($matches.Count -ne 1) { throw "В архиве $($assets[0].name) ожидался один $dllName, найдено $($matches.Count)." }
                New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
                Copy-Item -LiteralPath $matches[0].FullName -Destination $destination -Force
            }
            finally { if (Test-Path -LiteralPath $unpacked) { Remove-Item -LiteralPath $unpacked -Recurse -Force } }
        }
    }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
}

if (-not (Assert-Hash $mer '2091A7D44BC3D09EBC7764BCD4B16BA5C7219149EABD82285511D81DADC4B150')) {
    Get-ReleaseAsset 'Michal78900/ProjectMER' '2026.7.6.1' '.dll' $mer 'ProjectMER.dll'
    if (-not (Assert-Hash $mer '2091A7D44BC3D09EBC7764BCD4B16BA5C7219149EABD82285511D81DADC4B150')) { throw 'Не удалось получить ProjectMER.dll.' }
}

if (-not (Assert-Hash $audio 'D034AA58D1F386C8D65F83ED3D57DC75995DEB9C45347F64435BD311E5F5772E')) {
    Get-ReleaseAsset 'Killers0992/AudioPlayerApi' '1.1.2' '.zip' $audio 'AudioPlayerApi.dll'
    if (-not (Assert-Hash $audio 'D034AA58D1F386C8D65F83ED3D57DC75995DEB9C45347F64435BD311E5F5772E')) { throw 'Не удалось получить AudioPlayerApi.dll.' }
}

Write-Host 'Зависимости ProjectMER и AudioPlayerApi проверены.'
