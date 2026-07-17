# Kerbal UI

Give Kerbal Space Program 1 a consistent dark-blue interface without changing
its layout or fonts. Kerbal UI applies one dark navy Zed palette and one vivid
blue decorative accent across flight, the VAB/SPH, KSC, KAL, and shared
windows. Gold, teal, green, and red remain where they communicate instrument,
status, or warning information.

This is a texture-only theme. It does not move the navball, staging stack, MET
display, portraits, or editor controls. It does not replace fonts or load a
layout DLL, so there is no per-frame layout work from this mod. KUI Overhaul is
a separate mod and is not bundled here.

Version 1.5.1 unifies all 665 textures around the dark navy Zed palette and
replaces the remaining gray/purple decorative styling with the same vivid blue
used by the newer elements. Version 1.5.0 fixed the install folder at
`GameData/KerbalUI` and removed the old layout plugin and font files so Kerbal
UI does exactly one job: apply the blue theme.

**KSP:** 1.12.x
**Requires:** ModuleManager, HUDReplacer, and ZTheme
**Nested dependencies:** HUDReplacer requires ModuleManager and HarmonyKSP;
ZTheme requires HUDReplacer and ModuleManager.
**License:** GPL-3.0-only
**Source:** https://github.com/javapythonsean-rgb/KerbalUI

Install the dependencies, then copy `GameData/KerbalUI` into KSP's
`GameData` folder. README, changelog, license, generator source, KSP-AVC version
metadata, and CKAN metadata are included in the download.
