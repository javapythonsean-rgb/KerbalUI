// ZThemeKSP2Layout v3 - KSP2-style layout + bespoke KSP2 widgets for KSP1.
// Part of the ZThemeKSP2 theme. GPLv3 (same as the theme / its ZTheme base).
//
// Movers (per-frame re-apply, config-tunable, never persisted):
//   navball -> bottom-left, staging -> right, MET/warp -> bottom-center,
//   kerbal portraits -> top-right, SAS mode wheel -> right of navball,
//   trim gauges hidden, editor LAUNCH -> bottom-right.
// Bespoke widgets (drawn from ZThemeKSP2/WidgetArt PNGs + DIN-style OS font):
//   breadcrumb location bar (top-left, "KERBOL \ KERBIN \ VESSEL"),
//   VESSEL ACTIONS rail (left edge: gear/lights/brakes/abort toggles),
//   green GO stage button (bottom-right, activates next stage).
// Fail-safe: every path try/catch-guarded + config-gated (Layout.cfg).
// Delete the DLL (or enabled=false) for a full revert - nothing is persisted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight;

namespace ZThemeKSP2Layout
{
    internal static class Cfg
    {
        public static bool loaded;
        public static bool enabled = true;

        public static bool moveNavball = true;
        // +0.70 = far RIGHT (KSP UI world space is center-origin; positive = right).
        // User moved the navball to the right side, so its satellites (SAS wheel, mode
        // frame) are mirrored to sit on its LEFT via negative offsets below.
        public static float navballPosX = 0.70f;
        public static bool moveStaging = true;
        // pushed right to butt against the app/MechJeb toolbar (user: "stick to the toolbar").
        // stagingToToolbar (below) glues it precisely to the toolbar's live left edge.
        public static Vector2 stagingOffset = new Vector2(0.90f, -0.04f);
        public static bool stagingToToolbar = true; // pin staging flush-left of the app toolbar
        public static bool pinStagingLeft = false;  // mirror: pin staging to the LEFT screen edge instead
        public static float stagingLeftGap = 8f;    // px gap from the left edge in left-pin mode
        public static bool moveMET = true;
        public static Vector2 metOffset = new Vector2(0.42f, -0.90f);
        public static bool hideTrimGauges = true;
        public static Vector2 altimeterOffset = Vector2.zero;
        public static bool movePortraits = true;
        // further left than before: with tall staging stacks (v1.3 default -0.16) the
        // portrait row still overlapped the staging column
        public static Vector2 portraitsOffset = new Vector2(-0.26f, 0.70f);
        public static float portraitsScale = 0.85f;  // KSP2's heads are smaller than stock
        public static bool showGForce = true;         // small G readout pinned next to the heads
        public static Vector2 gforcePos = new Vector2(-0.38f, 0.84f); // screen fraction offset for G-force meter
        // CommNet signal widget: sit INLINE with the breadcrumb bar (right of it) like KSP2
        public static bool moveComms = true;
        public static bool commsInBar = true;         // false -> use the plain commsOffset mover
        public static Vector2 commsOffset = new Vector2(0.005f, -0.045f);
        // keep the warp/MET quadrant expanded so the warp arrows show from startup
        public static bool expandWarpPanel = true;
        // pause + warp-to-sunrise buttons pinned just LEFT of the visible warp/MET box
        // (TimeQuadrant); x = px gap from the box's left edge, y = vertical nudge
        public static bool showTimeButtons = true;
        public static Vector2 timeButtonsPos = new Vector2(-8f, 0f);
        // pause/dawn straddle the box's two native rows (WARP / MET) - measured as a
        // fraction of the box's real height rather than a guessed pixel offset, so it
        // stays aligned regardless of the box's actual size; tune via live-reload
        public static float timeButtonRowSplit = 0.25f;
        // the stock "next alarm" clock icon that appears beside MET - user wants it gone
        public static bool hideAlarmClock = true;
        // the stock warp-to-next-morning sun button on the warp/MET box - redundant with
        // our own dawn button, and not part of the KSP2 look
        public static bool hideWarpToMorning = true;
        public static bool moveSASModes = true;
        // navball now on the RIGHT, so the SAS wheel mirrors to its LEFT (was +530)
        public static Vector2 sasModesOffset = new Vector2(-530f, 20f);
        // the mode frame (staging/docking/map/maneuver switcher) sits beside the navball;
        // navball moved right -> shift the mode frame LEFT (was +340) so it aligns with the
        // right-edge mode buttons ("align with the four buttons")
        public static bool moveModeFrame = true;
        public static Vector2 modeFrameOffset = new Vector2(-340f, 0f); // canvas px

        // Docking control quadrant (CTRL MODE ROT/LIN translation+rotation input display).
        // Stock puts it bottom-center; move it into the gap right of the navball. canvas px.
        public static bool moveDockQuadrant = true;
        public static Vector2 dockQuadrantOffset = new Vector2(-470f, 0f);

        // Maneuver-mode panel (node editor + node handle editor) - same treatment as the
        // docking quadrant: moved into the gap right of the navball. canvas px.
        public static bool moveManQuadrant = true;
        public static Vector2 manQuadrantOffset = new Vector2(-470f, 0f);

        public static bool showBreadcrumb = true;
        public static Vector2 breadcrumbPos = Vector2.zero;              // flush to top-left edge (KSP2)
        public static bool showActionsRail = true;
        public static Vector2 railPos = new Vector2(8f, -44f);           // px from TOP-left, under the breadcrumb
        public static bool showGoButton = true;
        public static Vector2 goPos = new Vector2(-14f, 14f);
        public static bool showResources = true;                         // keep the resources panel open (KSP2)
        public static bool paintStaging = true;                          // recolor stage cards + dV to KSP2
        public static Color stageCardColor = new Color(0.102f, 0.118f, 0.153f, 1f); // #1A1E27 slate
        public static Color stageDvColor = new Color(0f, 0.890f, 0.322f, 1f);       // #00E352 green

        public static bool moveLaunchButton = true;
        // anchored bottom-CENTER (clears the right-edge staging stack); x = px right of center
        public static Vector2 launchButtonPos = new Vector2(150f, 16f);
        public static bool nukeStagingTotal = true; // hide the stock total-dV; show it on GO/LAUNCH
        // OFF by default: on some installs the strip's parent carries the whole build menu
        public static bool moveEditorTools = false;
        public static Vector2 editorToolsPos = new Vector2(0.5f, 0.955f); // screen fraction, top-center
        public static bool fillTopRow = true;        // shift New/Open/Save/Exit right into the launch gap
        // the stock launch-site selector (KSP.UI.UILaunchsiteController - a hover slide
        // panel, same family as the warp chevrons) pinned permanently open ABOVE the
        // LAUNCH button instead of our old custom popup dialog
        public static bool expandLaunchSiteSelector = true;
        public static float launchSiteSelectorGap = 10f; // px between the selector and the LAUNCH button

        public static bool swapFont = true;
        public static string fontName = "JetBrains Mono Medium"; // the real KSP2 font (installed per-user by this mod)
        public static float fontSweepInterval = 1.5f; // steady-state sweep cadence; flight/editor/everywhere clamp it to >= 5s (0.4s burst after scene load) - keep in sync with Layout.cfg
        public static bool swapFontInMenus = false; // main menu/settings buttons overlap under mono - skip
        // JetBrains Mono is wider than KSP's stock font - shrink swapped text so it
        // fits its original boxes (fixes overlapping lines in dialogs/lists)
        public static float fontSizeMult = 0.80f;
        // editor part-size badges ("1.25m", "2.5m"...) are tiny in stock KSP - boost
        // them instead of applying the global shrink
        public static float sizeTagFontMult = 1.30f;
        // the altimeter's ATMOSPHERE caption wraps its final E onto a second row under
        // the wider mono font - shrink it harder (word wrap is also disabled for it)
        public static float atmoLabelMult = 0.68f;
        // user wants the ATMOSPHERE word GONE entirely (not shrunk); hide its GameObject
        public static bool hideAtmosphere = true;

        public static bool showHints = true;
        // F6/F8: unbound in stock KSP (F10/F11 collide with temp gauges/thermal overlay)
        public static KeyCode dumpKey = KeyCode.F8;
        public static KeyCode reloadKey = KeyCode.F6;
        public static bool dumpRequiresModifier = true;

        public static void Load()
        {
            loaded = true;
            try
            {
                ConfigNode node = null;
                ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("ZThemeKSP2Layout");
                if (nodes != null && nodes.Length > 0) node = nodes[nodes.Length - 1];
                if (node == null) { Debug.Log("[ZThemeKSP2Layout] no config node; using defaults"); return; }
                LoadNode(node);
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] config load failed: " + e); }
        }

        // Live-reload: re-read Layout.cfg straight from disk (GameDatabase is a startup
        // snapshot, so it can't be used for this).
        public static bool ReloadFromDisk()
        {
            try
            {
                string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/ZThemeKSP2/Layout.cfg");
                if (!File.Exists(path)) { Debug.LogWarning("[ZThemeKSP2Layout] Layout.cfg not found for reload"); return false; }
                ConfigNode root = ConfigNode.Load(path);
                if (root == null) return false;
                ConfigNode node = root.GetNode("ZThemeKSP2Layout");
                if (node == null) { Debug.LogWarning("[ZThemeKSP2Layout] no ZThemeKSP2Layout node in Layout.cfg"); return false; }
                LoadNode(node);
                Debug.Log("[ZThemeKSP2Layout] Layout.cfg reloaded from disk");
                return true;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] reload failed: " + e); return false; }
        }

        static void LoadNode(ConfigNode node)
        {
            try
            {
                bool b; float f;
                if (bool.TryParse(node.GetValue("enabled"), out b)) enabled = b;
                if (bool.TryParse(node.GetValue("moveNavball"), out b)) moveNavball = b;
                if (float.TryParse(node.GetValue("navballPosX"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) navballPosX = Mathf.Clamp(f, -0.95f, 0.95f);
                if (bool.TryParse(node.GetValue("moveStaging"), out b)) moveStaging = b;
                stagingOffset = ParseV2(node.GetValue("stagingOffset"), stagingOffset);
                if (bool.TryParse(node.GetValue("stagingToToolbar"), out b)) stagingToToolbar = b;
                if (bool.TryParse(node.GetValue("pinStagingLeft"), out b)) pinStagingLeft = b;
                if (float.TryParse(node.GetValue("stagingLeftGap"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) stagingLeftGap = f;
                if (bool.TryParse(node.GetValue("moveMET"), out b)) moveMET = b;
                metOffset = ParseV2(node.GetValue("metOffset"), metOffset);
                if (bool.TryParse(node.GetValue("hideTrimGauges"), out b)) hideTrimGauges = b;
                altimeterOffset = ParseV2(node.GetValue("altimeterOffset"), altimeterOffset);
                if (bool.TryParse(node.GetValue("movePortraits"), out b)) movePortraits = b;
                portraitsOffset = ParseV2(node.GetValue("portraitsOffset"), portraitsOffset);
                if (float.TryParse(node.GetValue("portraitsScale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) portraitsScale = Mathf.Clamp(f, 0.4f, 1.5f);
                if (bool.TryParse(node.GetValue("showGForce"), out b)) showGForce = b;
                gforcePos = ParseV2(node.GetValue("gforcePos"), gforcePos);
                if (bool.TryParse(node.GetValue("moveComms"), out b)) moveComms = b;
                if (bool.TryParse(node.GetValue("commsInBar"), out b)) commsInBar = b;
                commsOffset = ParseV2(node.GetValue("commsOffset"), commsOffset);
                if (bool.TryParse(node.GetValue("expandWarpPanel"), out b)) expandWarpPanel = b;
                if (bool.TryParse(node.GetValue("showTimeButtons"), out b)) showTimeButtons = b;
                timeButtonsPos = ParseV2(node.GetValue("timeButtonsPos"), timeButtonsPos);
                if (float.TryParse(node.GetValue("timeButtonRowSplit"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) timeButtonRowSplit = Mathf.Clamp(f, 0.05f, 0.45f);
                if (bool.TryParse(node.GetValue("hideAlarmClock"), out b)) hideAlarmClock = b;
                if (bool.TryParse(node.GetValue("hideWarpToMorning"), out b)) hideWarpToMorning = b;
                if (bool.TryParse(node.GetValue("moveSASModes"), out b)) moveSASModes = b;
                sasModesOffset = ParseV2(node.GetValue("sasModesOffset"), sasModesOffset);
                if (bool.TryParse(node.GetValue("moveModeFrame"), out b)) moveModeFrame = b;
                modeFrameOffset = ParseV2(node.GetValue("modeFrameOffset"), modeFrameOffset);
                if (bool.TryParse(node.GetValue("moveDockQuadrant"), out b)) moveDockQuadrant = b;
                dockQuadrantOffset = ParseV2(node.GetValue("dockQuadrantOffset"), dockQuadrantOffset);
                if (bool.TryParse(node.GetValue("moveManQuadrant"), out b)) moveManQuadrant = b;
                manQuadrantOffset = ParseV2(node.GetValue("manQuadrantOffset"), manQuadrantOffset);
                if (bool.TryParse(node.GetValue("showBreadcrumb"), out b)) showBreadcrumb = b;
                breadcrumbPos = ParseV2(node.GetValue("breadcrumbPos"), breadcrumbPos);
                if (bool.TryParse(node.GetValue("showActionsRail"), out b)) showActionsRail = b;
                railPos = ParseV2(node.GetValue("railPos"), railPos);
                if (bool.TryParse(node.GetValue("showGoButton"), out b)) showGoButton = b;
                goPos = ParseV2(node.GetValue("goPos"), goPos);
                if (bool.TryParse(node.GetValue("showResources"), out b)) showResources = b;
                if (bool.TryParse(node.GetValue("paintStaging"), out b)) paintStaging = b;
                stageCardColor = ParseColor(node.GetValue("stageCardColor"), stageCardColor);
                stageDvColor = ParseColor(node.GetValue("stageDvColor"), stageDvColor);
                if (bool.TryParse(node.GetValue("moveLaunchButton"), out b)) moveLaunchButton = b;
                launchButtonPos = ParseV2(node.GetValue("launchButtonPos"), launchButtonPos);
                if (bool.TryParse(node.GetValue("nukeStagingTotal"), out b)) nukeStagingTotal = b;
                if (bool.TryParse(node.GetValue("moveEditorTools"), out b)) moveEditorTools = b;
                editorToolsPos = ParseV2(node.GetValue("editorToolsPos"), editorToolsPos);
                if (bool.TryParse(node.GetValue("fillTopRow"), out b)) fillTopRow = b;
                if (bool.TryParse(node.GetValue("expandLaunchSiteSelector"), out b)) expandLaunchSiteSelector = b;
                if (float.TryParse(node.GetValue("launchSiteSelectorGap"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) launchSiteSelectorGap = Mathf.Clamp(f, 0f, 200f);
                if (bool.TryParse(node.GetValue("swapFont"), out b)) swapFont = b;
                string fn = node.GetValue("fontName");
                if (!string.IsNullOrEmpty(fn)) fontName = fn;
                if (float.TryParse(node.GetValue("fontSweepInterval"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) fontSweepInterval = Mathf.Clamp(f, 0.25f, 60f);
                if (bool.TryParse(node.GetValue("swapFontInMenus"), out b)) swapFontInMenus = b;
                if (float.TryParse(node.GetValue("fontSizeMult"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) fontSizeMult = Mathf.Clamp(f, 0.5f, 1.2f);
                if (float.TryParse(node.GetValue("sizeTagFontMult"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) sizeTagFontMult = Mathf.Clamp(f, 0.5f, 2f);
                if (float.TryParse(node.GetValue("atmoLabelMult"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) atmoLabelMult = Mathf.Clamp(f, 0.3f, 1.2f);
                if (bool.TryParse(node.GetValue("hideAtmosphere"), out b)) hideAtmosphere = b;
                if (bool.TryParse(node.GetValue("showHints"), out b)) showHints = b;
                string dk = node.GetValue("dumpKey");
                if (!string.IsNullOrEmpty(dk)) { try { dumpKey = (KeyCode)Enum.Parse(typeof(KeyCode), dk, true); } catch { } }
                string rk = node.GetValue("reloadKey");
                if (!string.IsNullOrEmpty(rk)) { try { reloadKey = (KeyCode)Enum.Parse(typeof(KeyCode), rk, true); } catch { } }
                if (bool.TryParse(node.GetValue("dumpRequiresModifier"), out b)) dumpRequiresModifier = b;

                Debug.Log("[ZThemeKSP2Layout] config loaded (v3)");
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] config node parse failed: " + e); }
        }

        public static bool ReloadTriggered()
        {
            try
            {
                if (!Input.GetKeyDown(reloadKey)) return false;
                return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            }
            catch { return false; }
        }

        static Vector2 ParseV2(string s, Vector2 def)
        {
            if (string.IsNullOrEmpty(s)) return def;
            string[] p = s.Split(',');
            float x, y;
            if (p.Length == 2 && float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) return new Vector2(x, y);
            return def;
        }

        // "R,G,B" ints 0-255
        static Color ParseColor(string s, Color def)
        {
            if (string.IsNullOrEmpty(s)) return def;
            string[] p = s.Split(',');
            int r, g, bb;
            if (p.Length >= 3 && int.TryParse(p[0].Trim(), out r) && int.TryParse(p[1].Trim(), out g) && int.TryParse(p[2].Trim(), out bb))
                return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(bb / 255f), 1f);
            return def;
        }

        public static bool DumpTriggered()
        {
            try
            {
                if (!Input.GetKeyDown(dumpKey)) return false;
                if (!dumpRequiresModifier) return true;
                return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            }
            catch { return false; }
        }
    }

    public class ReapplyMover : MonoBehaviour
    {
        public bool useRatioX;
        public float ratioX;
        public bool useOffset;
        public Vector2 offset;
        public bool useLocalOffset;
        public Vector2 localOffset;
        Vector3 baseline;
        Vector3 baselineLocal;
        bool haveBaseline;

        // live-reload: restore any axis whose offset component transitions nonzero -> zero,
        // so a "0" edit snaps that axis back to stock instead of sticking mid-move
        public void SetOffset(Vector2 v)
        {
            try
            {
                if (haveBaseline)
                {
                    Vector3 p = transform.position;
                    if (offset.x != 0f && v.x == 0f) p.x = baseline.x;
                    if (offset.y != 0f && v.y == 0f) p.y = baseline.y;
                    transform.position = p;
                }
            }
            catch { }
            useOffset = true; offset = v;
        }

        // removing the mover puts the element back where stock had it (live-reload/disable)
        void OnDestroy()
        {
            try
            {
                if (!haveBaseline) return;
                if (useLocalOffset) transform.localPosition = baselineLocal;
                else transform.position = baseline;
            }
            catch { }
        }

        void LateUpdate()
        {
            try
            {
                if (!haveBaseline) { baseline = transform.position; baselineLocal = transform.localPosition; haveBaseline = true; }

                if (useRatioX)
                {
                    Vector3 p = transform.position;
                    float half = Screen.width / 2f;
                    if (half <= 0f) half = 960f;
                    float r = Mathf.Clamp(ratioX, -0.95f, 0.95f);
                    p.x = r * half;
                    // NOTE: deliberately NOT writing GameSettings.UI_POS_NAVBALL - stock code
                    // calls SaveSettings() during normal play (PAW collapse, exit-to-menu, ...)
                    // and would bake our value into settings.cfg, breaking clean revert.
                    transform.position = p;
                }
                if (useOffset)
                {
                    // per-axis: only override axes with a non-zero offset so stock collapse/
                    // slide animations keep working on the untouched axis
                    Vector3 p = transform.position;
                    if (offset.x != 0f) p.x = baseline.x + offset.x * Screen.width;
                    if (offset.y != 0f) p.y = baseline.y + offset.y * Screen.height;
                    transform.position = p;
                }
                if (useLocalOffset)
                {
                    Vector3 lp = baselineLocal;
                    lp.x += localOffset.x;
                    lp.y += localOffset.y;
                    transform.localPosition = lp;
                }
            }
            catch { }
        }
    }

    // Attach = idempotent: creates the mover or live-updates its values (F6 reload).
    // Detach destroys the mover, which restores the stock position via OnDestroy.
    internal static class Movers
    {
        public static void AttachRatio(GameObject go, float ratio)
        {
            if (go == null) return;
            ReapplyMover m = go.GetComponent<ReapplyMover>();
            if (m == null) { m = go.AddComponent<ReapplyMover>(); Debug.Log("[ZThemeKSP2Layout] ratio-mover '" + go.name + "'"); }
            m.useRatioX = true; m.ratioX = ratio;
        }
        public static void AttachOffset(GameObject go, Vector2 offset)
        {
            if (go == null) return;
            if (offset == Vector2.zero) { Detach(go); return; }
            ReapplyMover m = go.GetComponent<ReapplyMover>();
            if (m == null) { m = go.AddComponent<ReapplyMover>(); Debug.Log("[ZThemeKSP2Layout] offset-mover '" + go.name + "'"); }
            m.SetOffset(offset);
        }
        public static void AttachLocal(GameObject go, Vector2 px)
        {
            if (go == null) return;
            if (px == Vector2.zero) { Detach(go); return; }
            ReapplyMover m = go.GetComponent<ReapplyMover>();
            if (m == null) { m = go.AddComponent<ReapplyMover>(); Debug.Log("[ZThemeKSP2Layout] local-mover '" + go.name + "'"); }
            m.useLocalOffset = true; m.localOffset = px;
        }
        public static void Detach(GameObject go)
        {
            if (go == null) return;
            ReapplyMover m = go.GetComponent<ReapplyMover>();
            if (m != null) UnityEngine.Object.Destroy(m);
        }
    }

    // Builds KSP2-style UGUI widgets from GameData/ZThemeKSP2/WidgetArt PNGs.
    internal static class Widgets
    {
        static Font _font;
        static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public static Font UiFont()
        {
            if (_font == null)
            {
                try { _font = Font.CreateDynamicFontFromOSFont(Cfg.fontName, 14); } catch { }
                if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Arial", 14); } catch { } }
            }
            return _font;
        }

        public static void ResetFont() { _font = null; } // live-reload: pick up fontName edits

        public static Sprite LoadSprite(string name, Vector4 border)
        {
            Sprite s;
            if (_sprites.TryGetValue(name, out s) && s != null) return s;
            try
            {
                string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/ZThemeKSP2/WidgetArt/" + name);
                if (!File.Exists(path)) { Debug.LogWarning("[ZThemeKSP2Layout] missing art " + name); return null; }
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, bytes)) { UnityEngine.Object.Destroy(tex); return null; }
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp; // stop 9-slice edge bleed from Repeat wrap
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                                  100f, 0, SpriteMeshType.FullRect, border);
                _sprites[name] = s;
                return s;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] sprite load failed " + name + ": " + e); return null; }
        }

        public static Canvas MainCanvas()
        {
            try
            {
                if (KSP.UI.UIMasterController.Instance != null && KSP.UI.UIMasterController.Instance.mainCanvas != null)
                    return KSP.UI.UIMasterController.Instance.mainCanvas;
            }
            catch { }
            try
            {
                Canvas[] all = UnityEngine.Object.FindObjectsOfType<Canvas>();
                foreach (Canvas c in all) if (c.name == "MainCanvas") return c;
                foreach (Canvas c in all) if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
            }
            catch { }
            return null;
        }

        // the persistent main canvas survives scene loads - kill any prior copy first
        public static void DestroyExisting(Transform canvasRoot, string goName)
        {
            try
            {
                Transform old = canvasRoot.Find(goName);
                if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            }
            catch { }
        }

        public static GameObject Panel(Transform parent, string goName, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            Sprite sp = LoadSprite("panel.png", new Vector4(7, 7, 7, 7));
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            else img.color = new Color(0.075f, 0.10f, 0.165f, 0.92f);
            return go;
        }

        public static Text Label(Transform parent, string goName, string txt, int size, TextAnchor align, Color color)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Text), typeof(Outline));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
            Text t = go.GetComponent<Text>();
            Font f = UiFont();
            if (f != null) t.font = f;
            t.fontSize = size; t.fontStyle = FontStyle.Bold;
            t.alignment = align; t.color = color; t.text = txt;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // labels never block clicks/camera-drag
            Outline o = go.GetComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.75f);
            o.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        public static void Deselect()
        {
            try
            {
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
            catch { }
        }

        public static void NoNavigation(Button b)
        {
            try
            {
                Navigation n = b.navigation;
                n.mode = Navigation.Mode.None;
                b.navigation = n; // Enter/Submit can never re-fire the last-clicked button
            }
            catch { }
        }
    }

    // Top-left "KERBOL \ KERBIN \ VESSEL" location bar (KSP2's breadcrumb).
    public class BreadcrumbBar : MonoBehaviour
    {
        // the live bar's rect - CommsPin glues the CommNet signal widget to its right edge
        public static RectTransform Bar;

        Text label;
        float next;

        public static GameObject Create(Canvas canvas)
        {
            try
            {
                Widgets.DestroyExisting(canvas.transform, "ZKSP2_Breadcrumb");
                GameObject panel = Widgets.Panel(canvas.transform, "ZKSP2_Breadcrumb",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Cfg.breadcrumbPos, new Vector2(430f, 30f));
                panel.GetComponent<Image>().raycastTarget = false; // purely decorative
                BreadcrumbBar bb = panel.AddComponent<BreadcrumbBar>();
                bb.label = Widgets.Label(panel.transform, "txt", "", 15, TextAnchor.MiddleLeft,
                    new Color(0.93f, 0.93f, 0.94f, 1f));
                Bar = (RectTransform)panel.transform;
                return panel;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] breadcrumb create failed: " + e); return null; }
        }

        void OnDestroy()
        {
            if (Bar == transform) Bar = null;
        }

        Vessel lastV;
        CelestialBody lastBody;
        string lastName;

        void Update()
        {
            try
            {
                if (Time.unscaledTime < next || label == null) return;
                next = Time.unscaledTime + 1f;
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null) return;
                // rebuild only when something shown changed (vessel switch, SOI change,
                // rename - a rename replaces the string instance, so == is a cheap
                // ref-compare in steady state). ReferenceEquals skips Unity's native
                // fake-null checks; a destroyed cached ref just fails the compare.
                if (ReferenceEquals(v, lastV) && ReferenceEquals(v.mainBody, lastBody)
                    && v.vesselName == lastName) return;
                lastV = v; lastBody = v.mainBody; lastName = v.vesselName;
                List<string> parts = new List<string>();
                CelestialBody b = v.mainBody;
                int guard = 0;
                while (b != null && guard++ < 8)
                {
                    // the stock star's internal name is "Sun" - players know it as Kerbol
                    parts.Insert(0, b.bodyName == "Sun" ? "Kerbol" : b.bodyName);
                    if (b.referenceBody == null || b.referenceBody == b) break;
                    b = b.referenceBody;
                }
                parts.Add(v.vesselName);
                label.text = string.Join("  \\  ", parts.ToArray());
            }
            catch { }
        }
    }

    // Left-edge VESSEL ACTIONS rail: gear / lights / brakes / abort (KSP2 style).
    public class ActionsRail : MonoBehaviour
    {
        enum Kind { Group, Button, Solar, Radiator }  // Group=toggle, Button=momentary (abort)
        class Entry { public Kind kind; public KSPActionGroup group; public Image icon; }
        readonly List<Entry> entries = new List<Entry>();
        float next;
        // the 4Hz tick used to walk every part x module twice per tick just to color two
        // icons - cache the module lists instead. Deployable-module sets can't change
        // without the vessel reference or its part count changing, so those two compares
        // are a complete invalidation key (switch/dock/undock/stage/part loss). No
        // GameEvents: FlightLayout destroys+rebuilds this rail on every re-apply, and
        // leaked handlers on destroyed components throw on every event.
        Vessel cachedVessel;
        int cachedPartCount;
        List<ModuleDeployableSolarPanel> solarCache;
        List<ModuleDeployableRadiator> radCache;
        static readonly Color OnCol = new Color(0.15f, 0.89f, 0.48f, 1f);
        static readonly Color OffCol = new Color(0.93f, 0.93f, 0.94f, 1f);

        public static GameObject Create(Canvas canvas)
        {
            try
            {
                Widgets.DestroyExisting(canvas.transform, "ZKSP2_ActionsRail");
                string[] icons = { "icon_gear.png", "icon_light.png", "icon_brakes.png", "icon_solar.png", "icon_radiator.png", "icon_abort.png" };
                Kind[] kinds = { Kind.Group, Kind.Group, Kind.Group, Kind.Solar, Kind.Radiator, Kind.Button };
                KSPActionGroup[] groups = { KSPActionGroup.Gear, KSPActionGroup.Light, KSPActionGroup.Brakes, KSPActionGroup.None, KSPActionGroup.None, KSPActionGroup.Abort };
                int n = icons.Length;

                float bs = 38f, pad = 8f;
                Vector2 size = new Vector2(66f, n * (bs + 6f) + 2 * pad);
                GameObject panel = Widgets.Panel(canvas.transform, "ZKSP2_ActionsRail",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Cfg.railPos, size);
                ActionsRail rail = panel.AddComponent<ActionsRail>();

                Text vert = Widgets.Label(panel.transform, "railLabel", "VESSEL ACTIONS", 10,
                    TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.94f, 0.9f));
                RectTransform vrt = (RectTransform)vert.transform;
                vrt.anchorMin = new Vector2(0f, 0.5f); vrt.anchorMax = new Vector2(0f, 0.5f);
                vrt.pivot = new Vector2(0.5f, 0.5f);
                vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
                vrt.sizeDelta = new Vector2(size.y - 10f, 12f);
                vrt.anchoredPosition = new Vector2(11f, 0f);
                vrt.localEulerAngles = new Vector3(0f, 0f, 90f);

                for (int i = 0; i < n; i++)
                {
                    Kind kind = kinds[i];
                    KSPActionGroup grp = groups[i];
                    GameObject bgo = new GameObject("btn_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                    RectTransform brt = (RectTransform)bgo.transform;
                    brt.SetParent(panel.transform, false);
                    brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f);
                    brt.pivot = new Vector2(0.5f, 1f);
                    brt.anchoredPosition = new Vector2(9f, -(pad + i * (bs + 6f)));
                    brt.sizeDelta = new Vector2(bs, bs);
                    Image bg = bgo.GetComponent<Image>();
                    Sprite bsp = Widgets.LoadSprite("btn.png", new Vector4(6, 6, 6, 6));
                    if (bsp != null) { bg.sprite = bsp; bg.type = Image.Type.Sliced; }
                    else bg.color = new Color(0.09f, 0.13f, 0.22f, 0.86f);

                    GameObject igo = new GameObject("icon", typeof(RectTransform), typeof(Image));
                    RectTransform irt = (RectTransform)igo.transform;
                    irt.SetParent(bgo.transform, false);
                    irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
                    irt.pivot = new Vector2(0.5f, 0.5f);
                    irt.anchoredPosition = Vector2.zero; irt.sizeDelta = new Vector2(26f, 26f);
                    Image ic = igo.GetComponent<Image>();
                    Sprite isp = Widgets.LoadSprite(icons[i], Vector4.zero);
                    if (isp != null) ic.sprite = isp;
                    ic.color = OffCol;
                    ic.raycastTarget = false;

                    Entry e = new Entry(); e.kind = kind; e.group = grp; e.icon = ic;
                    rail.entries.Add(e);
                    Button btn = bgo.GetComponent<Button>();
                    Widgets.NoNavigation(btn);
                    Entry captured = e;
                    btn.onClick.AddListener(delegate { rail.Activate(captured); Widgets.Deselect(); });
                }
                return panel;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] actions rail create failed: " + e); return null; }
        }

        void Activate(Entry e)
        {
            try
            {
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null) return;
                if (e.kind == Kind.Group || e.kind == Kind.Button) v.ActionGroups.ToggleGroup(e.group);
                else if (e.kind == Kind.Solar) ToggleSolar(v);
                else if (e.kind == Kind.Radiator) ToggleRadiator(v);
            }
            catch { }
        }

        static void ToggleSolar(Vessel v)
        {
            List<ModuleDeployableSolarPanel> mods = v.FindPartModulesImplementing<ModuleDeployableSolarPanel>();
            if (mods == null) return;
            bool ext = AnyExtended(mods);
            for (int i = 0; i < mods.Count; i++)
                try { if (ext) mods[i].Retract(); else mods[i].Extend(); } catch { }
        }
        static void ToggleRadiator(Vessel v)
        {
            List<ModuleDeployableRadiator> mods = v.FindPartModulesImplementing<ModuleDeployableRadiator>();
            if (mods == null) return;
            bool ext = AnyExtendedR(mods);
            for (int i = 0; i < mods.Count; i++)
                try { if (ext) mods[i].Retract(); else mods[i].Extend(); } catch { }
        }
        static bool AnyExtended(List<ModuleDeployableSolarPanel> m)
        {
            if (m == null) return false;
            for (int i = 0; i < m.Count; i++)
                if (m[i].deployState == ModuleDeployablePart.DeployState.EXTENDED
                    || m[i].deployState == ModuleDeployablePart.DeployState.EXTENDING) return true;
            return false;
        }
        static bool AnyExtendedR(List<ModuleDeployableRadiator> m)
        {
            if (m == null) return false;
            for (int i = 0; i < m.Count; i++)
                if (m[i].deployState == ModuleDeployablePart.DeployState.EXTENDED
                    || m[i].deployState == ModuleDeployablePart.DeployState.EXTENDING) return true;
            return false;
        }

        void Update()
        {
            try
            {
                if (Time.unscaledTime < next) return;
                next = Time.unscaledTime + 0.25f;
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null) return;
                if (v != cachedVessel || v.parts.Count != cachedPartCount)
                {
                    cachedVessel = v;
                    cachedPartCount = v.parts.Count;
                    solarCache = v.FindPartModulesImplementing<ModuleDeployableSolarPanel>();
                    radCache = v.FindPartModulesImplementing<ModuleDeployableRadiator>();
                }
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry e = entries[i];
                    if (e.icon == null) continue;
                    bool on = false;
                    switch (e.kind)
                    {
                        case Kind.Group: on = v.ActionGroups[e.group]; break;
                        case Kind.Button: on = false; break; // momentary - never latch (abort)
                        case Kind.Solar: on = AnyExtended(solarCache); break;
                        case Kind.Radiator: on = AnyExtendedR(radCache); break;
                    }
                    e.icon.color = on ? OnCol : OffCol;
                }
            }
            catch { }
        }
    }

    // Bottom-right green GO button: activates the next stage (KSP2's GO).
    // Pause + warp-to-sunrise buttons beside the warp/MET cluster (KSP2 app-bar style).
    public class TimeButtons : MonoBehaviour
    {
        // pin the widget's RIGHT edge just LEFT of the visible warp box ("TimeQuadrant",
        // the same object FlightLayout moves to bottom-center). Last round this glued to
        // FlightUIModeController.timeFrame.panelTransform - a DIFFERENT, unmoved tab panel
        // at top-right - so the buttons flew off-screen. Pin to TimeQuadrant's live world
        // corners instead (the proven CommsPin pattern), immune to where the box is moved.
        RectTransform box;   // the TimeQuadrant rect
        RectTransform pauseBtn, dawnBtn;
        float findNext;
        static readonly Vector3[] bc = new Vector3[4];
        static readonly Vector3[] sc = new Vector3[4];

        public static GameObject Create(Canvas canvas)
        {
            try
            {
                Widgets.DestroyExisting(canvas.transform, "ZKSP2_TimeButtons");
                // on the canvas at a safe on-screen fallback spot (bottom-center); the pin
                // takes over once TimeQuadrant is found. Stacked vertically so pause lines
                // up with the WARP row and dawn with the MET row - the real row positions
                // are measured LIVE in LateUpdate (a creation-time measurement was wrong
                // whenever the box's final size settled later).
                GameObject panel = Widgets.Panel(canvas.transform, "ZKSP2_TimeButtons",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 16f), new Vector2(48f, 96f));
                TimeButtons tb = panel.AddComponent<TimeButtons>();
                tb.pauseBtn = MakeBtn(panel.transform, "pause", "icon_pause.png", new Vector2(0f, 24f), OnPause);
                tb.dawnBtn = MakeBtn(panel.transform, "dawn", "icon_sunrise.png", new Vector2(0f, -24f), OnSunrise);
                return panel;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] time buttons create failed: " + e); return null; }
        }

        void LateUpdate()
        {
            try
            {
                if (box == null)
                {
                    if (Time.unscaledTime < findNext) return;
                    findNext = Time.unscaledTime + 1f;
                    GameObject tq = GameObject.Find("TimeQuadrant");
                    if (tq == null) return; // stay at the bottom-center fallback until found
                    box = tq.transform as RectTransform;
                    if (box == null) return;
                }
                RectTransform self = transform as RectTransform;
                if (self == null) return;
                box.GetWorldCorners(bc);   // 0=BL 1=TL 2=TR 3=BR

                // match the panel's height to the box's LIVE height so its edges line up
                // with the box's edges exactly (world -> local px via our lossy scale)
                float scaleY = self.lossyScale.y;
                if (scaleY < 1e-5f) scaleY = 1e-5f;
                float boxHLocal = (bc[1].y - bc[0].y) / scaleY;
                Vector2 sd = self.sizeDelta;
                if (Mathf.Abs(sd.y - boxHLocal) > 0.5f) { sd.y = boxHLocal; self.sizeDelta = sd; }

                // the two buttons straddle the box's two native rows (WARP top, MET bottom):
                // centers at +/- rowSplit of the box height from its middle
                float half = boxHLocal * Cfg.timeButtonRowSplit;
                if (pauseBtn != null && Mathf.Abs(pauseBtn.anchoredPosition.y - half) > 0.5f)
                    pauseBtn.anchoredPosition = new Vector2(pauseBtn.anchoredPosition.x, half);
                if (dawnBtn != null && Mathf.Abs(dawnBtn.anchoredPosition.y + half) > 0.5f)
                    dawnBtn.anchoredPosition = new Vector2(dawnBtn.anchoredPosition.x, -half);

                self.GetWorldCorners(sc);
                Vector3 center = (sc[0] + sc[2]) * 0.5f;
                float w = sc[2].x - sc[0].x;
                float boxMidY = (bc[0].y + bc[1].y) * 0.5f;
                // right edge of widget sits timeButtonsPos.x px left of the box's left edge
                Vector3 target = new Vector3(bc[0].x + Cfg.timeButtonsPos.x - w * 0.5f,
                                             boxMidY + Cfg.timeButtonsPos.y, center.z);
                transform.position += target - center; // idempotent delta
            }
            catch { }
        }

        static RectTransform MakeBtn(Transform parent, string name, string icon, Vector2 pos, UnityEngine.Events.UnityAction act)
        {
            GameObject bgo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform brt = (RectTransform)bgo.transform;
            brt.SetParent(parent, false);
            brt.anchorMin = new Vector2(1f, 0.5f); brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos; brt.sizeDelta = new Vector2(38f, 38f);
            Image bg = bgo.GetComponent<Image>();
            Sprite bsp = Widgets.LoadSprite("btn.png", new Vector4(6, 6, 6, 6));
            if (bsp != null) { bg.sprite = bsp; bg.type = Image.Type.Sliced; }
            else bg.color = new Color(0.09f, 0.13f, 0.22f, 0.86f);
            GameObject igo = new GameObject("icon", typeof(RectTransform), typeof(Image));
            RectTransform irt = (RectTransform)igo.transform;
            irt.SetParent(bgo.transform, false);
            irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero; irt.sizeDelta = new Vector2(26f, 26f);
            Image ic = igo.GetComponent<Image>();
            Sprite isp = Widgets.LoadSprite(icon, Vector4.zero);
            if (isp != null) ic.sprite = isp;
            ic.color = new Color(0.93f, 0.93f, 0.94f, 1f);
            ic.raycastTarget = false;
            Button btn = bgo.GetComponent<Button>();
            Widgets.NoNavigation(btn);
            btn.onClick.AddListener(delegate { act(); Widgets.Deselect(); });
            return brt;
        }

        // Mirror stock ESC (PauseMenu.Update) EXACTLY. Calling FlightDriver.SetPause
        // directly is a trap (decompiled): it locks ALL input under "gamePause" without
        // opening the menu, and ESC only closes a pause whose menu is displayed - the
        // game wedges frozen until a scene change. Display()/Close() are the safe pair.
        static void OnPause()
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight) return;
                if (!PauseMenu.exists) return; // isOpen/Display/Close NRE without a fetch
                if (!FlightDriver.Pause)
                {
                    // same gate stock uses before opening the menu
                    if (InputLockManager.IsUnlocked(ControlTypes.PAUSE)) PauseMenu.Display();
                }
                else if (PauseMenu.isOpen) PauseMenu.Close();
                // else: paused by someone else (KSPedia, results dialog, another mod) -
                // never unpause behind their back
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] pause failed: " + e); }
        }

        static double ElevOf(Vector3d up, Vector3d toSun) { return 90.0 - Vector3d.Angle(up, toSun); }

        // scan one solar day for the next time the sun rises above the local horizon,
        // then TimeWarp.WarpTo it. Landed/splashed vessels only.
        static void OnSunrise()
        {
            try
            {
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null) return;
                if (!v.LandedOrSplashed)
                {
                    ScreenMessages.PostScreenMessage("Warp to sunrise: only while landed", 3f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                CelestialBody body = v.mainBody;
                CelestialBody sun = Planetarium.fetch != null ? Planetarium.fetch.Sun : null;
                if (sun == null) sun = FlightGlobals.Bodies[0];
                if (body == null || sun == null || body == sun)
                {
                    ScreenMessages.PostScreenMessage("Warp to sunrise: no sun here", 3f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                double rot = Math.Abs(body.rotationPeriod);
                Vector3d axis = body.angularVelocity;
                if (rot < 1 || axis.magnitude < 1e-9)
                {
                    ScreenMessages.PostScreenMessage("Warp to sunrise: this body doesn't rotate", 3f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                double solarDay = (!double.IsNaN(body.solarDayLength) && !double.IsInfinity(body.solarDayLength)
                    && Math.Abs(body.solarDayLength) > 1) ? Math.Abs(body.solarDayLength) : rot;
                Vector3d up0 = (v.GetWorldPos3D() - body.position).normalized;
                Vector3d toSun = (sun.position - body.position).normalized;
                axis = axis.normalized;
                double horizon = -0.5; // degrees; sun just breaking the horizon
                double prevElev = ElevOf(up0, toSun);
                int steps = 760;
                double stepT = solarDay / 720.0;
                for (int i = 1; i <= steps; i++)
                {
                    double t = i * stepT;
                    // toSun is frozen for the scan, so the sun-relative hour angle advances
                    // at the SOLAR rate (360deg per solar day), not the sidereal rate
                    double ang = 360.0 * t / solarDay;
                    Vector3d up = QuaternionD.AngleAxis(ang, axis) * up0;
                    double elev = ElevOf(up, toSun);
                    if (prevElev < horizon && elev >= horizon)
                    {
                        TimeWarp.fetch.WarpTo(Planetarium.GetUniversalTime() + t);
                        ScreenMessages.PostScreenMessage("Warping to sunrise", 3f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    prevElev = elev;
                }
                ScreenMessages.PostScreenMessage("No sunrise within a day here", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] sunrise warp failed: " + e); }
        }
    }

    // Glues the CommNet signal widget to the breadcrumb bar's right edge so the
    // "transmit data" readout sits inline with the location text (KSP2 style).
    // Attached to the TelemetryUpdate object; OnDestroy restores the stock spot.
    public class CommsPin : MonoBehaviour
    {
        Vector3 baseline;
        bool have;
        static readonly Vector3[] bc = new Vector3[4];
        static readonly Vector3[] sc = new Vector3[4];

        void OnDestroy()
        {
            try { if (have) transform.position = baseline; } catch { }
        }

        void LateUpdate()
        {
            try
            {
                if (!have) { baseline = transform.position; have = true; }
                RectTransform bar = BreadcrumbBar.Bar;
                RectTransform self = transform as RectTransform;
                if (bar == null || self == null) return;
                // in map view (or whenever the bar is hidden) the breadcrumb is inactive -
                // don't pin to its stale corners; sit at the stock spot instead
                if (MapView.MapIsEnabled || !bar.gameObject.activeInHierarchy)
                {
                    transform.position = baseline;
                    return;
                }
                bar.GetWorldCorners(bc);   // 0=BL 1=TL 2=TR 3=BR
                self.GetWorldCorners(sc);
                Vector3 center = (sc[0] + sc[2]) * 0.5f;
                float width = sc[2].x - sc[0].x;
                float barMidY = (bc[1].y + bc[3].y) * 0.5f;
                Vector3 target = new Vector3(bc[2].x + 8f + width * 0.5f, barMidY, center.z);
                transform.position += target - center; // idempotent: delta -> 0 once placed
            }
            catch { }
        }
    }

    // Glues the staging stack's RIGHT edge flush-left of the app/MechJeb toolbar
    // (KSP.UI.ApplicationLauncher's launcherSpace rect). Attached to StageManager's
    // GameObject; OnDestroy restores the stock spot. Tracks the toolbar's live left
    // edge, so it stays flush as the toolbar grows/shrinks or the resolution changes.
    public class StagingToolbarPin : MonoBehaviour
    {
        Vector3 baseline;
        bool have;
        float findNext;
        RectTransform toolbar;
        static readonly Vector3[] tc = new Vector3[4];
        static readonly Vector3[] sc = new Vector3[4];

        void OnDestroy() { try { if (have) transform.position = baseline; } catch { } }

        void LateUpdate()
        {
            try
            {
                if (!have) { baseline = transform.position; have = true; }
                // map view: stock hides staging; sit at the stock spot (symmetry w/ CommsPin)
                if (MapView.MapIsEnabled) { transform.position = baseline; return; }

                // LEFT-edge mode: glue the stack's left edge to the screen's left edge (mirror
                // of the toolbar pin). No toolbar needed. Keeps stock Y like the right pin.
                if (Cfg.pinStagingLeft)
                {
                    RectTransform selfL = transform as RectTransform;
                    if (selfL == null) return;
                    selfL.GetWorldCorners(sc);
                    Vector3 centerL = (sc[0] + sc[2]) * 0.5f;
                    float wl = sc[2].x - sc[0].x;
                    float targetCenterX = Cfg.stagingLeftGap + wl * 0.5f; // left edge at the gap
                    transform.position += new Vector3(targetCenterX - centerL.x, 0f, 0f);
                    return;
                }

                if (toolbar == null)
                {
                    if (Time.unscaledTime < findNext) return;
                    findNext = Time.unscaledTime + 1f;
                    if (KSP.UI.Screens.ApplicationLauncher.Instance != null)
                        toolbar = KSP.UI.Screens.ApplicationLauncher.Instance.launcherSpace;
                    if (toolbar == null) return;
                }
                RectTransform self = transform as RectTransform;
                if (self == null) return;
                toolbar.GetWorldCorners(tc); // 0=BL 1=TL 2=TR 3=BR
                self.GetWorldCorners(sc);
                Vector3 center = (sc[0] + sc[2]) * 0.5f;
                float w = sc[2].x - sc[0].x;
                // widget's right edge = toolbar left edge - 6px gap; keep stock Y
                float rightTarget = tc[0].x - 6f;
                // SAFETY: launcherSpace may be a wide container whose left edge is nowhere
                // near the visible toolbar. If it lands in the left half of the screen
                // (center-origin: x<0), it's not the real toolbar edge - keep staging on the
                // right side instead of shoving it to center.
                float minRight = Screen.width * 0.30f;
                if (rightTarget < minRight) rightTarget = minRight;
                transform.position += new Vector3(rightTarget - w * 0.5f - center.x, 0f, 0f);
            }
            catch { }
        }
    }

    // Small G-force readout pinned to the LEFT of the kerbal portraits (KSP2 shows
    // the crew's G load right next to the heads).
    public class GForceMeter : MonoBehaviour
    {
        Text val;
        KerbalPortraitGallery pg;
        float findNext, textNext;
        int lastG10 = int.MinValue; // geeForce x10 rounded - the displayed precision
        static readonly Vector3[] gc = new Vector3[4];
        static readonly Vector3[] sc2 = new Vector3[4];

        public static GameObject Create(Canvas canvas)
        {
            try
            {
                Widgets.DestroyExisting(canvas.transform, "ZKSP2_GForce");
                GameObject panel = Widgets.Panel(canvas.transform, "ZKSP2_GForce",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Cfg.gforcePos, new Vector2(64f, 46f));
                panel.GetComponent<Image>().raycastTarget = false;
                GForceMeter m = panel.AddComponent<GForceMeter>();
                Text cap = Widgets.Label(panel.transform, "cap", "G-FORCE", 9, TextAnchor.UpperCenter,
                    new Color(0.62f, 0.66f, 0.72f, 1f));
                RectTransform crt = (RectTransform)cap.transform;
                crt.offsetMin = new Vector2(2f, 20f); crt.offsetMax = new Vector2(-2f, -4f);
                m.val = Widgets.Label(panel.transform, "val", "", 17, TextAnchor.LowerCenter,
                    new Color(0.93f, 0.93f, 0.94f, 1f));
                RectTransform vrt = (RectTransform)m.val.transform;
                vrt.offsetMin = new Vector2(2f, 4f); vrt.offsetMax = new Vector2(-2f, -18f);
                return panel;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] g-force meter create failed: " + e); return null; }
        }

        void LateUpdate()
        {
            try
            {
                if (pg == null)
                {
                    if (Time.unscaledTime < findNext) return;
                    findNext = Time.unscaledTime + 1f;
                    pg = FindObjectOfType<KerbalPortraitGallery>();
                    if (pg == null) return;
                }
                RectTransform grt = pg.transform as RectTransform;
                RectTransform self = transform as RectTransform;
                if (grt == null || self == null) return;
                grt.GetWorldCorners(gc);
                self.GetWorldCorners(sc2);
                Vector3 center = (sc2[0] + sc2[2]) * 0.5f;
                float w = sc2[2].x - sc2[0].x, h = sc2[1].y - sc2[0].y;
                // left of the gallery, tops aligned
                Vector3 target = new Vector3(gc[0].x - 8f - w * 0.5f, gc[1].y - h * 0.5f, center.z);
                transform.position += target - center;

                if (Time.unscaledTime >= textNext && val != null)
                {
                    textNext = Time.unscaledTime + 0.15f;
                    Vessel v = FlightGlobals.ActiveVessel;
                    // only build the string when the value changes at display precision
                    int g10 = v != null ? (int)Math.Round(v.geeForce * 10.0) : int.MinValue + 1;
                    if (g10 != lastG10)
                    {
                        lastG10 = g10;
                        val.text = v != null ? v.geeForce.ToString("0.0") + "g" : "";
                    }
                }
            }
            catch { }
        }
    }

    // Reads the stock total-delta-V string and (if nukeStagingTotal) hides the stock
    // readout via a CanvasGroup alpha=0 - which keeps the object ACTIVE so its text keeps
    // updating and we can still read it for the GO / LAUNCH buttons.
    internal static class StageDv
    {
        // two-slot CanvasGroup cache (the two callers pass two fixed objects). Keyed by
        // GameObject reference; Unity's fake-null == on a destroyed cached group makes
        // the slot re-fetch itself, so mid-scene staging-UI rebuilds self-heal and the
        // periodic re-apply keeps working (it's what keeps the stock readout hidden).
        static GameObject cachedGo1, cachedGo2;
        static CanvasGroup cachedCg1, cachedCg2;

        static void Hide(GameObject g, bool hide)
        {
            if (g == null) return;
            CanvasGroup cg;
            if (g == cachedGo1 && cachedCg1 != null) cg = cachedCg1;
            else if (g == cachedGo2 && cachedCg2 != null) cg = cachedCg2;
            else
            {
                cg = g.GetComponent<CanvasGroup>();
                if (cg == null) cg = g.AddComponent<CanvasGroup>();
                if (g == cachedGo1 || cachedGo1 == null) { cachedGo1 = g; cachedCg1 = cg; }
                else { cachedGo2 = g; cachedCg2 = cg; }
            }
            float a = hide ? 0f : 1f;
            if (cg.alpha != a) cg.alpha = a; // skip redundant alpha propagation
        }

        public static string Read()
        {
            try
            {
                StageManager sm = StageManager.Instance;
                if (sm == null) return "";
                bool hide = Cfg.nukeStagingTotal;
                if (sm.deltaVTotalSection != null) Hide(sm.deltaVTotalSection.gameObject, hide);
                if (sm.deltaVTotalText != null)
                {
                    Hide(sm.deltaVTotalText.gameObject, hide);
                    return sm.deltaVTotalText.text ?? "";
                }
                return "";
            }
            catch { return ""; }
        }

        public static string Format(string dv)
        {
            if (string.IsNullOrEmpty(dv)) return "";
            dv = dv.Trim();
            return dv.IndexOf('Δ') >= 0 ? dv : ("Δv " + dv); // prefix "Δv " unless present
        }

        // restore the stock readout - call when the GO/LAUNCH widget is removed/disabled
        // so hiding it never gets stuck (the widget's Update is what normally toggles it)
        public static void Unhide()
        {
            try
            {
                StageManager sm = StageManager.Instance;
                if (sm == null) return;
                if (sm.deltaVTotalSection != null) Hide(sm.deltaVTotalSection.gameObject, false);
                if (sm.deltaVTotalText != null) Hide(sm.deltaVTotalText.gameObject, false);
            }
            catch { }
        }
    }

    public class GoStageButton : MonoBehaviour
    {
        Text stageNum;
        Text dvText;
        float next;
        string lastRawDv; // last string Read() returned - skip Format when unchanged
        const float BTN_W = 172f, BTN_H = 50f, DV_H = 26f;

        public static GameObject Create(Canvas canvas)
        {
            try
            {
                Widgets.DestroyExisting(canvas.transform, "ZKSP2_GoButton");
                // dark container holds the green GO button (top) + a dV readout strip (bottom)
                GameObject root = new GameObject("ZKSP2_GoButton", typeof(RectTransform), typeof(Image));
                RectTransform rrt = (RectTransform)root.transform;
                rrt.SetParent(canvas.transform, false);
                rrt.anchorMin = new Vector2(1f, 0f); rrt.anchorMax = new Vector2(1f, 0f);
                rrt.pivot = new Vector2(1f, 0f);
                rrt.anchoredPosition = Cfg.goPos; rrt.sizeDelta = new Vector2(BTN_W, BTN_H + DV_H + 4f);
                Image rootImg = root.GetComponent<Image>();
                Sprite panel = Widgets.LoadSprite("panel.png", new Vector4(7, 7, 7, 7));
                if (panel != null) { rootImg.sprite = panel; rootImg.type = Image.Type.Sliced; }
                else rootImg.color = new Color(0.075f, 0.10f, 0.165f, 0.94f);
                rootImg.raycastTarget = false;
                GoStageButton comp = root.AddComponent<GoStageButton>();

                // the green GO button (top)
                GameObject go = new GameObject("go", typeof(RectTransform), typeof(Image), typeof(Button));
                RectTransform rt = (RectTransform)go.transform;
                rt.SetParent(root.transform, false);
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -2f); rt.sizeDelta = new Vector2(BTN_W - 4f, BTN_H);
                Image img = go.GetComponent<Image>();
                Sprite normal = Widgets.LoadSprite("go_normal.png", Vector4.zero);
                Sprite hover = Widgets.LoadSprite("go_hover.png", Vector4.zero);
                Sprite down = Widgets.LoadSprite("go_down.png", Vector4.zero);
                if (normal != null) img.sprite = normal; else img.color = new Color(0f, 0.8f, 0.32f, 1f);
                Button btn = go.GetComponent<Button>();
                if (hover != null && down != null)
                {
                    btn.transition = Selectable.Transition.SpriteSwap;
                    SpriteState ss = new SpriteState();
                    ss.highlightedSprite = hover; ss.pressedSprite = down;
                    btn.spriteState = ss;
                }
                Widgets.NoNavigation(btn);
                btn.onClick.AddListener(delegate { Fire(); Widgets.Deselect(); });

                Text goTxt = Widgets.Label(go.transform, "goTxt", "GO", 26, TextAnchor.MiddleLeft, new Color(0.02f, 0.10f, 0.05f, 1f));
                RectTransform grt = (RectTransform)goTxt.transform;
                grt.offsetMin = new Vector2(18f, 2f); grt.offsetMax = new Vector2(-44f, -2f);
                goTxt.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.12f);

                comp.stageNum = Widgets.Label(go.transform, "stg", "", 16, TextAnchor.MiddleCenter,
                    new Color(0.04f, 0.06f, 0.05f, 1f));
                comp.stageNum.GetComponent<Outline>().effectColor = Color.clear;
                RectTransform srt = (RectTransform)comp.stageNum.transform;
                srt.anchorMin = new Vector2(1f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
                srt.sizeDelta = new Vector2(26f, 22f);
                srt.anchoredPosition = new Vector2(-22f, 0f);

                // dV readout strip (bottom) - green mono on the dark container, KSP2 style
                comp.dvText = Widgets.Label(root.transform, "dv", "", 15, TextAnchor.MiddleCenter,
                    new Color(0f, 0.89f, 0.322f, 1f));
                RectTransform dvrt = (RectTransform)comp.dvText.transform;
                dvrt.anchorMin = new Vector2(0f, 0f); dvrt.anchorMax = new Vector2(1f, 0f);
                dvrt.pivot = new Vector2(0.5f, 0f);
                dvrt.offsetMin = new Vector2(6f, 3f); dvrt.offsetMax = new Vector2(-6f, 0f);
                dvrt.sizeDelta = new Vector2(0f, DV_H);
                dvrt.anchoredPosition = new Vector2(0f, 3f);
                return root;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] GO button create failed: " + e); return null; }
        }

        static void Fire()
        {
            try
            {
                // mirror the stock spacebar guards (InputLock, stage lock, control blocked, EVA)
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null || v.isEVA) return;
                if (!InputLockManager.IsUnlocked(ControlTypes.STAGING)) return;
                if (FlightInputHandler.fetch != null && FlightInputHandler.fetch.stageLock) return;
                if (v.ActionControlBlocked(KSPActionGroup.Stage)) return;
                StageManager.ActivateNextStage();
                v.ActionGroups.ToggleGroup(KSPActionGroup.Stage); // stock also fires the Stage AG
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] stage activate failed: " + e); }
        }

        void Update()
        {
            try
            {
                if (Time.unscaledTime < next) return;
                next = Time.unscaledTime + 0.5f;
                Vessel v = FlightGlobals.ActiveVessel;
                if (stageNum != null) stageNum.text = v != null ? v.currentStage.ToString() : "";
                // Read() must run every tick - its Hide() side effect owns keeping the
                // stock readout hidden - but Format+assign only when the raw text changed
                string raw = StageDv.Read();
                if (dvText != null && !string.Equals(raw, lastRawDv))
                {
                    lastRawDv = raw;
                    dvText.text = StageDv.Format(raw);
                }
            }
            catch { }
        }
    }

    // Permanently offsets the bottom-left UI mode frame by patching its
    // UIPanelTransition.states[] (survives collapse/expand; no per-frame work).
    // Recipe verified by decompiling FlightUIModeController: Instance.uiModeFrame is a
    // public field; state positions are public; SetNavBallHPos never moves this frame.
    internal static class ModeFramePatcher
    {
        static Vector2 _applied;

        public static void ResetScene() { _applied = Vector2.zero; } // scene objects are fresh

        public static void Apply(Vector2 target)
        {
            try
            {
                FlightUIModeController c = FlightUIModeController.Instance;
                if (c == null || c.uiModeFrame == null) return;
                Vector2 delta = target - _applied;
                if (delta == Vector2.zero) return;
                KSP.UI.UIPanelTransition mf = c.uiModeFrame;
                if (mf.states != null)
                    for (int i = 0; i < mf.states.Length; i++)
                        if (mf.states[i] != null) mf.states[i].position += delta;
                if (mf.panelTransform != null) mf.panelTransform.anchoredPosition += delta;
                _applied = target;
                Debug.Log("[ZThemeKSP2Layout] mode frame offset -> " + target);
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] mode frame patch failed: " + e); }
        }
    }

    // Moves the docking-mode control quadrant (CTRL MODE ROT/LIN input display) the same
    // way ModeFramePatcher moves uiModeFrame: FlightUIModeController.dockingRotQuadrant and
    // .dockingLinQuadrant are both public UIPanelTransition fields. Both are shifted by the
    // same offset so the panel lands in one spot whichever CTRL MODE the player is in.
    internal static class DockQuadrantPatcher
    {
        static Vector2 _appliedRot, _appliedLin;

        public static void ResetScene() { _appliedRot = Vector2.zero; _appliedLin = Vector2.zero; }

        public static void Apply(Vector2 target)
        {
            try
            {
                FlightUIModeController c = FlightUIModeController.Instance;
                if (c == null) return;
                Move(c.dockingRotQuadrant, target, ref _appliedRot);
                Move(c.dockingLinQuadrant, target, ref _appliedLin);
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] dock quadrant patch failed: " + e); }
        }

        static void Move(KSP.UI.UIPanelTransition mf, Vector2 target, ref Vector2 applied)
        {
            if (mf == null) return;
            Vector2 delta = target - applied;
            if (delta == Vector2.zero) return;
            if (mf.states != null)
                for (int i = 0; i < mf.states.Length; i++)
                    if (mf.states[i] != null) mf.states[i].position += delta;
            if (mf.panelTransform != null) mf.panelTransform.anchoredPosition += delta;
            applied = target;
        }
    }

    // Same technique for maneuver mode: FlightUIModeController.manNodeEditor and
    // .manNodeHandleEditor are both public UIPanelTransition fields (the maneuver-mode
    // bottom panel + the node dV handle editor). Both shifted by the same offset.
    internal static class ManQuadrantPatcher
    {
        static Vector2 _appliedNode, _appliedHandle;

        public static void ResetScene() { _appliedNode = Vector2.zero; _appliedHandle = Vector2.zero; }

        public static void Apply(Vector2 target)
        {
            try
            {
                FlightUIModeController c = FlightUIModeController.Instance;
                if (c == null) return;
                Move(c.manNodeEditor, target, ref _appliedNode);
                Move(c.manNodeHandleEditor, target, ref _appliedHandle);
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] man quadrant patch failed: " + e); }
        }

        static void Move(KSP.UI.UIPanelTransition mf, Vector2 target, ref Vector2 applied)
        {
            if (mf == null) return;
            Vector2 delta = target - applied;
            if (delta == Vector2.zero) return;
            if (mf.states != null)
                for (int i = 0; i < mf.states.Length; i++)
                    if (mf.states[i] != null) mf.states[i].position += delta;
            if (mf.panelTransform != null) mf.panelTransform.anchoredPosition += delta;
            applied = target;
        }
    }

    // Recolors the flight staging cards toward KSP2 (dark slate backplate + green dV text).
    // Uses StageIcon's PUBLIC SetBackgroundColor, StageManager.deltaVTotalText (public TMP),
    // and reflects StageGroup.DeltaVHeadingText. Re-applied on a slow cadence because stock
    // rewrites these on every rebuild. Borders are left alone (they're selection highlights).
    internal static class StagePainter
    {
        static System.Reflection.FieldInfo _dvHeadingField;
        static bool _reflectTried;

        public static void Paint()
        {
            try
            {
                StageManager sm = StageManager.Instance;
                if (sm == null || sm.Stages == null) return;

                if (sm.deltaVTotalText != null) sm.deltaVTotalText.color = Cfg.stageDvColor;

                if (!_reflectTried)
                {
                    _reflectTried = true;
                    try
                    {
                        _dvHeadingField = typeof(StageGroup).GetField("DeltaVHeadingText",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.NonPublic);
                    }
                    catch { }
                }

                for (int s = 0; s < sm.Stages.Count; s++)
                {
                    StageGroup grp = sm.Stages[s];
                    if (grp == null) continue;
                    if (_dvHeadingField != null)
                    {
                        try
                        {
                            TMP_Text dvt = _dvHeadingField.GetValue(grp) as TMP_Text;
                            if (dvt != null) dvt.color = Cfg.stageDvColor;
                        }
                        catch { }
                    }
                    if (grp.Icons == null) continue;
                    for (int i = 0; i < grp.Icons.Count; i++)
                    {
                        StageIcon ic = grp.Icons[i];
                        if (ic == null) continue;
                        try { ic.SetBackgroundColor(Cfg.stageCardColor); } catch { }
                    }
                }
            }
            catch { }
        }
    }

    // Builds a TMP_FontAsset at runtime for KSP's OLD TextMeshPro from the mod-shipped
    // SDF atlas of JetBrains Mono (the real KSP2 UI font, OFL). Mirrors the exact recipe
    // KSP itself uses internally (KSPFontAsset.GetFontAsset): CreateInstance -> atlas ->
    // material (TextMeshPro/Distance Field) -> AddFaceInfo -> AddGlyphInfo ->
    // AddKerningInfo -> ReadFontDefinition. Order is load-bearing.
    internal static class TmpFontBuilder
    {
        static TMP_FontAsset _asset;
        static bool _failed;

        public static TMP_FontAsset Get()
        {
            if (_asset != null || _failed) return _asset;
            try { _asset = Build(); }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] TMP font build failed: " + e); }
            if (_asset == null) { _failed = true; Debug.LogWarning("[ZThemeKSP2Layout] TMP font unavailable; stock TMP text keeps its font"); }
            else Debug.Log("[ZThemeKSP2Layout] TMP font asset built: " + _asset.name);
            return _asset;
        }

        static float PF(ConfigNode n, string key, float def)
        {
            float f;
            return float.TryParse(n.GetValue(key), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out f) ? f : def;
        }

        static TMP_FontAsset Build()
        {
            string dir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/ZThemeKSP2/FontData/");
            string pngPath = dir + "JBM_SDF.png";
            string cfgPath = dir + "JBM_SDF.cfg";
            if (!File.Exists(pngPath) || !File.Exists(cfgPath)) return null;

            ConfigNode root = ConfigNode.Load(cfgPath);
            ConfigNode n = root != null ? root.GetNode("ZKSP2FontData") : null;
            if (n == null) return null;

            Texture2D atlasTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(atlasTex, File.ReadAllBytes(pngPath))) return null;
            atlasTex.wrapMode = TextureWrapMode.Clamp;
            atlasTex.filterMode = FilterMode.Bilinear;

            float pointSize = PF(n, "pointSize", 64f);
            float padding = PF(n, "padding", 6f);
            float atlasSize = PF(n, "atlasSize", 1024f);

            Shader sh = Shader.Find("TextMeshPro/Distance Field");
            if (sh == null) { Debug.LogWarning("[ZThemeKSP2Layout] TMP DF shader not found"); return null; }

            TMP_FontAsset fa = ScriptableObject.CreateInstance<TMP_FontAsset>();
            string faName = n.GetValue("name");
            fa.name = string.IsNullOrEmpty(faName) ? "ZKSP2 JBM SDF" : faName;
            fa.atlas = atlasTex;

            Material mat = new Material(sh);
            mat.name = fa.name + " Material";
            mat.SetTexture("_MainTex", atlasTex);
            mat.SetFloat("_TextureWidth", atlasTex.width);
            mat.SetFloat("_TextureHeight", atlasTex.height);
            mat.SetFloat("_GradientScale", padding + 1f);
            mat.SetFloat("_WeightNormal", 0f);
            mat.SetFloat("_WeightBold", 0.75f);
            try { ShaderUtilities.UpdateShaderRatios(mat); } catch { }
            fa.material = mat;
            fa.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;

            FaceInfo fi = new FaceInfo();
            fi.Name = fa.name;
            fi.PointSize = pointSize;
            fi.Scale = 1f;
            fi.LineHeight = PF(n, "lineHeight", pointSize * 1.32f);
            fi.Ascender = PF(n, "ascender", pointSize * 1.02f);
            fi.Descender = PF(n, "descender", -pointSize * 0.3f);
            fi.Baseline = 0f;
            fi.CapHeight = PF(n, "capHeight", 0f); // halo-free; 0 = derive from 'H'
            fi.CenterLine = 0f;
            fi.SuperscriptOffset = fi.Ascender;
            fi.SubscriptOffset = fi.Descender;
            fi.SubSize = 0.5f;
            fi.Underline = fi.Descender * 0.5f;
            fi.UnderlineThickness = 2f;
            fi.Padding = padding;
            fi.AtlasWidth = atlasSize;
            fi.AtlasHeight = atlasSize;
            fa.AddFaceInfo(fi); // MUST precede AddGlyphInfo

            ConfigNode[] gs = n.GetNodes("GLYPH");
            if (gs == null || gs.Length == 0) return null;
            TMP_Glyph[] glyphs = new TMP_Glyph[gs.Length];
            for (int i = 0; i < gs.Length; i++)
            {
                TMP_Glyph tg = new TMP_Glyph();
                tg.id = (int)PF(gs[i], "id", 0f);
                tg.x = PF(gs[i], "x", 0f);
                tg.y = PF(gs[i], "y", 0f);
                tg.width = PF(gs[i], "w", 0f);
                tg.height = PF(gs[i], "h", 0f);
                tg.xOffset = PF(gs[i], "ox", 0f);
                tg.yOffset = PF(gs[i], "oy", 0f);
                tg.xAdvance = PF(gs[i], "adv", 0f);
                tg.scale = 1f;
                glyphs[i] = tg;
            }
            fa.AddGlyphInfo(glyphs);
            fa.AddKerningInfo(new KerningTable()); // required even when empty
            fa.normalStyle = 0f; fa.normalSpacingOffset = 0f;
            fa.boldStyle = 0.75f; fa.boldSpacing = 7f;
            fa.italicStyle = 35; fa.tabSize = 10;
            fa.fallbackFontAssets = new List<TMP_FontAsset>();
            fa.ReadFontDefinition(); // builds the character dictionary; must be last
            return fa;
        }
    }

    // Per-text marker: stores the ONE true native font+size, captured the first time THIS
    // component's object is seen. It lives and dies with the text object, so pooled or
    // rebuilt UI (VAB part list!) can never inherit stale sizes from a dead object the way
    // the old static instance-id maps allowed - that stale-size re-apply was fighting
    // stock's own dynamic sizing every sweep (periodic lag + "some text small sometimes").
    internal class ZFontMark : MonoBehaviour
    {
        public TMP_FontAsset origTmp; // set for TMP texts
        public Font origLegacy;      // set for legacy UI.Text
        public float origSize;       // native size at first sight
    }

    internal static class FontSwap
    {
        static Font _osFont;
        static string _osFontName;
        static bool _osFailed;
        // original fonts already chained as fallback - bounds the shared asset's fallback list
        static readonly HashSet<int> _fallbackSeen = new HashSet<int>();
        // editor part-size badge text: "1.25m", "2.5m", "0.625m"... (nothing else in KSP's UI
        // reads as a bare number+"m") - these get boosted rather than globally shrunk
        static readonly Regex SizeTagRe = new Regex(@"^\d+(\.\d+)?m$", RegexOptions.Compiled);
        static bool IsSizeTag(string s) { return HighLogic.LoadedSceneIsEditor && !string.IsNullOrEmpty(s) && SizeTagRe.IsMatch(s.Trim()); }
        // the altimeter's ATMOSPHERE caption (a serialized prefab label - decompile found
        // no code-side field for it) wraps under the wider mono font; match it by text
        static bool IsAtmoLabel(string s)
        {
            return HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(s)
                && s.Trim().Equals("atmosphere", StringComparison.OrdinalIgnoreCase);
        }

        // live-reload: only rebuild the OS font if the configured name actually changed,
        // so the `t.font == _osFont` early-out keeps skipping already-swapped persistent text
        public static void Reset() { if (_osFontName != Cfg.fontName) _osFont = null; _osFailed = false; }
        public static void NewScene()
        {
            // Per-text state needs no clearing: it lives on ZFontMark components, which
            // are destroyed with their objects. Persistent (DontDestroyOnLoad) texts
            // keep their mark - and with it their TRUE native size - across scenes, so the
            // size multiplier can never compound (the original "huge/tiny" bug) and dead
            // objects can never leak stale sizes onto recycled ones.
            InvalidateRoots(); // the scene swap replaced most root canvases
        }

        // pick the size multiplier for one text element: badges grow, ATMOSPHERE shrinks
        // harder, everything else gets the global monospace shrink
        static float MultFor(string txt, out bool atmo)
        {
            atmo = IsAtmoLabel(txt);
            if (atmo) return Cfg.atmoLabelMult;
            return IsSizeTag(txt) ? Cfg.sizeTagFontMult : Cfg.fontSizeMult;
        }

        static void ApplyTmp(TMP_Text t, TMP_FontAsset fa)
        {
            try
            {
                if (t == null) return;
                // font compare first: it's a managed read, while scene.IsValid costs 3
                // native calls - already-swapped texts (the common case every sweep) now
                // exit before any interop. No mutation happens before the scene check, so
                // prefab assets still can't be touched.
                if (t.font == fa || t.font == null) return;
                if (!t.gameObject.scene.IsValid()) return;
                ZFontMark mark = t.GetComponent<ZFontMark>();
                bool atmo;
                float mult = MultFor(t.text, out atmo);
                if (mark == null)
                {
                    // first sight of THIS object: capture its true native font+size, then
                    // adjust the size exactly once (absolute, from native - can't compound)
                    mark = t.gameObject.AddComponent<ZFontMark>();
                    mark.origTmp = t.font;
                    mark.origSize = t.fontSize;
                    if (!t.enableAutoSizing && mult != 1f)
                        t.fontSize = mark.origSize * mult;
                }
                else if (!t.enableAutoSizing && mult != 1f && t.fontSize == mark.origSize)
                {
                    // stock rebuilt/rebound this pooled label: it reset the font (that's why
                    // we're here) and set the size back to the SAME native value we captured
                    // - re-apply the adjust for the CURRENT text (a rebind can change a label
                    // from body text to a "1.25m" badge or back). Any OTHER size means stock
                    // is managing it dynamically - leave it alone, never fight stock.
                    t.fontSize = mark.origSize * mult;
                }
                if (atmo) t.enableWordWrapping = false; // never let the final E wrap
                // chain the original as fallback so glyphs we lack still render
                // (bounded: each original font added at most once ever)
                if (fa.fallbackFontAssets != null && _fallbackSeen.Add(t.font.GetInstanceID())
                    && !fa.fallbackFontAssets.Contains(t.font))
                    fa.fallbackFontAssets.Add(t.font);
                t.font = fa;
            }
            catch { }
        }

        static void ApplyLegacy(Text t)
        {
            try
            {
                if (t == null) return;
                // null-font guard (mirrors ApplyTmp): marking a null-font text would store
                // origLegacy=null, which Restore()'s dispatch can never match.
                // font compare before scene.IsValid for the same reason as ApplyTmp.
                if (t.font == _osFont || t.font == null) return;
                if (!t.gameObject.scene.IsValid()) return;
                ZFontMark mark = t.GetComponent<ZFontMark>();
                bool atmo;
                float mult = MultFor(t.text, out atmo);
                if (mark == null)
                {
                    mark = t.gameObject.AddComponent<ZFontMark>();
                    mark.origLegacy = t.font;
                    mark.origSize = t.fontSize;
                    if (mult != 1f)
                        t.fontSize = Mathf.Max(1, Mathf.RoundToInt(mark.origSize * mult));
                }
                else if (mult != 1f && t.fontSize == Mathf.RoundToInt(mark.origSize))
                {
                    // pooled-label rebind (see ApplyTmp): size is back at native - re-adjust
                    t.fontSize = Mathf.Max(1, Mathf.RoundToInt(mark.origSize * mult));
                }
                if (atmo) t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.font = _osFont;
            }
            catch { }
        }

        static void EnsureOsFont()
        {
            if (_osFont == null && !_osFailed)
            {
                try { _osFont = Font.CreateDynamicFontFromOSFont(Cfg.fontName, 16); _osFontName = Cfg.fontName; } catch { }
                if (_osFont == null) { _osFailed = true; Debug.LogWarning("[ZThemeKSP2Layout] OS font not found: " + Cfg.fontName); }
            }
        }

        // periodic sweeps walk only LIVE canvas hierarchies. The old code ran
        // Resources.FindObjectsOfTypeAll<TMP_Text>/<Text> here - a walk of the ENTIRE
        // loaded-object table (incl. every part prefab's texts, ~10^5 objects) plus a
        // large managed array, twice per tick: that burst WAS the periodic in-game hitch.
        // The canvas list itself still needs one FindObjectsOfTypeAll<Canvas> (the only
        // way to also catch inactive/mod-created canvases) but is refreshed at most every
        // ROOT_REFRESH seconds; a text on a brand-new ROOT canvas waits for the next
        // refresh - nested canvases and new texts under known roots are caught every sweep.
        // Known limit: a world-space 3D TMP label with NO canvas ancestor (mod-created;
        // stock UI text always lives under a canvas) is never reached - accepted, it's
        // outside this mod's UI-theming scope.
        static readonly List<Canvas> _roots = new List<Canvas>();
        static float _rootsNext;
        static float _fastRefreshUntil; // scenes build root canvases for several seconds
        const float ROOT_REFRESH = 10f;
        const float ROOT_REFRESH_FAST = 2f;

        // scene change killed/created root canvases - refresh the list on the next sweep,
        // and keep refreshing fast while the new scene is still building its UI (otherwise
        // a root canvas born mid-burst would wait the full steady interval for its font)
        public static void InvalidateRoots()
        {
            _rootsNext = 0f;
            _fastRefreshUntil = Time.unscaledTime + 9f;
        }

        public static void Sweep()
        {
            try
            {
                if (!Cfg.swapFont) { Restore(); return; }

                if (Time.unscaledTime >= _rootsNext)
                {
                    _rootsNext = Time.unscaledTime +
                        (Time.unscaledTime < _fastRefreshUntil ? ROOT_REFRESH_FAST : ROOT_REFRESH);
                    _roots.Clear();
                    Canvas[] all = Resources.FindObjectsOfTypeAll<Canvas>();
                    for (int i = 0; i < all.Length; i++)
                    {
                        Canvas c = all[i];
                        if (c == null || !c.gameObject.scene.IsValid()) continue; // prefab/asset
                        if (!c.isRootCanvas) continue; // reached via its root's walk
                        _roots.Add(c);
                    }
                }

                for (int i = 0; i < _roots.Count; i++)
                {
                    Canvas c = _roots[i];
                    if (c == null) continue; // destroyed since the last refresh
                    SweepScope(c.transform);
                }
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] font sweep error: " + e); }
        }

        // targeted sweep of ONE subtree (a freshly-built part-action window) - runs the
        // same swap the frame the window's texts spawn, so the font applies instantly
        // without paying for a full-scene FindObjectsOfTypeAll
        // reused result buffers: the list overloads clear + fill in place, so the
        // periodic sweeps allocate nothing (arrays here fed the Boehm GC every tick)
        static readonly List<TMP_Text> _tmpBuf = new List<TMP_Text>();
        static readonly List<Text> _txtBuf = new List<Text>();

        public static void SweepScope(Transform root)
        {
            try
            {
                if (!Cfg.swapFont || root == null) return;
                TMP_FontAsset fa = TmpFontBuilder.Get();
                if (fa != null)
                {
                    root.GetComponentsInChildren<TMP_Text>(true, _tmpBuf);
                    for (int i = 0; i < _tmpBuf.Count; i++) ApplyTmp(_tmpBuf[i], fa);
                    _tmpBuf.Clear(); // don't pin dead texts between sweeps
                }
                EnsureOsFont();
                if (_osFont != null)
                {
                    root.GetComponentsInChildren<Text>(true, _txtBuf);
                    for (int i = 0; i < _txtBuf.Count; i++) ApplyLegacy(_txtBuf[i]);
                    _txtBuf.Clear();
                }
            }
            catch { }
        }

        // swapFont turned off via live-reload: put every marked text back and drop the marks.
        // PUBLIC + called directly from the reload handlers: every periodic Sweep() call is
        // gated on swapFont==true, so the old "Sweep routes to Restore" path was unreachable.
        public static void Restore()
        {
            try
            {
                ZFontMark[] marks = Resources.FindObjectsOfTypeAll<ZFontMark>();
                for (int i = 0; i < marks.Length; i++)
                {
                    try
                    {
                        ZFontMark m = marks[i];
                        if (m == null || !m.gameObject.scene.IsValid()) continue;
                        if (m.origTmp != null)
                        {
                            TMP_Text t = m.GetComponent<TMP_Text>();
                            if (t != null)
                            {
                                if (t.font != m.origTmp) t.font = m.origTmp;
                                // restore unconditionally so an autosizing flip can't strand a shrunk size
                                t.fontSize = m.origSize;
                            }
                        }
                        else if (m.origLegacy != null)
                        {
                            Text t = m.GetComponent<Text>();
                            if (t != null)
                            {
                                if (t.font != m.origLegacy) t.font = m.origLegacy;
                                t.fontSize = Mathf.Max(1, Mathf.RoundToInt(m.origSize));
                            }
                        }
                        // IMMEDIATE, not deferred: the F6 reload path re-sweeps in this same
                        // frame, and a pending-destroyed mark still satisfies GetComponent -
                        // the re-sweep would then never create a fresh mark, so the SECOND
                        // multiplier tweak (and any later swapFont-off revert) went dead
                        UnityEngine.Object.DestroyImmediate(m);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    internal static class UiDump
    {
        public static void DumpAll(string tag)
        {
            try
            {
                Debug.Log("[ZKSP2DUMP] ================ UI TREE (" + tag + ") ================");
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (Canvas c in canvases)
                {
                    if (c == null || !c.gameObject.scene.IsValid()) continue;
                    Debug.Log("[ZKSP2DUMP] ==== CANVAS: " + c.name + " ====");
                    Walk(c.transform, 0);
                }
                Debug.Log("[ZKSP2DUMP] ================ END (" + tag + ") ================");
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] dump failed: " + e); }
        }

        static void Walk(Transform t, int depth)
        {
            if (t == null || depth > 40) return;
            StringBuilder sb = new StringBuilder();
            sb.Append("[ZKSP2DUMP]");
            for (int i = 0; i < depth; i++) sb.Append("  ");
            sb.Append(t.name);
            RectTransform rt = t as RectTransform;
            if (rt != null)
                sb.Append(" | anchoredPos=" + rt.anchoredPosition + " worldPos=" + rt.position +
                          " size=" + rt.sizeDelta + " active=" + t.gameObject.activeInHierarchy);
            Component[] comps = t.GetComponents<Component>();
            foreach (Component comp in comps)
                if (comp != null && !(comp is Transform)) sb.Append(" <" + comp.GetType().Name + ">");
            Debug.Log(sb.ToString());
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1);
        }
    }

    // Instant PAW font swap. Ground truth (decompiled UIPartActionWindow):
    //  - onPartActionUIShown fires at the END of Setup, AFTER every item text exists ->
    //    a targeted SweepScope that same frame applies the font instantly.
    //  - onPartActionUICreate fires at the START of every CreatePartList call, BEFORE
    //    items are instantiated - and once per frame per OPEN window. The old handler
    //    ran a full-scene sweep there: too early for the new texts AND a per-frame
    //    perf hit. It now schedules one throttled end-of-frame targeted sweep instead
    //    (covers dirty rebuilds + incremental field adds, which never re-fire UIShown).
    internal static class PawFontHook
    {
        static MonoBehaviour runner; // live addon that can host the end-of-frame coroutine
        static float nextCreateSweep;
        static bool hooked;

        public static void Add(MonoBehaviour owner)
        {
            runner = owner;
            if (hooked) return;
            try
            {
                GameEvents.onPartActionUIShown.Add(OnShown);
                GameEvents.onPartActionUICreate.Add(OnCreate);
                hooked = true;
            }
            catch { }
        }

        public static void Remove(MonoBehaviour owner)
        {
            if (runner == owner) runner = null;
            if (!hooked || runner != null) return; // another scene's addon took over
            try
            {
                GameEvents.onPartActionUIShown.Remove(OnShown);
                GameEvents.onPartActionUICreate.Remove(OnCreate);
                hooked = false;
            }
            catch { }
        }

        static void OnShown(UIPartActionWindow w, Part p)
        {
            try { if (Cfg.enabled && Cfg.swapFont && w != null) FontSwap.SweepScope(w.transform); } catch { }
        }

        static void OnCreate(Part p)
        {
            try
            {
                if (!Cfg.enabled || !Cfg.swapFont || runner == null || p == null) return;
                // one coalesced end-of-frame sweep of ALL open windows (a per-part throttle
                // would let one rapidly-rebuilding window starve another's sweep)
                if (Time.unscaledTime < nextCreateSweep) return;
                nextCreateSweep = Time.unscaledTime + 0.15f;
                runner.StartCoroutine(EndOfFrameSweep());
            }
            catch { }
        }

        // items spawn synchronously right after the event, inside the same frame -
        // end-of-frame is the earliest moment they all exist
        static readonly WaitForEndOfFrame _eof = new WaitForEndOfFrame(); // yield objects are reusable
        static IEnumerator EndOfFrameSweep()
        {
            yield return _eof;
            SweepAllWindows();
        }

        static void SweepAllWindows()
        {
            try
            {
                if (!Cfg.enabled || !Cfg.swapFont) return;
                UIPartActionController c = UIPartActionController.Instance;
                if (c == null || c.windows == null) return;
                for (int i = 0; i < c.windows.Count; i++)
                {
                    UIPartActionWindow w = c.windows[i];
                    if (w != null) FontSwap.SweepScope(w.transform);
                }
            }
            catch { }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightLayout : MonoBehaviour
    {
        GameObject trimGauges;
        readonly List<GameObject> ownedWidgets = new List<GameObject>();
        float nextSweep;
        float nextHousekeep; // staging paint / resources / unhide - configured cadence
        float sweepBurstUntil; // sweep fast while the scene's UI is still being built
        bool resourcesOpened; // open the panel once per scene, then respect the user closing it
        KSP.UI.UITimeWarpHoverController hoverCtrl; // the chevron slide-panel (pinned open)
        KerbalPortraitGallery portraitsRef;
        Vector3 portraitOrigScale = Vector3.zero;
        GameObject atmoLabel; // the altimeter "ATMOSPHERE" caption we hide (cached)
        GameObject alarmBtn; // the stock "next alarm" clock icon beside MET we hide (cached)
        GameObject morningBtn; // the stock warp-to-next-morning sun button we hide (cached)
        float morningFindNext; // FindObjectOfType is slow - rescan at most 1/s until found

        void Start()
        {
            if (!Cfg.loaded) Cfg.Load();
            ModeFramePatcher.ResetScene();
            DockQuadrantPatcher.ResetScene();
            ManQuadrantPatcher.ResetScene();
            resourcesOpened = false;
            PawFontHook.Add(this);
            sweepBurstUntil = Time.unscaledTime + 8f;
            FontSwap.InvalidateRoots(); // flight scene = fresh canvas set
            // build the TMP font asset NOW so the first real swap doesn't hitch ("font
            // takes time to load") + swap once immediately, before the settle coroutine
            try { if (Cfg.enabled && Cfg.swapFont) { TmpFontBuilder.Get(); FontSwap.Sweep(); } } catch { }
            if (Cfg.enabled) StartCoroutine(Apply());
        }

        void OnDestroy()
        {
            PawFontHook.Remove(this);
            RestoreWarpHover();
            RestorePortraitScale();
            RestoreAtmosphereLabel();
            RestoreAlarmClock();
            RestoreMorningBtn();
            // the main canvas persists across scenes - remove our widgets on flight exit
            for (int i = 0; i < ownedWidgets.Count; i++)
                if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
            ownedWidgets.Clear();
        }

        // Hide (or restore) the altimeter's "ATMOSPHERE" caption. Decompile confirmed it's
        // a serialized prefab TMP/Text child with NO code-side field, so we tree-walk the
        // altimeterFrame by text and cache the GameObject.
        void ApplyAtmosphereLabel()
        {
            try
            {
                if (atmoLabel == null && Cfg.hideAtmosphere) atmoLabel = FindAtmoLabel();
                if (atmoLabel != null) atmoLabel.SetActive(!Cfg.hideAtmosphere);
            }
            catch { }
        }

        void RestoreAtmosphereLabel()
        {
            try { if (atmoLabel != null) atmoLabel.SetActive(true); } catch { }
        }

        static GameObject FindAtmoLabel()
        {
            try
            {
                FlightUIModeController c = FlightUIModeController.Instance;
                if (c == null || c.altimeterFrame == null || c.altimeterFrame.panelTransform == null) return null;
                Transform root = c.altimeterFrame.panelTransform;
                foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
                    if (t != null && t.text != null && t.text.Trim().Equals("ATMOSPHERE", StringComparison.OrdinalIgnoreCase))
                        return t.gameObject;
                foreach (Text t in root.GetComponentsInChildren<Text>(true))
                    if (t != null && t.text != null && t.text.Trim().Equals("ATMOSPHERE", StringComparison.OrdinalIgnoreCase))
                        return t.gameObject;
            }
            catch { }
            return null;
        }

        // Hide (or restore) the stock "next alarm" clock icon beside MET. It's a fixed
        // serialized field on FlightUIModeController that stock itself toggles active
        // whenever an alarm gets scheduled/cleared, so a one-time hide isn't enough -
        // Update() re-forces it every frame it turns back on.
        void ApplyAlarmClock()
        {
            try
            {
                if (alarmBtn == null)
                {
                    FlightUIModeController c = FlightUIModeController.Instance;
                    if (c != null && c.alarmClockButton != null) alarmBtn = c.alarmClockButton.gameObject;
                }
                if (alarmBtn != null && alarmBtn.activeSelf == Cfg.hideAlarmClock)
                    alarmBtn.SetActive(!Cfg.hideAlarmClock);
            }
            catch { }
        }

        void RestoreAlarmClock()
        {
            try { if (alarmBtn != null) alarmBtn.SetActive(true); } catch { }
        }

        // Hide (or restore) the stock warp-to-next-morning sun button on the warp/MET
        // box (KSP.UI.UIWarpToNextMorning, public Button field) - redundant with our
        // dawn button. Its own Update() can re-enable it, so force it every frame.
        void ApplyMorningBtn()
        {
            try
            {
                if (morningBtn == null)
                {
                    if (Time.unscaledTime < morningFindNext) return;
                    morningFindNext = Time.unscaledTime + 1f;
                    KSP.UI.UIWarpToNextMorning w = FindObjectOfType<KSP.UI.UIWarpToNextMorning>();
                    if (w != null && w.button != null) morningBtn = w.button.gameObject;
                }
                if (morningBtn != null && morningBtn.activeSelf == Cfg.hideWarpToMorning)
                    morningBtn.SetActive(!Cfg.hideWarpToMorning);
            }
            catch { }
        }

        void RestoreMorningBtn()
        {
            try { if (morningBtn != null) morningBtn.SetActive(true); } catch { }
        }

        void Update()
        {
            if (Cfg.DumpTriggered()) UiDump.DumpAll("FLIGHT");
            if (Cfg.ReloadTriggered())
            {
                if (Cfg.ReloadFromDisk())
                {
                    FontSwap.Reset(); Widgets.ResetFont();
                    // reload = re-apply fonts FROM SCRATCH: restore every text to native
                    // (also the only reachable revert - periodic Sweep() is gated on
                    // swapFont==true), then ApplyAll's closing Sweep re-marks with the NEW
                    // multipliers, so fontSizeMult edits take effect immediately on F6
                    FontSwap.Restore();
                    if (Cfg.enabled) ApplyAll(); else TeardownAll();
                    try
                    {
                        ScreenMessages.PostScreenMessage(
                            Cfg.enabled ? "ZThemeKSP2: layout reloaded" : "ZThemeKSP2: layout disabled (stock positions restored)",
                            3f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    catch { }
                }
            }
            try
            {
                if (Cfg.enabled && Cfg.hideTrimGauges && trimGauges != null && trimGauges.activeSelf)
                    trimGauges.SetActive(false);

                if (Cfg.enabled) { ApplyAlarmClock(); ApplyMorningBtn(); }

                // KSP2's map has its own UI - hide our flight widgets in map view
                bool inMap = MapView.MapIsEnabled;
                for (int i = 0; i < ownedWidgets.Count; i++)
                    if (ownedWidgets[i] != null && ownedWidgets[i].activeSelf == inMap)
                        ownedWidgets[i].SetActive(!inMap);

                // per-frame: stock re-collapses the chevron panel on hover-out and on
                // warp returning to 1x - keep it pinned open
                if (Cfg.enabled && Cfg.expandWarpPanel) KeepWarpExpanded();

                if (Cfg.enabled && Time.unscaledTime >= nextSweep)
                {
                    // burst-then-settle (same pattern as FontEverywhere): late-built UI is
                    // caught fast right after scene load; steady state only needs to catch
                    // rare pooled-label font resets (PAWs re-sweep instantly via PawFontHook),
                    // so a 5s floor keeps the periodic cost far below hitch territory
                    nextSweep = Time.unscaledTime + (Time.unscaledTime < sweepBurstUntil
                        ? 0.4f : Mathf.Max(Cfg.fontSweepInterval, 5f));
                    if (Cfg.swapFont) FontSwap.Sweep();
                }

                // housekeeping on its OWN timer at the configured cadence: only the font
                // sweep needed the 5s floor - stage cards must recolor promptly after a
                // staging rebuild, and Paint is cheap (walks StageManager.Stages only)
                if (Cfg.enabled && Time.unscaledTime >= nextHousekeep)
                {
                    nextHousekeep = Time.unscaledTime + Cfg.fontSweepInterval;
                    // KSP2 opens VESSEL RESOURCES on entry - but do it ONCE so the user can
                    // still close it (don't fight a manual close every tick)
                    if (Cfg.showResources && !resourcesOpened && ResourceDisplay.Instance != null
                        && !ResourceDisplay.Instance.panelEnabled)
                    {
                        ResourceDisplay.Instance.ShowResourceList(true);
                        resourcesOpened = true;
                    }
                    if (Cfg.paintStaging) StagePainter.Paint();
                    // if we're not showing the GO widget (which owns the hide), make sure the
                    // stock total-dV readout isn't left stuck hidden
                    if (!Cfg.showGoButton || !Cfg.nukeStagingTotal) StageDv.Unhide();
                }
            }
            catch { }
        }

        static Transform FindChildContaining(Transform root, string fragment, int depth)
        {
            if (root == null || depth > 6) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return c;
                Transform found = FindChildContaining(c, fragment, depth + 1);
                if (found != null) return found;
            }
            return null;
        }

        IEnumerator Apply()
        {
            NavBall nb = null;
            for (int i = 0; i < 600 && nb == null; i++)
            {
                try { nb = FindObjectOfType<NavBall>(); } catch { }
                if (nb == null) yield return null;
            }
            // let time-based slide-in animations settle before baselines are captured
            // (frame counts are fps-dependent; 0.75s covers stock transitions at any fps)
            yield return new WaitForSecondsRealtime(0.75f);

            ApplyAll();
            if (Cfg.movePortraits) StartCoroutine(AttachPortraitsWhenReady());
            if (Cfg.showHints)
            {
                try
                {
                    ScreenMessages.PostScreenMessage(
                        "ZThemeKSP2  |  Ctrl+Alt+" + Cfg.reloadKey + " reload Layout.cfg  |  Ctrl+Alt+" + Cfg.dumpKey + " UI dump",
                        6f, ScreenMessageStyle.UPPER_CENTER);
                }
                catch { }
            }
        }

        // Idempotent: attaches movers / updates their values / detaches disabled ones,
        // and rebuilds the widgets. Called on scene start AND on Ctrl+Alt+F6 reload.
        void ApplyAll()
        {
            try
            {
                NavBall nb = FindObjectOfType<NavBall>();
                GameObject cluster = null;
                if (nb != null && nb.transform.parent != null) cluster = nb.transform.parent.gameObject;

                if (cluster != null)
                {
                    if (Cfg.moveNavball) Movers.AttachRatio(cluster, Cfg.navballPosX);
                    else Movers.Detach(cluster);

                    Transform sas = FindChildContaining(cluster.transform, "utopilot", 0);
                    if (sas != null)
                    {
                        if (Cfg.moveSASModes) Movers.AttachLocal(sas.gameObject, Cfg.sasModesOffset);
                        else Movers.Detach(sas.gameObject);
                    }
                    else Debug.Log("[ZThemeKSP2Layout] SAS mode cluster not found under navball - use dump hotkey");
                }

                ModeFramePatcher.Apply(Cfg.moveModeFrame ? Cfg.modeFrameOffset : Vector2.zero);
                DockQuadrantPatcher.Apply(Cfg.moveDockQuadrant ? Cfg.dockQuadrantOffset : Vector2.zero);
                ManQuadrantPatcher.Apply(Cfg.moveManQuadrant ? Cfg.manQuadrantOffset : Vector2.zero);

                if (StageManager.Instance != null)
                {
                    GameObject sg = StageManager.Instance.gameObject;
                    StagingToolbarPin sp = sg.GetComponent<StagingToolbarPin>();
                    if (Cfg.moveStaging && (Cfg.stagingToToolbar || Cfg.pinStagingLeft))
                    {
                        // pin flush to the toolbar (or the left edge); drop the offset mover
                        // first (sync restore) so its baseline doesn't fight the pin
                        ReapplyMover rm = sg.GetComponent<ReapplyMover>();
                        if (rm != null) DestroyImmediate(rm);
                        if (sp == null) sg.AddComponent<StagingToolbarPin>();
                    }
                    else
                    {
                        if (sp != null) DestroyImmediate(sp); // sync restore before mover
                        if (Cfg.moveStaging) Movers.AttachOffset(sg, Cfg.stagingOffset);
                        else Movers.Detach(sg);
                    }
                }

                GameObject tq = GameObject.Find("TimeQuadrant");
                if (tq != null)
                {
                    if (Cfg.moveMET) Movers.AttachOffset(tq, Cfg.metOffset);
                    else Movers.Detach(tq);
                }
                else Debug.Log("[ZThemeKSP2Layout] TimeQuadrant not found - use dump hotkey");

                KerbalPortraitGallery pg = FindObjectOfType<KerbalPortraitGallery>();
                if (pg != null)
                {
                    if (Cfg.movePortraits) Movers.AttachOffset(pg.gameObject, Cfg.portraitsOffset);
                    else Movers.Detach(pg.gameObject);
                    ScalePortraits(pg);
                }

                // CommNet signal widget: glued inline with the breadcrumb bar (KSP2), or a
                // plain offset mover when commsInBar=false / no breadcrumb to glue to.
                // The outgoing component (mover <-> pin) is destroyed IMMEDIATELY so its
                // OnDestroy restore runs BEFORE the successor samples transform.position -
                // deferred Destroy would leave the successor with a polluted baseline.
                TelemetryUpdate tu = TelemetryUpdate.Instance;
                if (tu != null)
                {
                    bool pin = Cfg.moveComms && Cfg.commsInBar && Cfg.showBreadcrumb;
                    CommsPin cp = tu.GetComponent<CommsPin>();
                    if (pin)
                    {
                        ReapplyMover m = tu.GetComponent<ReapplyMover>();
                        if (m != null) DestroyImmediate(m); // sync restore to stock spot first
                        if (cp == null) tu.gameObject.AddComponent<CommsPin>();
                    }
                    else
                    {
                        if (cp != null) DestroyImmediate(cp); // sync restore to stock spot first
                        if (Cfg.moveComms) Movers.AttachOffset(tu.gameObject, Cfg.commsOffset);
                        else Movers.Detach(tu.gameObject);
                    }
                }

                if (hoverCtrl == null) hoverCtrl = FindObjectOfType<KSP.UI.UITimeWarpHoverController>();
                if (Cfg.expandWarpPanel) KeepWarpExpanded();
                else RestoreWarpHover();

                if (trimGauges == null)
                {
                    LinearControlGauges g = FindObjectOfType<LinearControlGauges>();
                    if (g != null) trimGauges = g.gameObject;
                }
                if (trimGauges != null && !Cfg.hideTrimGauges && !trimGauges.activeSelf)
                    trimGauges.SetActive(true); // un-hide when turned off via reload

                GameObject alt = GameObject.Find("Altimeter");
                if (alt != null && alt.transform.parent != null && alt.transform.parent.parent != null)
                    Movers.AttachOffset(alt.transform.parent.parent.gameObject, Cfg.altimeterOffset);

                ApplyAtmosphereLabel();

                // widgets: rebuild from scratch so position/show changes take effect
                for (int i = 0; i < ownedWidgets.Count; i++)
                    if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
                ownedWidgets.Clear();
                Canvas canvas = Widgets.MainCanvas();
                if (canvas != null)
                {
                    GameObject w;
                    if (Cfg.showBreadcrumb) { w = BreadcrumbBar.Create(canvas); if (w != null) ownedWidgets.Add(w); }
                    if (Cfg.showActionsRail) { w = ActionsRail.Create(canvas); if (w != null) ownedWidgets.Add(w); }
                    if (Cfg.showGoButton) { w = GoStageButton.Create(canvas); if (w != null) ownedWidgets.Add(w); }
                    if (Cfg.showTimeButtons) { w = TimeButtons.Create(canvas); if (w != null) ownedWidgets.Add(w); }
                    if (Cfg.showGForce) { w = GForceMeter.Create(canvas); if (w != null) ownedWidgets.Add(w); }
                }
                else Debug.LogWarning("[ZThemeKSP2Layout] no main canvas found; widgets skipped");

                if (Cfg.swapFont) FontSwap.Sweep();
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] flight apply failed: " + e); }
        }

        // enabled=false + reload: detach every mover (restores stock spots), remove
        // widgets, un-hide the trim gauges
        void TeardownAll()
        {
            try
            {
                NavBall nb = FindObjectOfType<NavBall>();
                if (nb != null && nb.transform.parent != null)
                {
                    GameObject cluster = nb.transform.parent.gameObject;
                    Movers.Detach(cluster);
                    Transform sas = FindChildContaining(cluster.transform, "utopilot", 0);
                    if (sas != null) Movers.Detach(sas.gameObject);
                }
                if (StageManager.Instance != null)
                {
                    GameObject sg = StageManager.Instance.gameObject;
                    StagingToolbarPin sp = sg.GetComponent<StagingToolbarPin>();
                    if (sp != null) Destroy(sp); // OnDestroy restores the stock spot
                    Movers.Detach(sg);
                }
                ModeFramePatcher.Apply(Vector2.zero);
                DockQuadrantPatcher.Apply(Vector2.zero);
                ManQuadrantPatcher.Apply(Vector2.zero);
                GameObject tq = GameObject.Find("TimeQuadrant");
                if (tq != null) Movers.Detach(tq);
                KerbalPortraitGallery pg = FindObjectOfType<KerbalPortraitGallery>();
                if (pg != null) Movers.Detach(pg.gameObject);
                RestorePortraitScale();
                TelemetryUpdate tu = TelemetryUpdate.Instance;
                if (tu != null)
                {
                    Movers.Detach(tu.gameObject);
                    CommsPin cp = tu.GetComponent<CommsPin>();
                    if (cp != null) Destroy(cp);
                }
                RestoreWarpHover();
                GameObject alt = GameObject.Find("Altimeter");
                if (alt != null && alt.transform.parent != null && alt.transform.parent.parent != null)
                    Movers.Detach(alt.transform.parent.parent.gameObject);
                for (int i = 0; i < ownedWidgets.Count; i++)
                    if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
                ownedWidgets.Clear();
                if (trimGauges != null && !trimGauges.activeSelf) trimGauges.SetActive(true);
                RestoreAtmosphereLabel(); // un-hide the ATMOSPHERE caption
                RestoreAlarmClock(); // un-hide the stock alarm-clock icon
                RestoreMorningBtn(); // un-hide the stock warp-to-morning button
                StageDv.Unhide(); // restore the stock total-dV readout
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] teardown failed: " + e); }
        }

        // The warp chevrons live in a KSP.UI.UITimeWarpHoverController (a hover slide
        // panel): stock SLIDES IT SHUT and SetActive(false)s its children whenever warp
        // returns to 1x or the mouse leaves - that's why the arrows were "gone when the
        // vehicle is idle". Decompile-verified pin: locked=true stops pointer-exit
        // collapses, killing its coroutine stops the rate==0 auto-collapse mid-slide,
        // and the children are re-activated. (The old timeFrame.TransitionImmediate
        // approach could never work - its states are just "In"/"Out" panel positions
        // and TransitionImmediate never touches SetActive.)
        // 'coroutine' is the one non-public field on UIHoverSlidePanel we need: stock
        // gates every new slide on it being null, so after StopAllCoroutines it must be
        // cleared or the panel's own hover logic stays wedged after our teardown
        static readonly System.Reflection.FieldInfo HoverCo =
            typeof(KSP.UI.UIHoverSlidePanel).GetField("coroutine",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);

        void KeepWarpExpanded()
        {
            try
            {
                if (hoverCtrl == null) return;
                hoverCtrl.locked = true;
                if (HoverCo != null && HoverCo.GetValue(hoverCtrl) != null)
                {
                    hoverCtrl.StopAllCoroutines(); // kill a collapse slide already running
                    HoverCo.SetValue(hoverCtrl, null);
                }
                if (hoverCtrl.panel != null)
                    hoverCtrl.panel.anchoredPosition = hoverCtrl.positionHovered;
                List<GameObject> kids = hoverCtrl.childrenForDeactivate;
                if (kids != null)
                    for (int i = 0; i < kids.Count; i++)
                        if (kids[i] != null && !kids[i].activeSelf) kids[i].SetActive(true);
            }
            catch { }
        }

        // put the hover panel back to its stock idle state (teardown / expandWarpPanel=false)
        void RestoreWarpHover()
        {
            try
            {
                if (hoverCtrl == null) return;
                hoverCtrl.locked = false;
                hoverCtrl.StopAllCoroutines();
                if (HoverCo != null) HoverCo.SetValue(hoverCtrl, null);
                if (hoverCtrl.panel != null)
                    hoverCtrl.panel.anchoredPosition = hoverCtrl.positionNormal;
                List<GameObject> kids = hoverCtrl.childrenForDeactivate;
                if (kids != null)
                    for (int i = 0; i < kids.Count; i++)
                        if (kids[i] != null && kids[i].activeSelf) kids[i].SetActive(false);
            }
            catch { }
        }

        // KSP2's portrait heads are smaller than stock - scale the gallery down
        void ScalePortraits(KerbalPortraitGallery pg)
        {
            try
            {
                if (pg == null) return;
                portraitsRef = pg;
                if (portraitOrigScale == Vector3.zero) portraitOrigScale = pg.transform.localScale;
                pg.transform.localScale = portraitOrigScale * (Cfg.movePortraits ? Cfg.portraitsScale : 1f);
            }
            catch { }
        }

        void RestorePortraitScale()
        {
            try
            {
                if (portraitsRef != null && portraitOrigScale != Vector3.zero)
                    portraitsRef.transform.localScale = portraitOrigScale;
            }
            catch { }
        }

        // the portrait gallery can activate a beat after the rest of the HUD
        IEnumerator AttachPortraitsWhenReady()
        {
            KerbalPortraitGallery pg = null;
            for (int i = 0; i < 300 && pg == null; i++)
            {
                try { pg = FindObjectOfType<KerbalPortraitGallery>(); } catch { }
                if (pg == null) yield return null;
            }
            if (pg != null) { Movers.AttachOffset(pg.gameObject, Cfg.portraitsOffset); ScalePortraits(pg); }
            else Debug.Log("[ZThemeKSP2Layout] portrait gallery not found - use dump hotkey");
        }
    }

    // The font belongs to the gameplay scenes (KSC, tracking station), not just
    // flight/editor. The MAIN MENU / SETTINGS / CREDITS are SKIPPED by default: their
    // stylized fixed-position buttons overlap under a wider monospace font.
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class FontEverywhere : MonoBehaviour
    {
        float next;
        float burstUntil;

        static bool SkipScene()
        {
            GameScenes s = HighLogic.LoadedScene;
            if (s == GameScenes.LOADING || s == GameScenes.LOADINGBUFFER) return true;
            if (s == GameScenes.FLIGHT || HighLogic.LoadedSceneIsEditor) return true; // those addons sweep
            // fixed-layout stylized menus break with a mono font - skip unless forced on
            if (!Cfg.swapFontInMenus &&
                (s == GameScenes.MAINMENU || s == GameScenes.SETTINGS || s == GameScenes.CREDITS))
                return true;
            return false;
        }

        void Start()
        {
            if (!Cfg.loaded) Cfg.Load();
            FontSwap.NewScene();
            burstUntil = Time.unscaledTime + 3f; // sweep often for the first few seconds
            try { if (Cfg.enabled && Cfg.swapFont && !SkipScene()) FontSwap.Sweep(); } catch { }
        }

        void Update()
        {
            try
            {
                if (!Cfg.enabled || !Cfg.swapFont || SkipScene()) return;
                if (Time.unscaledTime < next) return;
                // catch late-created text quickly right after a scene loads, then settle
                // (5s floor matches FlightLayout/EditorLayout - steady-state sweeps only
                // exist to catch stray late text, so slow is fine)
                next = Time.unscaledTime + (Time.unscaledTime < burstUntil ? 0.4f : Mathf.Max(Cfg.fontSweepInterval, 5f));
                FontSwap.Sweep();
            }
            catch { }
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorLayout : MonoBehaviour
    {
        float nextSweep;
        float sweepBurstUntil; // sweep fast while the scene's UI is still being built
        readonly List<GameObject> ownedWidgets = new List<GameObject>();
        GameObject stockLaunchBtn;
        GameObject toolbarObj;

        void Start()
        {
            if (!Cfg.loaded) Cfg.Load();
            PawFontHook.Add(this);
            sweepBurstUntil = Time.unscaledTime + 8f;
            FontSwap.InvalidateRoots(); // editor scene = fresh canvas set
            // prewarm the TMP font asset + swap immediately so editor text doesn't hitch
            try { if (Cfg.enabled && Cfg.swapFont) { TmpFontBuilder.Get(); FontSwap.Sweep(); } } catch { }
            if (Cfg.enabled) StartCoroutine(Apply());
        }

        void OnDestroy()
        {
            PawFontHook.Remove(this);
            RestoreLaunchSiteSelector();
            for (int i = 0; i < ownedWidgets.Count; i++)
                if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
            ownedWidgets.Clear();
        }

        void Update()
        {
            if (Cfg.DumpTriggered()) UiDump.DumpAll("EDITOR");
            if (Cfg.ReloadTriggered())
            {
                if (Cfg.ReloadFromDisk())
                {
                    FontSwap.Reset(); Widgets.ResetFont();
                    // reload = re-apply fonts FROM SCRATCH: restore every text to native
                    // (also the only reachable revert - periodic Sweep() is gated on
                    // swapFont==true), then ApplyAll's closing Sweep re-marks with the NEW
                    // multipliers, so fontSizeMult edits take effect immediately on F6
                    FontSwap.Restore();
                    if (Cfg.enabled) ApplyAll(); else TeardownAll();
                    try { ScreenMessages.PostScreenMessage("ZThemeKSP2: layout reloaded", 3f, ScreenMessageStyle.UPPER_CENTER); } catch { }
                }
            }
            try
            {
                if (Cfg.enabled && Cfg.swapFont && Time.unscaledTime >= nextSweep)
                {
                    // burst-then-settle, same rationale as FlightLayout.Update: the VAB
                    // part list is the worst full-sweep victim, so the 5s settle matters
                    // most here (part-list pooled labels re-font within one settle tick)
                    nextSweep = Time.unscaledTime + (Time.unscaledTime < sweepBurstUntil
                        ? 0.4f : Mathf.Max(Cfg.fontSweepInterval, 5f));
                    FontSwap.Sweep();
                }
            }
            catch { }
        }

        IEnumerator Apply()
        {
            EditorLogic el = null;
            for (int i = 0; i < 600 && el == null; i++)
            {
                try { el = EditorLogic.fetch; } catch { }
                if (el == null) yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            ApplyAll();
        }

        // enabled=false + reload: remove our LAUNCH widget and restore the stock button
        void TeardownAll()
        {
            try
            {
                for (int i = 0; i < ownedWidgets.Count; i++)
                    if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
                ownedWidgets.Clear();
                if (stockLaunchBtn != null && !stockLaunchBtn.activeSelf) stockLaunchBtn.SetActive(true);
                UnfillTopRow();
                RestoreLaunchSiteSelector();
                StageDv.Unhide(); // restore the stock total-dV readout
                if (toolbarObj != null)
                {
                    AbsoluteMover am = toolbarObj.GetComponent<AbsoluteMover>();
                    if (am != null) Destroy(am); // restore stock toolbar position
                }
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] editor teardown failed: " + e); }
        }

        void ApplyAll()
        {
            try
            {
                EditorLogic el = EditorLogic.fetch;
                if (el != null && el.launchBtn != null) stockLaunchBtn = el.launchBtn.gameObject;

                for (int i = 0; i < ownedWidgets.Count; i++)
                    if (ownedWidgets[i] != null) Destroy(ownedWidgets[i]);
                ownedWidgets.Clear();

                // no LAUNCH widget (or not nuking) -> make sure the stock dV readout isn't stuck hidden
                if (!Cfg.moveLaunchButton || !Cfg.nukeStagingTotal) StageDv.Unhide();

                if (Cfg.moveLaunchButton && el != null && el.launchBtn != null)
                {
                    // big KSP2-style LAUNCH button; fires the stock button's own handler
                    // (keeps all stock checks/crew dialogs), stock arrow hidden
                    Canvas canvas = Widgets.MainCanvas();
                    if (canvas != null)
                    {
                        Widgets.DestroyExisting(canvas.transform, "ZKSP2_LaunchButton");
                        const float LW = 224f, LH = 60f, LDV = 26f;
                        // dark container: big green LAUNCH on top + total-dV readout strip below
                        GameObject root = new GameObject("ZKSP2_LaunchButton", typeof(RectTransform), typeof(Image));
                        RectTransform rrt = (RectTransform)root.transform;
                        rrt.SetParent(canvas.transform, false);
                        rrt.anchorMin = new Vector2(0.5f, 0f); rrt.anchorMax = new Vector2(0.5f, 0f);
                        rrt.pivot = new Vector2(0.5f, 0f);
                        rrt.anchoredPosition = Cfg.launchButtonPos; // bottom-center-relative, clears right-edge staging
                        rrt.sizeDelta = new Vector2(LW, LH + LDV + 4f);
                        Image rootImg = root.GetComponent<Image>();
                        Sprite panel = Widgets.LoadSprite("panel.png", new Vector4(7, 7, 7, 7));
                        if (panel != null) { rootImg.sprite = panel; rootImg.type = Image.Type.Sliced; }
                        else rootImg.color = new Color(0.075f, 0.10f, 0.165f, 0.94f);
                        rootImg.raycastTarget = false;

                        GameObject go = new GameObject("launch", typeof(RectTransform), typeof(Image), typeof(Button));
                        RectTransform rt = (RectTransform)go.transform;
                        rt.SetParent(root.transform, false);
                        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.anchoredPosition = new Vector2(0f, -2f); rt.sizeDelta = new Vector2(LW - 4f, LH);
                        Image img = go.GetComponent<Image>();
                        Sprite normal = Widgets.LoadSprite("launch_normal.png", Vector4.zero);
                        Sprite hover = Widgets.LoadSprite("launch_hover.png", Vector4.zero);
                        Sprite down = Widgets.LoadSprite("launch_down.png", Vector4.zero);
                        if (normal != null) img.sprite = normal; else img.color = new Color(0f, 0.8f, 0.32f, 1f);
                        Button btn = go.GetComponent<Button>();
                        if (hover != null && down != null)
                        {
                            btn.transition = Selectable.Transition.SpriteSwap;
                            SpriteState ss = new SpriteState();
                            ss.highlightedSprite = hover; ss.pressedSprite = down;
                            btn.spriteState = ss;
                        }
                        Widgets.NoNavigation(btn);
                        Button stock = el.launchBtn;
                        btn.onClick.AddListener(delegate
                        {
                            // site selection now happens on the always-open stock
                            // selector above this button, not a popup at click time
                            if (stock != null) stock.onClick.Invoke();
                            Widgets.Deselect();
                        });
                        Text txt = Widgets.Label(go.transform, "txt", "LAUNCH", 24, TextAnchor.MiddleLeft,
                            new Color(0.02f, 0.10f, 0.05f, 1f));
                        RectTransform trt = (RectTransform)txt.transform;
                        trt.offsetMin = new Vector2(22f, 2f); trt.offsetMax = new Vector2(-46f, -2f);
                        txt.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.10f);

                        // total-dV strip (bottom) - green mono, updated by LaunchDv
                        Text dv = Widgets.Label(root.transform, "dv", "", 15, TextAnchor.MiddleCenter,
                            new Color(0f, 0.89f, 0.322f, 1f));
                        RectTransform dvrt = (RectTransform)dv.transform;
                        dvrt.anchorMin = new Vector2(0f, 0f); dvrt.anchorMax = new Vector2(1f, 0f);
                        dvrt.pivot = new Vector2(0.5f, 0f);
                        dvrt.offsetMin = new Vector2(6f, 3f); dvrt.offsetMax = new Vector2(-6f, 0f);
                        dvrt.sizeDelta = new Vector2(0f, LDV);
                        dvrt.anchoredPosition = new Vector2(0f, 3f);
                        LaunchDv ld = root.AddComponent<LaunchDv>();
                        ld.dvText = dv;

                        ownedWidgets.Add(root);
                        if (stockLaunchBtn != null) stockLaunchBtn.SetActive(false);
                        ApplyLaunchSiteSelector(rrt);
                    }
                }
                else
                {
                    if (stockLaunchBtn != null && !stockLaunchBtn.activeSelf)
                        stockLaunchBtn.SetActive(true);
                    ApplyLaunchSiteSelector(stockLaunchBtn != null ? stockLaunchBtn.transform as RectTransform : null);
                }

                // gate on the stock button actually being HIDDEN (i.e. our LAUNCH widget
                // really took over): if widget creation was skipped/failed, shifting the
                // row would pile New/Open/Save/Exit onto the still-visible stock button
                if (Cfg.fillTopRow && Cfg.moveLaunchButton && el != null
                    && stockLaunchBtn != null && !stockLaunchBtn.activeSelf)
                    FillTopRow(el);
                else if (topRowFilled)
                    UnfillTopRow(); // stock launch is back - close the gap-fill too

                MoveTools();

                if (Cfg.swapFont) FontSwap.Sweep();
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] editor apply failed: " + e); }
        }

        bool topRowFilled;
        readonly List<RectTransform> shiftedBtns = new List<RectTransform>();
        readonly List<Vector2> shiftedOrig = new List<Vector2>();

        // The hidden stock launch button leaves a hole in the top-right button row;
        // shift New/Open/Save/Exit AND the Steam workshop button right by the launch
        // button's width to close it (steamBtn was the leftover gap the user kept seeing).
        void FillTopRow(EditorLogic el)
        {
            try
            {
                if (topRowFilled) return;
                RectTransform lrt = stockLaunchBtn.transform as RectTransform;
                if (lrt == null) return;
                float shift = lrt.rect.width + 4f;
                float lx = lrt.anchoredPosition.x;
                Button[] row = { el.newBtn, el.loadBtn, el.saveBtn, el.exitBtn, el.steamBtn };
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] == null) continue;
                    RectTransform brt = row[i].transform as RectTransform;
                    if (brt == null || brt.parent != lrt.parent) continue; // only true siblings
                    if (brt.anchoredPosition.x < lx)
                    {
                        shiftedBtns.Add(brt);
                        shiftedOrig.Add(brt.anchoredPosition);
                        brt.anchoredPosition = new Vector2(brt.anchoredPosition.x + shift, brt.anchoredPosition.y);
                    }
                }
                topRowFilled = true;
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] top row fill failed: " + e); }
        }

        // full revert: put the shifted top-row buttons back where stock had them
        void UnfillTopRow()
        {
            try
            {
                for (int i = 0; i < shiftedBtns.Count; i++)
                    if (shiftedBtns[i] != null) shiftedBtns[i].anchoredPosition = shiftedOrig[i];
                shiftedBtns.Clear(); shiftedOrig.Clear();
                topRowFilled = false;
            }
            catch { }
        }

        // The stock launch-site selector (KSP.UI.UILaunchsiteController, a hover slide
        // panel - same family as the warp chevrons) normally only expands on mouse
        // hover and sits wherever stock put it. Pin it permanently expanded and glue
        // it above the LAUNCH button instead of our old click-time popup dialog.
        KSP.UI.UILaunchsiteController launchSiteCtrl;

        void ApplyLaunchSiteSelector(RectTransform launchBtnRect)
        {
            try
            {
                if (launchSiteCtrl == null) launchSiteCtrl = FindObjectOfType<KSP.UI.UILaunchsiteController>();
                if (launchSiteCtrl == null) return;
                LaunchSitePin pin = launchSiteCtrl.GetComponent<LaunchSitePin>();
                if (Cfg.expandLaunchSiteSelector && launchBtnRect != null)
                {
                    if (pin == null) pin = launchSiteCtrl.gameObject.AddComponent<LaunchSitePin>();
                    pin.launchBtn = launchBtnRect;
                }
                else if (pin != null) Destroy(pin); // OnDestroy restores stock hover behavior
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] launch site selector failed: " + e); }
        }

        void RestoreLaunchSiteSelector()
        {
            try
            {
                if (launchSiteCtrl == null) return;
                LaunchSitePin pin = launchSiteCtrl.GetComponent<LaunchSitePin>();
                if (pin != null) Destroy(pin);
            }
            catch { }
        }

        // Move the gizmo/symmetry tool strip to the top (KSP2). The gizmo panel root is
        // EditorLogic.fetch.toolsUI.panelTransition.panelTransform (all public); its parent
        // carries the sym/snap group too. AbsoluteMover pins a screen-fraction spot and
        // restores the stock position when removed (its OnDestroy).
        void MoveTools()
        {
            try
            {
                EditorLogic el = EditorLogic.fetch;
                if (el == null || el.toolsUI == null || el.toolsUI.panelTransition == null
                    || el.toolsUI.panelTransition.panelTransform == null) return;
                // move ONLY the gizmo panel itself - its parent carries the whole build
                // menu on some installs (learned the hard way)
                Transform strip = el.toolsUI.panelTransition.panelTransform;
                toolbarObj = strip.gameObject;
                AbsoluteMover am = toolbarObj.GetComponent<AbsoluteMover>();
                if (Cfg.moveEditorTools)
                {
                    if (am == null) am = toolbarObj.AddComponent<AbsoluteMover>();
                    am.posFrac = Cfg.editorToolsPos;
                    am.centerRect = true; // center the strip at the target, ignore stock pivot
                }
                else if (am != null) Destroy(am); // restores stock position
            }
            catch (Exception e) { Debug.LogError("[ZThemeKSP2Layout] move tools failed: " + e); }
        }
    }

    // updates the LAUNCH button's total-dV strip in the editor
    public class LaunchDv : MonoBehaviour
    {
        public Text dvText;
        float next;
        string lastRawDv; // last string Read() returned - skip Format when unchanged
        void Update()
        {
            try
            {
                if (Time.unscaledTime < next || dvText == null) return;
                next = Time.unscaledTime + 0.5f;
                // Read() every tick (its Hide() side effect owns the stock-readout hide),
                // Format+assign only on change
                string raw = StageDv.Read();
                if (!string.Equals(raw, lastRawDv))
                {
                    lastRawDv = raw;
                    dvText.text = StageDv.Format(raw);
                }
            }
            catch { }
        }
    }

    public class AbsoluteMover : MonoBehaviour
    {
        public Vector2 posFrac;
        public bool centerRect; // put the RectTransform's CENTER (not its pivot) at posFrac
        Vector3 baseline;
        bool haveBaseline;

        void OnDestroy()
        {
            try { if (haveBaseline) transform.position = baseline; } catch { }
        }

        void LateUpdate()
        {
            try
            {
                // KSP's UI world space is CENTER-origin pixels (uiCamera.orthographicSize
                // = Screen.height/2), so fractions are mapped from screen corner to center-origin
                Vector3 p = transform.position;
                if (!haveBaseline) { baseline = p; haveBaseline = true; }
                p.x = (posFrac.x - 0.5f) * Screen.width;
                p.y = (posFrac.y - 0.5f) * Screen.height;
                if (centerRect)
                {
                    // a stock strip's pivot is usually its edge; shift so its visual CENTER
                    // (not the pivot) lands at posFrac
                    RectTransform rt = transform as RectTransform;
                    if (rt != null)
                    {
                        Rect r = rt.rect;
                        Vector3 sc = rt.lossyScale;
                        p.x += (0.5f - rt.pivot.x) * r.width * sc.x;
                        p.y += (0.5f - rt.pivot.y) * r.height * sc.y;
                    }
                }
                transform.position = p;
            }
            catch { }
        }
    }

    // Pins the stock launch-site selector (KSP.UI.UILaunchsiteController) permanently
    // expanded and glues it above the LAUNCH button, replacing our old click-time
    // popup. Same UIHoverSlidePanel family + recipe as FlightLayout.KeepWarpExpanded:
    // locked=true stops pointer-driven collapses, killing 'coroutine' (non-public,
    // reflected) stops a slide already in progress, childrenForDeactivate re-shown.
    public class LaunchSitePin : MonoBehaviour
    {
        public RectTransform launchBtn; // our (or the stock) LAUNCH button, set externally
        KSP.UI.UILaunchsiteController ctrl;
        RectTransform panel;
        static readonly Vector3[] bc = new Vector3[4];
        static readonly Vector3[] sc = new Vector3[4];
        static readonly System.Reflection.FieldInfo HoverCo =
            typeof(KSP.UI.UIHoverSlidePanel).GetField("coroutine",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);

        void Awake()
        {
            ctrl = GetComponent<KSP.UI.UILaunchsiteController>();
            panel = ctrl != null ? ctrl.panel : GetComponent<RectTransform>();
        }

        // removing the pin puts the selector back to stock hover-collapsed behavior
        void OnDestroy()
        {
            try
            {
                if (ctrl == null) return;
                ctrl.locked = false;
                ctrl.StopAllCoroutines();
                if (HoverCo != null) HoverCo.SetValue(ctrl, null);
                if (ctrl.panel != null) ctrl.panel.anchoredPosition = ctrl.positionNormal;
                List<GameObject> kids = ctrl.childrenForDeactivate;
                if (kids != null)
                    for (int i = 0; i < kids.Count; i++)
                        if (kids[i] != null && kids[i].activeSelf) kids[i].SetActive(false);
            }
            catch { }
        }

        void KeepExpanded()
        {
            try
            {
                if (ctrl == null) return;
                ctrl.locked = true;
                if (HoverCo != null && HoverCo.GetValue(ctrl) != null)
                {
                    ctrl.StopAllCoroutines(); // kill a collapse slide already running
                    HoverCo.SetValue(ctrl, null);
                }
                if (ctrl.panel != null) ctrl.panel.anchoredPosition = ctrl.positionHovered;
                List<GameObject> kids = ctrl.childrenForDeactivate;
                if (kids != null)
                    for (int i = 0; i < kids.Count; i++)
                        if (kids[i] != null && !kids[i].activeSelf) kids[i].SetActive(true);
            }
            catch { }
        }

        void LateUpdate()
        {
            try
            {
                KeepExpanded();
                if (launchBtn == null || panel == null) return;
                launchBtn.GetWorldCorners(bc); // 0=BL 1=TL 2=TR 3=BR
                panel.GetWorldCorners(sc);
                float h = sc[1].y - sc[0].y;
                Vector3 targetCenter = new Vector3((bc[0].x + bc[2].x) * 0.5f,
                    bc[1].y + Cfg.launchSiteSelectorGap + h * 0.5f, panel.position.z);
                Vector3 curCenter = (sc[0] + sc[2]) * 0.5f;
                panel.position += targetCenter - curCenter; // idempotent delta
            }
            catch { }
        }
    }
}

