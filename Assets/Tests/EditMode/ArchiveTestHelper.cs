using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class ArchiveTestHelper
{
    public static string NewTempDir(string tag)
    {
        var dir = Path.Combine(Path.GetTempPath(), "s4p_" + tag + "_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    //Creates a zip from name -> content; names ending in "/" are directory entries
    public static string MakeZip(string dir, string fileName, IDictionary<string, string> entries)
    {
        var path = Path.Combine(dir, fileName);
        using(var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach(var e in entries)
            {
                var entry = zip.CreateEntry(e.Key);
                if(e.Key.EndsWith("/")) { continue; }
                using(var w = new StreamWriter(entry.Open(), new UTF8Encoding(false))) { w.Write(e.Value); }
            }
        }
        return path;
    }

    public static string Fixture(string name)
    {
        var guids = UnityEditor.AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(name) + " t:DefaultAsset");
        foreach(var g in guids)
        {
            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            if(p.EndsWith("/" + name)) { return Path.GetFullPath(p); }
        }
        return Path.GetFullPath("Assets/Tests/EditMode/Fixtures/" + name);
    }

    public static void Delete(string dir)
    {
        if(Directory.Exists(dir)) { Directory.Delete(dir, true); }
    }
}
