using System;

public static class LibraryLinkClassifier
{
    //File/Folder only for https MediaFire links; Patreon pages are opened in the browser; everything else is dropped
    public static bool TryClassify(string url, out LinkKind kind)
    {
        kind = LinkKind.Browser;
        if(string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) { return false; }

        var host = uri.Host.ToLowerInvariant();
        var isHttps = uri.Scheme == Uri.UriSchemeHttps;
        var isMediaFire = host == "mediafire.com" || host.EndsWith(".mediafire.com");

        if(isMediaFire && isHttps)
        {
            var path = uri.AbsolutePath;
            if(path.StartsWith("/file/") || path.StartsWith("/file_premium/")) { kind = LinkKind.File; return true; }
            if(path.StartsWith("/folder/")) { kind = LinkKind.Folder; return true; }
            return false;
        }

        if((host == "patreon.com" || host == "www.patreon.com") && (isHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            kind = LinkKind.Browser;
            return true;
        }
        return false;
    }
}
