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
    internal static Brush Primary=>new LinearGradientBrush(Color.FromRgb(0x27,0xC6,0xB4),Color.FromRgb(0x7B,0xE6,0xDA),0);
    internal static Brush Brush(string name)=>(Brush)Application.Current.FindResource(name);
    internal static void Apply(string appearance)
    {
        // Bozkurt Çark is always the Ergenekon night look, like Bozkurt Pro: the appearance setting no longer changes it.
        Dark=true;
        // Ergenekon glass (Bozkurt Pro): translucent white panels over the night scene, turquoise for focus and
        // selection. Paper stays opaque for popups and windows without the scene. Float is the dark glass of
        // floating overlays (wheels, toasts): they sit on the desktop, where the white panels would vanish.
        foreach(var (key,light,dark) in new[]{("Paper","#15131D","#15131D"),("Ink","#F5FFFFFF","#F5FFFFFF"),("Surface","#1CFFFFFF","#1CFFFFFF"),("Inset","#14FFFFFF","#14FFFFFF"),("Muted","#9EFFFFFF","#9EFFFFFF"),("Line","#29FFFFFF","#29FFFFFF"),("GlassRim","#52FFFFFF","#52FFFFFF"),("Sage","#5927C6B4","#5927C6B4"),("Sand","#33E2BC74","#33E2BC74"),("Gold","#27C6B4","#27C6B4"),("Float","#EB1A1726","#EB1A1726")})
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
    // Framed windows show the Bozkurt night scene; layered and frameless windows keep the opaque Paper.
    private static void ApplyBackdrop(Window window,IntPtr handle)
    {
        if(window.AllowsTransparency||window.WindowStyle==WindowStyle.None)return;
        window.Background=NightBackdrop.SceneBrush;
    }
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
}
