using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

//Names and plans for converted copies of mod clips. Originals are only read; copies live in a separate cache folder.
public static class VideoCache
{
    public const string Extension = ".webm";

    public sealed class Clip
    {
        public string Source;
        public long Size;
        public DateTime ModifiedUtc;
    }

    //Stable for the same file; changes when the file is replaced or edited
    public static string KeyFor(string sourcePath, long size, DateTime modifiedUtc)
    {
        var text = sourcePath + "|" + size + "|" + modifiedUtc.Ticks;
        using(var sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2"))); }
    }

    public static string CachePath(string cacheDir, string key) { return Path.Combine(cacheDir, key + Extension); }

    //The clips that still need converting (cache copy missing or unusable), each once
    public static IReadOnlyList<Clip> Plan(IEnumerable<Clip> clips, string cacheDir, Func<string, bool> cacheIsValid)
    {
        var seen = new HashSet<string>();
        var todo = new List<Clip>();
        foreach(var clip in clips)
        {
            var path = CachePath(cacheDir, KeyFor(clip.Source, clip.Size, clip.ModifiedUtc));
            if(!seen.Add(path) || cacheIsValid(path)) { continue; }
            todo.Add(clip);
        }
        return todo;
    }
}
