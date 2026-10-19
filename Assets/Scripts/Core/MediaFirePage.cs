using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public static class MediaFirePage
{
    private static readonly Regex ButtonTag = new Regex("<a\\b[^>]*\\bid=\"downloadButton\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Href = new Regex("\\bhref=\"([^\"]*)\"", RegexOptions.IgnoreCase);
    private static readonly Regex Scrambled = new Regex("\\bdata-scrambled-url=\"([^\"]*)\"", RegexOptions.IgnoreCase);
    private static readonly Regex FolderKeyPattern = new Regex("/folder/([^/?#]+)", RegexOptions.IgnoreCase);

    //Direct download URL from a MediaFire file page; only https URLs on mediafire.com hosts are accepted
    public static bool TryGetDirectUrl(string html, out string url)
    {
        url = null;
        if(string.IsNullOrEmpty(html)) { return false; }

        var tag = ButtonTag.Match(html);
        if(!tag.Success) { return false; }

        var candidates = new System.Collections.Generic.List<string>();
        var scrambled = Scrambled.Match(tag.Value);
        if(scrambled.Success) { candidates.Add(DecodeBase64(scrambled.Groups[1].Value)); }
        var href = Href.Match(tag.Value);
        if(href.Success) { candidates.Add(WebUtility.HtmlDecode(href.Groups[1].Value)); }

        foreach(var candidate in candidates)
        {
            if(IsMediaFireHttps(candidate)) { url = candidate; return true; }
        }
        return false;
    }

    //MediaFire used to answer a file link with a web page holding the download button; it now often redirects straight to the file.
    //True when the response content type says it is a web page that must be read for the button.
    public static bool IsHtmlPage(string mediaType)
    {
        return !string.IsNullOrEmpty(mediaType) && mediaType.IndexOf("html", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    //The page MediaFire shows while it regenerates a file's download key
    public static bool IsRepairPage(string html)
    {
        return !string.IsNullOrEmpty(html) && html.IndexOf("DownloadRepair", StringComparison.Ordinal) >= 0;
    }

    public static string FolderKey(string folderUrl)
    {
        if(string.IsNullOrEmpty(folderUrl)) { return null; }
        var m = FolderKeyPattern.Match(folderUrl);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string FolderApiUrl(string folderKey, int chunk)
    {
        return "https://www.mediafire.com/api/1.5/folder/get_content.php?folder_key=" + Uri.EscapeDataString(folderKey) +
            "&content_type=files&chunk=" + chunk + "&chunk_size=100&response_format=json";
    }

    public static string FileUrlFromQuickKey(string quickKey)
    {
        return "https://www.mediafire.com/file/" + Uri.EscapeDataString(quickKey);
    }

    public static bool IsMediaFireHttps(string url)
    {
        if(string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) { return false; }
        var host = uri.Host.ToLowerInvariant();
        return uri.Scheme == Uri.UriSchemeHttps && (host == "mediafire.com" || host.EndsWith(".mediafire.com"));
    }

    private static string DecodeBase64(string value)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch(FormatException) { return null; }
    }
}
