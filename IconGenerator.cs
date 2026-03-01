using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace FeColorizer;

/// <summary>
/// Generates and caches colored folder .ico files in %AppData%\FeColorizer\icons\.
/// Icons are created once on first use and reused on subsequent runs.
/// </summary>
public static class IconGenerator
{
    private static readonly string IconDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FeColorizer", "icons");

    /// <summary>
    /// Returns the path to the .ico file for the given color, generating it if needed.
    /// </summary>
    public static string GetOrCreateIcon(char letter, Color color)
    {
        Directory.CreateDirectory(IconDir);
        string path = Path.Combine(IconDir, $"{letter}.ico");

        if (!File.Exists(path))
            GenerateIcon(color, path);

        return path;
    }

    /// <summary>
    /// Pre-generates all 26 folder icons, always overwriting existing files
    /// so that size/format changes are picked up on reinstall.
    /// </summary>
    public static void GenerateAll()
    {
        Directory.CreateDirectory(IconDir);
        foreach (var kvp in ColorMap.Map)
        {
            string path = Path.Combine(IconDir, $"{kvp.Key}.ico");
            GenerateIcon(kvp.Value.Color, path);
        }
    }

    private static void GenerateIcon(Color color, string outputPath)
    {
        using var bmp256 = DrawFolder(color, 256);
        using var bmp48  = DrawFolder(color, 48);
        using var bmp32  = DrawFolder(color, 32);
        using var bmp16  = DrawFolder(color, 16);

        WriteIco(outputPath, [
            (256, ToPngBytes(bmp256)),
            (48,  ToPngBytes(bmp48)),
            (32,  ToPngBytes(bmp32)),
            (16,  ToPngBytes(bmp16)),
        ]);
    }

    /// <summary>
    /// Draws a flat folder icon at the given pixel size.
    /// </summary>
    private static Bitmap DrawFolder(Color color, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float s = size / 32f;

        Color tabColor    = Darken(color, 0.15f);
        Color shadowColor = Darken(color, 0.30f);

        // Folder tab (trapezoid at top-left)
        using (var tabPath = new GraphicsPath())
        {
            tabPath.AddLines(new PointF[]
            {
                new(1  * s, 11 * s),
                new(1  * s,  6 * s),
                new(11 * s,  6 * s),
                new(14 * s, 11 * s),
            });
            tabPath.CloseFigure();
            using var br = new SolidBrush(tabColor);
            g.FillPath(br, tabPath);
        }

        // Folder body
        var bodyRect = new RectangleF(1 * s, 10 * s, 30 * s, 20 * s);
        using (var bodyBrush = new SolidBrush(color))
            FillRoundRect(g, bodyBrush, bodyRect, 2 * s);

        // Subtle bottom shadow strip
        var shadowRect = new RectangleF(1 * s, 26 * s, 30 * s, 4 * s);
        using (var shadowBrush = new SolidBrush(Color.FromArgb(60, shadowColor)))
            FillRoundRect(g, shadowBrush, shadowRect, 2 * s);

        return bmp;
    }

    // -------------------------------------------------------------------------
    // Drawing helpers
    // -------------------------------------------------------------------------

    private static void FillRoundRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static GraphicsPath RoundRectPath(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X,         r.Y,          d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Darken(Color c, float amount) => Color.FromArgb(
        c.A,
        Math.Max(0, (int)(c.R * (1f - amount))),
        Math.Max(0, (int)(c.G * (1f - amount))),
        Math.Max(0, (int)(c.B * (1f - amount))));

    private static byte[] ToPngBytes(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a minimal .ico file containing PNG-compressed frames.
    /// </summary>
    private static void WriteIco(string path, (int Size, byte[] PngData)[] frames)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        int count = frames.Length;

        bw.Write((short)0);
        bw.Write((short)1);
        bw.Write((short)count);

        int currentOffset = 6 + count * 16;

        foreach (var (sz, png) in frames)
        {
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write((int)png.Length);
            bw.Write((int)currentOffset);
            currentOffset += png.Length;
        }

        foreach (var (_, png) in frames)
            bw.Write(png);
    }
}
