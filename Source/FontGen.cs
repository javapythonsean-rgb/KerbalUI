using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;

// Generates an SDF glyph atlas + metrics for KSP's old TextMeshPro from a TTF
// (JetBrains Mono - the actual KSP2 UI font, OFL licensed).
// Output: atlas PNG (white RGB, SDF in ALPHA - TMP's Distance Field shader samples alpha)
// and a KSP ConfigNode metrics file the plugin turns into TMP_Glyph[] + FaceInfo.
// SDF via two 8SSEDT distance transforms on a 4x hi-res render, sampled down.
public static class Ksp2FontGen
{
    const int EmPx = 64;          // atlas glyph em size (FaceInfo.PointSize)
    const int Scale = 4;          // hi-res render multiplier
    const int Cell = 80;          // atlas cell (EmPx + padding margins)
    const int Spread = 6;         // SDF spread in atlas px (FaceInfo.Padding)
    const int Atlas = 1024;

    static readonly int[] Extra = { 176, 177, 181, 215, 247, 916, 8226, 8722, 8734 }; // ° ± µ × ÷ Δ • − ∞

    static PrivateFontCollection _pfc; // static: must outlive all Font handles (GDI+ AV otherwise)

    public static void Generate(string ttfPath, string outPng, string outCfg)
    {
        FontFamily fam = null;
        try { fam = new FontFamily("JetBrains Mono Medium"); } catch { }
        if (fam == null)
        {
            _pfc = new PrivateFontCollection();
            _pfc.AddFontFile(ttfPath);
            fam = _pfc.Families[0];
        }

        int emDesign = fam.GetEmHeight(FontStyle.Regular);
        float ascent = EmPx * (float)fam.GetCellAscent(FontStyle.Regular) / emDesign;
        float descent = EmPx * (float)fam.GetCellDescent(FontStyle.Regular) / emDesign;
        float lineH = EmPx * (float)fam.GetLineSpacing(FontStyle.Regular) / emDesign;
        float advance = EmPx * 0.6f; // JetBrains Mono is monospace, 600/1000 em advance

        List<int> chars = new List<int>();
        for (int c = 33; c <= 126; c++) chars.Add(c); // space synthesized by ReadFontDefinition
        chars.AddRange(Extra);

        int hiEm = EmPx * Scale;
        int hiCanvas = hiEm * 3; // room for overshoot
        float hiBaseline = hiEm * 1.5f;
        float hiOriginX = hiEm;

        using (Bitmap atlas = new Bitmap(Atlas, Atlas, PixelFormat.Format32bppArgb))
        using (Font font = new Font(fam, hiEm, FontStyle.Regular, GraphicsUnit.Pixel))
        using (Bitmap hi = new Bitmap(hiCanvas, hiCanvas, PixelFormat.Format32bppArgb))
        using (Graphics hg = Graphics.FromImage(hi))
        {
            hg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            hg.SmoothingMode = SmoothingMode.AntiAlias;
            StringFormat sf = StringFormat.GenericTypographic;

            // clear atlas alpha
            using (Graphics ag = Graphics.FromImage(atlas)) ag.Clear(Color.FromArgb(0, 255, 255, 255));

            // NOTE: KSP's ConfigNode parser splits each line at the FIRST '=' only, so
            // every key must be on its own line - no compact one-line nodes.
            StringBuilder cfg = new StringBuilder();
            StringBuilder glyphCfg = new StringBuilder();

            float hiAscent = ascent * Scale;
            float capHeight = 0f;
            int col = 0, row = 0, done = 0;

            // explicit space glyph (id 32) so ReadFontDefinition does NOT synthesize one
            // with its hardcoded PointSize/4 advance (16px) - monospace needs 38.4
            AppendGlyph(glyphCfg, 32, 0, 0, 0, 0, 0f, 0f, advance);
            done++;

            foreach (int code in chars)
            {
                string s = char.ConvertFromUtf32(code);
                hg.Clear(Color.Transparent);
                // draw so glyph baseline sits at hiBaseline: GenericTypographic draws from cell top
                float cellTop = hiBaseline - hiAscent;
                hg.DrawString(s, font, Brushes.White, hiOriginX, cellTop, sf);

                // ink bounding box in hi-res
                int minX, minY, maxX, maxY;
                if (!InkBox(hi, out minX, out minY, out maxX, out maxY))
                {
                    // blank glyph (shouldn't happen for 33..126); skip
                    continue;
                }
                // true cap height from the un-padded 'H' ink top (halo-free)
                if (code == 72) capHeight = (hiBaseline - minY) / (float)Scale;

                // pad the box by spread (hi-res) for the SDF halo
                int pad = Spread * Scale + Scale;
                minX = Math.Max(0, minX - pad); minY = Math.Max(0, minY - pad);
                maxX = Math.Min(hiCanvas - 1, maxX + pad); maxY = Math.Min(hiCanvas - 1, maxY + pad);
                int hw = maxX - minX + 1, hh = maxY - minY + 1;

                // low-res glyph size
                int gw = (hw + Scale - 1) / Scale, gh = (hh + Scale - 1) / Scale;
                if (gw > Cell || gh > Cell) { gw = Math.Min(gw, Cell); gh = Math.Min(gh, Cell); }

                // SDF on the hi-res sub-rect
                float[,] sdf = SdfFromMask(hi, minX, minY, hw, hh);

                // place in atlas
                if (col * Cell + gw >= Atlas) { col = 0; row++; }
                int ax = col * Cell, ay = row * Cell;
                col++;
                if ((row + 1) * Cell >= Atlas) throw new Exception("atlas full");

                for (int y = 0; y < gh; y++)
                    for (int x = 0; x < gw; x++)
                    {
                        // sample hi-res sd at texel center
                        int sx = Math.Min(hw - 1, x * Scale + Scale / 2);
                        int sy = Math.Min(hh - 1, y * Scale + Scale / 2);
                        float sd = sdf[sx, sy] / Scale;            // in atlas px
                        float a = 0.5f - sd / (2f * Spread);       // 0.5 at edge
                        int ai = (int)Math.Round(Mathc(a) * 255f);
                        atlas.SetPixel(ax + x, ay + y, Color.FromArgb(ai, 255, 255, 255));
                    }

                // metrics (atlas px, TMP bmfont convention: x/y from atlas TOP-LEFT,
                // yOffset = baseline -> glyph top, positive up)
                float xOffset = (minX - hiOriginX) / (float)Scale;
                float yOffset = (hiBaseline - minY) / (float)Scale;
                AppendGlyph(glyphCfg, code, ax, ay, gw, gh, xOffset, yOffset, advance);
                done++;
            }

            cfg.AppendLine("ZKSP2FontData");
            cfg.AppendLine("{");
            cfg.AppendLine("\tname = JetBrainsMono-Medium SDF");
            cfg.AppendLine("\tpointSize = " + EmPx);
            cfg.AppendLine("\tascender = " + F(ascent));
            cfg.AppendLine("\tdescender = " + F(-descent));
            cfg.AppendLine("\tlineHeight = " + F(lineH));
            cfg.AppendLine("\tpadding = " + Spread);
            cfg.AppendLine("\tatlasSize = " + Atlas);
            cfg.AppendLine("\tadvance = " + F(advance));
            cfg.AppendLine("\tcapHeight = " + F(capHeight));
            cfg.Append(glyphCfg);
            cfg.AppendLine("}");
            File.WriteAllText(outCfg, cfg.ToString());
            atlas.Save(outPng, ImageFormat.Png);
            Console.WriteLine("glyphs: " + done);
        }
    }

    static string F(float v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }

    static void AppendGlyph(StringBuilder sb, int id, int x, int y, int w, int h, float ox, float oy, float adv)
    {
        sb.AppendLine("\tGLYPH");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tid = " + id);
        sb.AppendLine("\t\tx = " + x);
        sb.AppendLine("\t\ty = " + y);
        sb.AppendLine("\t\tw = " + w);
        sb.AppendLine("\t\th = " + h);
        sb.AppendLine("\t\tox = " + F(ox));
        sb.AppendLine("\t\toy = " + F(oy));
        sb.AppendLine("\t\tadv = " + F(adv));
        sb.AppendLine("\t}");
    }
    static float Mathc(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }

    static bool InkBox(Bitmap b, out int minX, out int minY, out int maxX, out int maxY)
    {
        BitmapData bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[Math.Abs(bd.Stride) * b.Height];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        b.UnlockBits(bd);
        minX = b.Width; minY = b.Height; maxX = -1; maxY = -1;
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
                if (px[y * bd.Stride + x * 4 + 3] > 32)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
        return maxX >= 0;
    }

    // signed distance (px): positive outside ink, negative inside; 8SSEDT two-pass
    static float[,] SdfFromMask(Bitmap b, int ox, int oy, int w, int h)
    {
        BitmapData bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] px = new byte[Math.Abs(bd.Stride) * b.Height];
        System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, px, 0, px.Length);
        b.UnlockBits(bd);

        bool[,] ink = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                ink[x, y] = px[(oy + y) * bd.Stride + (ox + x) * 4 + 3] >= 128;

        float[,] dOut = Edt(ink, w, h, true);  // distance to nearest INK (meaningful outside)
        float[,] dIn = Edt(ink, w, h, false);  // distance to nearest EMPTY (meaningful inside)

        float[,] sd = new float[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                sd[x, y] = ink[x, y] ? -dIn[x, y] : dOut[x, y];
        return sd;
    }

    // two-pass chamfer distance transform to nearest pixel where ink==target
    static float[,] Edt(bool[,] ink, int w, int h, bool target)
    {
        const float BIG = 1e9f, D1 = 1f, D2 = 1.4142f;
        float[,] d = new float[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                d[x, y] = ink[x, y] == target ? 0f : BIG;
        // forward
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = d[x, y];
                if (x > 0) v = Math.Min(v, d[x - 1, y] + D1);
                if (y > 0) v = Math.Min(v, d[x, y - 1] + D1);
                if (x > 0 && y > 0) v = Math.Min(v, d[x - 1, y - 1] + D2);
                if (x < w - 1 && y > 0) v = Math.Min(v, d[x + 1, y - 1] + D2);
                d[x, y] = v;
            }
        // backward
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                float v = d[x, y];
                if (x < w - 1) v = Math.Min(v, d[x + 1, y] + D1);
                if (y < h - 1) v = Math.Min(v, d[x, y + 1] + D1);
                if (x < w - 1 && y < h - 1) v = Math.Min(v, d[x + 1, y + 1] + D2);
                if (x > 0 && y < h - 1) v = Math.Min(v, d[x - 1, y + 1] + D2);
                d[x, y] = v;
            }
        return d;
    }
}
