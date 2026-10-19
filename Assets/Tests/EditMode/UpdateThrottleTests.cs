using System;
using NUnit.Framework;

public class UpdateThrottleTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    [Test]
    public void Never_checked_means_check()
    {
        Assert.IsTrue(UpdateThrottle.ShouldCheck(null, Now, Day));
        Assert.IsTrue(UpdateThrottle.ShouldCheck("", Now, Day));
        Assert.IsTrue(UpdateThrottle.ShouldCheck("garbage", Now, Day));
    }

    [Test]
    public void Recent_check_means_wait()
    {
        Assert.IsFalse(UpdateThrottle.ShouldCheck(UpdateThrottle.Stamp(Now.AddHours(-2)), Now, Day));
    }

    [Test]
    public void Old_check_means_check_again()
    {
        Assert.IsTrue(UpdateThrottle.ShouldCheck(UpdateThrottle.Stamp(Now.AddHours(-25)), Now, Day));
        Assert.IsTrue(UpdateThrottle.ShouldCheck(UpdateThrottle.Stamp(Now.AddHours(-24)), Now, Day));
    }

    [Test]
    public void A_stamp_in_the_future_is_ignored_so_a_clock_change_cannot_block_checks_forever()
    {
        Assert.IsTrue(UpdateThrottle.ShouldCheck(UpdateThrottle.Stamp(Now.AddDays(3)), Now, Day));
    }

    [Test]
    public void Stamp_round_trips()
    {
        var s = UpdateThrottle.Stamp(Now);
        Assert.IsFalse(UpdateThrottle.ShouldCheck(s, Now.AddMinutes(1), Day));
    }

    [Test]
    public void Skipped_version_matches_only_the_same_normalized_version()
    {
        Assert.IsTrue(UpdateThrottle.IsSkipped("v1.3.0", "1.3.0"));
        Assert.IsFalse(UpdateThrottle.IsSkipped("1.3.1", "1.3.0"));
        Assert.IsFalse(UpdateThrottle.IsSkipped("1.3.0", ""));
        Assert.IsFalse(UpdateThrottle.IsSkipped("1.3.0", null));
    }
}
