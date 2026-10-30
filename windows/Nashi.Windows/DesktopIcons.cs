using Nashi.Core;
using System.Runtime.InteropServices;
namespace Nashi.Windows;

/// Explorer drops a new desktop file in the first free grid cell, far from the file it came
/// from. Moves it next to its source. Purely cosmetic: any failure leaves the icon where it is.
internal static partial class DesktopIcons
{
    private const int SWC_DESKTOP=8,SWFO_NEEDDISPATCH=1,CSIDL_DESKTOP=0,SVGIO_ALLVIEW=2,SVSI_POSITIONITEM=0x80,MONITOR_DEFAULTTONULL=0,S_OK=0;
    private static readonly Guid TopLevelBrowser=new("4C96BE40-915C-11CF-99D3-00AA004AE837");
    private static readonly Guid ShellWindowsClass=new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    public static async Task PlaceBesideAsync(string source,IReadOnlyList<string> outputs)
    {
        try
        {
            if(!OnDesktop(source)||!outputs.All(OnDesktop))return;
            var view=DesktopView(out var window);if(view is null||view.GetAutoArrange()==S_OK)return;
            foreach(var output in outputs)await PlaceAsync(view,window,source,output);
        }
        catch(COMException){}catch(InvalidCastException){}catch(ArgumentException){}catch(IOException){}
    }
    private static bool OnDesktop(string path)
    {
        var parent=Path.GetDirectoryName(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar));
        return new[]{Environment.SpecialFolder.DesktopDirectory,Environment.SpecialFolder.CommonDesktopDirectory}.Any(folder=>string.Equals(parent,Environment.GetFolderPath(folder),StringComparison.OrdinalIgnoreCase));
    }
    private static async Task PlaceAsync(IFolderView view,IntPtr window,string source,string output)
    {
        var sourceItem=Pidl(source);var outputItem=Pidl(output);
        try
        {
            var sourceChild=ILFindLastID(sourceItem);var outputChild=ILFindLastID(outputItem);
            if(view.GetItemPosition(sourceChild,out var from)!=S_OK)return;
            // The view learns about a new file from a shell notification, shortly after it is written.
            var known=false;for(var attempt=0;attempt<20&&!known;attempt++){known=view.GetItemPosition(outputChild,out _)==S_OK;if(!known)await Task.Delay(150);}
            if(!known)return;
            var spacing=default(Point32);view.GetSpacing(ref spacing);
            // GetSpacing is scaled for the display; icon positions and the grid are not.
            var scale=GetDpiForWindow(window)/96d;var cell=(X:(int)Math.Round(spacing.X/scale),Y:(int)Math.Round(spacing.Y/scale));if(cell.X<=0||cell.Y<=0)return;
            var home=MonitorAt(window,from);if(home==IntPtr.Zero)return;
            var slot=DesktopSlots.Next((from.X,from.Y),cell,Occupied(view,outputChild),point=>MonitorAt(window,new(point.X+cell.X/2,point.Y+cell.Y/2))==home);
            if(slot is {} free)view.SelectAndPositionItems(1,[outputChild],[new(free.X,free.Y)],SVSI_POSITIONITEM);
        }
        finally{ILFree(sourceItem);ILFree(outputItem);}
    }
    private static List<(int X,int Y)> Occupied(IFolderView view,IntPtr moving)
    {
        var icons=new List<(int X,int Y)>();view.ItemCount(SVGIO_ALLVIEW,out var count);
        for(var index=0;index<count;index++)
        {
            if(view.Item(index,out var child)!=S_OK)continue;
            try{if(!ILIsEqual(child,moving)&&view.GetItemPosition(child,out var point)==S_OK)icons.Add((point.X,point.Y));}
            finally{ILFree(child);}
        }
        return icons;
    }
    private static IntPtr MonitorAt(IntPtr window,Point32 point){ClientToScreen(window,ref point);return MonitorFromPoint(point,MONITOR_DEFAULTTONULL);}
    private static IntPtr Pidl(string path)
    {
        var result=SHParseDisplayName(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),IntPtr.Zero,out var pidl,0,out _);
        if(result!=S_OK)throw new COMException("The item is not in the shell namespace.",result);return pidl;
    }
    private static IFolderView? DesktopView(out IntPtr window)
    {
        window=IntPtr.Zero;var shell=(IShellWindows)Activator.CreateInstance(Type.GetTypeFromCLSID(ShellWindowsClass)!)!;
        object location=CSIDL_DESKTOP,root=Type.Missing;
        var browserService=(IServiceProvider)shell.FindWindowSW(ref location,ref root,SWC_DESKTOP,out _,SWFO_NEEDDISPATCH);
        var service=TopLevelBrowser;var shellBrowser=typeof(IShellBrowser).GUID;
        var browser=(IShellBrowser)browserService.QueryService(ref service,ref shellBrowser);
        var active=browser.QueryActiveShellView();active.GetWindow(out window);return active as IFolderView;
    }
    [StructLayout(LayoutKind.Sequential)]private struct Point32(int x,int y){public int X=x,Y=y;}
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]private static extern int SHParseDisplayName(string name,IntPtr context,out IntPtr pidl,uint attributesIn,out uint attributesOut);
    [DllImport("shell32.dll")]private static extern IntPtr ILFindLastID(IntPtr pidl);
    [DllImport("shell32.dll")]private static extern void ILFree(IntPtr pidl);
    [DllImport("shell32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool ILIsEqual(IntPtr first,IntPtr second);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool ClientToScreen(IntPtr window,ref Point32 point);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(Point32 point,int flags);
    [DllImport("user32.dll")]private static extern uint GetDpiForWindow(IntPtr window);
    [ComImport,Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"),InterfaceType(ComInterfaceType.InterfaceIsDual)]private interface IShellWindows
    {
        int Count{get;}[return:MarshalAs(UnmanagedType.IDispatch)]object Item(object index);[return:MarshalAs(UnmanagedType.IUnknown)]object NewEnum();
        void Register([MarshalAs(UnmanagedType.IDispatch)]object window,int handle,int windowClass,out int cookie);void RegisterPending(int thread,ref object location,ref object root,int windowClass,out int cookie);void Revoke(int cookie);
        void OnNavigate(int cookie,ref object location);void OnActivated(int cookie,[MarshalAs(UnmanagedType.VariantBool)]bool active);
        [return:MarshalAs(UnmanagedType.IDispatch)]object FindWindowSW(ref object location,ref object root,int windowClass,out int handle,int options);
    }
    [ComImport,Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IServiceProvider
    {
        [return:MarshalAs(UnmanagedType.IUnknown)]object QueryService(ref Guid service,ref Guid interfaceId);
    }
    [ComImport,Guid("000214E2-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IShellBrowser
    {
        void GetWindow(out IntPtr window);void ContextSensitiveHelp(int enter);void InsertMenusSB(IntPtr shared,IntPtr widths);void SetMenuSB(IntPtr shared,IntPtr ole,IntPtr active);void RemoveMenusSB(IntPtr shared);void SetStatusTextSB(IntPtr text);void EnableModelessSB(int enable);void TranslateAcceleratorSB(IntPtr message,short id);void BrowseObject(IntPtr pidl,uint flags);void GetViewStateStream(uint mode,out IntPtr stream);void GetControlWindow(uint id,out IntPtr window);void SendControlMsg(uint id,uint message,IntPtr wide,IntPtr low,out IntPtr result);
        IShellView QueryActiveShellView();
    }
    [ComImport,Guid("000214E3-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IShellView{void GetWindow(out IntPtr window);}
    [ComImport,Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IFolderView
    {
        void GetCurrentViewMode(out uint mode);void SetCurrentViewMode(uint mode);void GetFolder(ref Guid id,out IntPtr folder);
        [PreserveSig]int Item(int index,out IntPtr child);void ItemCount(int flags,out int count);void Items(uint flags,ref Guid id,out IntPtr items);void GetSelectionMarkedItem(out int item);void GetFocusedItem(out int item);
        [PreserveSig]int GetItemPosition(IntPtr child,out Point32 point);void GetSpacing(ref Point32 point);void GetDefaultSpacing(out Point32 point);[PreserveSig]int GetAutoArrange();void SelectItem(int item,uint flags);
        void SelectAndPositionItems(uint count,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]IntPtr[] children,[MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]Point32[] points,uint flags);
    }
}
