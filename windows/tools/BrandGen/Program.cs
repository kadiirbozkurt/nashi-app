using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace Nashi.Windows;

/// Writes the Bozkurt Çark brand images into the folder given as the only argument (Nashi.Windows/Brand).
internal static class Program
{
    private static readonly int[] Sizes={16,20,24,32,40,48,64,128,256};
    private const int PngFrom=64;

    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1){Console.Error.WriteLine("usage: BrandGen <Nashi.Windows/Brand folder>");return 2;}
        var dir=args[0];
        WriteIco(Path.Combine(dir,"app.ico"));
        File.WriteAllBytes(Path.Combine(dir,"icon.png"),Png(Render(new AppIcon{Width=256,Height=256},256)));
        File.WriteAllBytes(Path.Combine(dir,"mark.png"),Png(Mark(active:false)));
        File.WriteAllBytes(Path.Combine(dir,"mark-active.png"),Png(Mark(active:true)));
        Console.WriteLine("wrote app.ico, icon.png, mark.png, mark-active.png");return 0;
    }

    /// The red-eyed wolf head on transparency; the active mark adds a turquoise glow (a selected file).
    private static BitmapSource Mark(bool active)
    {
        var crop=BrandArt.HeadCrop;
        var head=new Image{Source=new CroppedBitmap(BrandArt.Logo,new Int32Rect((int)crop.X,(int)crop.Y,(int)crop.Width,(int)crop.Height)),Width=256,Height=256};
        RenderOptions.SetBitmapScalingMode(head,BitmapScalingMode.HighQuality);
        if(active)head.Effect=new DropShadowEffect{Color=BrandArt.Accent,ShadowDepth=0,BlurRadius=28,Opacity=1};
        return Render(head,256);
    }

    private static BitmapSource Render(FrameworkElement element,int size)
    {
        element.Measure(new Size(size,size));element.Arrange(new Rect(0,0,size,size));
        var bitmap=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
        var straight=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);straight.Freeze();return straight;
    }

    private static byte[] Png(BitmapSource bitmap)
    {
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=new MemoryStream();encoder.Save(stream);return stream.ToArray();
    }

    // Same layout as Bozkurt Pro's IconGen: PNG entries from 64 px, 32-bit BMP with AND mask below.
    private static void WriteIco(string path)
    {
        var images=Sizes.Select(size=>{var icon=Render(new AppIcon{Width=size,Height=size},size);return (size,data:size>=PngFrom?Png(icon):Dib(icon));}).ToList();
        using var writer=new BinaryWriter(File.Create(path));
        writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)images.Count);
        int offset=6+16*images.Count;
        foreach(var (size,data) in images)
        {
            writer.Write((byte)(size>=256?0:size));writer.Write((byte)(size>=256?0:size));writer.Write((byte)0);writer.Write((byte)0);
            writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(data.Length);writer.Write(offset);offset+=data.Length;
        }
        foreach(var (_,data) in images)writer.Write(data);
    }

    private static byte[] Dib(BitmapSource bitmap)
    {
        int size=bitmap.PixelWidth,stride=size*4;var pixels=new byte[stride*size];bitmap.CopyPixels(pixels,stride,0);int maskStride=(size+31)/32*4;
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write(40);writer.Write(size);writer.Write(size*2);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);
        writer.Write(stride*size+maskStride*size);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
        for(int y=size-1;y>=0;y--)writer.Write(pixels,y*stride,stride);
        for(int y=size-1;y>=0;y--){var row=new byte[maskStride];for(int x=0;x<size;x++)if(pixels[y*stride+x*4+3]==0)row[x/8]|=(byte)(0x80>>(x%8));writer.Write(row);}
        writer.Flush();return stream.ToArray();
    }
}
