using Nashi.Core;
using System.Text;
internal static class FolderArchiveChecks
{
    public static async Task Run(Action<bool,string> check,string root)
    {
        var folder=Path.Combine(root,"Düğün Klasörü");Directory.CreateDirectory(Path.Combine(folder,"İç"));
        await File.WriteAllTextAsync(Path.Combine(folder,"İç","not.txt"),"merhaba",Encoding.UTF8);
        var loose=Path.Combine(root,"Ek.txt");await File.WriteAllTextAsync(loose,"ek",Encoding.UTF8);
        check(FeatureCatalog.Formats([folder]).Contains("zip"),"A folder offered no archive format");
        check(FeatureCatalog.Formats([folder,loose]).Contains("zip"),"A folder with a file offered no archive format");
        check(FeatureCatalog.Tools([folder]).Length==0,"A folder offered file tools it cannot run");
        check(FeatureCatalog.Formats(["a.png"]).All(format=>format is not "rar"),"A single image offered RAR");
        var bundle=Path.Combine(root,"Doc.pages");Directory.CreateDirectory(bundle);
        check(!FolderArchive.IsFolder(bundle),"A Pages bundle was treated as a plain folder");

        var output=Path.Combine(root,"Output");Directory.CreateDirectory(output);
        var zip=(await FolderArchive.CreateAsync([folder],"zip",output,null,default)).Single();
        check(Path.GetFileName(zip)=="Düğün Klasörü.zip","A folder ZIP did not keep the folder name");
        Inside(check,zip,output,"ZIP");

        if(FolderArchive.RarExecutable() is null)return;
        check(FeatureCatalog.Formats([folder])[0]=="rar","RAR was not offered first when WinRAR is installed");
        var rar=(await FolderArchive.CreateAsync([folder],"rar",output,null,default)).Single();
        check(Path.GetFileName(rar)=="Düğün Klasörü.rar","A folder RAR did not keep the folder name");
        Inside(check,rar,output,"RAR");
        var both=(await FolderArchive.CreateAsync([folder,loose],"rar",output,null,default)).Single();
        check(Path.GetFileName(both)=="Archive.rar","Several inputs were not packed into one archive");
        var again=(await FolderArchive.CreateAsync([folder],"rar",output,null,default)).Single();
        check(again!=rar&&File.Exists(rar),"A second archive overwrote the first");

        var refused=false;try{await FolderArchive.CreateAsync([folder],"rar",Path.Combine(folder,"İç"),null,default);}catch(InvalidDataException){refused=true;}
        check(refused,"An archive was written inside the folder it packs");
    }
    public static void Slots(Action<bool,string> check)
    {
        (int X,int Y) source=(100,100),cell=(80,90);bool Anywhere((int X,int Y) p)=>true;
        check(DesktopSlots.Next(source,cell,[source],Anywhere)==(180,100),"A new icon did not land right of its source");
        check(DesktopSlots.Next(source,cell,[source,(180,100)],Anywhere)==(100,190),"A taken right slot did not fall back to below");
        check(DesktopSlots.Next(source,cell,[source,(170,120)],Anywhere)==(20,100),"An icon overlapping two cells was ignored");
        check(DesktopSlots.Next(source,cell,[source],p=>p.X<=100)==(100,190),"A slot off the monitor was used");
        check(DesktopSlots.Next(source,cell,[source,(180,100),(100,190),(20,100),(100,10)],Anywhere)==(180,190),"A full ring did not move to a corner");
        check(DesktopSlots.Next(source,cell,[source],p=>false) is null,"No free slot still returned a position");
    }
    private static void Inside(Action<bool,string> check,string archive,string output,string label)
    {
        var unpacked=Path.Combine(output,"Unpacked "+label);Directory.CreateDirectory(unpacked);ArchiveEngine.Extract(archive,unpacked);
        var found=Directory.GetFiles(unpacked,"not.txt",SearchOption.AllDirectories).SingleOrDefault();
        check(found is not null&&found.Contains(Path.Combine("Düğün Klasörü","İç"))&&File.ReadAllText(found,Encoding.UTF8)=="merhaba",label+" lost the folder structure or its Turkish names");
    }
}
