using System;

public readonly struct ModsPaths
{
    public readonly string Mods;
    public readonly string Music;
    public readonly bool UsedFallback;

    public ModsPaths(string mods, bool usedFallback)
    {
        Mods = mods;
        Music = mods + "/music";
        UsedFallback = usedFallback;
    }
}

public static class ModsPathResolver
{
    // A configured path wins when it exists; otherwise the default is used,
    // and UsedFallback is set only when a configured path was given but missing.
    public static ModsPaths Resolve(string configured, string defaultMods, Func<string, bool> dirExists)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return new ModsPaths(Normalize(defaultMods), false);
        }

        var normalized = Normalize(configured);
        return dirExists(normalized)
            ? new ModsPaths(normalized, false)
            : new ModsPaths(Normalize(defaultMods), true);
    }

    public static string Normalize(string path)
    {
        var p = path.Replace('\\', '/');
        while (p.Length > 1 && p.EndsWith("/")) { p = p.Substring(0, p.Length - 1); }
        return p;
    }
}
