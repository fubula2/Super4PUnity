using System.Collections.Generic;
using System.IO;
using System.Linq;

//The explanation shown when the system refuses the app access to files in a mod folder
public static class FileAccessMessage
{
    public static string Describe(IReadOnlyList<string> paths, int maxExamples = 3)
    {
        if(paths == null || paths.Count == 0) { return string.Empty; }

        var examples = paths.Distinct().Take(maxExamples)
            .Select(p => (Path.GetFileName(Path.GetDirectoryName(p) ?? string.Empty) ?? string.Empty) + "/" + Path.GetFileName(p)).ToList();
        var more = paths.Distinct().Count() - examples.Count;
        var list = string.Join("\n", examples) + (more > 0 ? "\n... and " + more + " more" : string.Empty);

        return "The system does not allow this app to read " + (paths.Distinct().Count() == 1 ? "a file" : "some files") + " in your mods folder:\n\n" + list +
            "\n\nThis usually happens when files were copied in by another app or tool. Copy the mod folder in again with your phone's Files app " +
            "(or a different file manager), then press Refresh.";
    }
}
