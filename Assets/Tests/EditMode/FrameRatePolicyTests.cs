using NUnit.Framework;

public class FrameRatePolicyTests
{
    [Test]
    public void A_saved_choice_wins_everywhere()
    {
        Assert.AreEqual(30, FrameRatePolicy.Resolve(30, false));
        Assert.AreEqual(60, FrameRatePolicy.Resolve(60, true));
    }

    [Test]
    public void Nothing_chosen_defaults_to_60()
    {
        Assert.AreEqual(60, FrameRatePolicy.Resolve(0, false));
        Assert.AreEqual(60, FrameRatePolicy.Resolve(0, true));
    }

    [Test]
    public void Unexpected_saved_values_are_ignored()
    {
        Assert.AreEqual(60, FrameRatePolicy.Resolve(144, false));
        Assert.AreEqual(60, FrameRatePolicy.Resolve(-1, true));
    }

    [Test]
    public void Dropdown_index_round_trips()
    {
        Assert.AreEqual(0, FrameRatePolicy.IndexOf(30));
        Assert.AreEqual(1, FrameRatePolicy.IndexOf(60));
        Assert.AreEqual(30, FrameRatePolicy.FromIndex(0));
        Assert.AreEqual(60, FrameRatePolicy.FromIndex(1));
    }
}
