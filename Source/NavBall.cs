using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

// Generates a KSP2-accurate navball texture (equirectangular 1024x512; N at x=512,
// S at the seam, +90 pitch at y=0). Colors pixel-sampled from real KSP2 screenshots:
// DARK DESATURATED navy sky over near-black chocolate ground - the opposite of KSP1's
// bright sky-blue / tan. Pale grid lines, thin near-white horizon, red cardinals,
// periwinkle heading numerals, a cyan zenith cap.
public static class Ksp2NavBall
{
    const int W = 1024, H = 512;

    static Color Lerp(Color a, Color b, double t)
    {
        if (t < 0) t = 0; if (t > 1) t = 1;
        return Color.FromArgb(255,
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    static float YOfPitch(double pitchDeg) { return (float)((90.0 - pitchDeg) / 180.0 * H); }
    static float XOfHeading(double hdg) { return (float)(((hdg + 180.0) % 360.0) / 360.0 * W); }

    public static void Generate(string outDiffuse, string outEmissive, string outThumb)
    {
        using (Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            // KSP2: sky slightly lighter at the horizon, darker toward the zenith
            Color skyHorizon = ColorTranslator.FromHtml("#7CC3DC");
            Color skyZenith = ColorTranslator.FromHtml("#A9E0EF");
            Color zenithCap = ColorTranslator.FromHtml("#C7EDF7"); // cyan cap at straight-up
            // ground: near-black warm chocolate, darkest at the horizon
            Color gndHorizon = ColorTranslator.FromHtml("#C97C3C");
            Color gndNadir = ColorTranslator.FromHtml("#9A5620");

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] px = new byte[Math.Abs(bd.Stride) * H];
            for (int y = 0; y < H; y++)
            {
                double pitch = 90.0 - (y / (double)H) * 180.0;
                Color c;
                if (pitch >= 0)
                {
                    double t = pitch / 90.0;
                    c = Lerp(skyHorizon, skyZenith, Math.Pow(t, 0.9));
                    // cyan cap: blend in over the top ~12 degrees
                    if (pitch > 78) c = Lerp(c, zenithCap, (pitch - 78) / 12.0 * 0.85);
                }
                else c = Lerp(gndHorizon, gndNadir, -pitch / 90.0);
                for (int x = 0; x < W; x++)
                {
                    int i = y * bd.Stride + x * 4;
                    px[i] = c.B; px[i + 1] = c.G; px[i + 2] = c.R; px[i + 3] = 255;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(px, 0, bd.Scan0, px.Length);
            bmp.UnlockBits(bd);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // pale grid lines (light on the dark ball), moderate opacity
                Color skyGrid = ColorTranslator.FromHtml("#1C4A5E");
                Color gndGrid = ColorTranslator.FromHtml("#57300E");
                Pen skyMinor = new Pen(Color.FromArgb(120, skyGrid), 1.1f);
                Pen skyMajor = new Pen(Color.FromArgb(190, skyGrid), 1.8f);
                Pen gndMinor = new Pen(Color.FromArgb(120, gndGrid), 1.1f);
                Pen gndMajor = new Pen(Color.FromArgb(190, gndGrid), 1.8f);

                for (int p = -80; p <= 80; p += 10)
                {
                    if (p == 0) continue;
                    float y = YOfPitch(p);
                    Pen pen = p > 0 ? (p % 30 == 0 ? skyMajor : skyMinor)
                                    : (p % 30 == 0 ? gndMajor : gndMinor);
                    g.DrawLine(pen, 0, y, W, y);
                }

                float yEq = YOfPitch(0);
                for (int hdg = 0; hdg < 360; hdg += 30)
                {
                    float x = XOfHeading(hdg);
                    bool card = hdg % 90 == 0;
                    g.DrawLine(card ? skyMajor : skyMinor, x, YOfPitch(80), x, yEq);
                    g.DrawLine(card ? gndMajor : gndMinor, x, yEq, x, YOfPitch(-80));
                }
                g.DrawLine(skyMajor, W - 1, YOfPitch(80), W - 1, yEq);
                g.DrawLine(gndMajor, W - 1, yEq, W - 1, YOfPitch(-80));

                // thin near-white horizon line + fine heading ticks
                using (Pen horizon = new Pen(ColorTranslator.FromHtml("#FFFFFF"), 3.6f))
                    g.DrawLine(horizon, 0, yEq, W, yEq);
                using (Pen tick = new Pen(Color.FromArgb(200, ColorTranslator.FromHtml("#DCDEDD")), 1.4f))
                    for (int hdg = 0; hdg < 360; hdg += 5)
                    {
                        float x = XOfHeading(hdg);
                        g.DrawLine(tick, x, yEq + 2f, x, yEq + (hdg % 15 == 0 ? 9f : 5f));
                    }

                FontFamily fam;
                try { fam = new FontFamily("JetBrains Mono Medium"); }
                catch { try { fam = new FontFamily("Bahnschrift"); } catch { fam = new FontFamily("Arial"); } }
                Font pitchFont = new Font(fam, 17f, FontStyle.Bold, GraphicsUnit.Pixel);
                Font cardFont = new Font(fam, 26f, FontStyle.Bold, GraphicsUnit.Pixel);
                Font numFont = new Font(fam, 15f, FontStyle.Bold, GraphicsUnit.Pixel);
                StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // pitch numerals - light, per hemisphere
                Brush skyNum = new SolidBrush(ColorTranslator.FromHtml("#16404F"));
                Brush gndNum = new SolidBrush(ColorTranslator.FromHtml("#4A2A0C"));
                foreach (int p in new[] { 30, 60, -30, -60 })
                {
                    float y = YOfPitch(p) - 15f;
                    Brush b = p > 0 ? skyNum : gndNum;
                    for (int hdg = 0; hdg < 360; hdg += 90)
                    {
                        float x = XOfHeading(hdg) + 26f;
                        g.DrawString(Math.Abs(p).ToString(), pitchFont, b, x, y, center);
                    }
                }

                // heading row just below the horizon: red cardinals, periwinkle 3-digit numbers
                Brush cardBrush = new SolidBrush(ColorTranslator.FromHtml("#E23A2A"));
                Brush hdgBrush = new SolidBrush(ColorTranslator.FromHtml("#2A3550"));
                float rowY = yEq + 28f;
                for (int hdg = 0; hdg < 360; hdg += 30)
                {
                    float x = XOfHeading(hdg);
                    bool card = hdg % 90 == 0;
                    string txt = card ? "NESW"[hdg / 90].ToString() : hdg.ToString("000");
                    Font f = card ? cardFont : numFont;
                    Brush b = card ? cardBrush : hdgBrush;
                    g.DrawString(txt, f, b, x, rowY, center);
                    if (x < 50f) g.DrawString(txt, f, b, x + W, rowY, center);
                    if (x > W - 50f) g.DrawString(txt, f, b, x - W, rowY, center);
                }
            }

            bmp.Save(outDiffuse, ImageFormat.Png);
            bmp.Save(outEmissive, ImageFormat.Png); // emissive = diffuse; NBTC dims it via EmissiveColor

            using (Bitmap thumb = new Bitmap(150, 75, PixelFormat.Format32bppArgb))
            {
                using (Graphics tg = Graphics.FromImage(thumb))
                {
                    tg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    tg.DrawImage(bmp, new Rectangle(0, 0, 150, 75));
                }
                thumb.Save(outThumb, ImageFormat.Png);
            }
        }
    }
}
