using NUnit.Framework;

public class LibraryLinkClassifierTests
{
    private static LinkKind? Classify(string url) => LibraryLinkClassifier.TryClassify(url, out var kind) ? kind : (LinkKind?)null;

    [Test]
    public void MediaFire_file_and_premium_file_are_File()
    {
        Assert.AreEqual(LinkKind.File, Classify("https://www.mediafire.com/file/abc/a.zip/file"));
        Assert.AreEqual(LinkKind.File, Classify("https://www.mediafire.com/file_premium/abc/a.zip/file"));
    }

    [Test]
    public void MediaFire_folder_is_Folder()
    {
        Assert.AreEqual(LinkKind.Folder, Classify("https://www.mediafire.com/folder/012i7mu55g21r/Legend+of+Zelda"));
    }

    [Test]
    public void Patreon_is_Browser()
    {
        Assert.AreEqual(LinkKind.Browser, Classify("https://www.patreon.com/posts/some-post-123"));
    }

    [Test]
    public void Other_hosts_empty_null_and_http_mediafire_are_dropped()
    {
        Assert.IsNull(Classify("https://drive.google.com/drive/folders/abc"));
        Assert.IsNull(Classify(""));
        Assert.IsNull(Classify(null));
        Assert.IsNull(Classify("not a url"));
        Assert.IsNull(Classify("http://evil.example/mediafire.com/file/x"));
        Assert.IsNull(Classify("https://mediafire.com.evil.example/file/x"));
        Assert.IsNull(Classify("http://www.mediafire.com/file/abc/a.zip/file"));
    }
}
