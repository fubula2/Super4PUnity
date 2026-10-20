using System.Linq;
using NUnit.Framework;

public class MediaFilesTests
{
    [TestCase("/m/CHAR_A/a01.mp4", true)]
    [TestCase("/m/CHAR_A/a01.MP4", true)]
    [TestCase("/m/CHAR_A/a01.Mp4", true)]
    [TestCase("/m/CHAR_A/a01.m4v", true)]
    [TestCase("/m/CHAR_A/a01.M4V", true)]
    [TestCase("/m/CHAR_A/a01.mov", false)]
    [TestCase("/m/CHAR_A/a01.mp4.txt", false)]
    [TestCase("/m/CHAR_A/mp4", false)]
    public void Video_extensions_ignore_case(string path, bool expected)
    {
        Assert.AreEqual(expected, MediaFiles.IsVideo(path));
    }

    [TestCase("/m/music/a.ogg", true)]
    [TestCase("/m/music/a.OGG", true)]
    [TestCase("/m/music/a.Mp3", true)]
    [TestCase("/m/music/a.wav", false)]
    public void Music_extensions_ignore_case(string path, bool expected)
    {
        Assert.AreEqual(expected, MediaFiles.IsMusic(path));
    }

    [Test]
    public void Icon_matches_the_short_name_without_regard_to_case()
    {
        var files = new[] { "/m/c/Fredina01.mp4", "/m/c/FREDINA.PNG", "/m/c/other.png" };
        Assert.AreEqual("/m/c/FREDINA.PNG", MediaFiles.FindIcon(files, "Fredina"));
    }

    [Test]
    public void A_single_picture_is_used_when_no_name_matches()
    {
        Assert.AreEqual("/m/c/icon.png", MediaFiles.FindIcon(new[] { "/m/c/a.mp4", "/m/c/icon.png" }, "Fredina"));
    }

    [Test]
    public void Several_pictures_and_no_match_gives_no_icon()
    {
        Assert.IsNull(MediaFiles.FindIcon(new[] { "/m/c/a.png", "/m/c/b.png" }, "Fredina"));
        Assert.IsNull(MediaFiles.FindIcon(new string[0], "Fredina"));
    }

    private static string Temp()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "s4p_media_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    [Test]
    public void VideosIn_lists_the_mod_folder_in_order_ignoring_case()
    {
        var dir = Temp();
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "b02.MP4"), "x");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "a01.mp4"), "x");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "icon.png"), "x");
            var names = MediaFiles.VideosIn(dir).Select(System.IO.Path.GetFileName).ToArray();
            CollectionAssert.AreEqual(new[] { "a01.mp4", "b02.MP4" }, names);
        }
        finally { System.IO.Directory.Delete(dir, true); }
    }

    [Test]
    public void VideosIn_looks_one_folder_deeper_when_the_mod_folder_has_none()
    {
        var dir = Temp();
        try
        {
            var inner = System.IO.Path.Combine(dir, "CHAR_X");
            System.IO.Directory.CreateDirectory(inner);
            System.IO.File.WriteAllText(System.IO.Path.Combine(inner, "x01.mp4"), "x");
            System.IO.File.WriteAllText(System.IO.Path.Combine(inner, "X.png"), "x");
            Assert.AreEqual(1, MediaFiles.VideosIn(dir).Count);
            Assert.AreEqual(System.IO.Path.Combine(inner, "X.png"), MediaFiles.IconIn(dir, "x"));
        }
        finally { System.IO.Directory.Delete(dir, true); }
    }

    [Test]
    public void VideosIn_is_empty_for_a_missing_or_empty_folder()
    {
        Assert.AreEqual(0, MediaFiles.VideosIn("/definitely/not/here").Count);
        var dir = Temp();
        try { Assert.AreEqual(0, MediaFiles.VideosIn(dir).Count); Assert.AreEqual("(the folder is empty)", MediaFiles.Peek(dir)); }
        finally { System.IO.Directory.Delete(dir, true); }
    }

    [Test]
    public void Peek_names_what_the_folder_holds()
    {
        var dir = Temp();
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "a.mp4.part"), "x");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "b.png"), "x");
            Assert.AreEqual("a.mp4.part, b.png", MediaFiles.Peek(dir));
        }
        finally { System.IO.Directory.Delete(dir, true); }
    }
}
