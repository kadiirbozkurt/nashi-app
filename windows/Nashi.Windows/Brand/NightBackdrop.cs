using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nashi.Windows;

/// <summary>
/// Window backdrop of the glass design: the night landscape Assets/night.jpg (painted by tools/IconGen), filling the
/// window with the mountains kept at the bottom. Glass panels sit on top of it and only tint what lies behind them.
/// </summary>
public sealed class NightBackdrop : Grid
{
    // One decoded bitmap and one cached brush for every window.
    private static readonly Lazy<ImageBrush> Scene = new(CreateScene);

    public NightBackdrop() => Background = Scene.Value;

    /// <summary>The shared night scene brush, for window backgrounds.</summary>
    internal static ImageBrush SceneBrush => Scene.Value;

    private static ImageBrush CreateScene()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri($"pack://application:,,,/{typeof(NightBackdrop).Assembly.GetName().Name};component/Assets/night.jpg");
        image.EndInit();
        image.Freeze();

        var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Bottom };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality); // shown about 2.3x down: stars must not sparkle
        RenderOptions.SetCachingHint(brush, CachingHint.Cache);                   // nothing re-scales it while the window redraws
        brush.Freeze();
        return brush;
    }
}
