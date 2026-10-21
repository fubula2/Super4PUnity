using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Finds a mod's videos and icon by name. Android and Linux treat ".MP4" and ".mp4" as different files, and mod makers use both,
//so extensions and icon names are compared without regard to case.
public static class MediaFiles
{
    public static bool IsVideo(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase) || ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase);
    }

    //A mod's videos. Mods unzipped by hand often end up one folder deeper (CHAR_X/CHAR_X/clip.mp4), so when the mod's own folder
    //holds none, its subfolders are searched too.
    public static List<string> VideosIn(string modDir)
    {
        try
        {
            if(!Directory.Exists(modDir)) { return new List<string>(); }
            var top = Directory.GetFiles(modDir).Where(IsVideo).OrderBy(f => f, StringComparer.Ordinal).ToList();
            if(top.Count > 0) { return top; }
            return Directory.GetFiles(modDir, "*", SearchOption.AllDirectories).Where(IsVideo).OrderBy(f => f, StringComparer.Ordinal).ToList();
        }
        catch(Exception e) when(e is UnauthorizedAccessException || e is IOException) { return new List<string>(); }   //a folder the system will not list
    }

    //The icon, looked for in the mod's folder and then in its subfolders
    public static string IconIn(string modDir, string shortName)
    {
        try
        {
            if(!Directory.Exists(modDir)) { return null; }
            return FindIcon(Directory.GetFiles(modDir), shortName) ?? FindIcon(Directory.GetFiles(modDir, "*", SearchOption.AllDirectories), shortName);
        }
        catch(Exception e) when(e is UnauthorizedAccessException || e is IOException) { return null; }
    }

    //A few names from the folder, for a message that says what is actually in there
    public static string Peek(string modDir, int max = 6)
    {
        if(!Directory.Exists(modDir)) { return "(the folder is missing)"; }
        List<string> names;
        try { names = Directory.GetFileSystemEntries(modDir).Select(Path.GetFileName).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(); }
        catch(Exception e) when(e is UnauthorizedAccessException || e is IOException) { return "(the system does not allow the app to list this folder)"; }
        if(names.Count == 0) { return "(the folder is empty)"; }
        return string.Join(", ", names.Take(max)) + (names.Count > max ? ", ... (" + names.Count + " items)" : string.Empty);
    }

    //Files the system refuses to open for reading (opened and closed right away); missing files are not counted
    public static List<string> Unreadable(IEnumerable<string> files)
    {
        var denied = new List<string>();
        foreach(var f in files)
        {
            try { using(File.OpenRead(f)) { } }
            catch(UnauthorizedAccessException) { denied.Add(f); }
            catch(IOException) { }
        }
        return denied;
    }

    public static bool IsMusic(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase);
    }

    //The icon is "<short name>.png"; when a mod has no such file but exactly one picture, that picture is used
    public static string FindIcon(IEnumerable<string> files, string shortName)
    {
        var pictures = files.Where(f => Path.GetExtension(f).Equals(".png", StringComparison.OrdinalIgnoreCase)).ToList();
        var exact = pictures.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(shortName, StringComparison.OrdinalIgnoreCase));
        if(exact != null) { return exact; }
        return pictures.Count == 1 ? pictures[0] : null;
    }
}
