using NUnit.Framework;

public class AppVersionTests
{
    [TestCase("1.2.2", "1.2.1", true)]
    [TestCase("1.3.0", "1.2.9", true)]
    [TestCase("2.0.0", "1.99.99", true)]
    [TestCase("1.2.10", "1.2.9", true)]
    [TestCase("v1.2.2", "1.2.1", true)]
    [TestCase("V1.2.2", "v1.2.1", true)]
    [TestCase("1.2", "1.1.9", true)]
    [TestCase("1.2.1.1", "1.2.1", true)]
    public void IsNewer_true_cases(string latest, string current, bool expected)
    {
        Assert.AreEqual(expected, AppVersion.IsNewer(latest, current));
    }

    [TestCase("1.2.1", "1.2.1")]
    [TestCase("1.2", "1.2.0")]
    [TestCase("v1.2.1", "1.2.1")]
    [TestCase("1.2.0", "1.2.1")]
    [TestCase("1.1.9", "1.2.0")]
    [TestCase("1.2.9", "1.2.10")]
    public void IsNewer_false_cases(string latest, string current)
    {
        Assert.IsFalse(AppVersion.IsNewer(latest, current));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("latest")]
    [TestCase("v")]
    [TestCase("1.x.3")]
    [TestCase("1..2")]
    public void Invalid_versions_never_count_as_newer(string bad)
    {
        Assert.IsFalse(AppVersion.IsNewer(bad, "1.2.1"));
        Assert.IsFalse(AppVersion.IsNewer("9.9.9", bad));
    }

    [TestCase("v1.2.2-beta", "1.2.2")]
    [TestCase("1.2.2+build5", "1.2.2")]
    [TestCase("  v1.2.2 ", "1.2.2")]
    public void Normalize_strips_prefix_suffix_and_space(string raw, string expected)
    {
        Assert.AreEqual(expected, AppVersion.Normalize(raw));
    }

    [Test]
    public void Normalize_returns_null_for_garbage()
    {
        Assert.IsNull(AppVersion.Normalize("hello"));
        Assert.IsNull(AppVersion.Normalize(null));
    }

    [Test]
    public void Prerelease_suffix_does_not_make_an_equal_version_newer()
    {
        Assert.IsFalse(AppVersion.IsNewer("1.2.1-beta", "1.2.1"));
    }
}
