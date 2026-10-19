using NUnit.Framework;

public class TextFitTests
{
    [Test]
    public void Short_text_is_unchanged()
    {
        Assert.AreEqual("abc", TextFit.MiddleEllipsis("abc", 10));
        Assert.AreEqual("abcdefghij", TextFit.MiddleEllipsis("abcdefghij", 10));
    }

    [Test]
    public void Long_text_keeps_both_ends_and_fits_the_limit()
    {
        var result = TextFit.MiddleEllipsis("abcdefghijklmnop", 9);
        Assert.AreEqual(9, result.Length);
        Assert.AreEqual("abc...nop", result);
    }

    [Test]
    public void Odd_limit_gives_the_extra_character_to_the_start()
    {
        Assert.AreEqual("ab...ij", TextFit.MiddleEllipsis("abcdefghij", 7));
        Assert.AreEqual("abc...ij", TextFit.MiddleEllipsis("abcdefghij", 8));
    }

    [Test]
    public void Null_and_empty_give_empty_text()
    {
        Assert.AreEqual("", TextFit.MiddleEllipsis(null, 10));
        Assert.AreEqual("", TextFit.MiddleEllipsis("", 10));
    }

    [Test]
    public void Tiny_limits_do_not_throw_and_never_grow_the_text()
    {
        Assert.LessOrEqual(TextFit.MiddleEllipsis("abcdefghij", 3).Length, 10);
        Assert.LessOrEqual(TextFit.MiddleEllipsis("abcdefghij", 0).Length, 10);
        Assert.LessOrEqual(TextFit.MiddleEllipsis("abcdefghij", -4).Length, 10);
    }

    [Test]
    public void PackLabel_drops_the_archive_extension()
    {
        Assert.AreEqual("AnonBluna_4PUxKK_ARMS_v1.1", TextFit.PackLabel("AnonBluna_4PUxKK_ARMS_v1.1.zip"));
        Assert.AreEqual("Pack_One", TextFit.PackLabel("Pack_One.RAR"));
        Assert.AreEqual("notes.txt", TextFit.PackLabel("notes.txt"));
    }

    [Test]
    public void PackLabel_shortens_long_names_but_keeps_the_version_at_the_end()
    {
        var label = TextFit.PackLabel("AnonBluna_4PUxKK_LegendOfZelda_PrincessZeldas_SomeVeryLongExtraName_V1.0.zip", 44);
        Assert.AreEqual(44, label.Length);
        StringAssert.StartsWith("AnonBluna", label);
        StringAssert.EndsWith("V1.0", label);
        StringAssert.Contains("...", label);
    }

    [Test]
    public void PackLabel_handles_null()
    {
        Assert.AreEqual("", TextFit.PackLabel(null));
    }
}
