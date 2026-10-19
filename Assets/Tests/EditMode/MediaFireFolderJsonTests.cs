using NUnit.Framework;

public class MediaFireFolderJsonTests
{
    private const string Sample = "{\"response\":{\"action\":\"folder\\/get_content\",\"folder_content\":{\"chunk_size\":\"100\",\"content_type\":\"files\",\"chunk_number\":\"1\",\"folderkey\":\"k\",\"files\":[" +
        "{\"quickkey\":\"q1\",\"filename\":\"Pack_One_V1.0.zip\",\"size\":\"167412547\",\"links\":{\"normal_download\":\"https:\\/\\/www.mediafire.com\\/file_premium\\/q1\\/x.zip\\/file\"}}," +
        "{\"quickkey\":\"q2\",\"filename\":\"Pack Two.rar\",\"size\":\"2438000000\"}]," +
        "\"more_chunks\":\"{MORE}\",\"revision\":\"1\"},\"result\":\"Success\"}}";

    [Test]
    public void Parses_name_size_quickkey()
    {
        var listing = MediaFireFolderJson.Parse(Sample.Replace("{MORE}", "no"));
        Assert.IsNull(listing.Error);
        Assert.AreEqual(2, listing.Files.Count);
        Assert.AreEqual("Pack_One_V1.0.zip", listing.Files[0].Name);
        Assert.AreEqual(167412547L, listing.Files[0].SizeBytes);
        Assert.AreEqual("q1", listing.Files[0].QuickKey);
        Assert.AreEqual(2438000000L, listing.Files[1].SizeBytes);
        Assert.IsFalse(listing.MoreChunks);
    }

    [Test]
    public void More_chunks_yes_sets_flag()
    {
        Assert.IsTrue(MediaFireFolderJson.Parse(Sample.Replace("{MORE}", "yes")).MoreChunks);
    }

    [Test]
    public void Error_result_sets_Error_with_message()
    {
        var listing = MediaFireFolderJson.Parse("{\"response\":{\"result\":\"Error\",\"message\":\"Unknown or Invalid FolderKey\",\"error\":110}}");
        Assert.IsNotNull(listing.Error);
        StringAssert.Contains("FolderKey", listing.Error);
        Assert.AreEqual(0, listing.Files.Count);
    }

    [Test]
    public void Garbage_and_empty_json_set_Error_not_exception()
    {
        Assert.IsNotNull(MediaFireFolderJson.Parse("<html>nope</html>").Error);
        Assert.IsNotNull(MediaFireFolderJson.Parse("").Error);
        Assert.IsNotNull(MediaFireFolderJson.Parse(null).Error);
        Assert.IsNotNull(MediaFireFolderJson.Parse("{\"response\":{}}").Error);
    }

    [Test]
    public void Unicode_and_escaped_characters_in_names()
    {
        var json = "{\"response\":{\"result\":\"Success\",\"folder_content\":{\"files\":[{\"quickkey\":\"q\",\"filename\":\"Caf\\u00e9 \\\"X\\\".zip\",\"size\":\"10\"}],\"more_chunks\":\"no\"}}}";
        var listing = MediaFireFolderJson.Parse(json);
        Assert.IsNull(listing.Error);
        Assert.AreEqual("Café \"X\".zip", listing.Files[0].Name);
    }
}
