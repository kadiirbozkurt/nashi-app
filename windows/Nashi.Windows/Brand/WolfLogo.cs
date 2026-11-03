using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Nashi.Windows;

/// <summary>
/// Bozkurt wolf head (cropped from Assets/logo.png, see <see cref="BrandArt"/>) with glowing red eyes.
/// The eyes are drawn in the logo image's own pixel coordinates inside a Viewbox, so they stay in place at
/// any size. The red glow breathes slowly (no blinking); it is static when Windows animations are off.
/// logo.png carries a "BOZKURT PRINT" wordmark; it is cropped away at render time and, when
/// <see cref="ShowWordmark"/> is set, replaced by a "BOZKURT / ÇARK" wordmark drawn in the same style.
/// <see cref="ShowHeaderLockup"/> sets the wordmark and the Old Turkic line beside the head instead, for the window
/// header. <see cref="IsWatermark"/> gives the still, halo-free wolf for large faded art.
/// </summary>
public sealed class WolfLogo : Grid
{
    /// <summary>"Boz Kurt" in Old Turkic (Orkhon) script.</summary>
    public const string OldTurkicLine = "𐰉𐰆𐰔 𐰴𐰆𐰺𐱃";

    private const string OldTurkicFont = "Segoe UI Historic";

    private static readonly Color WordmarkWhite = Color.FromRgb(244, 247, 248);

    private readonly FrameworkElement _wordmark;
    private readonly ColumnDefinition _headColumn = new();
    private readonly Image _image = new() { Source = BrandArt.Logo, Stretch = Stretch.Fill };
    private readonly Canvas _eyes;
    private FrameworkElement? _lockup;
    private bool _isWatermark;

    // Wide soft red bloom behind the eyes; it is the layer that breathes.
    private readonly Canvas _bloom;

    public WolfLogo()
    {
        var art = new Grid { Width = BrandArt.ArtWidth, Height = BrandArt.ArtHeight };
        art.Children.Add(_image);

        _bloom = EyeLayer(scale: 1.5);
        _bloom.Effect = new BlurEffect { Radius = 24 };
        art.Children.Add(_bloom);
        _eyes = EyeLayer(scale: 1);   // the eyes themselves, with a tight red glow
        _eyes.Effect = new DropShadowEffect { Color = BrandArt.Red, ShadowDepth = 0, BlurRadius = 7, Opacity = 0.9 };
        art.Children.Add(_eyes);

        // Crop: show only the head region of the full 600×720 art.
        Canvas.SetLeft(art, -BrandArt.HeadCrop.X);
        Canvas.SetTop(art, -BrandArt.HeadCrop.Y);
        var head = new Canvas { Width = BrandArt.HeadCrop.Width, Height = BrandArt.HeadCrop.Height, ClipToBounds = true, Children = { art } };

        _wordmark = BuildWordmark();
        _wordmark.Visibility = Visibility.Collapsed;
        var layout = new StackPanel { Width = BrandArt.HeadCrop.Width, Children = { head, _wordmark } };
        ColumnDefinitions.Add(_headColumn);
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = layout });

        Loaded += (_, _) => Pulse(still: _isWatermark || !SystemParameters.ClientAreaAnimation);
        Unloaded += (_, _) => Pulse(still: true);
    }

    /// <summary>Shows the "BOZKURT / ÇARK" wordmark under the head (default: head only).</summary>
    public bool ShowWordmark
    {
        get => _wordmark.Visibility == Visibility.Visible;
        set => _wordmark.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Shows "BOZKURT ÇARK" and the Old Turkic line to the right of the head: give the logo a height, not a width.</summary>
    public bool ShowHeaderLockup
    {
        get => _lockup?.Visibility == Visibility.Visible;
        set
        {
            if (value == ShowHeaderLockup) return;
            if (_lockup is null) Children.Add(_lockup = BuildHeaderLockup());
            _lockup.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            _headColumn.Width = value ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        }
    }

    /// <summary>
    /// Still art without halo, glow or breathing, for large faded use. The eyes stay red: they are part of
    /// <see cref="BrandArt.WolfOnly"/>.
    /// </summary>
    public bool IsWatermark
    {
        get => _isWatermark;
        set
        {
            _isWatermark = value;
            _image.Source = value ? BrandArt.WolfOnly : BrandArt.Logo;
            _bloom.Visibility = _eyes.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>Both eye polygons, scaled by <paramref name="scale"/> around their own centres.</summary>
    private static Canvas EyeLayer(double scale)
    {
        var layer = new Canvas();
        foreach (var eye in BrandArt.Eyes(scale))
            layer.Children.Add(new Polygon { Points = eye, Fill = new SolidColorBrush(BrandArt.Red) });
        return layer;
    }

    /// <summary>
    /// "BOZKURT" (white, bold, wide tracking) over a thin red gradient line, then "ÇARK" (light turquoise, small,
    /// wider tracking): the BOZKURT / PRINT layout of the Bozkurt Print card back, with ÇARK in place of PRINT.
    /// Sizes are in logo.png pixels.
    /// </summary>
    private static FrameworkElement BuildWordmark()
    {
        var line = new Rectangle
        {
            Width = 270, Height = 3, RadiusX = 1.5, RadiusY = 1.5, Margin = new Thickness(0, 8, 0, 0),
            Fill = new LinearGradientBrush(BrandArt.Red, BrandArt.RedLight, 0),
        };
        return new StackPanel
        {
            Margin = new Thickness(0, -6, 0, 12),
            Children =
            {
                // Fitted to the original wordmark's width (457 px), whatever the font's own metrics.
                new Viewbox { Width = 457, Stretch = Stretch.Uniform, Child = Tracked("BOZKURT", WordmarkWhite, 90, 11) },
                line,
                ProText(),
            },
        };
    }

    /// <summary>The header's text block: the wordmark on one line, the Old Turkic line in gold under it.</summary>
    private static StackPanel BuildHeaderLockup()
    {
        var bozkurt = Tracked("BOZKURT", WordmarkWhite, 21, 4.5);
        var pro = Tracked("ÇARK", BrandArt.AccentLight, 21, 4.5, new Thickness(9, 0, 0, 0));
        var wordmark = new StackPanel { Orientation = Orientation.Horizontal, Children = { bozkurt, pro } };
        var oldTurkic = new TextBlock
        {
            Text = OldTurkicLine, FontFamily = new FontFamily(OldTurkicFont), FontSize = 11, Opacity = 0.8,
            Foreground = new SolidColorBrush(BrandArt.Gold), HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(1, 1, 0, 0),
        };
        var lockup = new StackPanel { Margin = new Thickness(2, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Children = { wordmark, oldTurkic } };
        SetColumn(lockup, 1);
        return lockup;
    }

    /// <summary>"ÇARK" sits on the night backdrop: the light turquoise over a soft dark halo.</summary>
    private static StackPanel ProText()
    {
        var pro = Tracked("ÇARK", BrandArt.AccentLight, 40, 40, new Thickness(0, 10, 0, 0));
        pro.Effect = new DropShadowEffect { Color = Colors.Black, ShadowDepth = 0, BlurRadius = 16, Opacity = 0.75 };
        return pro;
    }

    /// <summary>Letter-spaced text: one TextBlock per letter (WPF has no letter-spacing property).</summary>
    private static StackPanel Tracked(string text, Color color, double fontSize, double tracking, Thickness margin = default)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = margin };
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        for (int i = 0; i < text.Length; i++)
            panel.Children.Add(new TextBlock
            {
                Text = text[i].ToString(), FontSize = fontSize, FontWeight = FontWeights.Bold, Foreground = brush,
                Margin = new Thickness(0, 0, i < text.Length - 1 ? tracking : 0, 0),
            });
        return panel;
    }

    /// <summary>Calm eye glow: bloom opacity breathes 0.75 ↔ 1 over 3.5 s; full glow when <paramref name="still"/>.</summary>
    private void Pulse(bool still)
    {
        if (still)
        {
            _bloom.BeginAnimation(OpacityProperty, null);
            _bloom.Opacity = 1;
            return;
        }
        var breathe = new DoubleAnimation(0.75, 1, TimeSpan.FromSeconds(1.75))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        _bloom.BeginAnimation(OpacityProperty, breathe);
    }
}
