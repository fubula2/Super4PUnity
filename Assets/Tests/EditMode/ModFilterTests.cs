using NUnit.Framework;

public class ModFilterTests
{
    [Test]
    public void Empty_or_whitespace_query_matches_everything()
    {
        Assert.IsTrue(ModFilter.Matches("01_Bulma_Pink", ""));
        Assert.IsTrue(ModFilter.Matches("01_Bulma_Pink", null));
        Assert.IsTrue(ModFilter.Matches("01_Bulma_Pink", "   "));
    }

    [Test]
    public void Match_is_case_insensitive_substring()
    {
        Assert.IsTrue(ModFilter.Matches("01Anime_DBZ_DbzBulmaPink", "bulma"));
        Assert.IsTrue(ModFilter.Matches("01Anime_DBZ_DbzBulmaPink", "DBZ_DBZBULMA"));
        Assert.IsFalse(ModFilter.Matches("01Anime_DBZ_DbzBulmaPink", "chichi"));
    }

    [Test]
    public void Query_is_trimmed()
    {
        Assert.IsTrue(ModFilter.Matches("Alpha", "  alp "));
    }

    [Test]
    public void Null_name_matches_only_empty_query()
    {
        Assert.IsFalse(ModFilter.Matches(null, "a"));
        Assert.IsTrue(ModFilter.Matches(null, ""));
    }

    [Test]
    public void Non_ascii_names_match()
    {
        Assert.IsTrue(ModFilter.Matches("Мария_01", "мария"));
    }
}
