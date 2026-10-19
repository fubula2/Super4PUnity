using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

//Android builds carry the default music track from Packaging/mods/music inside the APK (other platforms ship it in their mods folder).
//The files are copied into StreamingAssets only for the duration of the build.
public class AndroidDefaultMusic : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string Source = "Packaging/mods/music";
    private const string Target = "Assets/StreamingAssets/DefaultMusic";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform != BuildTarget.Android || !Directory.Exists(Source)) { return; }
        Directory.CreateDirectory(Target);
        var names = Directory.GetFiles(Source, "*.ogg").Select(Path.GetFileName).ToArray();
        foreach(var name in names) { File.Copy(Path.Combine(Source, name), Path.Combine(Target, name), true); }
        File.WriteAllText(Path.Combine(Target, "list.txt"), string.Join("\n", names));
        AssetDatabase.Refresh();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if(report.summary.platform != BuildTarget.Android) { return; }
        if(Directory.Exists(Target)) { FileUtil.DeleteFileOrDirectory(Target); }
        if(File.Exists(Target + ".meta")) { FileUtil.DeleteFileOrDirectory(Target + ".meta"); }
        if(Directory.Exists("Assets/StreamingAssets") && !Directory.EnumerateFileSystemEntries("Assets/StreamingAssets").Any(e => !e.EndsWith(".meta")))
        {
            FileUtil.DeleteFileOrDirectory("Assets/StreamingAssets");
            if(File.Exists("Assets/StreamingAssets.meta")) { FileUtil.DeleteFileOrDirectory("Assets/StreamingAssets.meta"); }
        }
        AssetDatabase.Refresh();
    }
}
