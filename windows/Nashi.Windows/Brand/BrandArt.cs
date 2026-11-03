using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nashi.Windows;

/// <summary>
/// The Bozkurt wolf artwork shared by <see cref="WolfLogo"/> and <see cref="AppIcon"/>: Assets/logo.png
/// (600×720, ported from Bozkurt Print through Bozkurt Pro) recoloured for this app — the wolf's turquoise eyes become brand red
/// and the turquoise halo behind the head is either tinted or removed. Coordinates are logo.png pixels.
/// </summary>
internal static class BrandArt
{
    public static readonly Color Red = Color.FromRgb(0xE8, 0x1E, 0x26);
    public static readonly Color RedLight = Color.FromRgb(0xFF, 0x4D, 0x57);
    public static readonly Color Gold = Color.FromRgb(0xE2, 0xBC, 0x74);
    // Bozkurt Çark accent: the Ergenekon turquoise, and its light tone for text on the night scene.
    public static readonly Color Accent = Color.FromRgb(0x27, 0xC6, 0xB4);
    public static readonly Color AccentLight = Color.FromRgb(0x7B, 0xE6, 0xDA);

    public const int ArtWidth = 600, ArtHeight = 720;

    // Eye triangles inside logo.png.
    public static readonly Point[] LeftEye = { new(239, 249), new(245, 251), new(265, 258), new(272, 261), new(260, 265), new(250, 267), new(243, 257) };
    public static readonly Point[] RightEye = { new(327, 261), new(337, 257), new(357, 250), new(360, 249), new(359, 252), new(350, 266), new(339, 265) };

    /// <summary>
    /// Both eye outlines, each scaled by <paramref name="scale"/> around its own centre (for glow layers and
    /// small-size legibility) and moved by (<paramref name="dx"/>, <paramref name="dy"/>).
    /// </summary>
    public static IEnumerable<PointCollection> Eyes(double scale = 1, double dx = 0, double dy = 0)
    {
        foreach (var eye in new[] { LeftEye, RightEye })
        {
            var mid = new Point(eye.Average(p => p.X), eye.Average(p => p.Y));
            yield return new PointCollection(eye.Select(p => new Point(mid.X + (p.X - mid.X) * scale + dx, mid.Y + (p.Y - mid.Y) * scale + dy)));
        }
    }

    // Head region: the wolf and its round halo, above the baked-in "BOZKURT PRINT" wordmark (which starts at y 496).
    public static readonly Rect HeadCrop = new(70, 14, 460, 460);

    // Turquoise sampled from logo.png (eyes and halo share it).
    private static readonly Color Turquoise = Color.FromRgb(45, 203, 214);

    // Halo for the in-app logo: a soft warm-white moonlight glow at 60 % of the original strength, instead of
    // turquoise (turquoise means "processing" in this app; a red halo competed with the red eyes).
    private static readonly Color HaloTint = Color.FromRgb(255, 236, 232);
    private const double HaloStrength = 0.6;

    private static readonly Lazy<BitmapSource> WithHalo = new(() => Recolor(keepHalo: true));
    private static readonly Lazy<BitmapSource> WithoutHalo = new(() => Recolor(keepHalo: false));

    /// <summary>Full 600×720 logo with red eyes and a neutral halo (frozen; safe on any thread).</summary>
    public static BitmapSource Logo => WithHalo.Value;

    /// <summary>Full 600×720 logo with red eyes and no halo: just the wolf on transparency (frozen).</summary>
    public static BitmapSource WolfOnly => WithoutHalo.Value;

    /// <summary>
    /// Recolours turquoise pixels. A pixel's "turquoise share" is how far its blue/green rise above its red,
    /// relative to pure turquoise: the grey wolf facets sit near 0, the eyes at 1, anti-aliased eye edges in
    /// between — so edges blend from grey to red exactly as they blended from grey to turquoise. Faint pixels
    /// outside the wolf are the halo.
    /// </summary>
    private static BitmapSource Recolor(bool keepHalo)
    {
        string asm = typeof(BrandArt).Assembly.GetName().Name!;
        var source = new BitmapImage();
        source.BeginInit();
        source.UriSource = new Uri($"pack://application:,,,/{asm};component/Assets/logo.png");
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.EndInit();
        var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int w = bgra.PixelWidth, h = bgra.PixelHeight, stride = w * 4;
        var px = new byte[stride * h];
        bgra.CopyPixels(px, stride, 0);

        const double greyGap = 30; // wolf facets: blue/green at most ~25 above red
        double fullGap = Math.Min(Turquoise.G, Turquoise.B) - Turquoise.R - greyGap;
        for (int i = 0; i < px.Length; i += 4)
        {
            byte b = px[i], g = px[i + 1], r = px[i + 2], a = px[i + 3];
            if (a == 0) continue;
            double share = Math.Clamp((Math.Min(g, b) - r - greyGap) / fullGap, 0, 1);
            if (share == 0) continue;

            if (a < 128) // halo
            {
                if (!keepHalo) { px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0; continue; }
                px[i] = HaloTint.B; px[i + 1] = HaloTint.G; px[i + 2] = HaloTint.R;
                px[i + 3] = (byte)Math.Round(a * HaloStrength);
                continue;
            }
            px[i] = Shift(b, share, Turquoise.B, Red.B);
            px[i + 1] = Shift(g, share, Turquoise.G, Red.G);
            px[i + 2] = Shift(r, share, Turquoise.R, Red.R);
        }

        var result = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, px, stride);
        result.Freeze();
        return result;
    }

    private static byte Shift(byte value, double share, byte from, byte to) =>
        (byte)Math.Clamp(Math.Round(value + share * (to - from)), 0, 255);
}
