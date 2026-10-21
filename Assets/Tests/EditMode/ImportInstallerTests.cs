using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public class ImportInstallerTests
{
    private string work;
    private string mods;
    private string temp;

    [SetUp]
    public void SetUp()
    {
        work = ArchiveTestHelper.NewTempDir("imp");
        mods = Path.Combine(work, "mods");
        temp = Path.Combine(work, "tmp");
    }

    [TearDown]
    public void TearDown() { ArchiveTestHelper.Delete(work); }

    private static Dictionary<string, string> Pack(params string[] names)
    {
        var d = new Dictionary<string, string>();
        foreach(var n in names) { d[n] = "data:" + n; }
        return d;
    }

    private string Folder(string name, params string[] files)
    {
        var dir = Path.Combine(work, "src", name);
        foreach(var f in files)
        {
            var p = Path.Combine(dir, f);
            Directory.CreateDirectory(Path.GetDirectoryName(p));
            File.WriteAllText(p, "x");
        }
        return dir;
    }

    private InstallResult Run(IReadOnlyList<string> sources, System.Func<ImportPreview, IReadOnlyList<string>> onSelect = null,
        System.Func<IReadOnlyList<string>, ConflictChoice> onConflict = null, CancellationToken ct = default)
    {
        return ImportInstaller.Install(sources, mods, temp, onSelect ?? (p => p.Names), onConflict ?? (_ => ConflictChoice.Skip), null, ct);
    }

    [Test]
    public void Single_zip_with_one_char_installs()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var r = Run(new[] { zip });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, r.Installed);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "a.mp4")));
    }

    [Test]
    public void Zip_and_folder_mixed()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var folder = Folder("CHAR_B", "b.mp4");
        var r = Run(new[] { zip, folder });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEquivalent(new[] { "CHAR_A", "CHAR_B" }, r.Installed);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_B", "b.mp4")));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "b.mp4")), "source folder must stay");
    }

    [Test]
    public void Nested_char_dir_found_in_wrapper_folder()
    {
        var folder = Folder("Wrapper", "Inner/CHAR_C/c.mp4");
        var r = Run(new[] { folder });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_C" }, r.Installed);
    }

    [Test]
    public void Source_without_char_reported_as_not_mod()
    {
        var good = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var bad = ArchiveTestHelper.MakeZip(work, "b.zip", Pack("Readme.txt"));
        ImportPreview seen = null;
        var r = Run(new[] { good, bad }, p => { seen = p; return p.Names; });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "b.zip" }, seen.NotMods);
    }

    [Test]
    public void All_sources_without_char_fails_and_changes_nothing()
    {
        var bad = ArchiveTestHelper.MakeZip(work, "b.zip", Pack("Readme.txt"));
        var r = Run(new[] { bad });
        Assert.IsFalse(r.Ok);
        StringAssert.Contains("b.zip", r.Error);
        Assert.IsFalse(Directory.Exists(mods) && Directory.GetDirectories(mods).Length > 0);
    }

    [Test]
    public void Duplicate_name_across_sources_first_wins_second_reported()
    {
        var z1 = ArchiveTestHelper.MakeZip(work, "1.zip", Pack("CHAR_A/one.mp4"));
        var z2 = ArchiveTestHelper.MakeZip(work, "2.zip", Pack("CHAR_A/two.mp4"));
        ImportPreview seen = null;
        var r = Run(new[] { z1, z2 }, p => { seen = p; return p.Names; });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, seen.Duplicates);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "one.mp4")));
        Assert.IsFalse(File.Exists(Path.Combine(mods, "CHAR_A", "two.mp4")));
    }

    [Test]
    public void Unreadable_archive_listed_and_others_continue()
    {
        var good = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var broken = Path.Combine(work, "broken.zip");
        File.WriteAllText(broken, "not a zip");
        ImportPreview seen = null;
        var r = Run(new[] { broken, good }, p => { seen = p; return p.Names; });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "broken.zip" }, seen.Unreadable);
    }

    [Test]
    public void Existing_mod_triggers_conflict_skip_and_overwrite()
    {
        Directory.CreateDirectory(Path.Combine(mods, "CHAR_A"));
        File.WriteAllText(Path.Combine(mods, "CHAR_A", "old.mp4"), "old");
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/new.mp4"));

        var skip = Run(new[] { zip }, null, _ => ConflictChoice.Skip);
        Assert.IsTrue(skip.Ok);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, skip.Skipped);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "old.mp4")));

        var over = Run(new[] { zip }, null, _ => ConflictChoice.Overwrite);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, over.Installed);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "new.mp4")));
        Assert.IsFalse(File.Exists(Path.Combine(mods, "CHAR_A", "old.mp4")));
    }

    [Test]
    public void Cancel_in_select_installs_nothing_and_cleans_temp()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var r = Run(new[] { zip }, p => null);
        Assert.IsFalse(r.Ok);
        Assert.AreEqual("Cancelled.", r.Error);
        Assert.IsFalse(Directory.Exists(Path.Combine(mods, "CHAR_A")));
        Assert.AreEqual(0, Directory.GetDirectories(temp).Length);
    }

    [Test]
    public void Cancelled_token_cleans_temp()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = Run(new[] { zip }, null, null, cts.Token);
        Assert.IsFalse(r.Ok);
        Assert.AreEqual("Cancelled.", r.Error);
        Assert.AreEqual(0, Directory.GetDirectories(temp).Length);
    }

    [Test]
    public void Source_archive_untouched()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "a.zip", Pack("CHAR_A/a.mp4"));
        Run(new[] { zip });
        Assert.IsTrue(File.Exists(zip));
    }
}
