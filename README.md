# ZThemeKSP2 - a KSP2-look UI theme + layout for KSP 1

Makes the KSP 1 UI look and lay out as close to Kerbal Space Program 2 as KSP 1
allows: dark navy terminal-style panels (#131A2A) with vivid blue frames (#4C7CD6),
gold (#EFB304), teal (#09B9C4), periwinkle (#5E5CDE), GO-green (#00E858) and KSP2
red (#EF5052) accents - all matched against official KSP2 screenshots, not guessed.

## MODIFICATION NOTICE (GPL v3 s5a)

This is a MODIFIED derivative of **ZTheme** by zapSNH
(https://github.com/zapSNH/ZTheme), created/modified 2026-07-02.
All 665 textures were programmatically recolored from ZTheme's originals onto the
KSP2 palette. Generator + plugin source is in `Source/` (the preferred form for
modifying this work, per GPL). Licensed GPL v3, same as ZTheme - see LICENSE.

## Textures (HUDReplacer)

- Requires **HUDReplacer** + **ZTheme** (both installed). This pack loads every
  texture folder at ZTheme's priority **+20**, overriding it per-texture; anything
  not covered falls back to ZTheme, then stock.
- A `HUDReplacerRecolor` block (priority 21) re-tints programmatic UI colors (MET
  clock, dV readouts, gauges, PAW buttons, SAS/RCS lights) - white readouts, gold
  labels, KSP2 palette.
- KSP2-style extras baked into the textures: vivid blue frames on ~35 panel
  backgrounds, all-green warp chevrons, dark gold-framed dV boxes.
- The **KSP2 navball** is a NavBallTextureChanger skin
  (`NavBallTextureChanger/PluginData/Skins/KSP2.png`): light cyan sky over
  burnt-orange ground (FDAI style), white horizon with heading ticks, dark
  N/E/S/W + 3-digit headings below the horizon, pitch numerals every 30 degrees.

## Layout plugin (Plugins/ZThemeKSP2Layout.dll)

KSP2 arrangement, all tunable in `Layout.cfg`:
- Navball bottom-LEFT; SAS mode wheel moved to the navball's RIGHT.
- Staging stack -> RIGHT side; MET/warp -> bottom-center; kerbal portraits ->
  top-right; pitch/roll/yaw trim bars hidden; editor LAUNCH -> bottom-right.
- New KSP2 widgets drawn from `WidgetArt/`: top-left location breadcrumb
  (KERBOL \ KERBIN \ VESSEL), left VESSEL ACTIONS rail (gear/lights/brakes/abort),
  bottom-right green GO stage button (with all stock staging safety guards).
- Fonts: new widgets + legacy UI text use a DIN-style OS font (Bahnschrift).
  KSP 1.12's TextMeshPro is too old for runtime typeface swaps, so stock TMP
  text keeps KSP's font.

### Hotkeys

- **Ctrl+Alt+F6 - LIVE RELOAD**: edit `Layout.cfg`, press it in-game, every
  position/toggle re-applies instantly (no restart). Disabled movers snap
  elements back to their stock spots.
- **Ctrl+Alt+F8 - UI dump**: logs the whole UI tree (names + positions) to
  KSP.log tagged `[ZKSP2DUMP]`, for precise repositioning requests.

## Revert

- Layout only: `enabled = false` in `Layout.cfg` + Ctrl+Alt+F6, or delete
  `Plugins/ZThemeKSP2Layout.dll`. Nothing is persisted to KSP's settings.cfg.
- Whole theme: delete `GameData/ZThemeKSP2` (ZTheme takes back over).
- Navball skin: pick another skin in the NavBallTextureChanger in-game menu
  (previous skin was Squeaky; backup at
  `NavBallTextureChanger/PluginData/config.cfg.bak-preKSP2`).

## Credits

- **zapSNH** - ZTheme, the base for every texture here (GPL v3)
- **UltraJohn & KSPModStewards** - HUDReplacer
- **andrew-vant** - dragnav, the navball-mover technique reference
- KSP2 palette + layout matched to Intercept Games' official KSP2 screenshots

