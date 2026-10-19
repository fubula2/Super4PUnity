using NUnit.Framework;

public class CharacterInfoTests
{
    [TestCase("Ankha", true)]
    [TestCase("Twintelle / MinMin", true)]
    [TestCase("Genshin Impact / N/A", true)]
    [TestCase("Samus Aran", true)]
    [TestCase("61+", false)]
    [TestCase("6", false)]
    [TestCase(" 37+ ", false)]
    [TestCase("-", false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase(null, false)]
    public void NamesKnown_classifies_cells(string cell, bool expected)
    {
        Assert.AreEqual(expected, CharacterInfo.NamesKnown(cell));
    }
}
