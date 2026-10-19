using NUnit.Framework;

public class VideoLayoutTests
{
    private const float Wide = 16f / 9f;

    [Test]
    public void On_a_wide_phone_the_whole_picture_is_full_height_with_bars_at_the_sides()
    {
        var (w, h) = VideoLayout.SizeFor(2400, 1080, Wide, VideoFit.Whole);
        Assert.AreEqual(1920f, w, 0.01f);
        Assert.AreEqual(1080f, h, 0.01f);
    }

    [Test]
    public void Fill_covers_the_whole_screen_and_overflows_one_way()
    {
        var (w, h) = VideoLayout.SizeFor(2400, 1080, Wide, VideoFit.Fill);
        Assert.AreEqual(2400f, w, 0.01f);
        Assert.AreEqual(1350f, h, 0.01f);
    }

    [Test]
    public void On_a_square_screen_whole_has_bars_above_and_below()
    {
        var (w, h) = VideoLayout.SizeFor(1000, 1000, Wide, VideoFit.Whole);
        Assert.AreEqual(1000f, w, 0.01f);
        Assert.AreEqual(562.5f, h, 0.01f);
    }

    [Test]
    public void Height_mode_always_uses_the_full_height()
    {
        var (w, h) = VideoLayout.SizeFor(1000, 1000, Wide, VideoFit.Height);
        Assert.AreEqual(1777.78f, w, 0.01f);
        Assert.AreEqual(1000f, h, 0.01f);
    }

    [Test]
    public void Bad_input_falls_back_to_the_screen_size()
    {
        Assert.AreEqual((800f, 600f), VideoLayout.SizeFor(800, 600, 0f, VideoFit.Whole));
        Assert.AreEqual((0f, 0f), VideoLayout.SizeFor(0, 0, Wide, VideoFit.Fill));
    }
}
