using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

//Reads the public "embedded folder view" page of a shared Google Drive folder into entries.
public static class DriveFolderParser
{
    private static readonly Regex EntryRx = new Regex(
        "<div class=\"flip-entry\" id=\"entry-(?<id>[^\"]+)\".*?href=\"(?<href>[^\"]*)\".*?class=\"flip-entry-title\">(?<title>[^<]*)<",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<MusicEntry> Parse(string html)
    {
        var result = new List<MusicEntry>();
        if(string.IsNullOrEmpty(html)) { return result; }
        foreach(Match m in EntryRx.Matches(html))
        {
            var name = WebUtility.HtmlDecode(m.Groups["title"].Value).Trim();
            if(name.Length == 0) { continue; }
            var isFolder = m.Groups["href"].Value.Contains("/folders/");
            result.Add(new MusicEntry { Id = m.Groups["id"].Value, Name = name, Kind = isFolder ? MusicEntryKind.Folder : MusicEntryKind.File });
        }
        return result;
    }
}
