using System;
using System.Collections.Generic;

//Turns the text a native file dialog prints into a list of paths
public static class PickerOutput
{
    //One path per line; blank lines dropped, a trailing slash removed (but "/" itself is kept)
    public static IReadOnlyList<string> ParseLines(string stdout)
    {
        var result = new List<string>();
        if(string.IsNullOrEmpty(stdout)) { return result; }
        foreach(var raw in stdout.Split('\n'))
        {
            var line = raw.Trim('\r', '\n').Trim();
            if(line.Length == 0) { continue; }
            if(line.Length > 1 && (line.EndsWith("/") || line.EndsWith("\\")) && !line.EndsWith(":\\")) { line = line.Substring(0, line.Length - 1); }
            result.Add(line);
        }
        return result;
    }

    //The Android plugin reports one line per item: "D<TAB>cacheDir", "P<TAB>path" (copied ok), "F<TAB>name" (could not copy)
    public static AndroidPickResult ParseAndroidResult(string message)
    {
        var result = new AndroidPickResult();
        if(string.IsNullOrEmpty(message)) { return result; }
        foreach(var raw in message.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var tab = line.IndexOf('\t');
            if(tab != 1 || tab + 1 >= line.Length) { continue; }
            var value = line.Substring(tab + 1);
            switch(line[0])
            {
                case 'D': result.TempDirs.Add(value); break;
                case 'P': result.Paths.Add(value); break;
                case 'F': result.Failed.Add(value); break;
            }
        }
        return result;
    }
}

public sealed class AndroidPickResult
{
    public readonly List<string> Paths = new List<string>();
    public readonly List<string> Failed = new List<string>();
    public readonly List<string> TempDirs = new List<string>();
}
