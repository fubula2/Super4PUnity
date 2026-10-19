using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public class ArchiveExtractorTests
{
    private string work;

    [SetUp]
    public void SetUp() { work = ArchiveTestHelper.NewTempDir("ext"); }

    [TearDown]
    public void TearDown() { ArchiveTestHelper.Delete(work); }

    private static ExtractResult Extract(string archive, string target, CancellationToken ct = default)
    {
        return ArchiveExtractor.Extract(archive, target, null, ct);
    }

    [Test]
    public void Zip_extracts_files_and_directories()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", new Dictionary<string, string> { { "Wrapper/", null }, { "Wrapper/CHAR_A/a.txt", "hello" }, { "Wrapper/b.txt", "x" } });
        var target = Path.Combine(work, "out");

        var r = Extract(zip, target);

        Assert.IsTrue(r.Ok, r.Error);
        Assert.AreEqual("hello", File.ReadAllText(Path.Combine(target, "Wrapper", "CHAR_A", "a.txt")));
        Assert.AreEqual(2, r.Files);
        Assert.AreEqual(0, r.Skipped);
    }

    [Test]
    public void Zip_with_traversal_entries_skips_them_and_writes_nothing_outside()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "evil.zip", new Dictionary<string, string> { { "ok.txt", "fine" }, { "../evil.txt", "bad" }, { "a/../../evil2.txt", "bad" }, { "/abs_evil.txt", "bad" } });
        var target = Path.Combine(work, "out");

        var r = Extract(zip, target);

        Assert.IsTrue(r.Ok, r.Error);
        Assert.AreEqual(3, r.Skipped);
        Assert.IsTrue(File.Exists(Path.Combine(target, "ok.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(work, "evil.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(work, "evil2.txt")));
        Assert.AreEqual(1, Directory.GetFiles(work, "*.txt", SearchOption.AllDirectories).Length);
    }

    [Test]
    public void Unsupported_extension_returns_error()
    {
        var path = Path.Combine(work, "x.7z");
        File.WriteAllText(path, "nope");
        var r = Extract(path, Path.Combine(work, "out"));
        Assert.IsFalse(r.Ok);
        StringAssert.Contains(".7z", r.Error);
    }

    [Test]
    public void Corrupt_zip_returns_error_not_exception()
    {
        var path = Path.Combine(work, "bad.zip");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
        var r = Extract(path, Path.Combine(work, "out"));
        Assert.IsFalse(r.Ok);
        Assert.IsNotEmpty(r.Error);
    }

    [Test]
    public void Missing_archive_returns_error()
    {
        var r = Extract(Path.Combine(work, "nope.zip"), Path.Combine(work, "out"));
        Assert.IsFalse(r.Ok);
    }

    [Test]
    public void Cancelled_token_stops_with_error()
    {
        var zip = ArchiveTestHelper.MakeZip(work, "p.zip", new Dictionary<string, string> { { "a.txt", "1" }, { "b.txt", "2" } });
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = Extract(zip, Path.Combine(work, "out"), cts.Token);
        Assert.IsFalse(r.Ok);
        StringAssert.Contains("ancel", r.Error);
    }

    [TestCase("Rar4.rar")]
    [TestCase("Rar5.rar")]
    [TestCase("Rar5.solid.rar")]
    [TestCase("Rar.solid.rar")]
    public void Rar_fixtures_extract_inside_target_only(string fixture)
    {
        var target = Path.Combine(work, "out");

        var r = Extract(ArchiveTestHelper.Fixture(fixture), target);

        Assert.IsTrue(r.Ok, fixture + ": " + r.Error);
        var files = Directory.GetFiles(target, "*", SearchOption.AllDirectories);
        TestContext.WriteLine(fixture + " -> " + files.Length + " files, skipped " + r.Skipped);
        Assert.Greater(files.Length, 0);
        Assert.AreEqual(files.Length, r.Files);
        Assert.IsTrue(files.All(f => Path.GetFullPath(f).StartsWith(Path.GetFullPath(target))));
        Assert.IsTrue(files.All(f => new FileInfo(f).Length > 0));
    }
}
