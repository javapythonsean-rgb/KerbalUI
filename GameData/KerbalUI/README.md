# Kerbal UI - Blue Theme

Kerbal UI is a texture-only blue interface theme for Kerbal Space Program 1.
It recolors stock UI textures and supported mod interfaces without moving any
HUD elements, replacing fonts, or running a layout plugin every frame.

## Features

- One dark navy Zed palette across stock and added UI surfaces.
- Consistent vivid-blue decorative frames and controls.
- Gold, teal, green, and red remain only where they communicate instrument,
  status, or warning information.
- Recolored flight, VAB/SPH, KSC, KAL, and shared UI textures.
- Matching colors for gauges, staging cards, PAW controls, SAS/RCS indicators,
  maneuver text, and other programmatic UI elements.
- Stock font and stock UI positions remain unchanged.
- No plugin DLL and no per-frame layout work.

## Install

1. Install ModuleManager, HUDReplacer, and ZTheme.
2. Copy `GameData/KerbalUI` from this ZIP into KSP's `GameData` folder.
3. Keep the folder name exactly `KerbalUI`; the texture paths use that name.

Dependency chain: HUDReplacer requires ModuleManager and HarmonyKSP. ZTheme
requires HUDReplacer and ModuleManager. CKAN can install the complete chain.

## Uninstall

Delete `GameData/KerbalUI`. ZTheme will take over again automatically.

## Modification notice

Kerbal UI is a modified derivative of ZTheme by zapSNH. The textures were
programmatically recolored from ZTheme's originals. Generator source and a
reproducible `Build-Textures.ps1` entry point are included in the ZIP under
`Source/`, which is the preferred form for modifying this work.

Kerbal UI is licensed under GPL v3, the same license as ZTheme. See `LICENSE`.
Source is published at https://github.com/javapythonsean-rgb/KerbalUI.

## Credits

- zapSNH - ZTheme
- UltraJohn and KSPModStewards - HUDReplacer
- Intercept Games - KSP2 visual reference
