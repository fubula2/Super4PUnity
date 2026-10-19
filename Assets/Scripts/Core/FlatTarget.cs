using System;
using System.IO;

public static class FlatTarget
{
    //Where a downloaded track goes: directly inside musicDir, by its leaf name, .ogg only
    public static bool TryResolve(string musicDir, string fileName, out string fullPath)
    {
        fullPath = null;
        if(string.IsNullOrEmpty(fileName)) { return false; }
        var normalized = fileName.Replace('\\', '/');
        var leaf = normalized.Substring(normalized.LastIndexOf('/') + 1).Trim();
        if(leaf.Length <= ".ogg".Length || !leaf.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) { return false; }
        return SafeExtract.TryResolve(musicDir, leaf, out fullPath);
    }
}
