param([switch]$SelfContained)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\dotnet'
    $env:NUGET_PACKAGES = Join-Path $projectRoot '.tools\nuget'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    dotnet run --project tests/JuanTool.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    $standalone = if ($SelfContained) { 'true' } else { 'false' }
    dotnet publish src/JuanTool/JuanTool.csproj -c Release -r win-x64 --self-contained $standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/JuanTool
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Write-Host "Ready: $projectRoot\artifacts\JuanTool\JuanTool.exe"
} finally { Pop-Location }
