param(
    [switch]$SkipBuild,
    [string]$CompilerPath
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot 'artifacts\JuanTool'
$executable = Join-Path $publishDir 'JuanTool.exe'

if (-not $CompilerPath) {
    # Inno Setup can be installed on another drive. Check registered installations.
    $uninstallRoots = @(
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )
    $installations = Get-ItemProperty -Path $uninstallRoots -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -like 'Inno Setup*' -and $_.InstallLocation }
    foreach ($installation in $installations) {
        $candidate = Join-Path $installation.InstallLocation 'ISCC.exe'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $CompilerPath = $candidate
            break
        }
    }
}
if (-not $CompilerPath) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $CompilerPath = $command.Source }
}
if (-not $CompilerPath) {
    $installRoots = @(
        [Environment]::GetFolderPath('ProgramFiles'),
        [Environment]::GetFolderPath('ProgramFilesX86'),
        (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs')
    ) | Select-Object -Unique
    foreach ($installRoot in $installRoots) {
        foreach ($folder in @('Inno Setup 7', 'Inno Setup 6')) {
            $candidate = Join-Path $installRoot "$folder\ISCC.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $CompilerPath = $candidate
                break
            }
        }
        if ($CompilerPath) { break }
    }
}
if (-not $CompilerPath) {
    # Start menu shortcuts also identify custom installations when registry entries
    # are unavailable. Reading their targets does not launch the compiler IDE.
    $programMenus = @(
        [Environment]::GetFolderPath('Programs'),
        [Environment]::GetFolderPath('CommonPrograms')
    )
    $shortcuts = foreach ($programMenu in $programMenus) {
        Get-ChildItem -LiteralPath $programMenu -Directory -Filter 'Inno Setup*' -ErrorAction SilentlyContinue |
            Get-ChildItem -File -Filter '*Compiler*.lnk' -ErrorAction SilentlyContinue
    }
    if ($shortcuts) {
        $shell = New-Object -ComObject WScript.Shell
        try {
            foreach ($shortcutFile in $shortcuts) {
                $shortcut = $shell.CreateShortcut($shortcutFile.FullName)
                try {
                    if (-not $shortcut.TargetPath) { continue }
                    $candidate = Join-Path (Split-Path -Parent $shortcut.TargetPath) 'ISCC.exe'
                    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                        $CompilerPath = $candidate
                        break
                    }
                } finally {
                    [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) | Out-Null
                }
            }
        } finally {
            [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null
        }
    }
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Install Inno Setup first: winget install --id JRSoftware.InnoSetup.7 -e -s winget -i. For a custom installation, pass -CompilerPath with the full path to ISCC.exe.'
}
Write-Host "Using Inno Setup compiler: $CompilerPath"

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1') -SelfContained
}
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw 'JuanTool.exe is missing. Run this script without -SkipBuild to test and publish the self-contained app first.'
}

# Read the version from the actual executable, including when reusing an earlier build.
$version = ([Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion -split '\+')[0]
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw "Unsupported executable version: $version"
}
$outputDir = Join-Path $projectRoot 'artifacts\Installer'
$outputName = "JuanTool-Setup-$version-win-x64"
& $CompilerPath "/DAppVersion=$version" "/DPublishDir=$publishDir" "/O$outputDir" "/F$outputName" (Join-Path $projectRoot 'installer\JuanTool.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$installer = Join-Path $outputDir "$outputName.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    throw 'Inno Setup did not produce the expected installer.'
}
Write-Host "Installer ready: $installer"
