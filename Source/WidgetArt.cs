using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Generates the runtime widget art for ZThemeKSP2Layout's bespoke KSP2-style UI
// (breadcrumb bar, vessel-actions rail, GO stage button). All KSP2-crop-matched:
// dark navy panels (#131A2A ~92% alpha), vivid blue frames (#4C7CD6), white glyphs.
public static class Ksp2WidgetArt
{
    static readonly Color PanelFill = Color.FromArgb(235, 0x13, 0x1A, 0x2A);
    static readonly Color Frame = Color.FromArgb(255, 0x4C, 0x7C, 0xD6);
    static readonly Color Glyph = Color.FromArgb(255, 0xED, 0xEE, 0xF0);

    public static void GenerateAll(string dir)
    {
        Directory.CreateDirectory(dir);
        Panel9(Path.Combine(dir, "panel.png"));
        ButtonBg(Path.Combine(dir, "btn.png"), Color.FromArgb(220, 0x18, 0x22, 0x38));
        GoButton(Path.Combine(dir, "go_normal.png"), Color.FromArgb(255, 0x00, 0xCC, 0x51));
        GoButton(Path.Combine(dir, "go_hover.png"), Color.FromArgb(255, 0x21, 0xE8, 0x6E));
        GoButton(Path.Combine(dir, "go_down.png"), Color.FromArgb(255, 0x00, 0x9E, 0x3F));
        LaunchButton(Path.Combine(dir, "launch_normal.png"), Color.FromArgb(255, 0x00, 0xCC, 0x51));
        LaunchButton(Path.Combine(dir, "launch_hover.png"), Color.FromArgb(255, 0x21, 0xE8, 0x6E));
        LaunchButton(Path.Combine(dir, "launch_down.png"), Color.FromArgb(255, 0x00, 0x9E, 0x3F));
        IconPause(Path.Combine(dir, "icon_pause.png"));
        IconSunrise(Path.Combine(dir, "icon_sunrise.png"));
        IconGear(Path.Combine(dir, "icon_gear.png"));
        IconLight(Path.Combine(dir, "icon_light.png"));
        IconBrakes(Path.Combine(dir, "icon_brakes.png"));
        IconAbort(Path.Combine(dir, "icon_abort.png"));
        IconSolar(Path.Combine(dir, "icon_solar.png"));
        IconRadiator(Path.Combine(dir, "icon_radiator.png"));
    }

    static Bitmap New32(int w, int h) { return new Bitmap(w, h, PixelFormat.Format32bppArgb); }

    static Graphics G(Bitmap b)
    {
        Graphics g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        return g;
    }

    static GraphicsPath Rounded(Rectangle r, int rad)
    {
        GraphicsPath p = new GraphicsPath();
        p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
        p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
        p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
        p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    // 24x24 rounded 9-slice panel: navy fill, 2px vivid blue frame
    static void Panel9(string path)
    {
        using (Bitmap b = New32(24, 24))
        {
            using (Graphics g = G(b))
            using (GraphicsPath p = Rounded(new Rectangle(1, 1, 21, 21), 5))
            {
                g.FillPath(new SolidBrush(PanelFill), p);
                g.DrawPath(new Pen(Frame, 2f), p);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 20x20 rounded button background (darker, thinner frame)
    static void ButtonBg(string path, Color fill)
    {
        using (Bitmap b = New32(20, 20))
        {
            using (Graphics g = G(b))
            using (GraphicsPath p = Rounded(new Rectangle(1, 1, 17, 17), 4))
            {
                g.FillPath(new SolidBrush(fill), p);
                g.DrawPath(new Pen(Color.FromArgb(200, Frame), 1.5f), p);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 168x50 green GO button, black hazard-stripe block on the right (sized 1:1 to display)
    static void GoButton(string path, Color green)
    {
        int W = 168, H = 50;
        using (Bitmap b = New32(W, H))
        {
            using (Graphics g = G(b))
            {
                using (GraphicsPath p = Rounded(new Rectangle(1, 1, W - 3, H - 3), 6))
                {
                    g.FillPath(new SolidBrush(green), p);
                    g.DrawPath(new Pen(Color.FromArgb(255, 6, 36, 16), 2f), p);
                }
                Rectangle hz = new Rectangle(W - 46, 4, 42, H - 8);
                using (GraphicsPath hp = Rounded(hz, 4))
                {
                    g.SetClip(hp);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 8, 12, 10)), hz);
                    using (Pen stripe = new Pen(green, 5f))
                        for (int x = hz.Left - 48; x < hz.Right + 14; x += 15)
                            g.DrawLine(stripe, x, hz.Bottom + 8, x + 30, hz.Top - 8);
                    g.ResetClip();
                    Rectangle chip = new Rectangle(hz.Left + 6, hz.Top + 9, 30, H - 26);
                    using (GraphicsPath cp = Rounded(chip, 3))
                    {
                        g.FillPath(new SolidBrush(Color.FromArgb(255, 0xF2, 0xF5, 0xF7)), cp);
                        g.DrawPath(new Pen(Color.FromArgb(255, 8, 12, 10), 1.5f), cp);
                    }
                }
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 220x60 big green LAUNCH button, hazard-stripe block right (sized 1:1 to display)
    static void LaunchButton(string path, Color green)
    {
        int W = 220, H = 60;
        using (Bitmap b = New32(W, H))
        {
            using (Graphics g = G(b))
            {
                using (GraphicsPath p = Rounded(new Rectangle(1, 1, W - 3, H - 3), 7))
                {
                    g.FillPath(new SolidBrush(green), p);
                    g.DrawPath(new Pen(Color.FromArgb(255, 6, 36, 16), 2f), p);
                }
                Rectangle hz = new Rectangle(W - 44, 4, 40, H - 8);
                using (GraphicsPath hp = Rounded(hz, 5))
                {
                    g.SetClip(hp);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 8, 12, 10)), hz);
                    using (Pen stripe = new Pen(green, 6f))
                        for (int x = hz.Left - 54; x < hz.Right + 16; x += 17)
                            g.DrawLine(stripe, x, hz.Bottom + 9, x + 34, hz.Top - 9);
                    g.ResetClip();
                }
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 pause icon (two bars)
    static void IconPause(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            {
                g.FillRectangle(new SolidBrush(Glyph), 9, 6, 5, 20);
                g.FillRectangle(new SolidBrush(Glyph), 18, 6, 5, 20);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 sunrise icon: half sun with rays over a horizon line
    static void IconSunrise(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            {
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddPie(9, 12, 14, 14, 180, 180); // upper half disc
                    g.FillPath(new SolidBrush(Glyph), p);
                }
                using (Pen ray = new Pen(Glyph, 2.4f))
                {
                    g.DrawLine(ray, 16, 3, 16, 8);    // up
                    g.DrawLine(ray, 6, 7, 9.5f, 10.5f);   // up-left
                    g.DrawLine(ray, 26, 7, 22.5f, 10.5f); // up-right
                }
                g.FillRectangle(new SolidBrush(Glyph), 4, 21, 24, 3); // horizon
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 white landing-gear icon (wheel + strut)
    static void IconGear(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            using (Pen p = new Pen(Glyph, 3f))
            {
                g.DrawLine(p, 16, 3, 16, 15);   // strut
                g.DrawLine(p, 16, 8, 8, 3);     // drag brace
                g.DrawEllipse(p, 9, 15, 14, 14); // wheel
                g.FillEllipse(new SolidBrush(Glyph), 14, 20, 4, 4);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 white light-bulb icon
    static void IconLight(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            {
                using (Pen p = new Pen(Glyph, 3f))
                    g.DrawEllipse(p, 8, 3, 16, 16);
                g.FillRectangle(new SolidBrush(Glyph), 12, 21, 8, 3);
                g.FillRectangle(new SolidBrush(Glyph), 13, 25, 6, 3);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 white brakes icon: (B) - circle with side arcs
    static void IconBrakes(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            using (Pen p = new Pen(Glyph, 3f))
            {
                g.DrawEllipse(p, 9, 9, 14, 14);
                g.DrawArc(p, 3, 6, 10, 20, 110, 140);
                g.DrawArc(p, 19, 6, 10, 20, -70, 140);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 white solar-panel icon: a small body + a paneled wing with cell grid
    static void IconSolar(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            using (Pen p = new Pen(Glyph, 2.4f))
            {
                g.FillRectangle(new SolidBrush(Glyph), 14, 6, 4, 20);   // central mast
                g.DrawRectangle(p, 3, 9, 9, 14);                        // left wing
                g.DrawRectangle(p, 20, 9, 9, 14);                       // right wing
                g.DrawLine(p, 3, 16, 12, 16); g.DrawLine(p, 20, 16, 29, 16);
                g.DrawLine(p, 7, 9, 7, 23); g.DrawLine(p, 25, 9, 25, 23);
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 white radiator icon: a spine with fins
    static void IconRadiator(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            using (Pen p = new Pen(Glyph, 2.6f))
            {
                g.DrawLine(p, 16, 4, 16, 28);        // spine
                for (int y = 8; y <= 24; y += 5)     // fins
                {
                    g.DrawLine(p, 6, y, 16, y);
                    g.DrawLine(p, 16, y, 26, y);
                }
            }
            b.Save(path, ImageFormat.Png);
        }
    }

    // 32x32 abort icon: warning triangle + !
    static void IconAbort(string path)
    {
        using (Bitmap b = New32(32, 32))
        {
            using (Graphics g = G(b))
            {
                Point[] tri = { new Point(16, 3), new Point(29, 27), new Point(3, 27) };
                g.DrawPolygon(new Pen(Glyph, 3f), tri);
                g.FillRectangle(new SolidBrush(Glyph), 14, 11, 4, 9);
                g.FillRectangle(new SolidBrush(Glyph), 14, 22, 4, 3);
            }
            b.Save(path, ImageFormat.Png);
        }
    }
}
