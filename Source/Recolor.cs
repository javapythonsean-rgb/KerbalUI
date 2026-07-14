using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// KSP2-style recolor engine for the ZThemeKSP2 texture pack (derived from ZTheme, GPLv3).
// v3: adds per-file rules - dark dV boxes w/ gold frames, all-green warp chevrons,
// KSP2 steel-blue frame tracing on panel backgrounds.
public static class Ksp2Recolor
{
    // "terminal-dark" ramp matched to real KSP2 flight/VAB screenshots
    static readonly int[] AL = { 0, 20, 37, 50, 66, 82, 98, 128, 160, 192, 224, 240, 255 };
    static readonly int[,] AC = {
        {0x00,0x00,0x00},
        {0x0A,0x0E,0x18},
        {0x13,0x1A,0x2A}, // panel
        {0x17,0x20,0x32},
        {0x26,0x31,0x4A}, // tile/header
        {0x33,0x40,0x5C},
        {0x42,0x50,0x6E},
        {0x63,0x71,0x90},
        {0x87,0x93,0xAC},
        {0xAC,0xB5,0xC8},
        {0xD0,0xD5,0xDE},
        {0xE8,0xEB,0xF0},
        {0xF6,0xF8,0xFB}
    };

    const double HU_RED = 359.2, SA_RED = 0.67;
    const double HU_GOLD = 44.7, SA_GOLD = 0.98;
    const double HU_GREEN = 142.8, SA_GREEN = 1.00;
    const double HU_TEAL = 183.5, SA_TEAL = 0.95;
    const double HU_PERI = 240.9, SA_PERI = 0.59;

    const int ChromaNeutral = 28;
    const int ChromaAccent = 48;

    // per-file rules (matched case-insensitively against the file name)
    static readonly string[] DarkAccentFiles = { "StagingDVButton" };          // accents -> dark panel (readable dV box)
    static readonly string[] WarpFiles = { "TimeWarpStates" };                 // active chevron green, inactive light grey (contrast on dark blue)
    static readonly string[] PanelFloorFiles = { "MCBackground" };             // full-screen panels: floor to readable navy
    // planet-type icons (KnowledgeBase / map lists): give each body tier a distinct hue
    // (stock Eve=purple and Kerbin=blue both landed in the periwinkle bucket -> twins)
    static readonly string[] TintPurpleFiles = { "02_eve" };
    static readonly string[] TintTealFiles = { "03_kerbin" };
    static readonly string[] GoldBorderFiles = { "StagingDVButton", "StageDV#", "StageManagerDVTotal" };
    static readonly string[] BlueBorderFiles = {
        "bottomBar_plain", "FlightUILinearQuadrant_bg", "FlightUIModeFrameBackground",
        "FlightUIRotQuadrant_bg", "FlightUIStagingQuadrant_bg", "ManMode_SelectorBackground",
        "ManMode_textFieldBackground", "panel3_afafaf", "panel4_2f323c", "panel4_44484f",
        "panel4_5c626e", "panel4_bfbfbf", "panel8_434750", "StageTumblerBackground",
        "TelemetryFrame", "TimeQuadrantBackground", "tooltip_bg", "KSCTopBar",
        "missc_logo_bg", "app_bg", "background_squared_inset", "bevel_bg",
        "gen_bg_textfield", "gen_panel_bg3", "gen_panel_bg_meui", "gen_panel_bg_trns",
        "MCBackground", "MCBackgroundPanel", "NextAlarmBackground", "vsd_bg_list",
        "vsd_bg_info", "TrackingStationBackground", "PartListBackground",
        "ShipNameBackground", "StageGroupBackground_square"
    };

    // KSP2 frame colors: periwinkle hairline (#3C3E92 base, glows #6564FE active) - the
    // core KSP2 panel signature; gold for VAB/dV boxes
    static readonly Color BlueFrame = Color.FromArgb(255, 0x45, 0x48, 0xA6);
    static readonly Color GoldFrame = Color.FromArgb(255, 0xC9, 0x94, 0x11);

    static bool NameMatches(string fileName, string[] list)
    {
        for (int i = 0; i < list.Length; i++)
            if (fileName.IndexOf(list[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    static void RgbToHsv(int r, int g, int b, out double h, out double s, out double v)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf)), min = Math.Min(rf, Math.Min(gf, bf));
        v = max;
        double d = max - min;
        s = max == 0 ? 0 : d / max;
        if (d == 0) { h = 0; return; }
        if (max == rf) h = 60.0 * (((gf - bf) / d) % 6.0);
        else if (max == gf) h = 60.0 * (((bf - rf) / d) + 2.0);
        else h = 60.0 * (((rf - gf) / d) + 4.0);
        if (h < 0) h += 360.0;
    }

    static void HsvToRgb(double h, double s, double v, out int r, out int g, out int b)
    {
        h = ((h % 360.0) + 360.0) % 360.0;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2.0 - 1));
        double m = v - c;
        double rf, gf, bf;
        if (h < 60) { rf = c; gf = x; bf = 0; }
        else if (h < 120) { rf = x; gf = c; bf = 0; }
        else if (h < 180) { rf = 0; gf = c; bf = x; }
        else if (h < 240) { rf = 0; gf = x; bf = c; }
        else if (h < 300) { rf = x; gf = 0; bf = c; }
        else { rf = c; gf = 0; bf = x; }
        r = Clamp((int)Math.Round((rf + m) * 255));
        g = Clamp((int)Math.Round((gf + m) * 255));
        b = Clamp((int)Math.Round((bf + m) * 255));
    }

    static int Clamp(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }

    static void MapNeutral(double lum, out int r, out int g, out int b)
    {
        int n = AL.Length;
        if (lum <= AL[0]) { r = AC[0, 0]; g = AC[0, 1]; b = AC[0, 2]; return; }
        if (lum >= AL[n - 1]) { r = AC[n - 1, 0]; g = AC[n - 1, 1]; b = AC[n - 1, 2]; return; }
        int i = 0;
        while (i < n - 2 && lum > AL[i + 1]) i++;
        double t = (lum - AL[i]) / (double)(AL[i + 1] - AL[i]);
        r = (int)Math.Round(AC[i, 0] + t * (AC[i + 1, 0] - AC[i, 0]));
        g = (int)Math.Round(AC[i, 1] + t * (AC[i + 1, 1] - AC[i, 1]));
        b = (int)Math.Round(AC[i, 2] + t * (AC[i + 1, 2] - AC[i, 2]));
    }

    // mode: 0=normal, 1=accents->dark neutral, 2=accents->green, 3=warp chevrons
    static void TransformPixel(int r, int g, int b, int mode, out int nr, out int ng, out int nb)
    {
        int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        int chroma = max - min;
        double lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        if (mode == 3)
        {
            // warp chevron strips: EVERY colored state (green AND the physics-warp
            // yellow/orange/red severity cells) -> uniform BRIGHT green so a lit chevron
            // always reads green like KSP2; dark/neutral (unlit) -> LIGHT grey for contrast
            if (chroma > 30)
                HsvToRgb(HU_GREEN, 0.85, 0.95, out nr, out ng, out nb);
            else { nr = 0xB6; ng = 0xBC; nb = 0xC6; } // light cool grey - reads on the dark blue panel
            return;
        }

        int gr, gg, gb;
        MapNeutral(lum, out gr, out gg, out gb);
        if (chroma <= ChromaNeutral || mode == 1) { nr = gr; ng = gg; nb = gb; return; }

        double h, s, v;
        RgbToHsv(r, g, b, out h, out s, out v);

        double th, ts, tv;
        if (mode == 2) { th = HU_GREEN; ts = SA_GREEN; tv = 0.910; }
        else if (h < 12 || h >= 340) { th = HU_RED; ts = SA_RED; tv = 0.937; }
        else if (h < 70) { th = HU_GOLD; ts = SA_GOLD; tv = 0.937; }
        else if (h < 165) { th = HU_GREEN; ts = SA_GREEN; tv = 0.910; }
        else if (h < 212) { th = HU_TEAL; ts = SA_TEAL; tv = 0.769; }
        else { th = HU_PERI; ts = SA_PERI; tv = 0.871; }

        v = Math.Min(v, tv + 0.12);
        double outS = ts * Math.Min(1.0, s / 0.55);
        int ar, ag, ab;
        HsvToRgb(th, outS, v, out ar, out ag, out ab);

        if (chroma < ChromaAccent)
        {
            double t = (chroma - ChromaNeutral) / (double)(ChromaAccent - ChromaNeutral);
            nr = (int)Math.Round(gr + t * (ar - gr));
            ng = (int)Math.Round(gg + t * (ag - gg));
            nb = (int)Math.Round(gb + t * (ab - gb));
        }
        else { nr = ar; ng = ag; nb = ab; }
    }

    // Traces a 1px frame along the opaque/transparent boundary (and the image edge where
    // opaque), plus a 45%-blended second ring inward. Follows rounded corners automatically
    // and survives 9-slicing since the frame hugs the sprite edge.
    static void BorderPass(byte[] px, int stride, int w, int h, Color frame)
    {
        bool[] opaque = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                opaque[y * w + x] = px[y * stride + x * 4 + 3] >= 200;

        bool[] ring1 = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!opaque[y * w + x]) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                    || !opaque[y * w + (x - 1)] || !opaque[y * w + (x + 1)]
                    || !opaque[(y - 1) * w + x] || !opaque[(y + 1) * w + x];
                if (edge) ring1[y * w + x] = true;
            }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * stride + x * 4;
                if (ring1[y * w + x])
                {
                    px[i + 2] = frame.R; px[i + 1] = frame.G; px[i] = frame.B;
                }
                else if (opaque[y * w + x])
                {
                    bool near = (x > 0 && ring1[y * w + x - 1]) || (x < w - 1 && ring1[y * w + x + 1])
                        || (y > 0 && ring1[(y - 1) * w + x]) || (y < h - 1 && ring1[(y + 1) * w + x]);
                    if (near)
                    {
                        px[i + 2] = (byte)((px[i + 2] * 55 + frame.R * 45) / 100);
                        px[i + 1] = (byte)((px[i + 1] * 55 + frame.G * 45) / 100);
                        px[i] = (byte)((px[i] * 55 + frame.B * 45) / 100);
                    }
                }
            }
    }

    public static int ProcessTree(string srcRoot, string dstRoot)
    {
        int count = 0;
        foreach (string src in Directory.GetFiles(srcRoot, "*.png", SearchOption.AllDirectories))
        {
            string rel = src.Substring(srcRoot.Length).TrimStart('\\', '/');
            string dst = Path.Combine(dstRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            ProcessFile(src, dst);
            count++;
        }
        return count;
    }

    public static void ProcessFile(string src, string dst)
    {
        string name = Path.GetFileName(src);
        int mode = NameMatches(name, DarkAccentFiles) ? 1 : (NameMatches(name, WarpFiles) ? 3 : 0);
        bool goldBorder = NameMatches(name, GoldBorderFiles);
        bool blueBorder = !goldBorder && NameMatches(name, BlueBorderFiles);
        bool panelFloor = NameMatches(name, PanelFloorFiles);
        int tint = NameMatches(name, TintPurpleFiles) ? 1 : (NameMatches(name, TintTealFiles) ? 2 : 0);

        using (Bitmap inBmp = new Bitmap(src))
        using (Bitmap bmp = new Bitmap(inBmp.Width, inBmp.Height, PixelFormat.Format32bppArgb))
        {
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                gfx.DrawImage(inBmp, new Rectangle(0, 0, inBmp.Width, inBmp.Height));
            }
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int bytes = Math.Abs(bd.Stride) * bmp.Height;
            byte[] px = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, bytes);
            for (int i = 0; i < bytes; i += 4)
            {
                if (px[i + 3] == 0) continue;
                int r = px[i + 2], g = px[i + 1], b = px[i];
                int nr, ng, nb;
                TransformPixel(r, g, b, mode, out nr, out ng, out nb);
                if (panelFloor && 0.2126 * nr + 0.7152 * ng + 0.0722 * nb < 22)
                { nr = 0x13; ng = 0x1A; nb = 0x2A; } // keep full-screen panel interiors readable
                if (tint != 0)
                {
                    // monochrome per-body tint, luminance-preserving; highlights stay whitish
                    double L = (0.2126 * nr + 0.7152 * ng + 0.0722 * nb) / 255.0;
                    double hu = tint == 1 ? 290.0 : 183.5;
                    double sa = (tint == 1 ? 0.58 : 0.62) * (1.0 - Math.Pow(L, 2.0));
                    double vv = Math.Min(1.0, 0.15 + L * 1.05);
                    HsvToRgb(hu, sa, vv, out nr, out ng, out nb);
                }
                px[i + 2] = (byte)nr; px[i + 1] = (byte)ng; px[i] = (byte)nb;
            }
            if (goldBorder) BorderPass(px, bd.Stride, bmp.Width, bmp.Height, GoldFrame);
            else if (blueBorder) BorderPass(px, bd.Stride, bmp.Width, bmp.Height, BlueFrame);
            System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, bytes);
            bmp.UnlockBits(bd);
            string tmp = dst + ".tmp";
            bmp.Save(tmp, ImageFormat.Png);
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(tmp, dst);
        }
    }
}

