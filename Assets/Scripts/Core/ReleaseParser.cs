using System;
using System.Collections.Generic;

public sealed class ReleaseInfo
{
    public string Version;
    public string Url;
    public string Notes = string.Empty;
    public string Error;
}

//Reads the "latest release" document from the GitHub releases API
public static class ReleaseParser
{
    public const int MaxNotes = 400;

    public static ReleaseInfo Parse(string json)
    {
        try { return ParseInternal(json); }
        catch(FormatException) { return new ReleaseInfo { Error = "The release information could not be read." }; }
    }

    private static ReleaseInfo ParseInternal(string json)
    {
        var root = MiniJson.Parse(json) as Dictionary<string, object>;
        if(root == null) { return Fail("The release information could not be read."); }

        if(Flag(root, "draft") || Flag(root, "prerelease")) { return Fail("The newest release is a draft or pre-release."); }

        var tag = Text(root, "tag_name");
        if(tag == null) { return Fail("No release was found."); }

        var version = AppVersion.Normalize(tag);
        if(version == null) { return Fail("The release tag '" + tag + "' is not a version number."); }

        var url = Text(root, "html_url");
        if(!IsTrustedUrl(url)) { return Fail("The release page address is not trusted."); }

        return new ReleaseInfo { Version = version, Url = url, Notes = Trim(Text(root, "body")) };
    }

    //Release pages must be https on github.com
    public static bool IsTrustedUrl(string url)
    {
        if(string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) { return false; }
        var host = uri.Host.ToLowerInvariant();
        return uri.Scheme == Uri.UriSchemeHttps && (host == "github.com" || host.EndsWith(".github.com"));
    }

    private static ReleaseInfo Fail(string message) { return new ReleaseInfo { Error = message }; }

    private static bool Flag(Dictionary<string, object> d, string key) { return d.TryGetValue(key, out var v) && v is bool b && b; }

    private static string Text(Dictionary<string, object> d, string key) { return d.TryGetValue(key, out var v) ? v as string : null; }

    private static string Trim(string notes)
    {
        if(string.IsNullOrWhiteSpace(notes)) { return string.Empty; }
        var clean = notes.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        return clean.Length > MaxNotes ? clean.Substring(0, MaxNotes) + "..." : clean;
    }
}
