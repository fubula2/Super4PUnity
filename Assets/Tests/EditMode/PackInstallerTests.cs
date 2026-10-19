using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class PackInstallerTests
{
    private string work;
    private string mods;
    private string temp;

    [SetUp]
    public void SetUp()
    {
        work = ArchiveTestHelper.NewTempDir("inst");
        mods = Path.Combine(work, "mods");
        temp = Path.Combine(work, "tmp");
    }

    [TearDown]
    public void TearDown() { ArchiveTestHelper.Delete(work); }

    private InstallResult Install(string archive, System.Func<IReadOnlyList<string>, ConflictChoice> onConflict = null,
        System.Func<IReadOnlyList<string>, IReadOnlyList<string>> onSelect = null)
    {
        return PackInstaller.Install(archive, mods, temp, onSelect ?? (names => names), onConflict ?? (_ => ConflictChoice.Skip), null, default);
    }

    private static Dictionary<string, string> Pack(params string[] names)
    {
        var d = new Dictionary<string, string>();
        foreach(var n in names) { d[n] = "data:" + n; }
        return d;
    }

    [Test]
    public void Installs_CHAR_dirs_from_wrapper_folder()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("Wrap_V1/CHAR_Alpha/Alpha.png", "Wrap_V1/CHAR_Alpha/Alpha01.mp4", "Wrap_V1/Credits.url"));

        var r = Install(zip);

        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_Alpha" }, r.Installed);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_Alpha", "Alpha01.mp4")));
        Assert.IsFalse(File.Exists(Path.Combine(mods, "Credits.url")));
    }

    [Test]
    public void Multiple_CHAR_dirs_all_installed()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.mp4", "Sub/CHAR_B/b.mp4", "CHAR_C/c.mp4"));
        var r = Install(zip);
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEquivalent(new[] { "CHAR_A", "CHAR_B", "CHAR_C" }, r.Installed);
    }

    [Test]
    public void No_CHAR_dir_rejects_and_moves_nothing()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("Wrap/readme.txt", "Wrap/music/song.mp3"));
        var r = Install(zip);
        Assert.IsFalse(r.Ok);
        StringAssert.Contains("CHAR_", r.Error);
        Assert.IsFalse(Directory.Exists(mods) && Directory.GetFileSystemEntries(mods).Length > 0);
    }

    [Test]
    public void Conflict_overwrite_replaces_existing()
    {
        Directory.CreateDirectory(Path.Combine(mods, "CHAR_A"));
        File.WriteAllText(Path.Combine(mods, "CHAR_A", "old.txt"), "old");
        IReadOnlyList<string> asked = null;
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/new.txt"));

        var r = Install(zip, names => { asked = names; return ConflictChoice.Overwrite; });

        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, asked);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "new.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(mods, "CHAR_A", "old.txt")));
    }

    [Test]
    public void Conflict_skip_keeps_existing_and_installs_the_rest()
    {
        Directory.CreateDirectory(Path.Combine(mods, "CHAR_A"));
        File.WriteAllText(Path.Combine(mods, "CHAR_A", "old.txt"), "old");
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/new.txt", "CHAR_B/b.txt"));

        var r = Install(zip, _ => ConflictChoice.Skip);

        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_B" }, r.Installed);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, r.Skipped);
        Assert.IsTrue(File.Exists(Path.Combine(mods, "CHAR_A", "old.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(mods, "CHAR_A", "new.txt")));
    }

    [Test]
    public void No_conflict_never_asks()
    {
        var asked = false;
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt"));
        var r = Install(zip, _ => { asked = true; return ConflictChoice.Skip; });
        Assert.IsTrue(r.Ok, r.Error);
        Assert.IsFalse(asked);
    }

    [Test]
    public void Duplicate_names_inside_one_pack_install_once()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("A/CHAR_X/one.txt", "B/CHAR_X/two.txt"));
        var r = Install(zip);
        Assert.IsTrue(r.Ok, r.Error);
        Assert.AreEqual(1, r.Installed.Count);
        Assert.AreEqual(1, r.Skipped.Count);
    }

    [Test]
    public void Temp_folder_removed_on_success_and_failure()
    {
        var good = ArchiveTestHelper.MakeZip(work, "good.zip", Pack("CHAR_A/a.txt"));
        var none = ArchiveTestHelper.MakeZip(work, "none.zip", Pack("x.txt"));
        var bad = Path.Combine(work, "bad.zip");
        File.WriteAllBytes(bad, new byte[] { 9, 9, 9 });

        Install(good);
        Install(none);
        var r = Install(bad);

        Assert.IsFalse(r.Ok);
        Assert.IsTrue(!Directory.Exists(temp) || Directory.GetFileSystemEntries(temp).Length == 0);
    }

    [Test]
    public void Rar_without_CHAR_dirs_rejects_and_cleans_up()
    {
        var r = Install(ArchiveTestHelper.Fixture("Rar5.rar"));
        Assert.IsFalse(r.Ok);
        StringAssert.Contains("CHAR_", r.Error);
        Assert.IsTrue(!Directory.Exists(temp) || Directory.GetFileSystemEntries(temp).Length == 0);
    }

    [Test]
    public void Cancelled_install_returns_error_and_cleans_up()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt"));
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();
        var r = PackInstaller.Install(zip, mods, temp, names => names, _ => ConflictChoice.Skip, null, cts.Token);
        Assert.IsFalse(r.Ok);
        Assert.IsTrue(!Directory.Exists(temp) || Directory.GetFileSystemEntries(temp).Length == 0);
        Assert.IsFalse(Directory.Exists(Path.Combine(mods, "CHAR_A")));
    }

    [Test]
    public void Select_receives_all_CHAR_names()
    {
        IReadOnlyList<string> offered = null;
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("W/CHAR_A/a.txt", "W/CHAR_B/b.txt"));

        Install(zip, onSelect: names => { offered = names; return names; });

        CollectionAssert.AreEquivalent(new[] { "CHAR_A", "CHAR_B" }, offered);
    }

    [Test]
    public void Select_subset_installs_only_chosen()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt", "CHAR_B/b.txt", "CHAR_C/c.txt"));

        var r = Install(zip, onSelect: names => new List<string> { "CHAR_B" });

        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_B" }, r.Installed);
        Assert.IsTrue(Directory.Exists(Path.Combine(mods, "CHAR_B")));
        Assert.IsFalse(Directory.Exists(Path.Combine(mods, "CHAR_A")));
        Assert.IsFalse(Directory.Exists(Path.Combine(mods, "CHAR_C")));
    }

    [Test]
    public void Select_none_or_null_cancels_and_cleans_up()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt"));

        var none = Install(zip, onSelect: names => new List<string>());
        var nul = Install(zip, onSelect: names => null);

        Assert.IsFalse(none.Ok);
        Assert.AreEqual("Cancelled.", none.Error);
        Assert.IsFalse(nul.Ok);
        Assert.AreEqual("Cancelled.", nul.Error);
        Assert.IsFalse(Directory.Exists(Path.Combine(mods, "CHAR_A")));
        Assert.IsTrue(!Directory.Exists(temp) || Directory.GetFileSystemEntries(temp).Length == 0);
    }

    [Test]
    public void Names_not_in_the_pack_are_ignored()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt"));
        var r = Install(zip, onSelect: names => new List<string> { "CHAR_A", "CHAR_DOES_NOT_EXIST" });
        Assert.IsTrue(r.Ok, r.Error);
        CollectionAssert.AreEqual(new[] { "CHAR_A" }, r.Installed);
    }

    [Test]
    public void Conflict_is_asked_only_for_chosen_characters()
    {
        Directory.CreateDirectory(Path.Combine(mods, "CHAR_A"));
        Directory.CreateDirectory(Path.Combine(mods, "CHAR_B"));
        IReadOnlyList<string> asked = null;
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", Pack("CHAR_A/a.txt", "CHAR_B/b.txt"));

        Install(zip, names => { asked = names; return ConflictChoice.Skip; }, names => new List<string> { "CHAR_B" });

        CollectionAssert.AreEqual(new[] { "CHAR_B" }, asked);
    }
}
