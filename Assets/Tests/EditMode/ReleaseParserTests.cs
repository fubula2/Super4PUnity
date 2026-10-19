using NUnit.Framework;

public class ReleaseParserTests
{
    private const string Sample = "{\"tag_name\":\"v1.3.0\",\"name\":\"Big update\",\"html_url\":\"https://github.com/owner/repo/releases/tag/v1.3.0\",\"draft\":false,\"prerelease\":false,\"body\":\"- faster downloads\\r\\n- fixes\\r\\n\\r\\nThanks!\"}";

    [Test]
    public void Parses_version_url_and_notes()
    {
        var r = ReleaseParser.Parse(Sample);
        Assert.IsNull(r.Error);
        Assert.AreEqual("1.3.0", r.Version);
        Assert.AreEqual("https://github.com/owner/repo/releases/tag/v1.3.0", r.Url);
        StringAssert.Contains("faster downloads", r.Notes);
        StringAssert.DoesNotContain("\r", r.Notes);
    }

    [Test]
    public void Notes_are_trimmed_to_a_short_length()
    {
        var long_ = "{\"tag_name\":\"v2.0.0\",\"html_url\":\"https://github.com/o/r/releases/tag/v2.0.0\",\"body\":\"" + new string('x', 5000) + "\"}";
        var r = ReleaseParser.Parse(long_);
        Assert.IsNull(r.Error);
        Assert.LessOrEqual(r.Notes.Length, ReleaseParser.MaxNotes + 3);
    }

    [Test]
    public void Missing_notes_are_fine()
    {
        var r = ReleaseParser.Parse("{\"tag_name\":\"v1.0.1\",\"html_url\":\"https://github.com/o/r/releases/tag/v1.0.1\"}");
        Assert.IsNull(r.Error);
        Assert.AreEqual("", r.Notes);
    }

    [Test]
    public void Untrusted_release_url_is_rejected()
    {
        var r = ReleaseParser.Parse("{\"tag_name\":\"v1.3.0\",\"html_url\":\"https://evil.example/github.com/x\"}");
        Assert.IsNotNull(r.Error);
        r = ReleaseParser.Parse("{\"tag_name\":\"v1.3.0\",\"html_url\":\"http://github.com/o/r/releases/tag/v1.3.0\"}");
        Assert.IsNotNull(r.Error);
    }

    [Test]
    public void Draft_and_prerelease_are_ignored()
    {
        var draft = ReleaseParser.Parse("{\"tag_name\":\"v1.3.0\",\"draft\":true,\"html_url\":\"https://github.com/o/r/releases/tag/v1.3.0\"}");
        var pre = ReleaseParser.Parse("{\"tag_name\":\"v1.3.0\",\"prerelease\":true,\"html_url\":\"https://github.com/o/r/releases/tag/v1.3.0\"}");
        Assert.IsNotNull(draft.Error);
        Assert.IsNotNull(pre.Error);
    }

    [Test]
    public void Bad_tag_or_garbage_json_gives_an_error_not_an_exception()
    {
        Assert.IsNotNull(ReleaseParser.Parse("{\"tag_name\":\"nightly\",\"html_url\":\"https://github.com/o/r/releases/tag/nightly\"}").Error);
        Assert.IsNotNull(ReleaseParser.Parse("<html>").Error);
        Assert.IsNotNull(ReleaseParser.Parse("").Error);
        Assert.IsNotNull(ReleaseParser.Parse(null).Error);
        Assert.IsNotNull(ReleaseParser.Parse("{\"message\":\"Not Found\"}").Error);
    }
}
