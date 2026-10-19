using System.IO;
using NUnit.Framework;

public class FlatTargetTests
{
    private static readonly string Dir = Path.Combine(Path.GetTempPath(), "s4p_music_test", "music");

    [Test]
    public void Plain_ogg_name_resolves_inside_music_dir()
    {
        Assert.IsTrue(FlatTarget.TryResolve(Dir, "Song.ogg", out var full));
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Dir, "Song.ogg")), full);
    }

    [Test]
    public void Traversal_or_subfolders_collapse_to_the_leaf()
    {
        Assert.IsTrue(FlatTarget.TryResolve(Dir, "../evil.ogg", out var a));
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Dir, "evil.ogg")), a);
        Assert.IsTrue(FlatTarget.TryResolve(Dir, "Anime\\x.ogg", out var b));
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Dir, "x.ogg")), b);
    }

    [Test]
    public void Non_ogg_or_empty_is_rejected()
    {
        Assert.IsFalse(FlatTarget.TryResolve(Dir, "song.mp3", out _));
        Assert.IsFalse(FlatTarget.TryResolve(Dir, "", out _));
        Assert.IsFalse(FlatTarget.TryResolve(Dir, "..", out _));
        Assert.IsFalse(FlatTarget.TryResolve(Dir, ".ogg", out _));
    }
}
