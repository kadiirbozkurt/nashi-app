namespace Nashi.Core;

/// Packs folders into one archive. RAR has no free encoder, so it needs WinRAR's Rar.exe;
/// without WinRAR only ZIP is offered.
public static class FolderArchive
{
    public static bool IsFolder(string path)=>Directory.Exists(path)&&FeatureCatalog.Extension(path) is not ("pages" or "rtfd");
    public static string? RarExecutable()=>new[]{Environment.SpecialFolder.ProgramFiles,Environment.SpecialFolder.ProgramFilesX86}
        .Select(root=>Path.Combine(Environment.GetFolderPath(root),"WinRAR","Rar.exe")).FirstOrDefault(File.Exists);
    public static string[] Formats()=>RarExecutable() is null?["zip"]:["rar","zip"];
    public static async Task<IReadOnlyList<string>> CreateAsync(IReadOnlyList<string> inputs,string format,string destination,IProgress<OperationProgress>? progress=null,CancellationToken token=default)
    {
        if(inputs.Count==0)throw new ArgumentException("Choose folders to pack.");
        var full=inputs.Select(input=>Path.GetFullPath(input).TrimEnd(Path.DirectorySeparatorChar)).ToArray();
        // Packing into a selected folder would make the archive contain its own staging file.
        var target=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);
        if(full.Any(input=>Directory.Exists(input)&&(target.Equals(input,StringComparison.OrdinalIgnoreCase)||target.StartsWith(input+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))))throw new InvalidDataException("Choose a destination outside the selected folder.");
        var name=full.Length==1?Path.GetFileName(full[0]):"Archive";
        if(format=="zip")return [await Task.Run(()=>DocumentEngine.Zip(full,destination,progress,token,name),token)];
        if(format!="rar")throw new ArgumentException("Choose RAR or ZIP.");
        var rar=RarExecutable()??throw new InvalidOperationException("Install WinRAR to create RAR archives.");
        using var tx=new ExportTransaction(destination);var output=Path.Combine(tx.StagingDirectory,name+".rar");
        progress?.Report(new(null,"Packing files…"));
        // -ep1 stores each input under its own name instead of its full path.
        await ProcessRunner.RunAsync(rar,["a","-r","-ep1","-idq","-y","--",output,..full],token);
        return tx.Publish([output],token);
    }
}
