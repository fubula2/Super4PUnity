using System.IO;
using NUnit.Framework;

public class ImportSourcesTests
{
    private string root;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "s4p_src_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
    }

    [TearDown]
    public void TearDown() { if(Directory.Exists(root)) { Directory.Delete(root, true); } }

    [Test]
    public void Classify_zip_rar_any_case()
    {
        Assert.AreEqual(ImportSourceKind.Archive, ImportSources.Classify("/x/a.zip"));
        Assert.AreEqual(ImportSourceKind.Archive, ImportSources.Classify("/x/a.RAR"));
        Assert.AreEqual(ImportSourceKind.Archive, ImportSources.Classify("/x/a.Zip"));
    }

    [Test]
    public void Classify_folder()
    {
        Assert.AreEqual(ImportSourceKind.Folder, ImportSources.Classify(root));
    }

    [Test]
    public void Classify_other_extension_unsupported()
    {
        Assert.AreEqual(ImportSourceKind.Unsupported, ImportSources.Classify("/x/a.7z"));
        Assert.AreEqual(ImportSourceKind.Unsupported, ImportSources.Classify(""));
        Assert.AreEqual(ImportSourceKind.Unsupported, ImportSources.Classify(null));
    }

    [Test]
    public void Supported_drops_unsupported_and_duplicates_and_counts()
    {
        var list = ImportSources.Supported(new[] { "/a/one.zip", "/a/ONE.zip", "/a/two.rar", "/a/x.txt", root }, out var unsupported);
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(1, unsupported);
    }

    [Test]
    public void ConfirmText_singular()
    {
        Assert.AreEqual("Import 1 item?\nfoo.zip", ImportSources.ConfirmText(new[] { "/a/foo.zip" }, 0));
    }

    [Test]
    public void ConfirmText_lists_names_and_and_more()
    {
        var paths = new[] { "/a/1.zip", "/a/2.zip", "/a/3.zip" };
        var text = ImportSources.ConfirmText(paths, 0, 2);
        StringAssert.StartsWith("Import 3 items?", text);
        StringAssert.Contains("1.zip", text);
        StringAssert.Contains("2.zip", text);
        StringAssert.DoesNotContain("3.zip", text);
        StringAssert.Contains("and 1 more", text);
    }

    [Test]
    public void ConfirmText_mentions_ignored()
    {
        StringAssert.Contains("2 ignored", ImportSources.ConfirmText(new[] { "/a/1.zip" }, 2));
    }

    [Test]
    public void SupportedAudio_keeps_ogg_and_mp3_only()
    {
        var list = ImportSources.SupportedAudio(new[] { "/a/x.ogg", "/a/y.MP3", "/a/z.wav", "/a/x.ogg" }, out var unsupported);
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(1, unsupported);
    }
}
