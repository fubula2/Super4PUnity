using System;
using System.Collections.Generic;
using NUnit.Framework;

public class MusicSourceSelectorTests
{
    [Test]
    public void Drive_url_selects_drive_source()
    {
        var source = MusicSourceSelector.ForUrl("https://drive.google.com/drive/folders/abc?usp=sharing", new IMusicSource[] { new DriveLinks() });
        Assert.IsNotNull(source);
        Assert.AreEqual("abc", source.RootFolderId("https://drive.google.com/drive/folders/abc?usp=sharing"));
    }

    [Test]
    public void Unknown_host_or_bad_url_returns_null()
    {
        var sources = new IMusicSource[] { new DriveLinks() };
        Assert.IsNull(MusicSourceSelector.ForUrl("https://example.com/x", sources));
        Assert.IsNull(MusicSourceSelector.ForUrl("not a url", sources));
        Assert.IsNull(MusicSourceSelector.ForUrl(null, sources));
    }

    [Test]
    public void Drive_ids_and_file_urls()
    {
        var d = new DriveLinks();
        Assert.AreEqual("1gay_B-x", d.RootFolderId("https://drive.google.com/drive/folders/1gay_B-x"));
        Assert.IsNull(d.RootFolderId("https://drive.google.com/file/d/1/view"));
        Assert.AreEqual("https://drive.google.com/uc?export=download&id=FILE1", d.FileUrl("FILE1"));
        Assert.AreEqual("https://drive.google.com/embeddedfolderview?id=F1", d.ListingUrl("F1"));
    }
}
