using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Nashi.Windows;

internal static class Theme
{
    internal static bool Dark{get;private set;}
    internal static Brush Primary=>new LinearGradientBrush(Color.FromRgb(227,184,89),Color.FromRgb(245,209,125),0);
    internal static Brush Brush(string name)=>(Brush)Application.Current.FindResource(name);
    internal static void Apply(string appearance)
    {
        Dark=appearance=="dark"||(appearance=="system"&&Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",1) is int value&&value==0);
        // Frosted-glass palette: surfaces are translucent so the Acrylic backdrop shows
        // through; GlassPaper tints the backdrop, GlassRim is the bright edge highlight.
        foreach(var (key,light,dark) in new[]{("Paper","#F9F6ED","#111A14"),("GlassPaper","#A6F9F6ED","#9E101813"),("Ink","#243B2B","#E7EBE6"),("Surface","#B3FFFFFF","#5C24342A"),("Inset","#73FFFFFF","#47101A13"),("Muted","#596650","#A9B5A8"),("Line","#99FFFFFF","#3DFFFFFF"),("GlassRim","#E6FFFFFF","#5CFFFFFF"),("Sage","#B3E0EBD2","#80304A35"),("Sand","#B3F6E7BE","#804A3F27")})
            Application.Current.Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark?dark:light));
        foreach(Window window in Application.Current.Windows)ApplyTitleBar(window);
    }
    internal static void Attach(Window window)
    {
        window.SetResourceReference(Window.BackgroundProperty,"Paper");window.SetResourceReference(Window.ForegroundProperty,"Ink");
        window.Loaded+=(_,_)=>{if(window.WindowStyle!=WindowStyle.None)DesktopGeometry.FitToWorkArea(window);L10n.Bind(window,Window.TitleProperty,window.Title);L10n.Attach(window);};
        window.SourceInitialized+=(_,_)=>ApplyTitleBar(window);
    }
    internal static void Entrance(UIElement element)
    {
        if(!WheelPresentation.Preferences.Animations||!SystemParameters.ClientAreaAnimation)return;
        var ease=new QuarticEase{EasingMode=EasingMode.EaseOut};
        element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(260)){EasingFunction=ease});
        var offset=new TranslateTransform();element.RenderTransform=offset;
        offset.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(12,0,TimeSpan.FromMilliseconds(320)){EasingFunction=ease});
    }
    private static void ApplyTitleBar(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;var dark=Dark?1:0;_ = DwmSetWindowAttribute(handle,20,ref dark,sizeof(int));
        ApplyBackdrop(window,handle);
    }
    // Windows 11 22H2+ blurs the desktop behind the window (DWMWA_SYSTEMBACKDROP_TYPE,
    // Acrylic). Layered windows cannot host it, and older systems keep the opaque Paper.
    private static void ApplyBackdrop(Window window,IntPtr handle)
    {
        if(window.AllowsTransparency||window.WindowStyle==WindowStyle.None)return;
        var acrylic=3;if(DwmSetWindowAttribute(handle,38,ref acrylic,sizeof(int))!=0)return;
        var margins=new Margins{Left=-1,Right=-1,Top=-1,Bottom=-1};if(DwmExtendFrameIntoClientArea(handle,ref margins)!=0)return;
        if(HwndSource.FromHwnd(handle)?.CompositionTarget is {} target)target.BackgroundColor=Colors.Transparent;
        window.SetResourceReference(Window.BackgroundProperty,"GlassPaper");
    }
    [StructLayout(LayoutKind.Sequential)]private struct Margins{public int Left,Right,Top,Bottom;}
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")]private static extern int DwmExtendFrameIntoClientArea(IntPtr window,ref Margins margins);
}
