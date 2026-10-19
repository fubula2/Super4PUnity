using System.Linq;
using NUnit.Framework;

public class LibrarySheetParserTests
{
    private const string MF = "https://www.mediafire.com/file/abc/pack.zip/file";

    private static XlsxFixture Headers(XlsxFixture f)
    {
        return f.Text("A1", "#").Text("B1", "Series Name").Text("E1", "Last Updated").Text("F1", "Status").Text("G1", "Download").Text("H1", "Character(s)");
    }

    [Test]
    public void Parses_entries_categories_and_links()
    {
        var f = Headers(new XlsxFixture())
            .Text("B4", "●18+ Warning!")
            .Text("B13", "Nintendo Games")
            .Number("A14", 1).Text("B14", "Animal\nCrossing").Number("E14", 44061).Text("F14", "❤").Link("G14", MF).Text("H14", "Ankha")
            .Number("A15", 2).Text("B15", "Chibi Robo").Text("F15", "●").Text("G15", "-").Text("H15", "-")
            .Text("B20", "Sega Games")
            .Number("A21", 3).Text("B21", "Zelda").Text("F21", "P").Link("G21", "https://www.mediafire.com/folder/key/Zelda").Text("H21", "Zelda\nImpa");

        var catalog = LibrarySheetParser.Parse(f.Build());

        Assert.IsNull(catalog.Error);
        Assert.AreEqual(2, catalog.Entries.Count);
        var a = catalog.Entries[0];
        Assert.AreEqual("Animal Crossing", a.Series);
        Assert.AreEqual("Ankha", a.Characters);
        Assert.AreEqual("Nintendo Games", a.Category);
        Assert.AreEqual(new System.DateTime(2020, 8, 18), a.Updated);
        Assert.AreEqual("Completed", a.Status);
        Assert.AreEqual(MF, a.Url);
        Assert.AreEqual(LinkKind.File, a.Kind);
        var z = catalog.Entries[1];
        Assert.AreEqual("Sega Games", z.Category);
        Assert.AreEqual("Zelda, Impa", z.Characters);
        Assert.AreEqual(LinkKind.Folder, z.Kind);
        Assert.AreEqual("Patreon exclusive", z.Status);
        Assert.IsNull(z.Updated);
    }

    [Test]
    public void Maps_columns_by_label_when_reordered()
    {
        var f = new XlsxFixture()
            .Text("A1", "Download").Text("B1", "Character(s)").Text("C1", "Series Name")
            .Link("A2", MF).Text("B2", "Ankha").Text("C2", "Animal Crossing");

        var catalog = LibrarySheetParser.Parse(f.Build());

        Assert.IsNull(catalog.Error);
        Assert.AreEqual(1, catalog.Entries.Count);
        Assert.AreEqual("Animal Crossing", catalog.Entries[0].Series);
        Assert.AreEqual("Ankha", catalog.Entries[0].Characters);
    }

    [Test]
    public void Missing_download_header_returns_error_not_exception()
    {
        var f = new XlsxFixture().Text("A1", "Series Name").Text("B1", "Character(s)").Text("A2", "X").Text("B2", "Y");
        var catalog = LibrarySheetParser.Parse(f.Build());
        Assert.IsNotNull(catalog.Error);
        Assert.AreEqual(0, catalog.Entries.Count);
    }

    [Test]
    public void Wrong_tab_name_returns_error()
    {
        var catalog = LibrarySheetParser.Parse(Headers(new XlsxFixture("Other")).Build());
        Assert.IsNotNull(catalog.Error);
        Assert.AreEqual(0, catalog.Entries.Count);
    }

    [Test]
    public void Display_text_without_hyperlink_is_dropped()
    {
        var f = Headers(new XlsxFixture()).Number("A2", 1).Text("B2", "Series").Text("G2", "Download").Text("H2", "Char");
        Assert.AreEqual(0, LibrarySheetParser.Parse(f.Build()).Entries.Count);
    }

    [Test]
    public void Patreon_row_is_listed_as_Browser_and_unsupported_links_dropped()
    {
        var f = Headers(new XlsxFixture())
            .Number("A2", 1).Text("B2", "One").Link("G2", "https://www.patreon.com/posts/x-1").Text("H2", "A")
            .Number("A3", 2).Text("B3", "Two").Link("G3", "https://drive.google.com/drive/folders/x").Text("H3", "B");
        var catalog = LibrarySheetParser.Parse(f.Build());
        Assert.AreEqual(1, catalog.Entries.Count);
        Assert.AreEqual(LinkKind.Browser, catalog.Entries[0].Kind);
    }

    [Test]
    public void Garbage_stream_returns_error_not_exception()
    {
        var catalog = LibrarySheetParser.Parse(new System.IO.MemoryStream(new byte[] { 1, 2, 3, 4 }));
        Assert.IsNotNull(catalog.Error);
    }

    [Test]
    public void Reads_music_folder_link_from_the_Music_Mods_cell()
    {
        var f = Headers(new XlsxFixture())
            .Link("E7", "https://drive.google.com/drive/folders/abc?usp=sharing", "\u266cMusic Mods\u266c")
            .Number("A14", 1).Text("B14", "Zelda").Link("G14", MF);

        var catalog = LibrarySheetParser.Parse(f.Build());

        Assert.IsNull(catalog.Error);
        Assert.AreEqual("https://drive.google.com/drive/folders/abc?usp=sharing", catalog.MusicFolderUrl);
        Assert.AreEqual(1, catalog.Entries.Count);
    }

    [Test]
    public void Music_link_is_null_when_the_sheet_has_none()
    {
        var f = Headers(new XlsxFixture()).Number("A14", 1).Text("B14", "Zelda").Link("G14", MF).Text("E7", "Music Mods");
        Assert.IsNull(LibrarySheetParser.Parse(f.Build()).MusicFolderUrl);
    }
}
