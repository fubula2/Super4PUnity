using NUnit.Framework;

public class ImportPolicyTests
{
    [Test]
    public void One_character_with_known_names_imports_automatically()
    {
        Assert.IsFalse(ImportPolicy.NeedsPrompt(1, true));
    }

    [Test]
    public void One_character_with_unknown_names_prompts()
    {
        Assert.IsTrue(ImportPolicy.NeedsPrompt(1, false));
    }

    [Test]
    public void Several_or_no_characters_prompt()
    {
        Assert.IsTrue(ImportPolicy.NeedsPrompt(2, true));
        Assert.IsTrue(ImportPolicy.NeedsPrompt(5, false));
        Assert.IsTrue(ImportPolicy.NeedsPrompt(0, true));
    }

    [Test]
    public void CharDisplayName_strips_prefix_only_at_start()
    {
        Assert.AreEqual("01Anime_DBZ_Bulma", CharDisplayName.From("CHAR_01Anime_DBZ_Bulma"));
        Assert.AreEqual("X_CHAR_Y", CharDisplayName.From("CHAR_X_CHAR_Y"));
        Assert.AreEqual("NoPrefix", CharDisplayName.From("NoPrefix"));
        Assert.AreEqual("", CharDisplayName.From(null));
    }
}
