using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public sealed class DriveLinks : IMusicSource
{
    private static readonly Regex FolderRx = new Regex("^/drive/(?:u/\\d+/)?folders/(?<id>[A-Za-z0-9_-]+)", RegexOptions.Compiled);

    public string Name => "Google Drive";

    public bool Handles(Uri url) { return url != null && url.Host.Equals("drive.google.com", StringComparison.OrdinalIgnoreCase); }

    public string RootFolderId(string folderUrl)
    {
        if(!Uri.TryCreate(folderUrl, UriKind.Absolute, out var uri) || !Handles(uri)) { return null; }
        var m = FolderRx.Match(uri.AbsolutePath);
        return m.Success ? m.Groups["id"].Value : null;
    }

    public string ListingUrl(string folderId) { return "https://drive.google.com/embeddedfolderview?id=" + folderId; }

    public string FileUrl(string fileId) { return "https://drive.google.com/uc?export=download&id=" + fileId; }

    public IReadOnlyList<MusicEntry> ParseListing(string body) { return DriveFolderParser.Parse(body); }
}
