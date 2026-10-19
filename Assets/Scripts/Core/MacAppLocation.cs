using System;
using System.IO;
using System.Text.RegularExpressions;

//Finds the folder a Mac app was unzipped into, so a "mods" folder next to the .app can be the default.
//macOS may run a freshly downloaded app from a randomized read-only path ("app translocation"); the
//real location is then listed in the output of `mount` (a nullfs mount whose source is the original).
public static class MacAppLocation
{
    private static readonly Regex NullfsMount = new Regex(@"^(?<src>.+) on (?<mp>/.+?) \((?<opts>nullfs[^)]*)\)\s*$");

    public static bool IsTranslocated(string path)
    {
        return !string.IsNullOrEmpty(path) && path.Contains("/AppTranslocation/");
    }

    public static string OriginalPathFromMount(string mountOutput, string translocatedPath)
    {
        if(string.IsNullOrEmpty(mountOutput) || string.IsNullOrEmpty(translocatedPath)) { return null; }

        string best = null;
        var bestLength = -1;
        foreach(var line in mountOutput.Split('\n'))
        {
            var m = NullfsMount.Match(line.TrimEnd('\r'));
            if(!m.Success) { continue; }

            var mountPoint = m.Groups["mp"].Value;
            if(translocatedPath != mountPoint && !translocatedPath.StartsWith(mountPoint + "/", StringComparison.Ordinal)) { continue; }
            if(mountPoint.Length <= bestLength) { continue; }

            bestLength = mountPoint.Length;

            //macOS lists the original .app as the source (the app then runs from <mount point>/d/<name>.app);
            //if the source is a folder instead, the rest of the path is relative to it
            var source = m.Groups["src"].Value;
            best = source.EndsWith(".app", StringComparison.OrdinalIgnoreCase) ? source : source + translocatedPath.Substring(mountPoint.Length);
        }
        return best;
    }

    //<folder containing the .app>/mods (the app and its mods folder always live in the same folder); null without a usable path
    public static string ModsFolderNextToApp(string appBundlePath)
    {
        if(string.IsNullOrEmpty(appBundlePath)) { return null; }

        var folder = Path.GetDirectoryName(appBundlePath.TrimEnd('/'));
        if(string.IsNullOrEmpty(folder)) { return null; }

        return folder.TrimEnd('/') + "/mods";
    }
}
