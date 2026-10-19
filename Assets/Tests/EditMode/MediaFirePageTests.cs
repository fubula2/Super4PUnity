using NUnit.Framework;

public class MediaFirePageTests
{
    private const string Button = "<a class=\"input popsok\"\n   aria-label=\"Download file\"\n   href=\"https://download1321.mediafire.com/abc/def/pack.zip\"           id=\"downloadButton\"\n   rel=\"nofollow\">\n   Download (94.86MB)\n</a>";

    [Test]
    public void Finds_href_on_downloadButton_across_lines()
    {
        Assert.IsTrue(MediaFirePage.TryGetDirectUrl("<html>" + Button + "</html>", out var url));
        Assert.AreEqual("https://download1321.mediafire.com/abc/def/pack.zip", url);
    }

    [Test]
    public void Html_entities_in_href_are_decoded()
    {
        var html = "<a href=\"https://download1.mediafire.com/x?a=1&amp;b=2\" id=\"downloadButton\">d</a>";
        Assert.IsTrue(MediaFirePage.TryGetDirectUrl(html, out var url));
        Assert.AreEqual("https://download1.mediafire.com/x?a=1&b=2", url);
    }

    [Test]
    public void Falls_back_to_scrambled_url_attribute()
    {
        var encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("https://download9.mediafire.com/p/q.zip"));
        var html = "<a href=\"#\" data-scrambled-url=\"" + encoded + "\" id=\"downloadButton\">d</a>";
        Assert.IsTrue(MediaFirePage.TryGetDirectUrl(html, out var url));
        Assert.AreEqual("https://download9.mediafire.com/p/q.zip", url);
    }

    [Test]
    public void No_button_or_empty_input_returns_false()
    {
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("<html>The file was removed</html>", out _));
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("", out _));
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl(null, out _));
    }

    [Test]
    public void Rejects_non_mediafire_host_and_http_scheme_and_placeholder_href()
    {
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("<a href=\"https://evil.example/x.zip\" id=\"downloadButton\">d</a>", out _));
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("<a href=\"https://mediafire.com.evil.example/x\" id=\"downloadButton\">d</a>", out _));
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("<a href=\"http://download1.mediafire.com/x.zip\" id=\"downloadButton\">d</a>", out _));
        Assert.IsFalse(MediaFirePage.TryGetDirectUrl("<a href=\"#\" id=\"downloadButton\">d</a>", out _));
    }

    [Test]
    public void FolderKey_from_url_with_and_without_name_and_query()
    {
        Assert.AreEqual("012i7mu55g21r", MediaFirePage.FolderKey("https://www.mediafire.com/folder/012i7mu55g21r/Legend+of+Zelda"));
        Assert.AreEqual("012i7mu55g21r", MediaFirePage.FolderKey("https://www.mediafire.com/folder/012i7mu55g21r"));
        Assert.AreEqual("012i7mu55g21r", MediaFirePage.FolderKey("https://www.mediafire.com/folder/012i7mu55g21r/x?y=1"));
        Assert.IsNull(MediaFirePage.FolderKey("https://www.mediafire.com/file/abc/a.zip/file"));
        Assert.IsNull(MediaFirePage.FolderKey(null));
    }

    [Test]
    public void Folder_api_and_quickkey_urls()
    {
        Assert.AreEqual("https://www.mediafire.com/api/1.5/folder/get_content.php?folder_key=K1&content_type=files&chunk=2&chunk_size=100&response_format=json", MediaFirePage.FolderApiUrl("K1", 2));
        Assert.AreEqual("https://www.mediafire.com/file/pcpegyalcpqv7fr", MediaFirePage.FileUrlFromQuickKey("pcpegyalcpqv7fr"));
    }

    [Test]
    public void IsHtmlPage_tells_a_web_page_from_a_direct_file()
    {
        Assert.IsTrue(MediaFirePage.IsHtmlPage("text/html"));
        Assert.IsTrue(MediaFirePage.IsHtmlPage("TEXT/HTML; charset=UTF-8"));
        Assert.IsFalse(MediaFirePage.IsHtmlPage("application/zip"));
        Assert.IsFalse(MediaFirePage.IsHtmlPage("application/octet-stream"));
        Assert.IsFalse(MediaFirePage.IsHtmlPage(null));
    }

    [Test]
    public void IsRepairPage_spots_the_key_regeneration_page()
    {
        Assert.IsTrue(MediaFirePage.IsRepairPage("<div class=\"DownloadRepair-title\">Generating new download key</div>"));
        Assert.IsFalse(MediaFirePage.IsRepairPage("<a id=\"downloadButton\" href=\"https://download1.mediafire.com/x\">"));
        Assert.IsFalse(MediaFirePage.IsRepairPage(null));
    }
}
