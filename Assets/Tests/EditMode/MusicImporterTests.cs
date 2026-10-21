using System.IO;
using NUnit.Framework;

public class MusicImporterTests
{
    private string work;

    [SetUp]
    public void SetUp() { work = ArchiveTestHelper.NewTempDir("mus"); }

    [TearDown]
    public void TearDown() { ArchiveTestHelper.Delete(work); }

    private string Src(string name, string text = "x")
    {
        var p = Path.Combine(work, "src", name);
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllText(p, text);
        return p;
    }

    [Test]
    public void Copies_flat_and_keeps_source()
    {
        var f = Src("a.ogg");
        var music = Path.Combine(work, "music");
        var r = MusicImporter.Import(new[] { f }, music, _ => ConflictChoice.Skip, null, default);
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "a.ogg" }, r.Installed);
        Assert.IsTrue(File.Exists(Path.Combine(music, "a.ogg")));
        Assert.IsTrue(File.Exists(f));
    }

    [Test]
    public void Existing_file_skip_and_overwrite()
    {
        var music = Path.Combine(work, "music");
        Directory.CreateDirectory(music);
        File.WriteAllText(Path.Combine(music, "a.ogg"), "old");
        var f = Src("a.ogg", "new");

        var skip = MusicImporter.Import(new[] { f }, music, _ => ConflictChoice.Skip, null, default);
        CollectionAssert.AreEqual(new[] { "a.ogg" }, skip.Skipped);
        Assert.AreEqual("old", File.ReadAllText(Path.Combine(music, "a.ogg")));

        var over = MusicImporter.Import(new[] { f }, music, _ => ConflictChoice.Overwrite, null, default);
        CollectionAssert.AreEqual(new[] { "a.ogg" }, over.Installed);
        Assert.AreEqual("new", File.ReadAllText(Path.Combine(music, "a.ogg")));
    }

    [Test]
    public void Readable_files_not_reported_unreadable_and_missing_ignored()
    {
        var f = Src("a.ogg");
        Assert.AreEqual(0, MediaFiles.Unreadable(new[] { f, Path.Combine(work, "nope.ogg") }).Count);
    }

    [Test]
    public void Unreadable_reports_denied_file()
    {
        if(System.Environment.OSVersion.Platform == System.PlatformID.Win32NT) { Assert.Ignore("no unix modes"); }
        var f = Src("locked.mp4");
        Chmod(f, "000");
        try { CollectionAssert.AreEqual(new[] { f }, MediaFiles.Unreadable(new[] { f })); }
        finally { Chmod(f, "644"); }
    }

    private static void Chmod(string path, string mode)
    {
        using(var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("chmod", mode + " \"" + path + "\"") { UseShellExecute = false }))
        {
            p.WaitForExit();
        }
    }
}
