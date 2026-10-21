using NUnit.Framework;

public class PickerOutputTests
{
    [Test]
    public void Blank_and_null_give_empty()
    {
        Assert.AreEqual(0, PickerOutput.ParseLines(null).Count);
        Assert.AreEqual(0, PickerOutput.ParseLines("\n\r\n  \n").Count);
    }

    [Test]
    public void Crlf_and_blank_lines_handled()
    {
        CollectionAssert.AreEqual(new[] { "/a/b.zip", "/c/d.rar" }, PickerOutput.ParseLines("/a/b.zip\r\n\r\n/c/d.rar\r\n"));
    }

    [Test]
    public void Trailing_slash_removed_but_spaces_and_commas_kept()
    {
        CollectionAssert.AreEqual(new[] { "/Users/me/My Mods, vol 1", "C:\\Mods\\Pack" }, PickerOutput.ParseLines("/Users/me/My Mods, vol 1/\nC:\\Mods\\Pack\\\n"));
    }

    [Test]
    public void Drive_root_and_unix_root_kept()
    {
        CollectionAssert.AreEqual(new[] { "C:\\", "/" }, PickerOutput.ParseLines("C:\\\n/\n"));
    }

    [Test]
    public void Android_result_parsed()
    {
        var r = PickerOutput.ParseAndroidResult("D\t/cache/import/1\nP\t/cache/import/1/a.zip\nP\t/cache/import/1/My Mods\nF\tbad.rar\n");
        CollectionAssert.AreEqual(new[] { "/cache/import/1" }, r.TempDirs);
        CollectionAssert.AreEqual(new[] { "/cache/import/1/a.zip", "/cache/import/1/My Mods" }, r.Paths);
        CollectionAssert.AreEqual(new[] { "bad.rar" }, r.Failed);
    }

    [Test]
    public void Android_cancel_and_garbage_give_empty()
    {
        Assert.AreEqual(0, PickerOutput.ParseAndroidResult("").Paths.Count);
        Assert.AreEqual(0, PickerOutput.ParseAndroidResult(null).Paths.Count);
        Assert.AreEqual(0, PickerOutput.ParseAndroidResult("junk\nPx\n").Paths.Count);
    }
}
