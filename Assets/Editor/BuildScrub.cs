using System;
using System.IO;
using System.Text;

//Compiled game files remember where they were built (for example the debug-symbol path of Assembly-CSharp.dll contains the
//builder's home folder). Releases are public, so the builder's user name is overwritten, same length, before packaging.
public static class BuildScrub
{
    private const long MaxFileBytes = 300L * 1024 * 1024;

    public static int ReplaceHomePath(string directory)
    {
        var user = Environment.UserName;
        if(string.IsNullOrEmpty(user) || !Directory.Exists(directory)) { return 0; }

        var neutral = new string('x', user.Length);
        var total = 0;
        foreach(var prefix in new[] { "/Users/", "/home/", "\\Users\\" })
        {
            var from = prefix + user;
            var to = prefix + neutral;
            foreach(var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                if(new FileInfo(file).Length > MaxFileBytes) { continue; }
                total += Replace(file, Encoding.ASCII.GetBytes(from), Encoding.ASCII.GetBytes(to));
                total += Replace(file, Encoding.Unicode.GetBytes(from), Encoding.Unicode.GetBytes(to));
            }
        }
        UnityEngine.Debug.Log("Build scrub: replaced " + total + " home-folder reference(s) in " + directory);
        return total;
    }

    private static int Replace(string file, byte[] from, byte[] to)
    {
        var data = File.ReadAllBytes(file);
        var count = 0;
        for(var i = 0; i <= data.Length - from.Length; i++)
        {
            var match = true;
            for(var j = 0; j < from.Length && match; j++) { match = data[i + j] == from[j]; }
            if(!match) { continue; }
            Array.Copy(to, 0, data, i, to.Length);
            count++;
            i += from.Length - 1;
        }
        if(count > 0) { File.WriteAllBytes(file, data); }
        return count;
    }
}
