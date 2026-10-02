param(
    [Parameter(Mandatory = $true)]
    [string] $ManagedPath,
    [switch] $DownloadDependencies
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($DownloadDependencies) {
    & (Join-Path $root 'prepare-dependencies.ps1')
}
$managed = (Resolve-Path -LiteralPath $ManagedPath).Path
$labApi = Join-Path $managed 'LabApi.dll'
$assembly = Join-Path $managed 'Assembly-CSharp.dll'
$mer = Join-Path $root 'dependencies/ProjectMER.dll'
$audio = Join-Path $root 'dependencies/AudioPlayerApi-1.1.2/dependencies/AudioPlayerApi.dll'

foreach ($path in @($labApi, $assembly, $mer, $audio)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Отсутствует обязательная сборка: $path. Запустите prepare-dependencies.ps1 или укажите правильный путь Managed."
    }
}

$labApiVersion = [Reflection.AssemblyName]::GetAssemblyName($labApi).Version
if ($labApiVersion -ne [version]'1.1.7.0') {
    throw "Требуется LabAPI 1.1.7.0; обнаружена $labApiVersion. Используйте совместимую сборку SCP:SL."
}

$env:SL_REFERENCES = $managed
Push-Location $root
try {
    dotnet restore 'AutoEvent/AutoEvent.csproj'
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось восстановить пакеты NuGet.' }

    dotnet build 'AutoEvent/AutoEvent.csproj' -c Release --no-restore "-p:SL_REFERENCES=$managed"
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось собрать AutoEvent в конфигурации Release.' }
}
finally {
    Pop-Location
}

Write-Host "Собрана библиотека $(Join-Path $root 'AutoEvent/bin/Release/net48/AutoEvent.dll')"
