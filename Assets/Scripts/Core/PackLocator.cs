using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class PackLocator
{
    private const string Prefix = "CHAR_";

    //Every directory named CHAR_* at any depth; a found directory is not searched further
    public static IReadOnlyList<string> FindCharDirs(string root)
    {
        var found = new List<string>();
        if(!string.IsNullOrEmpty(root) && Directory.Exists(root)) { Walk(root, found); }
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static void Walk(string dir, List<string> found)
    {
        foreach(var sub in Directory.GetDirectories(dir))
        {
            if(Path.GetFileName(sub).StartsWith(Prefix, StringComparison.Ordinal)) { found.Add(sub); }
            else { Walk(sub, found); }
        }
    }
}
