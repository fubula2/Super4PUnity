using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class FolderFile
{
    public string Name;
    public long SizeBytes;
    public string QuickKey;
}

public sealed class FolderListing
{
    public IReadOnlyList<FolderFile> Files = new List<FolderFile>();
    public bool MoreChunks;
    public string Error;
}

public static class MediaFireFolderJson
{
    public static FolderListing Parse(string json)
    {
        try { return ParseInternal(json); }
        catch(FormatException) { return new FolderListing { Error = "MediaFire returned an unreadable folder listing." }; }
    }

    private static FolderListing ParseInternal(string json)
    {
        var root = MiniJson.Parse(json) as Dictionary<string, object>;
        var response = root != null && root.TryGetValue("response", out var r) ? r as Dictionary<string, object> : null;
        if(response == null) { return new FolderListing { Error = "MediaFire returned an unexpected response." }; }

        var result = Str(response, "result");
        if(result != "Success")
        {
            var message = Str(response, "message");
            return new FolderListing { Error = "MediaFire folder error: " + (string.IsNullOrEmpty(message) ? "unknown error" : message) };
        }

        var content = response.TryGetValue("folder_content", out var c) ? c as Dictionary<string, object> : null;
        if(content == null) { return new FolderListing { Error = "MediaFire folder listing was empty." }; }

        var files = new List<FolderFile>();
        if(content.TryGetValue("files", out var f) && f is List<object> list)
        {
            foreach(var item in list)
            {
                var file = item as Dictionary<string, object>;
                if(file == null) { continue; }

                long.TryParse(Str(file, "size"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var size);
                var quickKey = Str(file, "quickkey");
                var name = Str(file, "filename");
                if(string.IsNullOrEmpty(quickKey) || string.IsNullOrEmpty(name)) { continue; }
                files.Add(new FolderFile { Name = name, SizeBytes = size, QuickKey = quickKey });
            }
        }
        return new FolderListing { Files = files, MoreChunks = Str(content, "more_chunks") == "yes" };
    }

    private static string Str(Dictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null;
    }
}
