using NUnit.Framework;

public class VideoSyncTests
{
    private const double L = 4.0;

    [Test]
    public void Phase_wraps_music_time()
    {
        Assert.AreEqual(1.0, VideoSync.Phase(9.0, L), 1e-9);
    }

    [Test]
    public void Phase_zero_negative_or_nan_length_returns_zero()
    {
        Assert.AreEqual(0.0, VideoSync.Phase(9.0, 0.0));
        Assert.AreEqual(0.0, VideoSync.Phase(9.0, -1.0));
        Assert.AreEqual(0.0, VideoSync.Phase(9.0, double.NaN));
    }

    [Test]
    public void Drift_across_wrap_is_small()
    {
        Assert.AreEqual(-0.02, VideoSync.Drift(3.99, 8.01, L), 1e-9);
    }

    [Test]
    public void Drift_is_wrapped_to_half_loop()
    {
        var d = VideoSync.Drift(0.1, 3.9, L);
        Assert.AreEqual(0.2, d, 1e-9);
        Assert.LessOrEqual(System.Math.Abs(d), L / 2);
    }

    [Test]
    public void Drift_zero_length_is_zero()
    {
        Assert.AreEqual(0.0, VideoSync.Drift(1.0, 2.0, 0.0));
    }

    [Test]
    public void Decide_within_deadband_is_None()
    {
        Assert.AreEqual(SyncKind.None, VideoSync.Decide(0.01, L, 1.0).Kind);
        Assert.AreEqual(SyncKind.None, VideoSync.Decide(-0.02, L, 1.0).Kind);
    }

    [Test]
    public void Decide_video_ahead_small_slows_down()
    {
        var c = VideoSync.Decide(0.05, L, 1.0);
        Assert.AreEqual(SyncKind.Nudge, c.Kind);
        Assert.AreEqual(0.98f, c.PlaybackSpeed, 1e-6f);
    }

    [Test]
    public void Decide_video_behind_small_speeds_up()
    {
        var c = VideoSync.Decide(-0.05, L, 1.0);
        Assert.AreEqual(SyncKind.Nudge, c.Kind);
        Assert.AreEqual(1.02f, c.PlaybackSpeed, 1e-6f);
    }

    [Test]
    public void Decide_large_drift_seeks_to_target_phase()
    {
        var c = VideoSync.Decide(0.5, L, 1.0);
        Assert.AreEqual(SyncKind.Seek, c.Kind);
        Assert.AreEqual(0.5, c.SeekTime, 1e-9);
        Assert.AreEqual(1.0f, c.PlaybackSpeed, 1e-6f);
    }

    [Test]
    public void Decide_seek_target_wraps_into_loop()
    {
        var c = VideoSync.Decide(0.5, L, 0.2);
        Assert.AreEqual(3.7, c.SeekTime, 1e-9);
    }

    [Test]
    public void Decide_zero_length_is_None()
    {
        Assert.AreEqual(SyncKind.None, VideoSync.Decide(0.5, 0.0, 1.0).Kind);
    }

    [Test]
    public void Decide_nan_drift_is_None()
    {
        Assert.AreEqual(SyncKind.None, VideoSync.Decide(double.NaN, L, 1.0).Kind);
    }

    [Test]
    public void BarLength_ignores_the_few_extra_frames_in_a_clip()
    {
        Assert.AreEqual(4.0, VideoSync.BarLength(4.0167), 1e-9);
        Assert.AreEqual(4.0, VideoSync.BarLength(4.0), 1e-9);
        Assert.AreEqual(4.0, VideoSync.BarLength(3.9), 1e-9);
    }

    [Test]
    public void BarLength_follows_clips_that_span_several_bars()
    {
        Assert.AreEqual(8.0, VideoSync.BarLength(8.03), 1e-9);
        Assert.AreEqual(12.0, VideoSync.BarLength(12.1), 1e-9);
    }

    [Test]
    public void BarLength_short_clips_use_their_own_length()
    {
        Assert.AreEqual(1.0, VideoSync.BarLength(1.0), 1e-9);
    }

    [Test]
    public void BarLength_invalid_lengths_fall_back_to_four_seconds()
    {
        Assert.AreEqual(4.0, VideoSync.BarLength(0.0), 1e-9);
        Assert.AreEqual(4.0, VideoSync.BarLength(-3.0), 1e-9);
        Assert.AreEqual(4.0, VideoSync.BarLength(double.NaN), 1e-9);
        Assert.AreEqual(4.0, VideoSync.BarLength(double.PositiveInfinity), 1e-9);
    }

    [Test]
    public void Using_the_bar_instead_of_a_slightly_longer_clip_removes_the_slow_slip()
    {
        //A 4.0167 s clip looping with a 4 s bar: after 50 bars of music the phase must still be right
        const double clip = 4.0167;
        var bar = VideoSync.BarLength(clip);
        var musicTime = 50 * 4.0 + 1.0;
        Assert.AreEqual(1.0, VideoSync.Phase(musicTime, bar), 1e-9);
        Assert.Greater(System.Math.Abs(VideoSync.Phase(musicTime, clip) - 1.0), 0.1);
    }
}
