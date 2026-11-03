using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Nashi.Windows;

/// <summary>
/// The Bozkurt Çark app icon as a control: the Bozkurt Pro tile with a turquoise wheel (çark) ring behind the head. a continuous-corner (squircle) tile in deep night teal with the
/// red-eyed wolf head, a soft glossy highlight and a thin inner edge light. Square; draws at its smaller side.
/// At 32 px and below the gloss and eye glow are dropped and the head is enlarged so it stays legible.
/// tools/IconGen renders this control into Assets/app.ico.
/// </summary>
public sealed class AppIcon : FrameworkElement
{
    /// <summary>At or below this size the icon switches to its simplified, larger-head drawing.</summary>
    public const double SmallSize = 32;

    // Superellipse exponent: |x|^n + |y|^n = 1. n = 5 matches the iOS icon outline closely
    // (corner radius ≈ 22.5 % of the side, with curvature that eases in instead of a circular arc).
    private const double SquircleExponent = 5;

    private static readonly Brush Tile = Frozen(new RadialGradientBrush
    {
        Center = new Point(0.5, 0), GradientOrigin = new Point(0.5, 0), RadiusX = 1.2, RadiusY = 0.9,
        GradientStops =
        {
            new GradientStop(Color.FromRgb(0x1F, 0x58, 0x66), 0),
            new GradientStop(Color.FromRgb(0x0D, 0x25, 0x30), 0.55),
            new GradientStop(Color.FromRgb(0x07, 0x14, 0x1A), 1),
        },
    });

    // Soft moonlight behind the head, echoing the halo of the Bozkurt Print night scene.
    private static readonly Brush Moonlight = Frozen(new RadialGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(60, 170, 225, 235), 0),
            new GradientStop(Color.FromArgb(18, 120, 200, 215), 0.55),
            new GradientStop(Color.FromArgb(0, 120, 200, 215), 1),
        }));

    // Glass gloss: bright at the top-left, fading out across the upper part of the tile.
    private static readonly Brush Gloss = Frozen(new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(84, 255, 255, 255), 0),
            new GradientStop(Color.FromArgb(24, 255, 255, 255), 0.42),
            new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.62),
        },
        new Point(0.3, 0), new Point(0.55, 1)));

    // Inner edge light: strongest along the top edge, a faint cool rim at the bottom.
    private static Brush EdgeLight(byte top) => Frozen(new LinearGradientBrush(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(top, 255, 255, 255), 0),
            new GradientStop(Color.FromArgb((byte)(top / 6), 255, 255, 255), 0.35),
            new GradientStop(Color.FromArgb((byte)(top / 8), 200, 235, 255), 0.8),
            new GradientStop(Color.FromArgb((byte)(top / 3), 200, 235, 255), 1),
        },
        new Point(0, 0), new Point(0, 1)));

    private static readonly Brush EdgeLarge = EdgeLight(110), EdgeSmall = EdgeLight(70);

    private static readonly Brush EyeRed = Frozen(new SolidColorBrush(BrandArt.Red));

    private static readonly Lazy<BitmapSource> HeadDetailed = new(RenderHead);
    private static readonly Lazy<BitmapSource> HeadPlain = new(() =>
    {
        var crop = BrandArt.HeadCrop;
        var plain = new CroppedBitmap(BrandArt.WolfOnly, new Int32Rect((int)crop.X, (int)crop.Y, (int)crop.Width, (int)crop.Height));
        plain.Freeze();
        return plain;
    });

    public AppIcon()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double side = Math.Min(availableSize.Width, availableSize.Height);
        return double.IsInfinity(side) ? new Size(64, 64) : new Size(side, side);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double s = Math.Min(ActualWidth, ActualHeight);
        if (s <= 0) return;
        var tile = new Rect((ActualWidth - s) / 2, (ActualHeight - s) / 2, s, s);
        bool small = s <= SmallSize;
        var outline = Squircle(tile);

        dc.PushClip(outline);
        dc.DrawRectangle(Tile, null, tile);

        // The head crop is mostly empty halo space around the wolf (wolf ≈ 61 % × 68 % of the crop), so the crop is
        // drawn at or above tile size: the wolf fills ~60 % of the tile at 128 px and up, growing towards ~70 %
        // as the tile shrinks, and ~83 % at small sizes.
        double head = s * (small ? 1.22 : 0.88 + 0.22 * Math.Clamp((128 - s) / 96, 0, 1));
        var center = new Point(tile.X + s / 2, tile.Y + s * (small ? 0.52 : 0.515));
        if (!small)
            dc.DrawEllipse(Moonlight, null, new Point(center.X, center.Y - s * 0.04), s * 0.46, s * 0.46);
        DrawWheel(dc, new Point(center.X, center.Y - s * 0.03), s, small);
        dc.DrawImage(small ? HeadPlain.Value : HeadDetailed.Value,
            new Rect(center.X - head / 2, center.Y - head / 2, head, head));
        if (small)
            DrawLargeEyes(dc, center, head, s);

        if (!small)
            dc.DrawRectangle(Gloss, null, tile);

        // Edge light: the outline stroked at double width and clipped, so only its inner half shows.
        double edge = small ? 1 : Math.Max(1, s / 96);
        dc.DrawGeometry(null, new Pen(small ? EdgeSmall : EdgeLarge, edge * 2), outline);
        dc.Pop();
    }

    /// <summary>
    /// The çark: eight turquoise arc segments with small gaps around the head, echoing the app's choice wheel.
    /// One thin unbroken ring at small sizes, where gaps would blur away.
    /// </summary>
    private static void DrawWheel(DrawingContext dc, Point center, double s, bool small)
    {
        double radius = s * (small ? 0.43 : 0.40), thickness = small ? Math.Max(1.2, s * 0.06) : s * 0.032;
        var pen = new Pen(WheelBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (small) { dc.DrawEllipse(null, pen, center, radius, radius); return; }
        dc.DrawEllipse(null, new Pen(WheelGlow, thickness * 3.2), center, radius, radius);
        const int segments = 8;
        double gap = 7 * Math.PI / 180, step = 2 * Math.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            double a0 = i * step + gap / 2 - Math.PI / 2, a1 = (i + 1) * step - gap / 2 - Math.PI / 2;
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(center.X + radius * Math.Cos(a0), center.Y + radius * Math.Sin(a0)), isFilled: false, isClosed: false);
                ctx.ArcTo(new Point(center.X + radius * Math.Cos(a1), center.Y + radius * Math.Sin(a1)), new Size(radius, radius), 0, false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }

    private static readonly Brush WheelBrush = Frozen(new SolidColorBrush(Color.FromArgb(230, 0x27, 0xC6, 0xB4)));
    private static readonly Brush WheelGlow = Frozen(new SolidColorBrush(Color.FromArgb(46, 0x27, 0xC6, 0xB4)));

    /// <summary>Superellipse outline filling <paramref name="r"/>.</summary>
    internal static Geometry Squircle(Rect r)
    {
        const int steps = 360;
        double a = r.Width / 2, b = r.Height / 2, cx = r.X + a, cy = r.Y + b, e = 2 / SquircleExponent;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i < steps; i++)
            {
                double t = 2 * Math.PI * i / steps, c = Math.Cos(t), sn = Math.Sin(t);
                var p = new Point(cx + a * Math.Sign(c) * Math.Pow(Math.Abs(c), e), cy + b * Math.Sign(sn) * Math.Pow(Math.Abs(sn), e));
                if (i == 0) ctx.BeginFigure(p, isFilled: true, isClosed: true);
                else ctx.LineTo(p, isStroked: true, isSmoothJoin: true);
            }
        }
        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// Small sizes: the eyes enlarged around their centres (1.5× at 32 px up to 2.1× at 16 px), so they still
    /// read as two red points once the head is only a dozen pixels wide.
    /// </summary>
    private static void DrawLargeEyes(DrawingContext dc, Point center, double head, double size)
    {
        var crop = BrandArt.HeadCrop;
        double scale = 1.5 + Math.Clamp((SmallSize - size) / 16, 0, 1) * 0.6, k = head / crop.Width;
        foreach (var eye in BrandArt.Eyes(scale, -crop.X - crop.Width / 2, -crop.Y - crop.Height / 2))
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(center.X + eye[0].X * k, center.Y + eye[0].Y * k), isFilled: true, isClosed: true);
                ctx.PolyLineTo(eye.Skip(1).Select(p => new Point(center.X + p.X * k, center.Y + p.Y * k)).ToList(), isStroked: false, isSmoothJoin: false);
            }
            geometry.Freeze();
            dc.DrawGeometry(EyeRed, null, geometry);
        }
    }

    /// <summary>
    /// Pre-renders the detailed wolf head (head crop of the halo-free logo) once, at 2× the crop size:
    /// a soft drop shadow under the head, a wide red bloom and a tight red glow around the eyes.
    /// </summary>
    private static BitmapSource RenderHead()
    {
        var crop = BrandArt.HeadCrop;
        var root = new Grid { Width = crop.Width, Height = crop.Height };
        var wolf = new Image
        {
            Source = HeadPlain.Value,
            Effect = new DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 16, BlurRadius = 34, Opacity = 0.55 },
        };
        RenderOptions.SetBitmapScalingMode(wolf, BitmapScalingMode.HighQuality);
        root.Children.Add(wolf);
        root.Children.Add(Eyes(crop, 1.6, new BlurEffect { Radius = 22 }));
        root.Children.Add(Eyes(crop, 1, new DropShadowEffect { Color = BrandArt.Red, ShadowDepth = 0, BlurRadius = 8, Opacity = 0.9 }));

        root.Measure(new Size(crop.Width, crop.Height));
        root.Arrange(new Rect(0, 0, crop.Width, crop.Height));
        var bitmap = new RenderTargetBitmap((int)crop.Width * 2, (int)crop.Height * 2, 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(root);
        bitmap.Freeze();
        return bitmap;
    }

    private static Canvas Eyes(Rect crop, double scale, Effect? effect)
    {
        var layer = new Canvas { Effect = effect };
        foreach (var eye in BrandArt.Eyes(scale, -crop.X, -crop.Y))
            layer.Children.Add(new Polygon { Points = eye, Fill = new SolidColorBrush(BrandArt.Red) });
        return layer;
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
