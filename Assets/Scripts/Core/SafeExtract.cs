using System;
using System.IO;

//Guards archive extraction against entries that would be written outside the target folder
public static class SafeExtract
{
    public static bool TryResolve(string root, string entryName, out string fullPath)
    {
        fullPath = null;
        if(string.IsNullOrEmpty(root) || string.IsNullOrEmpty(entryName)) { return false; }

        var name = entryName.Replace('\\', '/');
        if(name.StartsWith("/") || (name.Length >= 2 && name[1] == ':')) { return false; }
        foreach(var segment in name.Split('/'))
        {
            if(segment == "..") { return false; }
        }

        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(rootFull, name));
        var isInside = candidate.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal) || candidate == rootFull;
        if(!isInside) { return false; }

        fullPath = candidate;
        return true;
    }
}
