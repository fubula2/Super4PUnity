using System;
using System.Linq;
using NUnit.Framework;

public class VideoCacheTests
{
    private static readonly DateTime T = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    [Test]
    public void Key_is_stable_for_same_inputs()
    {
        Assert.AreEqual(VideoCache.KeyFor("/m/a.mp4", 100, T), VideoCache.KeyFor("/m/a.mp4", 100, T));
    }

    [Test]
    public void Key_changes_when_size_time_or_path_changes()
    {
        var k = VideoCache.KeyFor("/m/a.mp4", 100, T);
        Assert.AreNotEqual(k, VideoCache.KeyFor("/m/a.mp4", 101, T));
        Assert.AreNotEqual(k, VideoCache.KeyFor("/m/a.mp4", 100, T.AddSeconds(1)));
        Assert.AreNotEqual(k, VideoCache.KeyFor("/m/b.mp4", 100, T));
    }

    [Test]
    public void CachePath_is_inside_cacheDir_with_webm_extension()
    {
        var p = VideoCache.CachePath("/mods/.cache/video", "abc");
        Assert.AreEqual(System.IO.Path.Combine("/mods/.cache/video", "abc.webm"), p);
    }

    [Test]
    public void Plan_skips_valid_cache_and_lists_missing_once()
    {
        var a = new VideoCache.Clip { Source = "/m/a.mp4", Size = 1, ModifiedUtc = T };
        var b = new VideoCache.Clip { Source = "/m/b.mp4", Size = 2, ModifiedUtc = T };
        var aCopy = new VideoCache.Clip { Source = "/m/a.mp4", Size = 1, ModifiedUtc = T };
        var aPath = VideoCache.CachePath("/c", VideoCache.KeyFor(a.Source, a.Size, a.ModifiedUtc));

        var todo = VideoCache.Plan(new[] { a, b, aCopy, b }, "/c", p => p == aPath);

        CollectionAssert.AreEqual(new[] { "/m/b.mp4" }, todo.Select(c => c.Source).ToArray());
    }
}
