using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public enum ImportSourceKind { Archive, Folder, Unsupported }

//Sorts what the player picked from the device into things we can import
public static class ImportSources
{
    public static ImportSourceKind Classify(string path)
    {
        if(string.IsNullOrEmpty(path)) { return ImportSourceKind.Unsupported; }
        if(Directory.Exists(path)) { return ImportSourceKind.Folder; }
        var ext = Path.GetExtension(path);
        if(string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase)) { return ImportSourceKind.Archive; }
        return ImportSourceKind.Unsupported;
    }

    //Archives and folders only, without duplicates; unsupported items are counted
    public static IReadOnlyList<string> Supported(IEnumerable<string> paths, out int unsupportedCount)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        unsupportedCount = 0;
        if(paths == null) { return result; }
        foreach(var p in paths)
        {
            if(string.IsNullOrWhiteSpace(p) || !seen.Add(p)) { continue; }
            if(Classify(p) == ImportSourceKind.Unsupported) { unsupportedCount++; }
            else { result.Add(p); }
        }
        return result;
    }

    //Music import: .ogg and .mp3 files only, without duplicates; others are counted
    public static IReadOnlyList<string> SupportedAudio(IEnumerable<string> paths, out int unsupportedCount)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        unsupportedCount = 0;
        if(paths == null) { return result; }
        foreach(var p in paths)
        {
            if(string.IsNullOrWhiteSpace(p) || !seen.Add(p)) { continue; }
            if(MediaFiles.IsMusic(p)) { result.Add(p); } else { unsupportedCount++; }
        }
        return result;
    }

    public static string ConfirmText(IReadOnlyList<string> paths, int unsupportedCount, int maxNames = 5, string ignoredReason = "not .zip, .rar or a folder")
    {
        var sb = new StringBuilder();
        sb.Append("Import ").Append(paths.Count).Append(paths.Count == 1 ? " item?" : " items?");
        for(var i = 0; i < paths.Count && i < maxNames; i++) { sb.Append("\n").Append(Path.GetFileName(paths[i].TrimEnd('/', '\\'))); }
        if(paths.Count > maxNames) { sb.Append("\nand ").Append(paths.Count - maxNames).Append(" more"); }
        if(unsupportedCount > 0) { sb.Append("\n\n(").Append(unsupportedCount).Append(" ignored: " + ignoredReason + ")"); }
        return sb.ToString();
    }
}
