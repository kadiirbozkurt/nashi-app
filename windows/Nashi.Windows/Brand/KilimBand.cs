using System.Windows;
using System.Windows.Media;

namespace Nashi.Windows;

/// <summary>
/// 7 px band of the "kurt agzi" (wolf mouth) kilim pattern, tiled across its width.
/// Geometry and colours come from the approved Ergenekon mockup.
/// </summary>
public sealed class KilimBand : FrameworkElement
{
    public const double BandHeight = 7;
    private const double TileWidth = 28;
    private const double BandOpacity = 0.75;

    private static readonly Brush Ground = Frozen(new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x14)));
    private static readonly Brush Turquoise = Frozen(new SolidColorBrush(Color.FromRgb(0x27, 0xC6, 0xB4)));
    private static readonly Brush Red = Frozen(new SolidColorBrush(Color.FromRgb(0xE8, 0x1E, 0x26)));
    private static readonly Brush Tile = BuildTile();

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, BandHeight);

    protected override void OnRender(DrawingContext dc)
    {
        dc.PushOpacity(BandOpacity);
        dc.DrawRectangle(Tile, null, new Rect(0, 0, ActualWidth, BandHeight));
        dc.Pop();
    }

    private static Brush BuildTile()
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            dc.DrawRectangle(Ground, null, new Rect(0, 0, TileWidth, BandHeight));
            dc.DrawGeometry(Turquoise, null, Triangle(0, 7, 7, 0, 14, 7));
            dc.DrawGeometry(Red, null, Triangle(14, 7, 21, 0, 28, 7));
            dc.DrawGeometry(Ground, null, Triangle(4, 7, 7, 4, 10, 7));
            dc.DrawGeometry(Ground, null, Triangle(18, 7, 21, 4, 24, 7));
        }
        group.Freeze();
        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, TileWidth, BandHeight),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, TileWidth, BandHeight),
        };
        brush.Freeze();
        return brush;
    }

    private static Geometry Triangle(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(x1, y1), true, true);
            ctx.LineTo(new Point(x2, y2), false, false);
            ctx.LineTo(new Point(x3, y3), false, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
