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
    private static void Inside(Action<bool,string> check,string archive,string output,string label)
    {
        var unpacked=Path.Combine(output,"Unpacked "+label);Directory.CreateDirectory(unpacked);ArchiveEngine.Extract(archive,unpacked);
        var found=Directory.GetFiles(unpacked,"not.txt",SearchOption.AllDirectories).SingleOrDefault();
        check(found is not null&&found.Contains(Path.Combine("Düğün Klasörü","İç"))&&File.ReadAllText(found,Encoding.UTF8)=="merhaba",label+" lost the folder structure or its Turkish names");
    }
}
