using System;
using System.Linq;

//Text and links for pointing a player at an Android storage folder from the Files app
public static class AndroidFolderLink
{
    private static readonly string[] Roots = { "/storage/emulated/0/", "/sdcard/", "/storage/self/primary/" };

    //"/storage/emulated/0/Android/media/x/mods" -> "Internal storage > Android > media > x > mods"
    public static string Friendly(string path)
    {
        var relative = RelativeToStorage(path);
        if(relative == null) { return path; }
        var parts = relative.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" > ", new[] { "Internal storage" }.Concat(parts));
    }

    //Link that opens a folder of the primary storage in the system document browser; null for any other location
    public static string DocumentUri(string path)
    {
        var relative = RelativeToStorage(path);
        if(relative == null) { return null; }
        return "content://com.android.externalstorage.documents/document/primary%3A" + Uri.EscapeDataString(relative.Trim('/'));
    }

    private static string RelativeToStorage(string path)
    {
        if(string.IsNullOrEmpty(path)) { return null; }
        foreach(var root in Roots)
        {
            if(path.StartsWith(root, StringComparison.Ordinal)) { return path.Substring(root.Length); }
        }
        return null;
    }
}
