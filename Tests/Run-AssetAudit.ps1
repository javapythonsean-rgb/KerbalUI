$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repo = Split-Path -Parent $PSScriptRoot
$modRoot = Join-Path $repo 'GameData\KerbalUI'
$script:checks = 0

function Assert-True([bool]$condition, [string]$label) {
    if (-not $condition) { throw $label }
    $script:checks++
}

Assert-True (Test-Path -LiteralPath $modRoot) 'GameData/KerbalUI is missing.'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $repo 'GameData\ZThemeKSP2'))) 'Legacy ZThemeKSP2 folder is present.'
Assert-True (Test-Path -LiteralPath (Join-Path $modRoot 'config.cfg')) 'config.cfg is missing.'
Assert-True (Test-Path -LiteralPath (Join-Path $modRoot 'KerbalUI.version')) 'KerbalUI.version is missing.'
Assert-True (Test-Path -LiteralPath (Join-Path $repo 'KerbalUI.netkan')) 'KerbalUI.netkan is missing.'
Assert-True (Test-Path -LiteralPath (Join-Path $repo 'Source\Recolor.cs')) 'Generator source is missing.'

$forbidden = @(Get-ChildItem -LiteralPath $repo -Recurse -File | Where-Object {
    $_.Extension -eq '.dll' -or
    $_.Name -eq 'Layout.cfg' -or
    $_.FullName -match '[\\/](Plugins|Fonts|FontData|WidgetArt)[\\/]'
})
Assert-True ($forbidden.Count -eq 0) 'Layout/plugin/font files leaked into Kerbal UI.'

$oldNameRefs = @(Get-ChildItem -LiteralPath $repo -Recurse -File |
    Where-Object { $_.Extension -in @('.cfg','.md','.json','.version','.netkan','.cs') } |
    Select-String -Pattern 'ZThemeKSP2' -List)
Assert-True ($oldNameRefs.Count -eq 0) 'A ZThemeKSP2 text reference remains.'

$config = Join-Path $modRoot 'config.cfg'
$configuredPaths = @(Select-String -LiteralPath $config -Pattern '^\s*filePath\s*=' |
    ForEach-Object {
        if ($_.Line -match '^\s*filePath\s*=\s*(.+?)\s*$') {
            $Matches[1].TrimEnd('/')
        }
    })
Assert-True ($configuredPaths.Count -eq 15) 'Expected 15 HUDReplacer texture paths.'
foreach ($configuredPath in $configuredPaths) {
    $absolute = Join-Path $repo ($configuredPath -replace '/', '\')
    Assert-True (Test-Path -LiteralPath $absolute) "Missing configured path: $configuredPath"
}

$textures = @(Get-ChildItem -LiteralPath $modRoot -Recurse -File -Filter '*.png')
Assert-True ($textures.Count -eq 665) "Expected 665 textures, found $($textures.Count)."
foreach ($texture in $textures) {
    $image = $null
    try {
        $image = [Drawing.Image]::FromFile($texture.FullName)
        Assert-True ($image.Width -gt 0 -and $image.Height -gt 0) "Invalid texture dimensions: $($texture.FullName)"
    }
    catch {
        throw "Unreadable PNG: $($texture.FullName): $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $image) { $image.Dispose() }
    }
}

$manifest = Get-Content -Raw -LiteralPath (Join-Path $modRoot 'KerbalUI.version') | ConvertFrom-Json
$version = "$($manifest.VERSION.MAJOR).$($manifest.VERSION.MINOR).$($manifest.VERSION.PATCH)"
Assert-True ($version -eq '1.5.0') "Unexpected manifest version: $version"
Assert-True ($manifest.URL -eq 'https://raw.githubusercontent.com/javapythonsean-rgb/KerbalUI/main/GameData/KerbalUI/KerbalUI.version') 'Manifest URL does not match the renamed folder.'

$netkan = Get-Content -Raw -LiteralPath (Join-Path $repo 'KerbalUI.netkan') | ConvertFrom-Json
$dependencies = @($netkan.depends | ForEach-Object { $_.name } | Sort-Object)
Assert-True (($dependencies -join ',') -eq 'HUDReplacer,ModuleManager,ZTheme') 'CKAN dependencies are incomplete.'

Assert-True ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo 'README.md')).Hash -eq
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $modRoot 'README.md')).Hash) 'Root and installed README files differ.'
Assert-True ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $repo 'CHANGELOG.md')).Hash -eq
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $modRoot 'CHANGELOG.md')).Hash) 'Root and installed changelog files differ.'

Write-Output "Kerbal UI asset audit passed: $script:checks checks"
