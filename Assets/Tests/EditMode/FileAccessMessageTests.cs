using NUnit.Framework;

public class FileAccessMessageTests
{
    [Test]
    public void Nothing_unreadable_gives_no_message()
    {
        Assert.AreEqual(string.Empty, FileAccessMessage.Describe(new string[0]));
        Assert.AreEqual(string.Empty, FileAccessMessage.Describe(null));
    }

    [Test]
    public void One_file_is_named_with_its_folder()
    {
        var text = FileAccessMessage.Describe(new[] { "/storage/emulated/0/Android/media/App/mods/CHAR_Persona_Mitsuru/Mitsuru.png" });
        StringAssert.Contains("a file", text);
        StringAssert.Contains("CHAR_Persona_Mitsuru/Mitsuru.png", text);
        StringAssert.Contains("Files app", text);
    }

    [Test]
    public void Many_files_show_a_few_and_a_count()
    {
        var paths = new[] { "/m/CHAR_A/a.png", "/m/CHAR_B/b.png", "/m/CHAR_C/c.png", "/m/CHAR_D/d.png", "/m/CHAR_E/e.png" };
        var text = FileAccessMessage.Describe(paths);
        StringAssert.Contains("some files", text);
        StringAssert.Contains("CHAR_A/a.png", text);
        StringAssert.Contains("and 2 more", text);
        StringAssert.DoesNotContain("CHAR_E/e.png", text);
    }

    [Test]
    public void The_same_file_listed_twice_counts_once()
    {
        var text = FileAccessMessage.Describe(new[] { "/m/CHAR_A/a.png", "/m/CHAR_A/a.png" });
        StringAssert.Contains("a file", text);
    }

    [Test]
    public void Line_breaks_are_real_line_breaks()
    {
        var text = FileAccessMessage.Describe(new[] { "/m/CHAR_A/a.png" });
        StringAssert.DoesNotContain("\\n", text);
        StringAssert.Contains("\n\n", text);
    }
}
