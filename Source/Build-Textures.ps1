param(
    [Parameter(Mandatory = $true)]
    [string]$ZThemeRoot,

    [string]$OutputRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'GameData\KerbalUI\PluginData')
)

$ErrorActionPreference = 'Stop'

$sourceRoot = Join-Path $ZThemeRoot 'PluginData'
if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw "ZTheme PluginData was not found at: $sourceRoot"
}

$sourceFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.png')
if ($sourceFiles.Count -ne 665) {
    throw "Expected 665 ZTheme textures, found $($sourceFiles.Count)."
}

Add-Type -Path (Join-Path $PSScriptRoot 'Recolor.cs') -ReferencedAssemblies 'System.Drawing'

$count = [KerbalUiRecolor]::ProcessTree($sourceRoot, $OutputRoot)
if ($count -ne 665) {
    throw "Expected to generate 665 Kerbal UI textures, generated $count."
}

Write-Output "Generated $count dark-blue Kerbal UI textures in $OutputRoot"
