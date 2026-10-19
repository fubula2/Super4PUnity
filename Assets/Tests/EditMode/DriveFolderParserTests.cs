using System.IO;
using System.Linq;
using NUnit.Framework;

public class DriveFolderParserTests
{
    private static string Fixture(string name)
    {
        return File.ReadAllText(Path.GetFullPath("Assets/Tests/EditMode/Fixtures/" + name));
    }

    private static string Entry(string id, string href, string title)
    {
        return "<div class=\"flip-entry\" id=\"entry-" + id + "\"><div class=\"flip-entry-info\"><a href=\"" + href +
               "\"><div class=\"flip-entry-title\">" + title + "</div></a></div></div>";
    }

    [Test]
    public void Root_listing_returns_seven_folders()
    {
        var entries = DriveFolderParser.Parse(Fixture("drive_root.html"));
        Assert.AreEqual(7, entries.Count);
        Assert.IsTrue(entries.All(e => e.Kind == MusicEntryKind.Folder));
        Assert.IsTrue(entries.Any(e => e.Name == "Anime/Manga"));
        Assert.IsTrue(entries.Any(e => e.Id == "1A6RQXH1tCzItCFJOGQ9Wlo3T3g3uSmtm" && e.Name == "_NewTracks"));
    }

    [Test]
    public void Sub_listing_returns_ogg_files_with_ids()
    {
        var entries = DriveFolderParser.Parse(Fixture("drive_sub.html"));
        Assert.AreEqual(21, entries.Count);
        Assert.AreEqual(MusicEntryKind.File, entries[0].Kind);
        Assert.AreEqual("BBS_CowboyBebop_AmericanMoney.ogg", entries[0].Name);
        Assert.AreEqual("1hIZFkVOKTeM3fn8wJG0YSNlHSfiHpmPg", entries[0].Id);
    }

    [Test]
    public void Names_are_html_decoded()
    {
        var html = Entry("abc123", "https://drive.google.com/file/d/abc123/view", "Tom &amp; Jerry&#39;s Theme.ogg");
        var entries = DriveFolderParser.Parse(html);
        Assert.AreEqual("Tom & Jerry's Theme.ogg", entries[0].Name);
    }

    [Test]
    public void Garbage_or_empty_returns_empty()
    {
        Assert.AreEqual(0, DriveFolderParser.Parse("<html>nope</html>").Count);
        Assert.AreEqual(0, DriveFolderParser.Parse("").Count);
        Assert.AreEqual(0, DriveFolderParser.Parse(null).Count);
    }
}
