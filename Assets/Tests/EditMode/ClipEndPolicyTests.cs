using NUnit.Framework;

public class ClipEndPolicyTests
{
    [Test]
    public void Known_bad_end_holds_shortly_before_the_end()
    {
        Assert.IsTrue(ClipEndPolicy.ShouldHoldEarly(true, 3.85, 4.02));
        Assert.IsTrue(ClipEndPolicy.ShouldHoldEarly(true, 4.0, 4.02));
    }

    [Test]
    public void Known_bad_end_does_not_hold_early_in_the_clip()
    {
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(true, 0.0, 4.02));
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(true, 3.5, 4.02));
    }

    [Test]
    public void Unknown_clips_never_hold_early()
    {
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(false, 3.99, 4.02));
    }

    [Test]
    public void Invalid_length_never_holds()
    {
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(true, 1.0, 0.0));
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(true, 1.0, double.NaN));
        Assert.IsFalse(ClipEndPolicy.ShouldHoldEarly(true, double.NaN, 4.0));
    }

    [Test]
    public void Error_near_the_end_counts_as_the_clip_ending()
    {
        Assert.IsTrue(ClipEndPolicy.IsErrorAtEnd(3.96, 4.02));
        Assert.IsTrue(ClipEndPolicy.IsErrorAtEnd(3.5, 4.02));
    }

    [Test]
    public void Error_early_in_the_clip_is_a_real_failure()
    {
        Assert.IsFalse(ClipEndPolicy.IsErrorAtEnd(0.0, 4.02));
        Assert.IsFalse(ClipEndPolicy.IsErrorAtEnd(1.0, 4.02));
        Assert.IsFalse(ClipEndPolicy.IsErrorAtEnd(-1, 4.02));
        Assert.IsFalse(ClipEndPolicy.IsErrorAtEnd(3.0, 0.0));
    }

    [Test]
    public void Hold_duration_is_the_time_left_and_never_negative()
    {
        Assert.AreEqual(0.17, ClipEndPolicy.HoldSeconds(3.85, 4.02), 1e-9);
        Assert.AreEqual(0.0, ClipEndPolicy.HoldSeconds(4.5, 4.02));
        Assert.AreEqual(0.0, ClipEndPolicy.HoldSeconds(1.0, double.NaN));
    }
}
